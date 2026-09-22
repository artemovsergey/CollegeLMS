using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Bot;

public class MaxBotService : BackgroundService
{
    private readonly MaxApiClient _max;
    private readonly CollegeLmsApiClient _api;
    private readonly IServiceProvider _sp;
    private readonly ILogger<MaxBotService> _logger;
    private readonly TimeZoneInfo _tz;
    private readonly MaxBotOptions _options;
    private readonly MaxUpdateQueue _updateQueue;

    private string _botUsername = "";

    private const int PageSize = 5;
    private const int PollTimeoutSeconds = 30;

    private readonly Dictionary<long, SearchState> _searchStates = [];

    private static readonly string[] WebhookUpdateTypes =
    [
        "message_created",
        "message_callback",
        "bot_started",
    ];

    public MaxBotService(
        MaxApiClient max,
        CollegeLmsApiClient api,
        IServiceProvider sp,
        TimeZoneInfo tz,
        IOptions<MaxBotOptions> options,
        MaxUpdateQueue updateQueue,
        ILogger<MaxBotService> logger
    )
    {
        _max = max;
        _api = api;
        _sp = sp;
        _tz = tz;
        _options = options.Value;
        _updateQueue = updateQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Max bot starting...");

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
            _logger.LogError(
                "MaxBot:AccessToken пуст — бот не сможет подключиться к MAX API. Проверьте переменную MAX_BOT_TOKEN."
            );

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var me = await _max.GetMeAsync(ct);
                _logger.LogInformation(
                    "Max bot started as {Name} (@{Username})",
                    me?.Name,
                    me?.Username
                );
                _botUsername = me?.Username ?? "";

                try
                {
                    await _max.SetCommandsAsync(
                        [
                            new BotCommand
                            {
                                Name = "start",
                                Description = "Регистрация и настройка",
                            },
                            new BotCommand { Name = "settings", Description = "Настройки" },
                            new BotCommand { Name = "help", Description = "Справка" },
                        ],
                        ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set bot commands (non-fatal)");
                }

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Max API, retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            if (await TryEnableWebhookAsync(ct))
            {
                await ConsumeWebhookUpdatesAsync(ct);
                return;
            }

            _logger.LogError(
                "Webhook не настроен — включаю резервный long polling (часть обновлений может теряться)"
            );
        }

        // Очередь читаем и в режиме polling: если подписка на вебхук осталась активной,
        // MAX продолжит слать события на webhook-эндпоинт.
        _ = ConsumeWebhookUpdatesAsync(ct);

        await RunPollingLoopAsync(ct);
    }

    private async Task<bool> TryEnableWebhookAsync(CancellationToken ct)
    {
        var url = _options.WebhookUrl;

        for (var attempt = 1; attempt <= 3 && !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                var existing = await _max.GetSubscriptionsAsync(ct);
                if (existing is not null)
                {
                    foreach (var stale in existing.Where(s => s.Url != url))
                    {
                        _logger.LogInformation(
                            "Удаляю устаревшую webhook-подписку: {Url}",
                            stale.Url
                        );
                        await _max.UnsubscribeWebhookAsync(stale.Url, ct);
                    }
                }

                var secret = string.IsNullOrWhiteSpace(_options.WebhookSecret)
                    ? null
                    : _options.WebhookSecret;
                var subscribed = await _max.SubscribeWebhookAsync(
                    url,
                    WebhookUpdateTypes,
                    secret,
                    ct
                );
                if (subscribed)
                {
                    _logger.LogInformation("Webhook-подписка активна: {Url}", url);
                    return true;
                }

                _logger.LogWarning("MAX отказал в webhook-подписке (попытка {Attempt}/3)", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Не удалось оформить webhook-подписку (попытка {Attempt}/3)",
                    attempt
                );
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }

        return false;
    }

    private async Task ConsumeWebhookUpdatesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Ожидание апдейтов через webhook");

        try
        {
            await foreach (var update in _updateQueue.ReadAllAsync(ct))
            {
                await HandleUpdateAsync(update, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        long? marker = null;

        _logger.LogInformation("Max bot polling loop started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = await _max.GetUpdatesAsync(marker, PollTimeoutSeconds, ct);
                if (updates is not null)
                {
                    if (updates.Updates is { Count: > 0 })
                    {
                        foreach (var update in updates.Updates)
                        {
                            await HandleUpdateAsync(update, ct);
                        }
                    }

                    marker = updates.Marker ?? marker;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in polling loop");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task HandleUpdateAsync(MaxUpdate update, CancellationToken ct)
    {
        try
        {
            var userId = MaxUserResolver.ResolveUserId(update);
            var chatId = MaxUserResolver.ResolveChatId(update);

            _logger.LogInformation(
                "Update {Type}: chatId={ChatId}, userId={UserId}, payload={Payload}, text={Text}",
                update.UpdateType,
                chatId,
                userId,
                update.Callback?.Payload,
                update.Message?.Body?.Text
            );

            switch (update.UpdateType)
            {
                case "bot_started":
                    await HandleBotStartedAsync(chatId, userId, ct);
                    break;
                case "message_created":
                    if (update.Message?.Body?.Text is { } text)
                        await HandleMessageAsync(chatId, userId, text, ct);
                    break;
                case "message_callback":
                    if (update.Callback is { } callback)
                        await HandleCallbackAsync(
                            chatId,
                            userId,
                            callback.CallbackId,
                            callback.Payload,
                            ct
                        );
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling update {Type} (chatId={ChatId}, userId={UserId})",
                update.UpdateType,
                MaxUserResolver.ResolveChatId(update),
                MaxUserResolver.ResolveUserId(update)
            );
        }
    }

    private async Task HandleBotStartedAsync(long chatId, long userId, CancellationToken ct)
    {
        _searchStates.Remove(userId);

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var existing = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (existing is null)
        {
            existing = new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = userId,
                MaxChatId = chatId,
                Role = "student",
                NotifyEnabled = true,
                NotifyDays = [1, 2, 3, 4, 5],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.UserSettings.Add(existing);
            await db.SaveChangesAsync(ct);
        }
        else if (existing.MaxChatId != chatId)
        {
            // Выбор мог быть сделан из мини-приложения до первого /start —
            // без актуального чата бот не сможет доставить сообщение.
            existing.MaxChatId = chatId;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        if (MaxBotRoleFlow.RequiresOnboarding(existing))
            await ShowWelcomeAsync(chatId, ct);

        await ShowMainMenuAsync(chatId, userId, ct);
    }

    /// <summary>Приветствие при первом запуске: название бота, описание и основные функции.</summary>
    private async Task ShowWelcomeAsync(long chatId, CancellationToken ct) =>
        await _max.SendMessageAsync(
            chatId,
            "📚 *Расписание колледжа*\n\n"
                + "Я бот расписания: слежу за изменениями и присылаю расписание на день.\n\n"
                + "Что умею:\n"
                + "• 📅 открывать расписание на день и неделю в мини-приложении;\n"
                + "• 🔔 присылать расписание на день и уведомления об изменениях;\n"
                + "• ⭐ хранить избранные группы и преподавателей.\n\n"
                + "Выбери, кто ты, и найди себя поиском.",
            ct: ct
        );

    private async Task HandleMessageAsync(
        long chatId,
        long userId,
        string text,
        CancellationToken ct
    )
    {
        var lower = text.ToLower().Trim();

        if (lower == "/start")
        {
            _searchStates.Remove(userId);
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        if (lower == "/help")
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
            var settings = await db
                .UserSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);

            if (MaxBotRoleFlow.RequiresOnboarding(settings))
            {
                await HandleBotStartedAsync(chatId, userId, ct);
                return;
            }

            await _max.SendMessageAsync(
                chatId,
                "🤖 *Бот расписания*\n\n"
                    + "Всё управление — кнопками под сообщениями.\n"
                    + "/start — главное меню\n"
                    + "/settings — настройки\n"
                    + "/help — справка",
                ct: ct
            );
            return;
        }

        if (lower == "/settings")
        {
            _searchStates.Remove(userId);
            await ShowSettingsAsync(chatId, userId, ct);
            return;
        }

        if (_searchStates.TryGetValue(userId, out var search))
        {
            await HandleSearchTextAsync(chatId, userId, text.Trim(), search, ct);
            return;
        }

        await _max.SendMessageAsync(
            chatId,
            "Используй кнопки меню — команды писать не нужно.",
            ct: ct
        );
        await ShowMainMenuAsync(chatId, userId, ct);
    }

    private async Task HandleCallbackAsync(
        long chatId,
        long userId,
        string callbackId,
        string? payload,
        CancellationToken ct
    )
    {
        if (payload is null)
        {
            _logger.LogWarning("Callback with no payload");
            return;
        }

        // Снимаем индикатор ожидания с кнопки
        try
        {
            await _max.AnswerCallbackAsync(callbackId, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to answer callback {CallbackId}", callbackId);
        }

        _logger.LogInformation("Callback from user {UserId}: {Payload}", userId, payload);

        var p = CallbackPayload.Parse(payload);
        if (p is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        switch (p.Action)
        {
            case "menu":
                _searchStates.Remove(userId);
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "role":
                await HandleRoleSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "group":
                await HandleGroupSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "teacher":
                await HandleTeacherSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "settings":
                if (p.Param1 is "student" or "teacher")
                    await StartSearchAsync(chatId, userId, p.Param1, ct);
                else
                    await ShowSettingsAsync(chatId, userId, ct);
                break;
            case "sresult":
                if (p.Param1 is "group" or "teacher")
                    await ShowSearchResultsAsync(chatId, userId, ParsePage(p.Param2), ct);
                break;
            case "notifications":
                await ShowNotificationsAsync(chatId, userId, ct);
                break;
            case "notify":
                await HandleNotifyToggleAsync(chatId, userId, ct);
                break;
            case "notifyday":
                await HandleNotifyDayToggleAsync(chatId, userId, int.Parse(p.Param1!), ct);
                break;
            case "notifytime":
                await HandleNotifyTimeAsync(chatId, userId, int.Parse(p.Param1!), ct);
                break;
            default:
                _logger.LogWarning("Unknown callback action {Action}", p.Action);
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
        }
    }

    /// <summary>Номер страницы из payload; отсутствие/мусор — первая страница.</summary>
    private static int ParsePage(string? value) =>
        value is not null && int.TryParse(value, out var page) && page >= 0 ? page : 0;

    private async Task HandleRoleSelectionAsync(
        long chatId,
        long userId,
        string role,
        CancellationToken ct
    ) => await StartSearchAsync(chatId, userId, role == "teacher" ? "teacher" : "student", ct);

    /// <summary>
    /// Запускает поиск: студент ищет группу, преподаватель — себя в списке.
    /// Роль и цель меняются только при подтверждении выбора.
    /// </summary>
    private async Task StartSearchAsync(
        long chatId,
        long userId,
        string target,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var registered = await db.UserSettings.AnyAsync(x => x.MaxUserId == userId, ct);
        if (!registered)
            return;

        _searchStates[userId] = new SearchState { Target = target };

        var prompt =
            target == "student"
                ? "👨‍🎓 *Студент*\n\nНапиши номер или название группы — я найду её в списке."
                : "👨‍🏫 *Преподаватель*\n\nНапиши ФИО преподавателя (или его часть) — я найду его в списке.";

        await _max.SendInlineKeyboardAsync(
            chatId,
            prompt,
            [
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = "🔙 Отмена",
                        Payload = "menu",
                    },
                },
            ],
            ct: ct
        );
    }

    /// <summary>Фильтрует варианты поиска по подстроке без учёта регистра.</summary>
    private static List<SearchOption> FilterOptions(
        IEnumerable<SearchOption> options,
        string query
    ) => options.Where(o => o.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    private async Task HandleSearchTextAsync(
        long chatId,
        long userId,
        string query,
        SearchState search,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await _max.SendMessageAsync(chatId, "✍️ Напиши текст для поиска.", ct: ct);
            return;
        }

        if (search.Target == "teacher")
        {
            var teachers = await _api.GetTeachersAsync(ct);
            search.Results = FilterOptions(
                teachers.Select(t => new SearchOption(t.Id, t.FullName)),
                query
            );
        }
        else
        {
            var groups = await _api.GetGroupsAsync(ct);
            search.Results = FilterOptions(
                groups.Select(g => new SearchOption(g.Id, g.Name)),
                query
            );
        }

        search.Query = query;

        if (search.Results.Count == 0)
        {
            await _max.SendMessageAsync(
                chatId,
                $"❌ По запросу «{query}» ничего не найдено. Попробуй другой вариант.",
                ct: ct
            );
            return;
        }

        await ShowSearchResultsAsync(chatId, userId, 0, ct);
    }

    private async Task ShowSearchResultsAsync(
        long chatId,
        long userId,
        int page,
        CancellationToken ct
    )
    {
        if (!_searchStates.TryGetValue(userId, out var search) || search.Results.Count == 0)
        {
            await _max.SendMessageAsync(chatId, "🔎 Сначала выполни поиск в настройках.", ct: ct);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)search.Results.Count / PageSize);
        page = Math.Clamp(page, 0, totalPages - 1);
        var paged = search.Results.Skip(page * PageSize).Take(PageSize).ToList();

        var buttons = paged
            .Select(o => new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = o.Name,
                    Payload = $"{(search.Target == "teacher" ? "teacher" : "group")}:{o.Id}",
                },
            })
            .ToList();

        var navRow = new List<MaxButton>();
        if (page > 0)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Назад",
                    Payload = $"sresult:{search.Target}:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = $"sresult:{search.Target}:{page + 1}",
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);

        var title = search.Target == "teacher" ? "Преподаватели" : "Группы";
        await _max.SendInlineKeyboardAsync(
            chatId,
            $"🔎 {title} по запросу «{search.Query}» (стр. {page + 1}/{totalPages}):",
            buttons,
            ct: ct
        );
    }

    private async Task HandleGroupSelectionAsync(
        long chatId,
        long userId,
        string groupId,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        var id = Guid.Parse(groupId);
        MaxBotRoleFlow.SelectGroup(settings, id);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        _searchStates.Remove(userId);

        await ShowMainMenuAsync(chatId, userId, ct);
    }

    private async Task HandleTeacherSelectionAsync(
        long chatId,
        long userId,
        string teacherId,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        var id = Guid.Parse(teacherId);
        MaxBotRoleFlow.SelectTeacher(settings, id);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        _searchStates.Remove(userId);

        await ShowMainMenuAsync(chatId, userId, ct);
    }

    private async Task<UserSettings?> GetSettingsAsync(long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        return await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
    }

    private async Task ShowMainMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null)
        {
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        if ((settings.GroupId ?? settings.TeacherId) is null)
        {
            await _max.SendInlineKeyboardAsync(
                chatId,
                "🏠 *Главное меню*\n\n"
                    + "Текущий выбор: не задан\n\n"
                    + "Выбери, кто ты, и найди себя поиском.",
                [
                    new List<MaxButton>
                    {
                        new()
                        {
                            Type = "callback",
                            Text = "🎓 Студент",
                            Payload = "role:student",
                        },
                    },
                    new List<MaxButton>
                    {
                        new()
                        {
                            Type = "callback",
                            Text = "👨‍🏫 Преподаватель",
                            Payload = "role:teacher",
                        },
                    },
                    new List<MaxButton>
                    {
                        new()
                        {
                            Type = "callback",
                            Text = "⚙️ Настройки",
                            Payload = "settings",
                        },
                    },
                ],
                ct: ct
            );
            return;
        }

        var groupName =
            settings.GroupId is { } groupId
                ? (await _api.GetGroupsAsync(ct)).FirstOrDefault(g => g.Id == groupId)?.Name
                : null;
        var teacherName =
            settings.TeacherId is { } teacherId
                ? (await _api.GetTeachersAsync(ct)).FirstOrDefault(t => t.Id == teacherId)?.FullName
                : null;

        var (text, buttons) = BotScreens.MainMenuWithSelection(
            settings.Role,
            settings.GroupId,
            settings.TeacherId,
            groupName,
            teacherName,
            _options
        );

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private async Task ShowSettingsAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
        {
            await _max.SendMessageAsync(chatId, "⚠️ Используй /start для регистрации.", ct: ct);
            return;
        }

        var groupName =
            settings.GroupId is { } groupId
                ? (await _api.GetGroupsAsync(ct)).FirstOrDefault(g => g.Id == groupId)?.Name
                : null;
        var teacherName =
            settings.TeacherId is { } teacherId
                ? (await _api.GetTeachersAsync(ct)).FirstOrDefault(t => t.Id == teacherId)?.FullName
                : null;
        var entity = groupName ?? teacherName ?? "не задан";

        var text = $"⚙️ *Настройки*\n\nТекущий выбор: {entity}";

        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔔 Уведомления",
                    Payload = "notifications",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🎓 Студент",
                    Payload = "settings:student",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "👨‍🏫 Преподаватель",
                    Payload = "settings:teacher",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔙 Меню",
                    Payload = "menu",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private async Task ShowNotificationsAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
        {
            await _max.SendMessageAsync(chatId, "⚠️ Используй /start для регистрации.", ct: ct);
            return;
        }

        var notifyDays = string.Join(
            ", ",
            settings.NotifyDays.Select(d => MessageFormatter.GetShortDayLabel(d))
        );
        var status = settings.NotifyEnabled ? "включено ✅" : "выключено ❌";

        var text =
            $"🔔 *Уведомления*\n\n"
            + $"Расписание на день: {status}\n"
            + $"Дни: {notifyDays}\n"
            + $"⏰ Время до начала занятий: {settings.NotifyTime:hh\\:mm} (МСК)\n\n"
            + "Нажми на день, чтобы включить или выключить его. "
            + "Время меняется кнопками ±5 минут (07:30–08:30).";

        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = settings.NotifyEnabled
                        ? "🔕 Выключить уведомления"
                        : "🔔 Включить уведомления",
                    Payload = "notify:toggle",
                },
            },
        };

        buttons.AddRange(BuildNotifyDayButtons(settings.NotifyDays));

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "⏰ −5 мин",
                    Payload = "notifytime:-5",
                },
                new()
                {
                    Type = "callback",
                    Text = "⏰ +5 мин",
                    Payload = "notifytime:5",
                },
            }
        );

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔙 Назад",
                    Payload = "settings",
                },
            }
        );

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private async Task HandleNotifyToggleAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        settings.NotifyEnabled = !settings.NotifyEnabled;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ShowNotificationsAsync(chatId, userId, ct);
    }

    /// <summary>Клавиатура из всех 7 дней: отмечены включённые, остальные можно включить.</summary>
    private static List<List<MaxButton>> BuildNotifyDayButtons(int[] notifyDays)
    {
        var buttons = Enumerable
            .Range(1, 7)
            .Select(d => new MaxButton
            {
                Type = "callback",
                Text = notifyDays.Contains(d)
                    ? $"{MessageFormatter.GetShortDayLabel(d)} ✓"
                    : MessageFormatter.GetShortDayLabel(d),
                Payload = $"notifyday:{d}",
            })
            .ToList();

        return [buttons.Take(4).ToList(), buttons.Skip(4).ToList()];
    }

    private async Task HandleNotifyTimeAsync(
        long chatId,
        long userId,
        int deltaMinutes,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        var candidate = settings.NotifyTime.Add(TimeSpan.FromMinutes(deltaMinutes));
        if (!NotificationTimeRules.IsValid(candidate))
        {
            await _max.SendMessageAsync(
                chatId,
                $"⏰ Время дайджеста — от {NotificationTimeRules.Min:hh\\:mm} до {NotificationTimeRules.Max:hh\\:mm} с шагом 5 минут.",
                ct: ct
            );
            await ShowNotificationsAsync(chatId, userId, ct);
            return;
        }

        settings.NotifyTime = candidate;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ShowNotificationsAsync(chatId, userId, ct);
    }

    private async Task HandleNotifyDayToggleAsync(
        long chatId,
        long userId,
        int day,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        if (settings.NotifyDays.Contains(day))
            settings.NotifyDays = settings.NotifyDays.Where(d => d != day).ToArray();
        else
            settings.NotifyDays = [.. settings.NotifyDays, day];

        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ShowNotificationsAsync(chatId, userId, ct);
    }

    /// <summary>Состояние поиска группы/преподавателя после выбора роли.</summary>
    private sealed class SearchState
    {
        public string Target { get; init; } = "group";
        public string Query { get; set; } = "";
        public List<SearchOption> Results { get; set; } = [];
    }

    private sealed record SearchOption(Guid Id, string Name);
}
