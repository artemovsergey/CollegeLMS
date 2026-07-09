using System.Collections.Concurrent;
using AgentBridge.Models;
using AgentBridge.OpenCode;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
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
    private readonly string _defaultModel;

    public MessageRouter(
        OpenCodeClient openCode,
        AgentTaskQueue queue,
        IConfiguration config,
        ILogger<MessageRouter> logger)
    {
        _openCode = openCode;
        _queue = queue;
        _logger = logger;
        _defaultModel = config["OpenCode:_defaultModel"] ?? "opencode/deepseek-v4-flash-free";
    }

    public async Task<string> HandleCommandAsync(string command, long chatId, string messenger, string? model = null)
    {
        var lower = command.ToLower();

        if (lower == "/cancel")
            return await CancelAsync(chatId);

        if (lower == "/start")
            return "Я бот AgentBridge.\n\nОтправьте задачу → агент OpenCode выполнит.\n\n/status — статус\n/cancel — отмена задачи";

        if (lower == "/status")
            return await GetStatusAsync();

        // Check if there's a task waiting for user reply in this chat
        if (_chatActiveSession.TryGetValue(chatId, out var existingSessionId) &&
            _activeTasks.TryGetValue(existingSessionId, out var existingTask) &&
            existingTask.Status == AgentTaskStatus.WaitingForReply)
        {
            return await SendFollowUpAsync(existingSessionId, existingTask, command, model);
        }

        return await CreateNewTaskAsync(command, chatId, messenger, model);
    }

    private async Task<string> CreateNewTaskAsync(string prompt, long chatId, string messenger, string? model)
    {
        model ??= _defaultModel;
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

    private async Task<string> SendFollowUpAsync(string sessionId, AgentTask task, string message, string? model)
    {
        model ??= _defaultModel;
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
        if (evt.Properties is not { } props) return;

        var sessionId = props.TryGetProperty("sessionID", out var sid) ? sid.GetString() : null;
        if (sessionId is null) return;
        if (!_activeTasks.ContainsKey(sessionId)) return;

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
        ITelegramBotClient telegramBot)
    {
        if (!props.TryGetProperty("part", out var part)) return;
        var partType = part.TryGetProperty("type", out var pt) ? pt.GetString() : null;
        if (partType != "step-finish") return;
        var reason = part.TryGetProperty("reason", out var r) ? r.GetString() : null;
        if (reason != "stop") return;

        await HandleStepFinishAsync(sessionId, telegramBot);
    }

    private async Task HandleStepFinishAsync(string sessionId, ITelegramBotClient telegramBot)
    {
        if (!_activeTasks.TryGetValue(sessionId, out var task)) return;

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
                : null;

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
            if (!_activeTasks.TryRemove(sessionId, out _)) return;
            task.Status = AgentTaskStatus.Completed;
            CleanupChat(task.ChatId, sessionId);

            await SendLongMessageAsync(telegramBot, task.ChatId, text ?? "✅ Агент завершил задачу.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completion result");
            if (!_activeTasks.TryRemove(sessionId, out _)) return;
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            CleanupChat(task.ChatId, sessionId);
            await telegramBot.SendMessage(task.ChatId, "❌ Не удалось получить результат.");
        }
    }

    private async Task HandleSessionStatusAsync(
        System.Text.Json.JsonElement props,
        string sessionId,
        ITelegramBotClient telegramBot)
    {
        if (!props.TryGetProperty("status", out var status)) return;
        var type = status.TryGetProperty("type", out var t) ? t.GetString() : null;

        if (type == "retry")
        {
            var message = status.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            if (!_activeTasks.TryRemove(sessionId, out var task)) return;
            task.Status = AgentTaskStatus.Failed;
            task.ErrorMessage = message;
            CleanupChat(task.ChatId, sessionId);
            await telegramBot.SendMessage(task.ChatId, $"❌ Ошибка агента: {message}");
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

        var msg = $"🔧 Запрос разрешения:\n\nИнструмент: {toolName}\n{description}";
        var keyboard = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("✅ Разрешить", $"allow:{permissionId}"),
                InlineKeyboardButton.WithCallbackData("❌ Отказать", $"deny:{permissionId}")
            ]
        ]);
        await telegramBot.SendMessage(task.ChatId, msg, replyMarkup: keyboard);
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
        var activeCount = _activeTasks.Count;
        var waitingReplies = _activeTasks.Values.Count(t => t.Status == AgentTaskStatus.WaitingForReply);
        var waitingPerms = _activeTasks.Values.Count(t => t.Status == AgentTaskStatus.WaitingPermission);

        return $"🤖 OpenCode: {(healthy ? "✅ онлайн" : "❌ офлайн")}\n"
            + $"Активных задач: {activeCount}\n"
            + $"Ожидают ответа: {waitingReplies}\n"
            + $"Ожидают разрешения: {waitingPerms}";
    }

    private async Task<string> CancelAsync(long chatId)
    {
        if (!_chatActiveSession.TryGetValue(chatId, out var sessionId) ||
            !_activeTasks.TryGetValue(sessionId, out var task))
        {
            return "Нет активных задач для отмены.";
        }

        if (task.Status is AgentTaskStatus.Completed or AgentTaskStatus.Failed or AgentTaskStatus.Cancelled)
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

    private static bool ContainsQuestion(string text)
    {
        return text.Contains('?');
    }

    private void CleanupChat(long chatId, string? sessionId)
    {
        if (sessionId is not null && _chatActiveSession.TryGetValue(chatId, out var active) && active == sessionId)
            _chatActiveSession.TryRemove(chatId, out _);
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
