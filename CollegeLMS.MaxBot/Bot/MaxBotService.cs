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

    private const int PageSize = 5;
    private const int PollTimeoutSeconds = 30;
    private const int ChangesPageSize = 20;

    private readonly HashSet<long> _pendingDispatcherPasswords = [];
    private readonly Dictionary<long, string> _dispatcherTokens = [];
    private readonly Dictionary<long, DispatcherWizardState> _wizardStates = [];
    private readonly DispatcherLoginThrottle _dispatcherLoginThrottle = new();

    public MaxBotService(
        MaxApiClient max,
        CollegeLmsApiClient api,
        IServiceProvider sp,
        TimeZoneInfo tz,
        IOptions<MaxBotOptions> options,
        ILogger<MaxBotService> logger
    )
    {
        _max = max;
        _api = api;
        _sp = sp;
        _tz = tz;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Max bot starting...");

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
                            new BotCommand { Name = "dispatcher", Description = "Диспетчер" },
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

        long? marker = null;

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
                            _logger.LogInformation(
                                "Update: type={Type}, marker={Marker}",
                                update.UpdateType,
                                updates.Marker
                            );
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
            _logger.LogError(ex, "Error handling update {Type}", update.UpdateType);
        }
    }

    private async Task HandleBotStartedAsync(long chatId, long userId, CancellationToken ct)
    {
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

        if (existing.GroupId is null && existing.TeacherId is null)
        {
            await ShowRoleSelectionAsync(chatId, ct, onboarding: true);
            return;
        }

        await ShowMainMenuAsync(chatId, userId, ct);
    }

    private async Task ShowRoleSelectionAsync(
        long chatId,
        CancellationToken ct,
        bool onboarding = false
    )
    {
        var rolePrefix = onboarding ? "onboard:role" : "role";
        var keyboard = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🎓 Студент",
                    Payload = $"{rolePrefix}:student",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "👨‍🏫 Преподаватель",
                    Payload = $"{rolePrefix}:teacher",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(
            chatId,
            "👋 Привет! Я бот расписания.\n\nВыбери свою роль:",
            keyboard,
            ct: ct
        );
    }

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
            _wizardStates.Remove(userId);
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        if (lower == "/help")
        {
            await _max.SendMessageAsync(
                chatId,
                "🤖 *Бот расписания*\n\n"
                    + "Всё управление — кнопками под сообщениями.\n"
                    + "/start — начать заново\n"
                    + "/settings — настройки\n"
                    + "/help — справка",
                ct: ct
            );
            return;
        }

        if (lower == "/settings")
        {
            await ShowSettingsAsync(chatId, userId, ct);
            return;
        }

        if (lower == "/dispatcher")
        {
            await HandleDispatcherCommandAsync(chatId, userId, ct);
            return;
        }

        if (_pendingDispatcherPasswords.Contains(userId))
        {
            await HandleDispatcherPasswordAsync(chatId, userId, text, ct);
            return;
        }

        if (
            _wizardStates.TryGetValue(userId, out var wizard)
            && wizard.Step is DispatcherWizardStep.Subject or DispatcherWizardStep.Note
        )
        {
            await HandleWizardTextInputAsync(chatId, userId, text.Trim(), wizard, ct);
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
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "today":
                await ShowDayAsync(chatId, userId, StudyWeek.Now(_tz), ct);
                break;
            case "day":
                var day = CallbackPayload.TryParseDate(p.Param1);
                if (day is not null)
                    await ShowDayAsync(chatId, userId, day.Value, ct);
                else
                    await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "dayprev":
                await ShowDayAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "daynext":
                await ShowDayAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "week":
                await ShowWeekAfterParse(chatId, userId, p.Param1, 0, ct);
                break;
            case "weekprev":
                await ShowWeekAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "weeknext":
                await ShowWeekAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "cal":
                var month = CallbackPayload.TryParseMonth(p.Param1);
                if (month is not null)
                    await ShowCalendarAsync(chatId, userId, month.Value, ct);
                else
                    await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "calprev":
                await ShowCalendarAfterParse(chatId, userId, p.Param1, -1, ct);
                break;
            case "calnext":
                await ShowCalendarAfterParse(chatId, userId, p.Param1, +1, ct);
                break;
            case "dayretry":
                var retryDay = CallbackPayload.TryParseDate(p.Param1);
                if (retryDay is not null)
                    await ShowDayAsync(chatId, userId, retryDay.Value, ct);
                else
                    await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "weekretry":
                await ShowWeekAfterParse(chatId, userId, p.Param1, 0, ct);
                break;
            case "calretry":
                var retryMonth = CallbackPayload.TryParseMonth(p.Param1);
                if (retryMonth is not null)
                    await ShowCalendarAsync(chatId, userId, retryMonth.Value, ct);
                else
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
            case "onboard":
                await HandleOnboardingCallbackAsync(chatId, userId, p, ct);
                break;
            case "page":
                if (p.Param1 == "groups")
                    await ShowGroupSelectionAsync(chatId, userId, int.Parse(p.Param2!), ct);
                if (p.Param1 == "teachers")
                    await ShowTeacherSelectionAsync(chatId, userId, int.Parse(p.Param2!), ct);
                break;
            case "settings":
                if (p.Param1 == "group")
                    await ShowGroupSelectionAsync(chatId, userId, 0, ct);
                else if (p.Param1 == "teacher")
                    await ShowTeacherSelectionAsync(chatId, userId, 0, ct);
                else
                    await ShowSettingsAsync(chatId, userId, ct);
                break;
            case "role-choice":
                await ShowRoleSelectionAsync(chatId, ct);
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
            case "changes":
                await ShowMyChangesAsync(chatId, userId, 0, ct);
                break;
            case "fav":
                if (p.Param1 == "toggle")
                    await HandleFavoriteToggleAsync(chatId, userId, ct);
                break;
            case "changes_page":
                var changesPage =
                    p.Param1 is { } changesPageText
                    && int.TryParse(changesPageText, out var changesPageValue)
                        ? changesPageValue
                        : 0;
                await ShowMyChangesAsync(chatId, userId, changesPage, ct);
                break;
            case "dispatcher":
                if (p.Param1 == "xlsx")
                    await ShowDispatcherChatChoiceAsync(chatId, userId, ct);
                else if (p.Param1 == "send")
                    await SendDispatcherXlsxAsync(chatId, userId, p.Param2!, ct);
                else if (p.Param1 == "wizard")
                    await StartWizardAsync(chatId, userId, ct);
                else
                    await ShowDispatcherMenuAsync(chatId, userId, ct);
                break;
            case "wiz":
                await HandleWizardCallbackAsync(chatId, userId, p.Param1, p.Param2, ct);
                break;
            default:
                _logger.LogWarning("Unknown callback action {Action}", p.Action);
                await ShowMainMenuAsync(chatId, userId, ct);
                break;
        }
    }

    /// <summary>Онбординг: выбор роли, группы/преподавателя и подтверждение после выбора.</summary>
    private async Task HandleOnboardingCallbackAsync(
        long chatId,
        long userId,
        CallbackPayload payload,
        CancellationToken ct
    )
    {
        switch (payload.Param1)
        {
            case "role":
                if (payload.Param2 is "student" or "teacher")
                    await HandleRoleSelectionAsync(
                        chatId,
                        userId,
                        payload.Param2,
                        ct,
                        onboarding: true
                    );
                else
                    await ShowMainMenuAsync(chatId, userId, ct);
                break;
            case "group":
                if (Guid.TryParse(payload.Param2, out var groupId))
                    await HandleGroupSelectionAsync(
                        chatId,
                        userId,
                        groupId.ToString(),
                        ct,
                        onboarding: true
                    );
                else
                    await ShowGroupSelectionAsync(
                        chatId,
                        userId,
                        ParsePage(payload.Param2),
                        ct,
                        onboarding: true
                    );
                break;
            case "teacher":
                if (Guid.TryParse(payload.Param2, out var teacherId))
                    await HandleTeacherSelectionAsync(
                        chatId,
                        userId,
                        teacherId.ToString(),
                        ct,
                        onboarding: true
                    );
                else
                    await ShowTeacherSelectionAsync(
                        chatId,
                        userId,
                        ParsePage(payload.Param2),
                        ct,
                        onboarding: true
                    );
                break;
            default:
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
        CancellationToken ct,
        bool onboarding = false
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        MaxBotRoleFlow.ApplyRole(settings, role);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (role == "student")
            await ShowGroupSelectionAsync(chatId, userId, 0, ct, onboarding);
        else
            await ShowTeacherSelectionAsync(chatId, userId, 0, ct, onboarding);
    }

    private async Task ShowGroupSelectionAsync(
        long chatId,
        long userId,
        int page,
        CancellationToken ct,
        bool onboarding = false
    )
    {
        var groups = await _api.GetGroupsAsync(ct);
        if (groups.Count == 0)
        {
            await _max.SendMessageAsync(chatId, "❌ Группы не найдены.", ct: ct);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)groups.Count / PageSize);
        var paged = groups.Skip(page * PageSize).Take(PageSize).ToList();

        var groupFavorites = await GetFavoritesAsync(userId, "Group", ct);
        var favIds = groupFavorites.Select(f => f.TargetId).ToHashSet();

        var selectionPrefix = onboarding ? "onboard:group" : "group";
        var buttons = new List<List<MaxButton>>();
        if (page == 0)
        {
            foreach (var f in groupFavorites)
                buttons.Add(
                    new List<MaxButton>
                    {
                        new()
                        {
                            Type = "callback",
                            Text = $"★ {f.Name}",
                            Payload = $"{selectionPrefix}:{f.TargetId}",
                        },
                    }
                );
        }

        foreach (var g in paged)
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = favIds.Contains(g.Id) ? $"★ {g.Name}" : g.Name,
                        Payload = $"{selectionPrefix}:{g.Id}",
                    },
                }
            );

        var navRow = new List<MaxButton>();
        if (page > 0)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Назад",
                    Payload = onboarding ? $"onboard:group:{page - 1}" : $"page:groups:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = onboarding ? $"onboard:group:{page + 1}" : $"page:groups:{page + 1}",
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"📚 *Выбери группу:* (стр. {page + 1}/{totalPages})",
            buttons,
            ct: ct
        );
    }

    private async Task HandleGroupSelectionAsync(
        long chatId,
        long userId,
        string groupId,
        CancellationToken ct,
        bool onboarding = false
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

        if (onboarding)
        {
            var name = (await _api.GetGroupsAsync(ct)).FirstOrDefault(g => g.Id == id)?.Name;
            await _max.SendMessageAsync(
                chatId,
                name is null ? "✅ Готово! Группа сохранена." : $"✅ Готово! Группа: {name}.",
                ct: ct
            );
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        await ShowSettingsAsync(chatId, userId, ct);
    }

    private async Task ShowTeacherSelectionAsync(
        long chatId,
        long userId,
        int page,
        CancellationToken ct,
        bool onboarding = false
    )
    {
        var teachers = await _api.GetTeachersAsync(ct);
        if (teachers.Count == 0)
        {
            await _max.SendMessageAsync(chatId, "❌ Преподаватели не найдены.", ct: ct);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)teachers.Count / PageSize);
        var paged = teachers.Skip(page * PageSize).Take(PageSize).ToList();

        var teacherFavorites = await GetFavoritesAsync(userId, "Teacher", ct);
        var favIds = teacherFavorites.Select(f => f.TargetId).ToHashSet();

        var selectionPrefix = onboarding ? "onboard:teacher" : "teacher";
        var buttons = new List<List<MaxButton>>();
        if (page == 0)
        {
            foreach (var f in teacherFavorites)
                buttons.Add(
                    new List<MaxButton>
                    {
                        new()
                        {
                            Type = "callback",
                            Text = $"★ {f.Name}",
                            Payload = $"{selectionPrefix}:{f.TargetId}",
                        },
                    }
                );
        }

        foreach (var t in paged)
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = favIds.Contains(t.Id) ? $"★ {t.FullName}" : t.FullName,
                        Payload = $"{selectionPrefix}:{t.Id}",
                    },
                }
            );

        var navRow = new List<MaxButton>();
        if (page > 0)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Назад",
                    Payload = onboarding
                        ? $"onboard:teacher:{page - 1}"
                        : $"page:teachers:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = onboarding
                        ? $"onboard:teacher:{page + 1}"
                        : $"page:teachers:{page + 1}",
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"👨‍🏫 *Выбери преподавателя:* (стр. {page + 1}/{totalPages})",
            buttons,
            ct: ct
        );
    }

    private async Task HandleTeacherSelectionAsync(
        long chatId,
        long userId,
        string teacherId,
        CancellationToken ct,
        bool onboarding = false
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

        if (onboarding)
        {
            var name = (await _api.GetTeachersAsync(ct)).FirstOrDefault(t => t.Id == id)?.FullName;
            await _max.SendMessageAsync(
                chatId,
                name is null
                    ? "✅ Готово! Преподаватель сохранён."
                    : $"✅ Готово! Преподаватель: {name}.",
                ct: ct
            );
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        await ShowSettingsAsync(chatId, userId, ct);
    }

    private async Task<UserSettings?> GetSettingsAsync(long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        return await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
    }

    private sealed record FavoriteTargetInfo(string Type, Guid Id);

    /// <summary>Текущая цель избранного: группа имеет приоритет над преподавателем.</summary>
    private static FavoriteTargetInfo? FavoriteTarget(UserSettings settings)
    {
        if (settings.GroupId.HasValue)
            return new FavoriteTargetInfo("Group", settings.GroupId.Value);
        if (settings.TeacherId.HasValue)
            return new FavoriteTargetInfo("Teacher", settings.TeacherId.Value);
        return null;
    }

    private async Task<List<BotFavorite>> GetFavoritesAsync(
        long userId,
        string targetType,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        return await db
            .BotFavorites.AsNoTracking()
            .Where(f => f.MaxUserId == userId && f.TargetType == targetType)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }

    private async Task<bool> IsFavoriteAsync(
        long userId,
        string targetType,
        Guid targetId,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        return await db.BotFavorites.AnyAsync(
            f => f.MaxUserId == userId && f.TargetType == targetType && f.TargetId == targetId,
            ct
        );
    }

    private async Task HandleFavoriteToggleAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        var target = FavoriteTarget(settings);
        if (target is null)
        {
            await SendEntityRequiredAsync(chatId, settings, ct);
            return;
        }

        var existing = await db.BotFavorites.FirstOrDefaultAsync(
            f => f.MaxUserId == userId && f.TargetType == target.Type && f.TargetId == target.Id,
            ct
        );

        if (existing is not null)
        {
            db.BotFavorites.Remove(existing);
            await db.SaveChangesAsync(ct);
            await _max.SendMessageAsync(chatId, "★ Убрано из избранного.", ct: ct);
        }
        else
        {
            var name =
                target.Type == "Group"
                    ? (await _api.GetGroupsAsync(ct)).FirstOrDefault(g => g.Id == target.Id)?.Name
                    : (await _api.GetTeachersAsync(ct))
                        .FirstOrDefault(t => t.Id == target.Id)
                        ?.FullName;

            if (name is null)
            {
                await _max.SendMessageAsync(
                    chatId,
                    "❌ Не удалось найти группу/преподавателя.",
                    ct: ct
                );
                return;
            }

            db.BotFavorites.Add(
                new BotFavorite
                {
                    Id = Guid.NewGuid(),
                    MaxUserId = userId,
                    TargetType = target.Type,
                    TargetId = target.Id,
                    Name = name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync(ct);
            await _max.SendMessageAsync(chatId, $"⭐ Добавлено в избранное: {name}", ct: ct);
        }

        await ShowMainMenuAsync(chatId, userId, ct);
    }

    /// <summary>Сообщение-подсказка с кнопкой выбора группы/преподавателя вместо дед-энда.</summary>
    private async Task SendEntityRequiredAsync(
        long chatId,
        UserSettings settings,
        CancellationToken ct
    )
    {
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "callback",
                    Text =
                        settings.Role == "student"
                            ? "📚 Выбрать группу"
                            : "👨‍🏫 Выбрать преподавателя",
                    Payload = settings.Role == "student" ? "settings:group" : "settings:teacher",
                },
            },
            new()
            {
                new()
                {
                    Type = "callback",
                    Text = "🔙 Меню",
                    Payload = "menu",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(
            chatId,
            "⚠️ Сначала выбери группу или преподавателя.",
            buttons,
            ct: ct
        );
    }

    private async Task ShowMainMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null)
        {
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        var roleLabel = settings.Role == "student" ? "Студент" : "Преподаватель";
        var entity =
            settings.GroupId.HasValue ? "группа"
            : settings.TeacherId.HasValue ? "преподаватель"
            : "не выбран";

        var today = StudyWeek.Now(_tz);
        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "📅 Сегодня",
                    Payload = "today",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "📆 Неделя",
                    Payload = CallbackPayload.Week(today),
                },
                new()
                {
                    Type = "callback",
                    Text = "🗓 Дата",
                    Payload = CallbackPayload.Cal(today),
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "open_app",
                    Text = "📱 Открыть расписание",
                    Url = BuildMiniAppUrl(settings, "today"),
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔄 Мои изменения",
                    Payload = CallbackPayload.Changes(),
                },
            },
        };

        var favTarget = FavoriteTarget(settings);
        if (favTarget is { } target)
        {
            var isFav = await IsFavoriteAsync(userId, target.Type, target.Id, ct);
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = isFav ? "★ Убрать из избранного" : "⭐ В избранное",
                        Payload = "fav:toggle",
                    },
                }
            );
        }

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "⚙️ Настройки",
                    Payload = "settings",
                },
            }
        );

        var text =
            $"🏠 *Главное меню*\n\n" + $"Роль: {roleLabel}\n" + $"Группа/Преподаватель: {entity}";

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private string BuildMiniAppUrl(UserSettings settings, string route, DateTime? date = null)
    {
        return MiniAppUrlBuilder.Build(
            _options.MiniAppUrl,
            route,
            date,
            settings.GroupId,
            settings.TeacherId
        );
    }

    private async Task ShowDayAsync(long chatId, long userId, DateTime date, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var entityName = settings.Role == "student" ? "Группа" : "Преподаватель";
        var buttons = DayNavButtons(date, settings);

        var day = await _api.GetDayViewAsync(date, settings.GroupId, settings.TeacherId, ct);
        if (day is null)
        {
            await _max.SendInlineKeyboardAsync(
                chatId,
                "❌ Не удалось загрузить расписание.",
                RetryButtons(CallbackPayload.RetryDay(date)),
                ct: ct
            );
            return;
        }

        var text = MessageFormatter.FormatDaySchedule(
            day,
            entityName,
            showGroup: settings.Role == "teacher"
        );
        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    /// <summary>Кнопка «Повторить» для ошибок загрузки расписания.</summary>
    private static List<List<MaxButton>> RetryButtons(string payload) =>
        [
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔄 Повторить",
                    Payload = payload,
                },
            },
        ];

    private List<List<MaxButton>> DayNavButtons(DateTime date, UserSettings settings)
    {
        return
        [
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "← Пред. день",
                    Payload = CallbackPayload.DayPrev(date),
                },
                new()
                {
                    Type = "callback",
                    Text = "След. день →",
                    Payload = CallbackPayload.DayNext(date),
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "open_app",
                    Text = "📱 Открыть в mini-app",
                    Url = BuildMiniAppUrl(settings, "day", date),
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "📆 Неделя",
                    Payload = CallbackPayload.Week(date),
                },
                new()
                {
                    Type = "callback",
                    Text = "🔙 Меню",
                    Payload = "menu",
                },
            },
        ];
    }

    private async Task ShowWeekAsync(
        long chatId,
        long userId,
        DateTime weekStart,
        CancellationToken ct
    )
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var entityName = settings.Role == "student" ? "Группа" : "Преподаватель";
        var week = await _api.GetWeekViewAsync(
            StudyWeek.ForDate(weekStart),
            settings.GroupId,
            settings.TeacherId,
            ct
        );
        if (week is null)
        {
            await _max.SendInlineKeyboardAsync(
                chatId,
                "❌ Не удалось загрузить расписание.",
                RetryButtons(CallbackPayload.RetryWeek(weekStart)),
                ct: ct
            );
            return;
        }

        var text = MessageFormatter.FormatWeekSchedule(
            week,
            entityName,
            showGroup: settings.Role == "teacher"
        );
        var buttons = WeekNavButtons(weekStart, settings);

        if (text.Length > 4000)
        {
            var header =
                $"📅 *Неделя {week.Week} · {MessageFormatter.FormatShortDate(week.WeekStart)}–{MessageFormatter.FormatShortDate(week.WeekStart.AddDays(6))}*";
            await _max.SendInlineKeyboardAsync(chatId, header, buttons, ct: ct);

            foreach (var day in week.Days.OrderBy(d => MessageFormatter.DayIndex(d.DayOfWeek)))
            {
                var dayText = MessageFormatter.FormatDaySchedule(
                    day,
                    entityName,
                    showGroup: settings.Role == "teacher"
                );
                await _max.SendMessageAsync(chatId, dayText, ct: ct);
                await Task.Delay(500, ct);
            }
            return;
        }

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private List<List<MaxButton>> WeekNavButtons(DateTime weekStart, UserSettings settings)
    {
        var firstRow = new List<MaxButton>();
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            firstRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text =
                        $"{MessageFormatter.DayAbbrForDate(date)} {MessageFormatter.FormatShortDate(date)}",
                    Payload = CallbackPayload.Day(date),
                }
            );
        }

        var navRow = new List<MaxButton>
        {
            new()
            {
                Type = "callback",
                Text = "← Неделя",
                Payload = CallbackPayload.WeekPrev(weekStart),
            },
            new()
            {
                Type = "callback",
                Text = "Неделя →",
                Payload = CallbackPayload.WeekNext(weekStart),
            },
        };

        var actionRow = new List<MaxButton>
        {
            new()
            {
                Type = "open_app",
                Text = "📱 Открыть в mini-app",
                Url = BuildMiniAppUrl(settings, "week", weekStart),
            },
            new()
            {
                Type = "callback",
                Text = "🗓 Дата",
                Payload = CallbackPayload.Cal(weekStart),
            },
            new()
            {
                Type = "callback",
                Text = "📅 Сегодня",
                Payload = "today",
            },
            new()
            {
                Type = "callback",
                Text = "🔙 Меню",
                Payload = "menu",
            },
        };

        return [firstRow, navRow, actionRow];
    }

    private async Task ShowCalendarAsync(
        long chatId,
        long userId,
        DateTime month,
        CancellationToken ct
    )
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var maxMonth = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(16 * 7);
        var meta = await _api.GetScheduleMetaAsync(ct);
        if (
            meta is { TotalWeeks: > 0 }
            && DateTime.TryParse(
                meta.SemesterStart,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var semesterStart
            )
        )
        {
            maxMonth = StudyWeek.MondayOf(semesterStart).AddDays(meta.TotalWeeks * 7);
        }
        var rows = CalendarFormatter.BuildGrid(month);

        var navRow = new List<MaxButton>();
        if (CalendarFormatter.CanGoPrev(month))
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Пред. месяц",
                    Payload = CallbackPayload.CalPrev(month),
                }
            );
        if (CalendarFormatter.CanGoNext(month, maxMonth))
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "След. месяц →",
                    Payload = CallbackPayload.CalNext(month),
                }
            );

        var actionRow = new List<MaxButton>
        {
            new()
            {
                Type = "callback",
                Text = "📅 Сегодня",
                Payload = "today",
            },
            new()
            {
                Type = "callback",
                Text = "🔙 Меню",
                Payload = "menu",
            },
        };

        var buttons = new List<List<MaxButton>>();
        buttons.AddRange(rows);
        if (navRow.Count > 0)
            buttons.Add(navRow);
        buttons.Add(actionRow);

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"🗓 *{CalendarFormatter.MonthTitle(month)}*",
            buttons,
            ct: ct
        );
    }

    private async Task ShowDayAfterParse(
        long chatId,
        long userId,
        string? dateText,
        int deltaDays,
        CancellationToken ct
    )
    {
        var date = CallbackPayload.TryParseDate(dateText);
        if (date is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        await ShowDayAsync(chatId, userId, date.Value.AddDays(deltaDays), ct);
    }

    private async Task ShowWeekAfterParse(
        long chatId,
        long userId,
        string? dateText,
        int deltaWeeks,
        CancellationToken ct
    )
    {
        var date = CallbackPayload.TryParseDate(dateText);
        if (date is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        var anchor = StudyWeek.MondayOf(date.Value).AddDays(deltaWeeks * 7);
        await ShowWeekAsync(chatId, userId, anchor, ct);
    }

    private async Task ShowCalendarAfterParse(
        long chatId,
        long userId,
        string? monthText,
        int deltaMonths,
        CancellationToken ct
    )
    {
        var month = CallbackPayload.TryParseMonth(monthText);
        if (month is null)
        {
            await ShowMainMenuAsync(chatId, userId, ct);
            return;
        }

        await ShowCalendarAsync(chatId, userId, month.Value.AddMonths(deltaMonths), ct);
    }

    private async Task ShowMyChangesAsync(long chatId, long userId, int page, CancellationToken ct)
    {
        var settings = await GetSettingsAsync(userId, ct);
        if (settings is null)
        {
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        if (settings.GroupId is null && settings.TeacherId is null)
        {
            await SendEntityRequiredAsync(chatId, settings, ct);
            return;
        }

        string? groupName = null;
        string? teacherName = null;
        if (settings.GroupId.HasValue)
            groupName = (await _api.GetGroupsAsync(ct))
                .FirstOrDefault(g => g.Id == settings.GroupId.Value)
                ?.Name;
        if (settings.TeacherId.HasValue)
            teacherName = (await _api.GetTeachersAsync(ct))
                .FirstOrDefault(t => t.Id == settings.TeacherId.Value)
                ?.FullName;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var query = db.ScheduleRevisions.AsNoTracking().AsQueryable();
        if (groupName is not null)
            query = query.Where(r => r.GroupName == groupName);
        if (teacherName is not null)
            query = query.Where(r => r.TeacherName == teacherName);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page * ChangesPageSize)
            .Take(ChangesPageSize)
            .ToListAsync(ct);

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / ChangesPageSize));
        var text = MessageFormatter.FormatMyChanges(
            items,
            page,
            totalPages,
            ChangesPageSize,
            _options.MiniAppUrl
        );

        var buttons = new List<List<MaxButton>>();
        var navRow = new List<MaxButton>();
        if (page > 0)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "← Назад",
                    Payload = CallbackPayload.ChangesPage(page - 1),
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = CallbackPayload.ChangesPage(page + 1),
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "open_app",
                    Text = "📱 Открыть изменения",
                    Url = BuildMiniAppUrl(settings, "changes"),
                },
            }
        );
        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔙 Меню",
                    Payload = "menu",
                },
            }
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

        var roleLabel = settings.Role == "student" ? "Студент" : "Преподаватель";
        var notifyDays = string.Join(
            ", ",
            settings.NotifyDays.Select(d => MessageFormatter.GetShortDayLabel(d))
        );
        var notifyStatus = settings.NotifyEnabled ? "Вкл" : "Выкл";
        var hasEntity = (settings.GroupId ?? settings.TeacherId) is not null;

        var text =
            $"⚙️ *Настройки*\n\n"
            + $"Роль: {roleLabel}\n"
            + $"Группа/Преподаватель: {(hasEntity ? "✅ выбран" : "❌ не выбран")}\n"
            + $"Уведомления: {notifyStatus} ({notifyDays})\n"
            + $"⏰ Время дайджеста: {settings.NotifyTime:hh\\:mm}";

        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🎓 Сменить роль",
                    Payload = "role-choice",
                },
            },
        };

        if (settings.Role == "student")
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = "📚 Сменить группу",
                        Payload = "settings:group",
                    },
                }
            );
        else
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = "👨‍🏫 Сменить преподавателя",
                        Payload = "settings:teacher",
                    },
                }
            );

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔔 Уведомления",
                    Payload = "notify:toggle",
                },
            }
        );

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
                    Text = "🔙 Меню",
                    Payload = "menu",
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

        var status = settings.NotifyEnabled ? "включены ✅" : "выключены ❌";

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"🔔 Уведомления {status}\n\nНажми на день, чтобы включить или выключить:",
            BuildNotifyDayButtons(settings.NotifyDays),
            ct: ct
        );
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

    private async Task HandleDispatcherCommandAsync(long chatId, long userId, CancellationToken ct)
    {
        _pendingDispatcherPasswords.Add(userId);
        await _max.SendMessageAsync(chatId, "🔐 Введи пароль диспетчера:", ct: ct);
    }

    private async Task HandleDispatcherPasswordAsync(
        long chatId,
        long userId,
        string password,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        if (_dispatcherLoginThrottle.IsBlocked(userId, now))
        {
            var minutes = (int)
                Math.Ceiling(_dispatcherLoginThrottle.RetryAfter(userId, now).TotalMinutes);
            await _max.SendMessageAsync(
                chatId,
                $"❌ Слишком много попыток. Повторите через {minutes} мин.",
                ct: ct
            );
            return;
        }

        var token = await _api.DispatcherLoginAsync(password.Trim(), ct);
        if (token is null)
        {
            _dispatcherLoginThrottle.RegisterFailure(userId, now);
            await _max.SendMessageAsync(chatId, "❌ Пароль неверный. Попробуй ещё раз.", ct: ct);
            return;
        }

        _dispatcherLoginThrottle.Reset(userId);
        _dispatcherTokens[userId] = token;
        _pendingDispatcherPasswords.Remove(userId);
        await _max.SendMessageAsync(chatId, "✅ Диспетчер авторизован.", ct: ct);
        await ShowDispatcherMenuAsync(chatId, userId, ct);
    }

    private async Task ShowDispatcherMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var buttons = new List<List<MaxButton>>
        {
            new()
            {
                new()
                {
                    Type = "callback",
                    Text = "➕ Новая корректировка",
                    Payload = "dispatcher:wizard",
                },
            },
        };

        if (_options.DispatchChatIds.Count > 0)
        {
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = "📊 Отправить XLSX расписания",
                        Payload = "dispatcher:xlsx",
                    },
                }
            );
        }
        else
        {
            await _max.SendMessageAsync(
                chatId,
                "⚠️ Отправка XLSX не настроена (DispatchChatIds пуст).",
                ct: ct
            );
        }

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔙 Меню",
                    Payload = "menu",
                },
            }
        );

        await _max.SendInlineKeyboardAsync(
            chatId,
            "🔐 *Панель диспетчера*\n\nКорректировки создаются в mini-app, итоговый XLSX отправляется по кнопке.",
            buttons,
            ct: ct
        );
    }

    private async Task ShowDispatcherChatChoiceAsync(long chatId, long userId, CancellationToken ct)
    {
        if (_options.DispatchChatIds.Count == 0)
        {
            await _max.SendMessageAsync(
                chatId,
                "⚠️ Отправка XLSX не настроена (DispatchChatIds пуст).",
                ct: ct
            );
            return;
        }

        var row = _options
            .DispatchChatIds.Select(target => new MaxButton
            {
                Type = "callback",
                Text = $"📊 {target}",
                Payload = $"dispatcher:send:{target}",
            })
            .ToList();

        await _max.SendInlineKeyboardAsync(
            chatId,
            "Выбери чат для отправки XLSX:",
            new List<List<MaxButton>> { row },
            ct: ct
        );
    }

    private async Task SendDispatcherXlsxAsync(
        long chatId,
        long userId,
        string targetChat,
        CancellationToken ct
    )
    {
        if (!_options.DispatchChatIds.Contains(targetChat))
        {
            await _max.SendMessageAsync(chatId, "❌ Чат не в whitelist диспетчера.", ct: ct);
            return;
        }

        if (!_dispatcherTokens.TryGetValue(userId, out var token))
        {
            await HandleDispatcherCommandAsync(chatId, userId, ct);
            return;
        }

        var settings = await GetSettingsAsync(userId, ct);
        var groupId = settings?.GroupId;
        if (!groupId.HasValue)
        {
            await _max.SendMessageAsync(chatId, "⚠️ Сначала выбери группу в настройках.", ct: ct);
            return;
        }

        var bytes = await _api.GetScheduleXlsxAsync(groupId, token, ct);
        if (bytes is null)
        {
            await _max.SendMessageAsync(chatId, "❌ Не удалось сформировать XLSX.", ct: ct);
            return;
        }

        var target = long.TryParse(targetChat, out var targetId) ? targetId : 0;
        var link = MiniAppUrlBuilder.BuildScheduleExportXlsxUrl(_options.MiniAppUrl, groupId);

        try
        {
            await _max.SendMessageAsync(
                target,
                $"📊 Расписание (XLSX, {bytes.Length} байт). Ссылка на скачивание:\n{link}",
                ct: ct
            );
            await _max.SendMessageAsync(chatId, "✅ XLSX отправлен.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send XLSX message");
            await _max.SendMessageAsync(chatId, "❌ Не удалось отправить XLSX.", ct: ct);
        }
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
            return;
        }

        settings.NotifyTime = candidate;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await _max.SendMessageAsync(
            chatId,
            $"⏰ Время дайджеста: {settings.NotifyTime:hh\\:mm} (МСК)",
            ct: ct
        );
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

        await _max.SendInlineKeyboardAsync(
            chatId,
            "🔔 Нажми на день, чтобы включить или выключить:",
            BuildNotifyDayButtons(settings.NotifyDays),
            ct: ct
        );
    }

    // ── Визард корректировок диспетчера ────────────────────────────────

    private async Task StartWizardAsync(long chatId, long userId, CancellationToken ct)
    {
        if (!_dispatcherTokens.ContainsKey(userId))
        {
            await _max.SendMessageAsync(
                chatId,
                "🔐 Сначала авторизуйся: /dispatcher и введи пароль.",
                ct: ct
            );
            return;
        }

        _wizardStates[userId] = new DispatcherWizardState { Step = DispatcherWizardStep.Type };

        await _max.SendInlineKeyboardAsync(
            chatId,
            "🛠 *Новая корректировка*",
            DispatcherCorrectionWizard.BuildTypeKeyboard(),
            ct: ct
        );
    }

    private async Task HandleWizardCallbackAsync(
        long chatId,
        long userId,
        string? action,
        string? param,
        CancellationToken ct
    )
    {
        if (action == "cancel")
        {
            await CancelWizardAsync(chatId, userId, ct);
            return;
        }

        if (!_wizardStates.TryGetValue(userId, out var ws))
        {
            await StartWizardAsync(chatId, userId, ct);
            return;
        }

        if (!_dispatcherTokens.ContainsKey(userId))
        {
            _wizardStates.Remove(userId);
            await _max.SendMessageAsync(
                chatId,
                "🔐 Сессия диспетчера истекла. Авторизуйся: /dispatcher.",
                ct: ct
            );
            return;
        }

        switch (action)
        {
            case "type":
                ws.ChangeType = param;
                ws.Step = DispatcherWizardStep.Day;
                await _max.SendInlineKeyboardAsync(
                    chatId,
                    "День недели:",
                    DispatcherCorrectionWizard.BuildDayKeyboard(),
                    ct: ct
                );
                break;
            case "day":
                ws.Day = int.TryParse(param, out var day) ? day : 1;
                await ShowWizardWeekKeyboardAsync(chatId, userId, ws, ct);
                break;
            case "week":
                ws.Week = int.TryParse(param, out var week) ? week : 1;
                ws.Step = DispatcherWizardStep.Group;
                await ShowWizardGroupsAsync(chatId, userId, ws, 0, ct);
                break;
            case "gpage":
                await ShowWizardGroupsAsync(
                    chatId,
                    userId,
                    ws,
                    int.TryParse(param, out var gpage) ? gpage : 0,
                    ct
                );
                break;
            case "group":
                var sep = param?.IndexOf(':') ?? -1;
                if (sep <= 0 || !Guid.TryParse(param![..sep], out var groupId))
                {
                    await CancelWizardAsync(chatId, userId, ct);
                    return;
                }
                ws.GroupId = groupId;
                ws.GroupName = param[(sep + 1)..];
                await AfterWizardGroupAsync(chatId, userId, ws, ct);
                break;
            case "rem":
                await HandleWizardRemovedPairAsync(chatId, userId, ws, param, ct);
                break;
            case "pair":
                ws.NewPair = int.TryParse(param, out var pair) ? pair : 1;
                if (ws.ChangeType == "Move")
                {
                    ws.Step = DispatcherWizardStep.Subject;
                    await _max.SendMessageAsync(
                        chatId,
                        "Предмет (переносим с указанной пары):",
                        ct: ct
                    );
                }
                else
                {
                    ws.Step = DispatcherWizardStep.Subject;
                    await _max.SendMessageAsync(chatId, "Предмет:", ct: ct);
                }
                break;
            case "tpage":
                await ShowWizardTeacherKeyboardAsync(
                    chatId,
                    userId,
                    ws,
                    int.TryParse(param, out var tpage) ? tpage : 0,
                    ct
                );
                break;
            case "teacher":
                await HandleWizardTeacherAsync(chatId, userId, ws, param, ct);
                break;
            case "apply":
                await ApplyWizardAsync(chatId, userId, ws, ct);
                break;
            default:
                _logger.LogWarning("Unknown wizard action {Action}", action);
                await CancelWizardAsync(chatId, userId, ct);
                break;
        }
    }

    private async Task ShowWizardWeekKeyboardAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        ws.Step = DispatcherWizardStep.Week;

        var meta = await _api.GetScheduleMetaAsync(ct);
        var totalWeeks = meta?.TotalWeeks is > 0 ? meta.TotalWeeks : 16;
        var currentWeek = meta?.CurrentWeek is > 0 ? meta.CurrentWeek : 1;

        var buttons = DispatcherCorrectionWizard.BuildWeekGrid(currentWeek, totalWeeks);
        buttons.Add(DispatcherCorrectionWizard.CancelRow());

        await _max.SendInlineKeyboardAsync(
            chatId,
            "Неделя семестра (• — текущая):",
            buttons,
            ct: ct
        );
    }

    private async Task ShowWizardGroupsAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        int page,
        CancellationToken ct
    )
    {
        var groups = await _api.GetGroupsAsync(ct);
        if (groups.Count == 0)
        {
            await CancelWizardAsync(chatId, userId, ct);
            return;
        }

        var totalPages = (int)Math.Ceiling((double)groups.Count / PageSize);
        var paged = groups.Skip(page * PageSize).Take(PageSize).ToList();

        var buttons = paged
            .Select(g => new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = g.Name,
                    Payload = $"wiz:group:{g.Id}:{g.Name}",
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
                    Payload = $"wiz:gpage:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = $"wiz:gpage:{page + 1}",
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);
        buttons.Add(DispatcherCorrectionWizard.CancelRow());

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"Выбери группу (стр. {page + 1}/{totalPages}):",
            buttons,
            ct: ct
        );
    }

    private async Task AfterWizardGroupAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        if (ws.ChangeType == "Add")
        {
            ws.Step = DispatcherWizardStep.NewPair;
            var buttons = DispatcherCorrectionWizard.BuildPairKeyboard("pair");
            buttons.Add(DispatcherCorrectionWizard.CancelRow());
            await _max.SendInlineKeyboardAsync(chatId, "Номер новой пары:", buttons, ct: ct);
            return;
        }

        await ShowWizardRemovedPairAsync(chatId, userId, ws, ct);
    }

    /// <summary>Показывает пары выбранной группы на день/неделю для выбора снимаемой.</summary>
    private async Task ShowWizardRemovedPairAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        ws.Step = DispatcherWizardStep.RemovedPair;

        var entries = await _api.GetScheduleAsync(
            groupId: ws.GroupId,
            week: ws.Week,
            dayOfWeek: ws.Day,
            ct: ct
        );

        var pairs = entries
            .OrderBy(e => e.NumberPair)
            .GroupBy(e => e.NumberPair)
            .Select(g => g.First())
            .ToList();

        if (pairs.Count == 0)
        {
            var buttons = new List<List<MaxButton>>
            {
                new()
                {
                    new()
                    {
                        Type = "callback",
                        Text = "🟢 Всё-таки добавить",
                        Payload = "wiz:type:Add",
                    },
                },
                DispatcherCorrectionWizard.CancelRow(),
            };
            await _max.SendInlineKeyboardAsync(
                chatId,
                "📭 У этой группы на выбранный день пар нет.\nСнять или заменить нечего — можно только добавить пару.",
                buttons,
                ct: ct
            );
            return;
        }

        var buttons2 = pairs
            .Select(e => new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = $"№{e.NumberPair} — {e.Subject}",
                    Payload = $"wiz:rem:{e.NumberPair}",
                },
            })
            .ToList();
        buttons2.Add(DispatcherCorrectionWizard.CancelRow());

        await _max.SendInlineKeyboardAsync(chatId, "Какую пару снимаем:", buttons2, ct: ct);
    }

    private async Task HandleWizardRemovedPairAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        string? param,
        CancellationToken ct
    )
    {
        if (!int.TryParse(param, out var removedPair))
        {
            await CancelWizardAsync(chatId, userId, ct);
            return;
        }

        var entries = await _api.GetScheduleAsync(
            groupId: ws.GroupId,
            week: ws.Week,
            dayOfWeek: ws.Day,
            ct: ct
        );
        var entry = entries
            .Where(e => e.NumberPair == removedPair)
            .OrderBy(e => e.NumberPair)
            .FirstOrDefault();

        ws.RemovedNumberPair = removedPair;
        ws.RemovedSubject = entry?.Subject;
        ws.RemovedTeacherId = entry?.TeacherId;
        ws.RemovedTeacherName = entry?.TeacherName;

        if (ws.ChangeType == "Remove")
        {
            ws.Step = DispatcherWizardStep.Note;
            await _max.SendMessageAsync(chatId, "Примечание (или «—» без примечания):", ct: ct);
            return;
        }

        if (ws.ChangeType == "Move")
        {
            ws.Step = DispatcherWizardStep.NewPair;
            var moveButtons = DispatcherCorrectionWizard.BuildPairKeyboard("pair");
            moveButtons.Add(DispatcherCorrectionWizard.CancelRow());
            await _max.SendInlineKeyboardAsync(
                chatId,
                "На какую пару переносим:",
                moveButtons,
                ct: ct
            );
            return;
        }

        // Replace — новый предмет на тот же слот
        ws.Step = DispatcherWizardStep.Subject;
        await _max.SendMessageAsync(chatId, $"Новый предмет вместо «{ws.RemovedSubject}»:", ct: ct);
    }

    private async Task ShowWizardTeacherKeyboardAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        int page,
        CancellationToken ct
    )
    {
        var teachers = await _api.GetTeachersAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)teachers.Count / PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);
        var paged = teachers.Skip(page * PageSize).Take(PageSize).ToList();

        var buttons = paged
            .Select(t => new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = t.FullName,
                    Payload = $"wiz:teacher:{t.Id}",
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
                    Payload = $"wiz:tpage:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = $"wiz:tpage:{page + 1}",
                }
            );
        if (navRow.Count > 0)
            buttons.Add(navRow);

        buttons.Add(
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "— без преподавателя",
                    Payload = "wiz:teacher:none",
                },
            }
        );
        buttons.Add(DispatcherCorrectionWizard.CancelRow());

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"Преподаватель (стр. {page + 1}/{totalPages}):",
            buttons,
            ct: ct
        );
    }

    private async Task HandleWizardTeacherAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        string? param,
        CancellationToken ct
    )
    {
        if (param == "none")
        {
            ws.TeacherId = null;
            ws.TeacherName = null;
        }
        else
        {
            if (!Guid.TryParse(param, out var teacherId))
            {
                await CancelWizardAsync(chatId, userId, ct);
                return;
            }
            var teacher = (await _api.GetTeachersAsync(ct)).FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                await CancelWizardAsync(chatId, userId, ct);
                return;
            }
            ws.TeacherId = teacher.Id;
            ws.TeacherName = teacher.FullName;
        }

        ws.Step = DispatcherWizardStep.Note;
        await _max.SendMessageAsync(chatId, "Примечание (или «—» без примечания):", ct: ct);
    }

    private async Task HandleWizardTextInputAsync(
        long chatId,
        long userId,
        string text,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        if (text.Length > 300)
            text = text[..300];

        if (ws.Step == DispatcherWizardStep.Subject)
        {
            ws.Subject = text;
            await ShowWizardTeacherKeyboardAsync(chatId, userId, ws, 0, ct);
            return;
        }

        ws.Note = text;
        await ShowWizardConfirmAsync(chatId, userId, ws, ct);
    }

    private async Task ShowWizardConfirmAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        await _max.SendInlineKeyboardAsync(
            chatId,
            DispatcherCorrectionWizard.BuildPreview(ws),
            DispatcherCorrectionWizard.BuildConfirmKeyboard(),
            ct: ct
        );
    }

    private async Task ApplyWizardAsync(
        long chatId,
        long userId,
        DispatcherWizardState ws,
        CancellationToken ct
    )
    {
        if (!_dispatcherTokens.TryGetValue(userId, out var token))
        {
            _wizardStates.Remove(userId);
            await _max.SendMessageAsync(
                chatId,
                "🔐 Сессия диспетчера истекла. Авторизуйся: /dispatcher.",
                ct: ct
            );
            return;
        }

        var meta = await _api.GetScheduleMetaAsync(ct);
        if (
            meta?.SemesterStart is null
            || !DateTime.TryParse(
                meta.SemesterStart,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var semesterStart
            )
            || ws.Day is < 1 or > 6
            || ws.Week < 1
            || ws.Week > meta.TotalWeeks
        )
        {
            await _max.SendMessageAsync(
                chatId,
                "❌ Не удалось определить дату корректировки по параметрам расписания.",
                ct: ct
            );
            await ShowWizardConfirmAsync(chatId, userId, ws, ct);
            return;
        }

        var correctionDate = DispatcherCorrectionWizard.CalculateCorrectionDate(
            semesterStart,
            ws.Week,
            ws.Day
        );
        var entry = DispatcherCorrectionWizard.BuildEntry(ws);
        var batch = await _api.CreateCorrectionBatchAsync(correctionDate, token, ct);
        if (batch is null)
        {
            await _max.SendMessageAsync(
                chatId,
                "❌ Не удалось создать пакет корректировки. Проверь данные и попробуй снова.",
                ct: ct
            );
            await ShowWizardConfirmAsync(chatId, userId, ws, ct);
            return;
        }

        var positionAdded = await _api.AddCorrectionPositionAsync(
            batch.Id,
            new CreateCorrectionPositionDto
            {
                ChangeType = entry.ChangeType,
                GroupId = entry.GroupId,
                GroupName = entry.GroupName,
                NumberPair = entry.NumberPair,
                Subject = entry.Subject,
                TeacherId = entry.TeacherId,
                TeacherName = entry.TeacherName,
                RemovedSubject = entry.RemovedSubject,
                RemovedTeacherId = entry.RemovedTeacherId,
                RemovedTeacherName = entry.RemovedTeacherName,
                RemovedNumberPair = entry.RemovedNumberPair,
                Note = entry.Note ?? (entry.ChangeType == "Move" ? $"вм.{entry.NumberPair}" : null),
            },
            token,
            ct
        );
        var result = positionAdded
            ? await _api.ApplyCorrectionBatchAsync(batch.Id, token, Guid.NewGuid().ToString(), ct)
            : null;

        if (result is null)
        {
            await _max.SendMessageAsync(
                chatId,
                "❌ API отклонил корректировку. Проверь данные и попробуй снова.",
                ct: ct
            );
            await ShowWizardConfirmAsync(chatId, userId, ws, ct);
            return;
        }

        _wizardStates.Remove(userId);
        await _max.SendMessageAsync(
            chatId,
            $"✅ Применено записей: {result.Applied}.\nПодписчики получат уведомления.",
            ct: ct
        );
        await ShowDispatcherMenuAsync(chatId, userId, ct);
    }

    private async Task CancelWizardAsync(long chatId, long userId, CancellationToken ct)
    {
        _wizardStates.Remove(userId);
        await _max.SendMessageAsync(chatId, "❌ Корректировка отменена.", ct: ct);
        await ShowDispatcherMenuAsync(chatId, userId, ct);
    }
}
