using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CollegeLMS.TelegramBot.Models;
using CollegeLMS.TelegramBot.OpenCode;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CollegeLMS.TelegramBot.Bot;

public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly OpenCodeClient _openCode;
    private readonly AgentTaskQueue _queue;
    private readonly OpenCodeSseListener _sse;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly HashSet<long> _allowedUsers;
    private readonly ConcurrentDictionary<string, AgentTask> _activeTasks = new();
    private readonly ConcurrentDictionary<string, PendingPermission> _pendingPermissions = new();
    private readonly ConcurrentDictionary<long, string> _chatActiveSession = new();
    private readonly ConcurrentDictionary<long, string> _chatModel = new();
    private readonly string _defaultModel;

    private static readonly BotCommand[] Commands =
    [
        new() { Command = "new", Description = "Новая сессия" },
        new() { Command = "status", Description = "Статус OpenCode" },
        new() { Command = "models", Description = "Список моделей" },
        new() { Command = "model", Description = "Сменить модель" },
        new() { Command = "cancel", Description = "Отменить задачу" },
        new() { Command = "files", Description = "Просмотр файлов" },
        new() { Command = "read", Description = "Чтение файла" },
        new() { Command = "search", Description = "Поиск по файлам" },
        new() { Command = "undo", Description = "Откатить изменения" },
        new() { Command = "menu", Description = "Показать меню" },
        new() { Command = "help", Description = "Помощь" },
    ];

    private static readonly ReplyKeyboardMarkup MenuKeyboard = new([
        [new KeyboardButton("📋 Новая сессия"), new KeyboardButton("📁 Файлы")],
        [new KeyboardButton("🤖 Статус"), new KeyboardButton("⚙️ Модель")],
        [new KeyboardButton("🔍 Поиск"), new KeyboardButton("💡 Помощь")],
        [new KeyboardButton("❌ Отмена")],
    ])
    {
        ResizeKeyboard = true,
        InputFieldPlaceholder = "Напишите задачу агенту...",
    };

    public TelegramBotService(
        ITelegramBotClient bot,
        OpenCodeClient openCode,
        AgentTaskQueue queue,
        OpenCodeSseListener sse,
        IConfiguration config,
        ILogger<TelegramBotService> logger
    )
    {
        _bot = bot;
        _openCode = openCode;
        _queue = queue;
        _sse = sse;
        _logger = logger;
        _defaultModel = config["OpenCode:DefaultModel"] ?? "opencode/deepseek-v4-flash-free";
        var allowed = config.GetSection("Telegram:AllowedUserIds").Get<List<long>>() ?? [];
        _allowedUsers = new HashSet<long>(allowed);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram bot starting...");

        _sse.OnEvent += async evt => await HandleSseEventAsync(evt);

        var me = await _bot.GetMe(stoppingToken);

        await _bot.SetMyCommands(Commands, cancellationToken: stoppingToken);
        _logger.LogInformation("Bot commands registered");

        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery] },
            stoppingToken
        );

        _logger.LogInformation("Telegram bot started as @{Username}", me.Username);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct
    )
    {
        try
        {
            if (update.CallbackQuery is { Data: { } callbackData } cbQuery)
            {
                await HandleCallbackQueryAsync(bot, cbQuery, ct);
                return;
            }

            if (update.Message is not { Text: { } text } message)
                return;

            var chatId = message.Chat.Id;
            var userId = message.From?.Id ?? 0;

            if (_allowedUsers.Count > 0 && !_allowedUsers.Contains(userId))
            {
                _logger.LogWarning("Unauthorized user {UserId} in chat {ChatId}", userId, chatId);
                return;
            }

            _logger.LogInformation(
                "Message from {UserId} in chat {ChatId}: {Text}",
                userId,
                chatId,
                text
            );

            _ = bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

            var response = await HandleCommandAsync(text, chatId);

            if (text == "/start" || text == "/menu")
            {
                await SendLongMessageAsync(bot, chatId, response, MenuKeyboard);
            }
            else
            {
                await SendLongMessageAsync(bot, chatId, response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
            if (update.Message?.Chat.Id is { } chatId)
            {
                await bot.SendMessage(
                    chatId,
                    $"❌ Внутренняя ошибка: {ex.Message}",
                    parseMode: ParseMode.None
                );
            }
        }
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient bot,
        CallbackQuery cbQuery,
        CancellationToken ct
    )
    {
        var data = cbQuery.Data;
        if (string.IsNullOrEmpty(data))
            return;

        try
        {
            if (data.StartsWith("allow:") || data.StartsWith("deny:"))
            {
                var parts = data.Split(':');
                var action = parts[0];
                var permissionId = parts[1];
                var allow = action == "allow";
                await HandlePermissionResponseAsync(permissionId, allow);
                await bot.AnswerCallbackQuery(cbQuery.Id,
                    text: allow ? "✅ Разрешено" : "❌ Отказано",
                    cancellationToken: ct);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback query");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Telegram bot error");
        return Task.CompletedTask;
    }

    public async Task<string> HandleCommandAsync(string command, long chatId, string? model = null)
    {
        var lower = command.ToLower().Trim();

        if (lower == "/cancel" || lower == "❌ отмена")
            return await CancelAsync(chatId);

        if (lower == "/start" || lower == "/menu")
            return "🤖 *CollegeLMS Telegram Bot*\n\nНапишите задачу — агент OpenCode выполнит.\n\nИспользуйте кнопки меню ниже или команды:";

        if (lower == "/new" || lower == "📋 новая сессия")
        {
            CleanupChat(chatId, _chatActiveSession.TryGetValue(chatId, out var sid) ? sid : null);
            return "✅ Новая сессия создана. Предыдущий контекст сброшен.";
        }

        if (lower == "/help" || lower == "💡 помощь")
            return "🤖 *CollegeLMS Telegram Bot — помощь*\n\n"
                + "Просто напишите задачу — агент выполнит.\n\n"
                + "*/new* — новая сессия (сброс контекста)\n"
                + "*/status* — статус OpenCode\n"
                + "*/models* — доступные модели\n"
                + "*/model <id>* — сменить модель\n"
                + "*/files* — файлы проекта\n"
                + "*/read <путь>* — чтение файла\n"
                + "*/search <текст>* — поиск по файлам\n"
                + "*/cancel* — отменить задачу\n"
                + "*/menu* — показать меню\n"
                + "*/help* — эта справка";

        if (lower == "/status" || lower == "🤖 статус")
            return await GetStatusAsync();

        if (lower == "/models")
            return await ListModelsAsync();

        if (lower == "/model" || lower.StartsWith("/model ") || lower == "⚙️ модель")
        {
            var parts = command.Split(' ', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]) || lower == "⚙️ модель")
            {
                var currentModel = _chatModel.TryGetValue(chatId, out var cm) ? cm : _defaultModel;
                return $"🤖 Текущая модель: `{currentModel}`\n\nСписок: `/models`\n\nСменить: `/model github-models/openai/gpt-4o-mini`";
            }
            _chatModel[chatId] = parts[1];
            return $"✅ Модель сменена на: `{parts[1]}`";
        }

        if (lower == "/files" || lower == "📁 файлы")
            return await ListFilesAsync();

        if (lower.StartsWith("/read "))
            return await ReadFileAsync(command["/read ".Length..].Trim());

        if (lower == "/search" || lower == "🔍 поиск")
            return "🔍 Отправьте запрос для поиска:\n\n`/search <текст>`";

        if (lower.StartsWith("/search "))
            return await SearchFilesAsync(command["/search ".Length..].Trim());

        if (lower == "/undo")
            return "⏪ `/undo` пока не поддерживается в Telegram боте.";

        if (
            _chatActiveSession.TryGetValue(chatId, out var existingSessionId)
            && _activeTasks.TryGetValue(existingSessionId, out var existingTask)
            && existingTask.Status == AgentTaskStatus.WaitingForReply
        )
        {
            return await SendFollowUpAsync(existingSessionId, existingTask, command, model);
        }

        return await CreateNewTaskAsync(command, chatId, model);
    }

    private async Task<string> CreateNewTaskAsync(string prompt, long chatId, string? model)
    {
        model ??= _chatModel.TryGetValue(chatId, out var cm) ? cm : _defaultModel;
        var task = _queue.Enqueue(prompt, chatId, "telegram");
        task.Status = AgentTaskStatus.Running;

        try
        {
            var session = await _openCode.CreateSessionAsync();
            if (session is null)
                throw new InvalidOperationException("Failed to create OpenCode session");

            task.OpenCodeSessionId = session.Id;
            _activeTasks[session.Id] = task;
            _chatActiveSession[chatId] = session.Id;

            await _openCode.SendPromptFireAndForgetAsync(session.Id, prompt, model);

            return "✅ Задача принята. Агент выполняет...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start prompt");
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            CleanupChat(chatId, task.OpenCodeSessionId);
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task<string> SendFollowUpAsync(string sessionId, AgentTask task, string message, string? model)
    {
        model ??= _chatModel.TryGetValue(task.ChatId, out var cm) ? cm : _defaultModel;
        task.Status = AgentTaskStatus.Running;

        try
        {
            await _openCode.SendPromptFireAndForgetAsync(sessionId, message, model);
            return "✅ Ответ отправлен агенту. Ожидайте...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send follow-up");
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            CleanupChat(task.ChatId, sessionId);
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    public async Task HandleSseEventAsync(SseEvent evt)
    {
        if (evt.Properties is not { } props)
            return;

        var sessionId = props.TryGetProperty("sessionID", out var sid) ? sid.GetString() : null;
        if (sessionId is null)
            return;
        if (!_activeTasks.ContainsKey(sessionId))
            return;

        switch (evt.Type)
        {
            case "message.part.updated":
                await HandlePartUpdatedAsync(props, sessionId);
                break;

            case "session.status":
                await HandleSessionStatusAsync(props, sessionId);
                break;

            case "permission.request":
                await HandlePermissionRequestAsync(props, sessionId);
                break;
        }
    }

    private async Task HandlePartUpdatedAsync(System.Text.Json.JsonElement props, string sessionId)
    {
        if (!props.TryGetProperty("part", out var part))
            return;
        var partType = part.TryGetProperty("type", out var pt) ? pt.GetString() : null;
        if (partType != "step-finish")
            return;
        var reason = part.TryGetProperty("reason", out var r) ? r.GetString() : null;
        if (reason != "stop")
            return;

        await HandleStepFinishAsync(sessionId);
    }

    private async Task HandleStepFinishAsync(string sessionId)
    {
        if (!_activeTasks.TryGetValue(sessionId, out var task))
            return;

        try
        {
            var messages = await _openCode.GetMessagesAsync(sessionId);
            var lastAssistant = messages?.Where(m => m.Info.Role == "assistant").LastOrDefault();

            var text = lastAssistant is not null
                ? string.Join(
                    "\n",
                    lastAssistant
                        .Parts.Where(p => p.Type == "text" && p.Text is not null)
                        .Select(p => p.Text)
                )
                : null;

            if (text is not null && text.Contains('Ë'))
                _logger.LogWarning(
                    "OpenCode response contains unresolved references Ë+N for session {Session}: {Text}",
                    sessionId,
                    text
                );

            if (task.Status == AgentTaskStatus.WaitingForReply)
                return;

            if (text is not null && text.Contains('?'))
            {
                task.Status = AgentTaskStatus.WaitingForReply;
                task.Result = text;
                await SendLongMessageAsync(_bot, task.ChatId, text);
                return;
            }

            if (!_activeTasks.TryRemove(sessionId, out _))
                return;
            task.Status = AgentTaskStatus.Completed;
            CleanupChat(task.ChatId, sessionId);

            await SendLongMessageAsync(
                _bot,
                task.ChatId,
                text ?? "✅ Агент завершил задачу."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completion result");
            if (!_activeTasks.TryRemove(sessionId, out _))
                return;
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            CleanupChat(task.ChatId, sessionId);
            await _bot.SendMessage(
                task.ChatId,
                "❌ Не удалось получить результат.",
                parseMode: ParseMode.None
            );
        }
    }

    private async Task HandleSessionStatusAsync(System.Text.Json.JsonElement props, string sessionId)
    {
        if (!props.TryGetProperty("status", out var status))
            return;
        var type = status.TryGetProperty("type", out var t) ? t.GetString() : null;

        if (type == "retry")
        {
            var message = status.TryGetProperty("message", out var m)
                ? m.GetString()
                : "Unknown error";
            if (!_activeTasks.TryRemove(sessionId, out var task))
                return;
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = message;
            CleanupChat(task.ChatId, sessionId);
            await _bot.SendMessage(
                task.ChatId,
                $"❌ Ошибка агента: {message}",
                parseMode: ParseMode.None
            );
        }
    }

    private async Task HandlePermissionRequestAsync(System.Text.Json.JsonElement props, string sessionId)
    {
        var permissionId = props.TryGetProperty("permissionID", out var pid)
            ? pid.GetString()
            : null;
        var toolName = props.TryGetProperty("tool", out var tool)
            ? tool.GetString() ?? "unknown"
            : "unknown";
        var description = props.TryGetProperty("description", out var desc)
            ? desc.GetString() ?? ""
            : "";

        if (permissionId is null)
            return;
        if (!_activeTasks.TryGetValue(sessionId, out var task))
            return;

        var pending = new PendingPermission
        {
            PermissionId = permissionId,
            SessionId = sessionId,
            ToolName = toolName,
            Description = description,
        };
        task.CurrentPermission = pending;
        task.Status = AgentTaskStatus.WaitingPermission;
        _pendingPermissions[permissionId] = pending;

        var msg = $"🔧 Запрос разрешения:\n\nИнструмент: {toolName}\n{description}";
        var keyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("✅ Разрешить", $"allow:{permissionId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отказать", $"deny:{permissionId}"),
            ],
        ]);
        await _bot.SendMessage(
            task.ChatId,
            msg,
            replyMarkup: keyboard,
            parseMode: ParseMode.None
        );
    }

    private async Task HandlePermissionResponseAsync(string permissionId, bool allow)
    {
        if (!_pendingPermissions.TryRemove(permissionId, out var pending))
            return;

        await _openCode.RespondToPermissionAsync(pending.SessionId, permissionId, allow);

        if (_activeTasks.TryGetValue(pending.SessionId, out var task))
        {
            task.CurrentPermission = null;
            task.Status = AgentTaskStatus.Running;
        }

        _logger.LogInformation("Permission {Id} resolved: {Allow}", permissionId, allow);
    }

    private async Task<string> GetStatusAsync()
    {
        var healthy = await _openCode.IsHealthyAsync();
        var activeCount = _activeTasks.Count;
        var waitingReplies = _activeTasks.Values.Count(t =>
            t.Status == AgentTaskStatus.WaitingForReply
        );
        var waitingPerms = _activeTasks.Values.Count(t =>
            t.Status == AgentTaskStatus.WaitingPermission
        );

        return $"🤖 OpenCode: {(healthy ? "✅ онлайн" : "❌ офлайн")}\n"
            + $"Активных задач: {activeCount}\n"
            + $"Ожидают ответа: {waitingReplies}\n"
            + $"Ожидают разрешения: {waitingPerms}";
    }

    private async Task<string> CancelAsync(long chatId)
    {
        if (
            !_chatActiveSession.TryGetValue(chatId, out var sessionId)
            || !_activeTasks.TryGetValue(sessionId, out var task)
        )
        {
            return "Нет активных задач для отмены.";
        }

        if (
            task.Status
            is AgentTaskStatus.Completed
                or AgentTaskStatus.Failed
                or AgentTaskStatus.Cancelled
        )
            return "Задача уже завершена.";

        try
        {
            await _openCode.AbortSessionAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to abort session {SessionId}", sessionId);
        }

        task.Status = AgentTaskStatus.Cancelled;
        if (_activeTasks.TryRemove(sessionId, out _))
            CleanupChat(chatId, sessionId);

        return "✅ Задача отменена.";
    }

    private async Task<string> ListModelsAsync()
    {
        try
        {
            var providers = await _openCode.GetProvidersAsync();
            if (providers?.Providers is null)
                return "❌ Не удалось получить список моделей.";

            var lines = new List<string> { "🤖 Доступные модели:\n" };
            foreach (var provider in providers.Providers)
            {
                var modelIds = provider.Models?.Keys.ToList();
                if (modelIds is null || modelIds.Count == 0)
                    continue;

                var connected = providers.Connected?.Contains(provider.Id) == true ? "✅" : "⛔";
                lines.Add($"{connected} {provider.Name} ({provider.Id})");

                foreach (var modelId in modelIds.Take(5))
                    lines.Add($"  • {provider.Id}/{modelId}");

                if (modelIds.Count > 5)
                    lines.Add($"  ... и ещё {modelIds.Count - 5}");

                lines.Add("");
            }

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list models");
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task<string> ListFilesAsync()
    {
        try
        {
            var files = await _openCode.ListFilesAsync();
            if (files is null || files.Count == 0)
                return "📁 Корень проекта. Напишите `/read <путь>` для чтения файла.";

            var lines = new List<string> { "📁 Файлы проекта:\n" };
            foreach (var file in files.Take(30))
                lines.Add($"{(file.IsDirectory ? "📁" : "📄")} {file.Path}");

            if (files.Count > 30)
                lines.Add($"\n... и ещё {files.Count - 30} файлов.");

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files");
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task<string> ReadFileAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Укажите путь: `/read <путь>`";

        try
        {
            var content = await _openCode.ReadFileAsync(path);
            if (content is null)
                return $"❌ Файл не найден: `{path}`";

            var lines = content.Split('\n');
            var maxLines = 100;
            var text = string.Join("\n", lines.Take(maxLines));

            if (lines.Length > maxLines)
                text += $"\n\n... и ещё {lines.Length - maxLines} строк.";

            return $"📄 `{path}`\n\n```\n{text}\n```";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file {Path}", path);
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    private async Task<string> SearchFilesAsync(string query)
    {
        if (string.IsNullOrEmpty(query))
            return "Укажите запрос: `/search <текст>`";

        try
        {
            var results = await _openCode.SearchFilesAsync(query);
            if (results is null || results.Count == 0)
                return $"🔍 Ничего не найдено по запросу: `{query}`";

            var lines = new List<string> { $"🔍 Результаты поиска: `{query}`\n" };
            foreach (var r in results.Take(20))
                lines.Add($"📄 `{r}`");

            if (results.Count > 20)
                lines.Add($"\n... и ещё {results.Count - 20} результатов.");

            return string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search files");
            return $"❌ Ошибка: {ex.Message}";
        }
    }

    private void CleanupChat(long chatId, string? sessionId)
    {
        if (
            sessionId is not null
            && _chatActiveSession.TryGetValue(chatId, out var active)
            && active == sessionId
        )
            _chatActiveSession.TryRemove(chatId, out _);
    }

    private async Task SendLongMessageAsync(ITelegramBotClient bot, long chatId, string text, ReplyKeyboardMarkup? keyboard = null)
    {
        var processed = ToTelegramMarkdown(text);
        const int maxLen = 4096;
        if (processed.Length <= maxLen)
        {
            await bot.SendMessage(chatId, processed, replyMarkup: keyboard, parseMode: ParseMode.Markdown);
            return;
        }

        for (var i = 0; i < processed.Length; i += maxLen)
        {
            var chunk = processed.Substring(i, Math.Min(maxLen, processed.Length - i));
            var markup = i == 0 ? keyboard : null;
            await bot.SendMessage(chatId, chunk, replyMarkup: markup, parseMode: ParseMode.Markdown);
        }
    }

    private static string ToTelegramMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = Regex.Replace(text, @"Ë\d+", "");

        var blocks = new Dictionary<string, string>();
        var idx = 0;

        string Save(string value)
        {
            var key = $"\a{idx++}\a";
            blocks[key] = value;
            return key;
        }

        text = Regex.Replace(text, @"```(\w*)\s*\n([\s\S]*?)```", m =>
            Save(m.Value));

        text = Regex.Replace(text, @"`([^`\n]+)`", m =>
            Save(m.Value));

        text = Regex.Replace(text, @"((?:^\|.+\|\s*$\n?)+)", m =>
        {
            var block = m.Value;
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.All(l => l.Trim().StartsWith('|') && l.Trim().EndsWith('|')))
                return Save(RenderTable(block));
            return m.Value;
        }, RegexOptions.Multiline);

        var boldMap = new Dictionary<string, string>();
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", m =>
        {
            var key = $"\a{idx++}\a";
            boldMap[key] = "*" + m.Groups[1].Value + "*";
            return key;
        });

        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "_$1_");

        foreach (var kv in boldMap)
            text = text.Replace(kv.Key, kv.Value);

        text = Regex.Replace(text, @"^(#{1,6})\s+(.+)$", "*$2*", RegexOptions.Multiline);

        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

        foreach (var kv in blocks)
            text = text.Replace(kv.Key, kv.Value);

        return text.Trim();
    }

    private static string RenderTable(string tableBlock)
    {
        var lines = tableBlock
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 2 && l.StartsWith('|') && l.EndsWith('|'))
            .ToList();

        var dataLines = lines
            .Where(l => !IsSeparatorRow(l))
            .ToList();

        if (dataLines.Count == 0)
            return "";

        var rows = dataLines
            .Select(l =>
            {
                var inner = l.TrimStart('|').TrimEnd('|');
                return inner.Split('|').Select(c => c.Trim()).ToArray();
            })
            .ToList();

        var colCount = rows.Max(r => r.Length);
        if (colCount == 0)
            return "";

        var widths = new int[colCount];
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        var sb = new StringBuilder();
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                if (c > 0) sb.Append(" │ ");
                if (c < row.Length - 1)
                    sb.Append(row[c].PadRight(widths[c]));
                else
                    sb.Append(row[c]);
            }
            sb.AppendLine();

            if (r == 0 && rows.Count > 1)
            {
                var totalWidth = widths.Sum(w => w) + (colCount - 1) * 3;
                sb.AppendLine(new string('─', totalWidth));
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static bool IsSeparatorRow(string line)
    {
        var inner = line.TrimStart('|').TrimEnd('|');
        var cells = inner.Split('|');
        return cells.Length > 0 && cells.All(c =>
        {
            var t = c.Trim();
            return t.Length > 0 && Regex.IsMatch(t, @"^[\s\-:]+$");
        });
    }
}
