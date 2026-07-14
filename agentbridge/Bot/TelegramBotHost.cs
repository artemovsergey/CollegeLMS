using AgentBridge.Models;
using AgentBridge.OpenCode;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AgentBridge.Bot;

public class TelegramBotHost : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly MessageRouter _router;
    private readonly AgentTaskQueue _queue;
    private readonly OpenCodeSseListener _sse;
    private readonly ILogger<TelegramBotHost> _logger;
    private readonly HashSet<long> _allowedUsers;

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

    public TelegramBotHost(
        ITelegramBotClient bot,
        MessageRouter router,
        AgentTaskQueue queue,
        OpenCodeSseListener sse,
        IConfiguration config,
        ILogger<TelegramBotHost> logger
    )
    {
        _bot = bot;
        _router = router;
        _queue = queue;
        _sse = sse;
        _logger = logger;
        var allowed = config.GetSection("Telegram:AllowedUserIds").Get<List<long>>() ?? [];
        _allowedUsers = new HashSet<long>(allowed);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram bot starting...");

        _sse.OnEvent += async evt => await _router.HandleSseEventAsync(evt, _bot);

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

            var response = await _router.HandleCommandAsync(text, chatId, "telegram");

            if (text == "/start" || text == "/menu")
            {
                await _router.SendLongMessageAsync(bot, chatId, response, MenuKeyboard);
            }
            else
            {
                await _router.SendLongMessageAsync(bot, chatId, response);
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
                await _router.HandlePermissionResponseAsync(permissionId, allow);
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
}
