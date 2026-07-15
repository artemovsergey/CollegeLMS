using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AgentBridge.Models;
using AgentBridge.OpenCode;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AgentBridge.Bot;

public class MessageRouter
{
    private readonly OpenCodeClient _openCode;
    private readonly AgentTaskQueue _queue;
    private readonly ILogger<MessageRouter> _logger;
    private readonly ConcurrentDictionary<string, AgentTask> _activeTasks = new();
    private readonly ConcurrentDictionary<string, PendingPermission> _pendingPermissions = new();
    private readonly ConcurrentDictionary<long, string> _chatActiveSession = new();
    private readonly ConcurrentDictionary<long, string> _chatModel = new();
    private readonly string _defaultModel;

    public MessageRouter(
        OpenCodeClient openCode,
        AgentTaskQueue queue,
        IConfiguration config,
        ILogger<MessageRouter> logger
    )
    {
        _openCode = openCode;
        _queue = queue;
        _logger = logger;
        _defaultModel = config["OpenCode:DefaultModel"] ?? "opencode/deepseek-v4-flash-free";
    }

    public async Task<string> HandleCommandAsync(
        string command,
        long chatId,
        string messenger,
        string? model = null
    )
    {
        var lower = command.ToLower().Trim();

        if (lower == "/cancel" || lower == "❌ отмена")
            return await CancelAsync(chatId);

        if (lower == "/start" || lower == "/menu")
            return "🤖 *AgentBridge*\n\nНапишите задачу — агент OpenCode выполнит.\n\nИспользуйте кнопки меню ниже или команды:";

        if (lower == "/new" || lower == "📋 новая сессия")
        {
            CleanupChat(chatId, _chatActiveSession.TryGetValue(chatId, out var sid) ? sid : null);
            return "✅ Новая сессия создана. Предыдущий контекст сброшен.";
        }

        if (lower == "/help" || lower == "💡 помощь")
            return "🤖 *AgentBridge — помощь*\n\n"
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

        // Check if there's a task waiting for user reply in this chat
        if (
            _chatActiveSession.TryGetValue(chatId, out var existingSessionId)
            && _activeTasks.TryGetValue(existingSessionId, out var existingTask)
            && existingTask.Status == AgentTaskStatus.WaitingForReply
        )
        {
            return await SendFollowUpAsync(existingSessionId, existingTask, command, model);
        }

        return await CreateNewTaskAsync(command, chatId, messenger, model);
    }

    private async Task<string> CreateNewTaskAsync(
        string prompt,
        long chatId,
        string messenger,
        string? model
    )
    {
        model ??= _chatModel.TryGetValue(chatId, out var cm) ? cm : _defaultModel;
        var task = _queue.Enqueue(prompt, chatId, messenger);
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

    private async Task<string> SendFollowUpAsync(
        string sessionId,
        AgentTask task,
        string message,
        string? model
    )
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

    public async Task HandleSseEventAsync(SseEvent evt, ITelegramBotClient telegramBot)
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
                await HandlePartUpdatedAsync(props, sessionId, telegramBot);
                break;

            case "session.status":
                await HandleSessionStatusAsync(props, sessionId, telegramBot);
                break;

            case "permission.request":
                await HandlePermissionRequestAsync(props, sessionId, telegramBot);
                break;
        }
    }

    private async Task HandlePartUpdatedAsync(
        System.Text.Json.JsonElement props,
        string sessionId,
        ITelegramBotClient telegramBot
    )
    {
        if (!props.TryGetProperty("part", out var part))
            return;
        var partType = part.TryGetProperty("type", out var pt) ? pt.GetString() : null;
        if (partType != "step-finish")
            return;
        var reason = part.TryGetProperty("reason", out var r) ? r.GetString() : null;
        if (reason != "stop")
            return;

        await HandleStepFinishAsync(sessionId, telegramBot);
    }

    private async Task HandleStepFinishAsync(string sessionId, ITelegramBotClient telegramBot)
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

            // If there's a follow-up pending user reply, don't send anything yet
            if (task.Status == AgentTaskStatus.WaitingForReply)
                return;

            // If assistant asked a question → wait for user reply
            if (text is not null && ContainsQuestion(text))
            {
                task.Status = AgentTaskStatus.WaitingForReply;
                task.Result = text;
                await SendLongMessageAsync(telegramBot, task.ChatId, text);
                return;
            }

            // Normal completion
            if (!_activeTasks.TryRemove(sessionId, out _))
                return;
            task.Status = AgentTaskStatus.Completed;
            CleanupChat(task.ChatId, sessionId);

            await SendLongMessageAsync(
                telegramBot,
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
            await telegramBot.SendMessage(
                task.ChatId,
                "❌ Не удалось получить результат.",
                parseMode: ParseMode.None
            );
        }
    }

    private async Task HandleSessionStatusAsync(
        System.Text.Json.JsonElement props,
        string sessionId,
        ITelegramBotClient telegramBot
    )
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
            await telegramBot.SendMessage(
                task.ChatId,
                $"❌ Ошибка агента: {message}",
                parseMode: ParseMode.None
            );
        }
    }

    private async Task HandlePermissionRequestAsync(
        System.Text.Json.JsonElement props,
        string sessionId,
        ITelegramBotClient telegramBot
    )
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
        await telegramBot.SendMessage(
            task.ChatId,
            msg,
            replyMarkup: keyboard,
            parseMode: ParseMode.None
        );
    }

    public async Task HandlePermissionResponseAsync(string permissionId, bool allow)
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

    private static bool ContainsQuestion(string text)
    {
        return text.Contains('?');
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

    public async Task SendLongMessageAsync(ITelegramBotClient bot, long chatId, string text, ReplyKeyboardMarkup? keyboard = null)
    {
        var html = MarkdownToHtml(text);
        const int maxLen = 4096;
        if (html.Length <= maxLen)
        {
            await bot.SendMessage(chatId, html, replyMarkup: keyboard, parseMode: ParseMode.Html);
            return;
        }

        for (var i = 0; i < html.Length; i += maxLen)
        {
            var chunk = html.Substring(i, Math.Min(maxLen, html.Length - i));
            var markup = i == 0 ? keyboard : null;
            await bot.SendMessage(chatId, chunk, replyMarkup: markup, parseMode: ParseMode.Html);
        }
    }

    private static string MarkdownToHtml(string text)
        var processed = ToTelegramMarkdown(text);
        const int maxLen = 4096;
        if (processed.Length <= maxLen)
        {
            await bot.SendMessage(chatId, processed, parseMode: ParseMode.Markdown);
            return;
        }

        for (var i = 0; i < processed.Length; i += maxLen)
        {
            var chunk = processed.Substring(i, Math.Min(maxLen, processed.Length - i));
            await bot.SendMessage(chatId, chunk, parseMode: ParseMode.Markdown);
        }
    }

    /// <summary>
    /// Конвертирует стандартный markdown в диалект Telegram.
    /// Telegram: *bold* (одна *), _italic_ (одно _), нет таблиц/заголовков.
    /// </summary>
    private static string ToTelegramMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var blocks = new List<string>();

        // 1. Extract fenced code blocks → placeholder
        text = Regex.Replace(text, @"```(\w*)\n([\s\S]*?)```", m =>
        {
            var encoded = WebUtility.HtmlEncode(m.Groups[2].Value);
            blocks.Add($"<pre>{encoded}</pre>");
            return $"\x00CB{blocks.Count - 1}\x00";
        });

        // 2. Extract inline code → placeholder
        text = Regex.Replace(text, @"`([^`]+)`", m =>
        {
            var encoded = WebUtility.HtmlEncode(m.Groups[1].Value);
            blocks.Add($"<code>{encoded}</code>");
            return $"\x00CB{blocks.Count - 1}\x00";
        });

        // 3. HTML-escape everything else (so <id> → &lt;id&gt; etc.)
        text = WebUtility.HtmlEncode(text);

        // 4. Restore code blocks
        for (var i = 0; i < blocks.Count; i++)
            text = text.Replace($"\x00CB{i}\x00", blocks[i]);

        // 5. Bold **text**
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");

        // 6. Italic *text* (but not ** which is bold)
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<i>$1</i>");

        // 7. Strikethrough ~~text~~
        text = Regex.Replace(text, @"~~(.+?)~~", "<s>$1</s>");

        // 8. Links [text](url)
        text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)", "<a href=\"$2\">$1</a>");

        // 9. Images ![alt](url) → keep alt
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");

        // 10. Headings # → bold
        text = Regex.Replace(text, @"^#{1,6}\s+(.+)$", "<b>$1</b>", RegexOptions.Multiline);

        // 11. Horizontal rules
        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "\n—\n", RegexOptions.Multiline);
        text = Regex.Replace(text, @"Ë\d+", "");

        var blocks = new Dictionary<string, string>();
        var idx = 0;

        string Save(string value)
        {
            var key = $"\a{idx++}\a";
            blocks[key] = value;
            return key;
        }

        // 1. Save fenced code blocks
        text = Regex.Replace(text, @"```(\w*)\s*\n([\s\S]*?)```", m =>
            Save(m.Value));

        // 2. Save inline code
        text = Regex.Replace(text, @"`([^`\n]+)`", m =>
            Save(m.Value));

        // 3. Convert tables → formatted text
        text = Regex.Replace(text, @"((?:^\|.+\|\s*$\n?)+)", m =>
        {
            var block = m.Value;
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.All(l => l.Trim().StartsWith('|') && l.Trim().EndsWith('|')))
                return Save(RenderTable(block));
            return m.Value;
        }, RegexOptions.Multiline);

        // 4. Convert **bold** → *bold*
        var boldMap = new Dictionary<string, string>();
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", m =>
        {
            var key = $"\a{idx++}\a";
            boldMap[key] = "*" + m.Groups[1].Value + "*";
            return key;
        });

        // 5. Convert *italic* → _italic_ (only single asterisks remain)
        text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "_$1_");

        // 6. Restore bold
        foreach (var kv in boldMap)
            text = text.Replace(kv.Key, kv.Value);

        // 7. Convert # headers → *header* 
        text = Regex.Replace(text, @"^(#{1,6})\s+(.+)$", "*$2*", RegexOptions.Multiline);

        // 8. Remove horizontal rules
        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

        // 9. Restore saved blocks (code, inline code, tables)
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
