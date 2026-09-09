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
    private const int ChangesPageSize = 20;

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
                    NotifyDays = [1, 2, 3, 4, 5],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        await ShowRoleSelectionAsync(chatId, ct);
    }

    private async Task ShowRoleSelectionAsync(long chatId, CancellationToken ct)
    {
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
            case "role":
                await HandleRoleSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "group":
                await HandleGroupSelectionAsync(chatId, userId, p.Param1!, ct);
                break;
            case "teacher":
                await HandleTeacherSelectionAsync(chatId, userId, p.Param1!, ct);
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
            case "notifysave":
                await _max.SendMessageAsync(chatId, "✅ Настройки уведомлений сохранены!", ct: ct);
                break;
            case "changes":
                await ShowMyChangesAsync(chatId, userId, 0, ct);
                break;
            case "changes_page":
                var changesPage =
                    p.Param1 is { } changesPageText
                    && int.TryParse(changesPageText, out var changesPageValue)
                        ? changesPageValue
                        : 0;
                await ShowMyChangesAsync(chatId, userId, changesPage, ct);
                break;
            default:
                _logger.LogWarning("Unknown callback action {Action}", p.Action);
                await ShowMainMenuAsync(chatId, userId, ct);
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

        MaxBotRoleFlow.ApplyRole(settings, role);
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

        MaxBotRoleFlow.SelectGroup(settings, Guid.Parse(groupId));
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ShowMainMenuAsync(chatId, userId, ct);
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

        MaxBotRoleFlow.SelectTeacher(settings, Guid.Parse(teacherId));
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

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
                    Type = "callback",
                    Text = "🔄 Мои изменения",
                    Payload = CallbackPayload.Changes(),
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
        };

        var text =
            $"🏠 *Главное меню*\n\n" + $"Роль: {roleLabel}\n" + $"Группа/Преподаватель: {entity}";

        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
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
        var buttons = DayNavButtons(date, entityName);

        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            await _max.SendInlineKeyboardAsync(
                chatId,
                MessageFormatter.FormatDaySchedule([], date, entityName),
                buttons,
                ct: ct
            );
            return;
        }

        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            week: StudyWeek.ForDate(date),
            dayOfWeek: MessageFormatter.ToApiDay(date.DayOfWeek),
            ct: ct
        );

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            date,
            entityName,
            showGroup: settings.Role == "teacher"
        );
        await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
    }

    private static List<List<MaxButton>> DayNavButtons(DateTime date, string entityName)
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
        var entries = await _api.GetScheduleAsync(
            groupId: settings.GroupId,
            teacherId: settings.TeacherId,
            period: "week",
            week: StudyWeek.ForDate(weekStart),
            ct: ct
        );

        var text = MessageFormatter.FormatWeekSchedule(
            entries,
            weekStart,
            entityName,
            showGroup: settings.Role == "teacher"
        );
        var buttons = WeekNavButtons(weekStart);

        if (text.Length > 4000)
        {
            var header =
                $"📅 *Неделя {MessageFormatter.FormatShortDate(weekStart)}–{MessageFormatter.FormatShortDate(weekStart.AddDays(6))}*";
            await _max.SendInlineKeyboardAsync(chatId, header, buttons, ct: ct);

            foreach (var group in entries.GroupBy(x => x.DayOfWeek).OrderBy(x => x.Key))
            {
                var date = MessageFormatter.DateForWeekDay(weekStart, group.Key);
                var dayText = MessageFormatter.FormatDaySchedule(
                    group.ToList(),
                    date,
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

    private static List<List<MaxButton>> WeekNavButtons(DateTime weekStart)
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
            await _max.SendMessageAsync(
                chatId,
                "⚠️ Сначала выбери группу или преподавателя в настройках.",
                ct: ct
            );
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

        var text = MessageFormatter.FormatMyChanges(items, page, ChangesPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / ChangesPageSize));

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
            + $"Уведомления: {notifyStatus} ({notifyDays})";

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
}
