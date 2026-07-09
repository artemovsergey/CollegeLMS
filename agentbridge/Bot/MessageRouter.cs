using System.Collections.Concurrent;
using AgentBridge.Models;
using AgentBridge.OpenCode;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace AgentBridge.Bot;

public class MessageRouter
{
    private readonly OpenCodeClient _openCode;
    private readonly AgentTaskQueue _queue;
    private readonly VkMaxBotHost _vkBot;
    private readonly ILogger<MessageRouter> _logger;
    private readonly ConcurrentDictionary<string, AgentTask> _activeTasks = new();
    private readonly ConcurrentDictionary<string, PendingPermission> _pendingPermissions = new();

    public MessageRouter(
        OpenCodeClient openCode,
        AgentTaskQueue queue,
        VkMaxBotHost vkBot,
        ILogger<MessageRouter> logger)
    {
        _openCode = openCode;
        _queue = queue;
        _vkBot = vkBot;
        _logger = logger;
    }

    public async Task<string> HandleCommandAsync(string command, long chatId, string messenger, string? model = null)
    {
        return command.ToLower() switch
        {
            "/start" => "Я бот AgentBridge.\n\nОтправьте задачу → агент OpenCode выполнит.\n\n/status — статус\n/cancel — отмена задачи",
            "/status" => await GetStatusAsync(),
            "/cancel" => await CancelAsync(chatId),
            _ => await ProcessPromptAsync(command, chatId, messenger, model)
        };
    }

    private async Task<string> ProcessPromptAsync(string prompt, long chatId, string messenger, string? model)
    {
        var task = _queue.Enqueue(prompt, chatId, messenger);
        task.Status = AgentTaskStatus.Running;

        try
        {
            var session = await _openCode.CreateSessionAsync(title: prompt[..Math.Min(50, prompt.Length)]);
            if (session is null)
                throw new InvalidOperationException("Failed to create OpenCode session");

            task.OpenCodeSessionId = session.Id;
            _activeTasks[session.Id] = task;

            await _openCode.SendPromptFireAndForgetAsync(session.Id, prompt, model);

            return "Задача принята. Агент выполняет...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start prompt");
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            return $"Ошибка: {ex.Message}";
        }
    }

    public async Task HandleSseEventAsync(SseEvent evt, ITelegramBotClient telegramBot)
    {
        if (evt.Properties is not { } props) return;

        var sessionId = props.TryGetProperty("sessionID", out var sid) ? sid.GetString() : null;
        if (sessionId is null) return;

        switch (evt.Type)
        {
            case "permission.request":
                await HandlePermissionRequestAsync(props, sessionId, telegramBot);
                break;

            case "session.completed":
            case "message.completed":
                await HandleCompletionAsync(sessionId, telegramBot);
                break;

            case "session.failed":
            case "message.failed":
                await HandleFailureAsync(sessionId, props, telegramBot);
                break;
        }
    }

    private async Task HandlePermissionRequestAsync(
        System.Text.Json.JsonElement props,
        string sessionId,
        ITelegramBotClient telegramBot)
    {
        var permissionId = props.TryGetProperty("permissionID", out var pid) ? pid.GetString() : null;
        var toolName = props.TryGetProperty("tool", out var tool) ? tool.GetString() ?? "unknown" : "unknown";
        var description = props.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

        if (permissionId is null) return;
        if (!_activeTasks.TryGetValue(sessionId, out var task)) return;

        var pending = new PendingPermission
        {
            PermissionId = permissionId,
            SessionId = sessionId,
            ToolName = toolName,
            Description = description
        };
        task.CurrentPermission = pending;
        task.Status = AgentTaskStatus.WaitingPermission;
        _pendingPermissions[permissionId] = pending;

        var msg = $"Запрос разрешения:\n\nИнструмент: {toolName}\n{description}";

        if (task.Messenger == "vkmax")
        {
            await _vkBot.SendKeyboardMessageAsync(task.ChatId, msg, permissionId);
        }
        else
        {
            var keyboard = new InlineKeyboardMarkup([
                [
                    InlineKeyboardButton.WithCallbackData("✅ Разрешить", $"allow:{permissionId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отказать", $"deny:{permissionId}")
                ]
            ]);
            await telegramBot.SendMessage(task.ChatId, msg, replyMarkup: keyboard);
        }
    }

    private async Task HandleCompletionAsync(string sessionId, ITelegramBotClient telegramBot)
    {
        if (!_activeTasks.TryRemove(sessionId, out var task)) return;

        task.Status = AgentTaskStatus.Completed;

        try
        {
            var messages = await _openCode.GetMessagesAsync(sessionId);
            var lastAssistant = messages?
                .Where(m => m.Info.Role == "assistant")
                .LastOrDefault();

            var text = lastAssistant is not null
                ? string.Join("\n", lastAssistant.Parts
                    .Where(p => p.Type == "text" && p.Text is not null)
                    .Select(p => p.Text))
                : "Агент завершил задачу.";

            task.Result = text;

            if (task.Messenger == "vkmax")
            {
                await _vkBot.SendMessageAsync(task.ChatId, text ?? "Готово.");
            }
            else
            {
                await SendLongMessageAsync(telegramBot, task.ChatId, text ?? "Готово.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completion result");
            var errorMsg = "Агент завершил задачу. (не удалось получить результат)";
            if (task.Messenger == "vkmax")
                await _vkBot.SendMessageAsync(task.ChatId, errorMsg);
            else
                await telegramBot.SendMessage(task.ChatId, errorMsg);
        }
    }

    private async Task HandleFailureAsync(
        string sessionId,
        System.Text.Json.JsonElement props,
        ITelegramBotClient telegramBot)
    {
        if (!_activeTasks.TryRemove(sessionId, out var task)) return;

        task.Status = AgentTaskStatus.Failed;
        var error = props.TryGetProperty("error", out var err) ? err.GetString() ?? "Unknown" : "Unknown";
        task.ErrorMessage = error;

        if (task.Messenger == "vkmax")
            await _vkBot.SendMessageAsync(task.ChatId, $"Ошибка агента: {error}");
        else
            await telegramBot.SendMessage(task.ChatId, $"Ошибка агента: {error}");
    }

    public async Task HandlePermissionResponseAsync(string permissionId, bool allow)
    {
        if (!_pendingPermissions.TryRemove(permissionId, out var pending)) return;

        await _openCode.RespondToPermissionAsync(
            pending.SessionId, permissionId, allow);

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
        var activeCount = _queue.ActiveCount;
        return $"OpenCode: {(healthy ? "онлайн" : "офлайн")}\nАктивных задач: {activeCount}";
    }

    private async Task<string> CancelAsync(long chatId)
    {
        return "Для отмены задачи отправьте /cancel. Агент завершит текущую задачу.";
    }

    public async Task SendLongMessageAsync(ITelegramBotClient bot, long chatId, string text)
    {
        const int maxLen = 4096;
        if (text.Length <= maxLen)
        {
            await bot.SendMessage(chatId, text);
            return;
        }

        for (var i = 0; i < text.Length; i += maxLen)
        {
            var chunk = text.Substring(i, Math.Min(maxLen, text.Length - i));
            await bot.SendMessage(chatId, chunk);
        }
    }
}
