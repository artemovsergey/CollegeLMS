using System.Collections.Concurrent;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Bot;

public class MaxBotService : BackgroundService
{
    private readonly MaxApiClient _max;
    private readonly CollegeLmsApiClient _api;
    private readonly IServiceProvider _sp;
    private readonly ILogger<MaxBotService> _logger;
    private readonly TimeZoneInfo _tz;

    private const int PageSize = 5;
    private const int PollTimeoutSeconds = 30;

    private readonly ConcurrentDictionary<long, InteractionState> _states = new();

    public MaxBotService(
        MaxApiClient max,
        CollegeLmsApiClient api,
        IServiceProvider sp,
        TimeZoneInfo tz,
        ILogger<MaxBotService> logger
    )
    {
        _max = max;
        _api = api;
        _sp = sp;
        _tz = tz;
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
                            new BotCommand
                            {
                                Name = "schedule",
                                Description = "Расписание на сегодня",
                            },
                            new BotCommand { Name = "week", Description = "Расписание на неделю" },
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
            db.UserSettings.Add(
                new UserSettings
                {
                    Id = Guid.NewGuid(),
                    MaxUserId = userId,
                    MaxChatId = chatId,
                    Role = "student",
                    NotifyEnabled = true,
                    NotifyDays = [1, 5],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        var keyboard = new List<List<MaxButton>>
        {
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
            await HandleBotStartedAsync(chatId, userId, ct);
            return;
        }

        if (lower == "/help")
        {
            await _max.SendMessageAsync(
                chatId,
                "🤖 *Бот расписания*\n\n"
                    + "/schedule — расписание на сегодня\n"
                    + "/schedule пн — расписание на понедельник\n"
                    + "/week — расписание на неделю\n"
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

        if (lower == "/schedule" || lower.StartsWith("/schedule "))
        {
            var dayPart = lower == "/schedule" ? null : text["/schedule ".Length..].Trim();
            await SendScheduleAsync(chatId, userId, dayPart, ct);
            return;
        }

        if (lower == "/week")
        {
            await SendWeekScheduleAsync(chatId, userId, ct);
            return;
        }

        if (_states.TryGetValue(userId, out _))
        {
            _states.TryRemove(userId, out _);
            await _max.SendMessageAsync(chatId, "Ок.", ct: ct);
            return;
        }

        await _max.SendMessageAsync(
            chatId,
            "Не понял команду. Используй /help для справки.",
            ct: ct
        );
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

        var parts = payload.Split(':');
        var action = parts[0];
        var param = parts.Length > 1 ? parts[1] : null;

        switch (action)
        {
            case "role":
                await HandleRoleSelectionAsync(chatId, userId, param!, ct);
                break;
            case "group":
                await HandleGroupSelectionAsync(chatId, userId, param!, ct);
                break;
            case "teacher":
                await HandleTeacherSelectionAsync(chatId, userId, param!, ct);
                break;
            case "day":
                await SendScheduleAsync(
                    chatId,
                    userId,
                    MessageFormatter.GetMinDayLabel(int.Parse(param!)),
                    ct
                );
                break;
            case "week":
                await SendWeekScheduleAsync(chatId, userId, ct);
                break;
            case "notify":
                await HandleNotifyToggleAsync(chatId, userId, ct);
                break;
            case "notifyday":
                await HandleNotifyDayToggleAsync(chatId, userId, int.Parse(param!), ct);
                break;
            case "notifysave":
                await _max.SendMessageAsync(chatId, "✅ Настройки уведомлений сохранены!", ct: ct);
                break;
            case "settings":
                if (param == "group")
                    await ShowGroupSelectionAsync(chatId, userId, 0, ct);
                if (param == "teacher")
                    await ShowTeacherSelectionAsync(chatId, userId, 0, ct);
                break;
            case "page":
                if (param == "groups")
                    await ShowGroupSelectionAsync(chatId, userId, int.Parse(parts[2]), ct);
                if (param == "teachers")
                    await ShowTeacherSelectionAsync(chatId, userId, int.Parse(parts[2]), ct);
                break;
        }
    }

    private async Task HandleRoleSelectionAsync(
        long chatId,
        long userId,
        string role,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        settings.Role = role;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (role == "student")
            await ShowGroupSelectionAsync(chatId, userId, 0, ct);
        else
            await ShowTeacherSelectionAsync(chatId, userId, 0, ct);
    }

    private async Task ShowGroupSelectionAsync(
        long chatId,
        long userId,
        int page,
        CancellationToken ct
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

        var buttons = new List<List<MaxButton>>();
        foreach (var g in paged)
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = g.Name,
                        Payload = $"group:{g.Id}",
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
                    Payload = $"page:groups:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = $"page:groups:{page + 1}",
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
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        settings.GroupId = Guid.Parse(groupId);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await _max.SendMessageAsync(
            chatId,
            "✅ Группа выбрана!\n\n"
                + "/schedule — расписание на сегодня\n"
                + "/week — расписание на неделю\n"
                + "/settings — настройки",
            ct: ct
        );
    }

    private async Task ShowTeacherSelectionAsync(
        long chatId,
        long userId,
        int page,
        CancellationToken ct
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

        var buttons = new List<List<MaxButton>>();
        foreach (var t in paged)
            buttons.Add(
                new List<MaxButton>
                {
                    new()
                    {
                        Type = "callback",
                        Text = t.FullName,
                        Payload = $"teacher:{t.Id}",
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
                    Payload = $"page:teachers:{page - 1}",
                }
            );
        if (page < totalPages - 1)
            navRow.Add(
                new MaxButton
                {
                    Type = "callback",
                    Text = "Далее →",
                    Payload = $"page:teachers:{page + 1}",
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
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null)
            return;

        settings.TeacherId = Guid.Parse(teacherId);
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await _max.SendMessageAsync(
            chatId,
            "✅ Преподаватель выбран!\n\n"
                + "/schedule — расписание на сегодня\n"
                + "/week — расписание на неделю\n"
                + "/settings — настройки",
            ct: ct
        );
    }

    private async Task SendScheduleAsync(
        long chatId,
        long userId,
        string? dayText,
        CancellationToken ct
    )
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await _max.SendMessageAsync(chatId, "⚠️ Сначала настрой профиль: /settings", ct: ct);
            return;
        }

        var dayOfWeek = MessageFormatter.ParseDayOfWeek(dayText);
        if (dayOfWeek == 0)
            dayOfWeek = MessageFormatter.ToApiDay(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz).DayOfWeek
            );
        if (dayOfWeek == 0)
        {
            await _max.SendMessageAsync(chatId, MessageFormatter.SundayMessage, ct: ct);
            return;
        }

        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            dayOfWeek: dayOfWeek,
            ct: ct
        );

        var entityName = settings.GroupId.HasValue ? "Группа" : "Преподаватель";
        var text = MessageFormatter.FormatDaySchedule(entries, dayOfWeek, entityName);

        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "← Пред. день",
                    Payload = $"day:{Math.Max(1, dayOfWeek - 1)}",
                },
                new()
                {
                    Type = "callback",
                    Text = "След. день →",
                    Payload = $"day:{Math.Min(6, dayOfWeek + 1)}",
                },
            },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "📅 На неделю",
                    Payload = "week:current",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private async Task SendWeekScheduleAsync(long chatId, long userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();

        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == userId, ct);
        if (settings is null || (settings.GroupId is null && settings.TeacherId is null))
        {
            await _max.SendMessageAsync(chatId, "⚠️ Сначала настрой профиль: /settings", ct: ct);
            return;
        }

        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            period: "week",
            week: StudyWeek.Current(_tz),
            ct: ct
        );

        var entityName = settings.GroupId.HasValue ? "Группа" : "Преподаватель";
        var text = MessageFormatter.FormatWeekSchedule(entries, entityName);

        if (text.Length > 4000)
        {
            var grouped = entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key);
            foreach (var group in grouped)
            {
                var dayText = MessageFormatter.FormatDaySchedule(
                    group.ToList(),
                    group.Key,
                    entityName
                );
                await _max.SendMessageAsync(chatId, dayText, ct: ct);
                await Task.Delay(500, ct);
            }
        }
        else
        {
            await _max.SendMessageAsync(chatId, text, ct: ct);
        }
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
            + $"Уведомления: {notifyStatus} ({notifyDays})";

        var buttons = new List<List<MaxButton>>
        {
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "🔄 Сменить роль",
                    Payload = "role:student",
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
        var dayButtons = settings
            .NotifyDays.OrderBy(d => d)
            .Select(d => new MaxButton
            {
                Type = "callback",
                Text = $"{MessageFormatter.GetShortDayLabel(d)} ✓",
                Payload = $"notifyday:{d}",
            })
            .ToList();

        var buttons = new List<List<MaxButton>>
        {
            dayButtons,
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "💾 Сохранить",
                    Payload = "notifysave",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(
            chatId,
            $"🔔 Уведомления {status}\n\nВыбери дни:",
            buttons,
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

        var dayButtons = settings
            .NotifyDays.OrderBy(d => d)
            .Select(d => new MaxButton
            {
                Type = "callback",
                Text = $"{MessageFormatter.GetShortDayLabel(d)} ✓",
                Payload = $"notifyday:{d}",
            })
            .ToList();

        var buttons = new List<List<MaxButton>>
        {
            dayButtons,
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "💾 Сохранить",
                    Payload = "notifysave",
                },
            },
        };

        await _max.SendInlineKeyboardAsync(chatId, "🔔 Выбери дни уведомлений:", buttons, ct: ct);
    }

    private record InteractionState(string Step, Dictionary<string, string> Data);
}
