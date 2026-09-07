using System.Text.Json;
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Tests;

public class MaxUpdateDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void DeserializesMessageCreated()
    {
        var json = """
            {
              "update_type": "message_created",
              "timestamp": 1725777000000,
              "message": {
                "sender": { "user_id": 42, "first_name": "Иван", "last_name": "Петров", "is_bot": false },
                "recipient": { "chat_type": "private", "chat_id": 777, "user_id": 42 },
                "body": { "mid": "abc-123", "seq": 1, "text": "/schedule" }
              }
            }
            """;

        var update = JsonSerializer.Deserialize<MaxUpdate>(json, JsonOpts);

        Assert.NotNull(update);
        update.UpdateType.Should().Be("message_created");
        update.Message.Should().NotBeNull();
        Assert.NotNull(update.Message);
        update.Message.Sender!.UserId.Should().Be(42);
        update.Message.Recipient!.ChatId.Should().Be(777);
        update.Message.Body!.Text.Should().Be("/schedule");
        update.Callback.Should().BeNull();
    }

    [Fact]
    public void DeserializesMessageCallback()
    {
        var json = """
            {
              "update_type": "message_callback",
              "timestamp": 1725777000000,
              "callback": {
                "timestamp": 1725777000000,
                "callback_id": "cb-999",
                "payload": "role:student",
                "user": { "user_id": 42, "first_name": "Иван", "is_bot": false }
              }
            }
            """;

        var update = JsonSerializer.Deserialize<MaxUpdate>(json, JsonOpts);

        Assert.NotNull(update);
        update.UpdateType.Should().Be("message_callback");
        update.Callback.Should().NotBeNull();
        Assert.NotNull(update.Callback);
        update.Callback.CallbackId.Should().Be("cb-999");
        update.Callback.Payload.Should().Be("role:student");
        update.Callback.User!.UserId.Should().Be(42);
        update.Message.Should().BeNull();
    }

    [Fact]
    public void DeserializesBotStarted()
    {
        var json = """
            {
              "update_type": "bot_started",
              "timestamp": 1725777000000,
              "chat_id": 777,
              "user": { "user_id": 42, "first_name": "Иван", "is_bot": false }
            }
            """;

        var update = JsonSerializer.Deserialize<MaxUpdate>(json, JsonOpts);

        Assert.NotNull(update);
        update.UpdateType.Should().Be("bot_started");
        update.ChatId.Should().Be(777);
        update.User!.UserId.Should().Be(42);
    }

    [Fact]
    public void DeserializesMessageCallbackWithBotSenderMessage()
    {
        // В message_callback поле message — это исходящее сообщение бота,
        // поэтому sender = сам бот, а настоящий пользователь — в callback.user.
        var json = """
            {
              "update_type": "message_callback",
              "timestamp": 1725777000000,
              "callback": {
                "timestamp": 1725777000000,
                "callback_id": "cb-999",
                "payload": "role:student",
                "user": { "user_id": 42, "first_name": "Иван", "is_bot": false }
              },
              "message": {
                "sender": { "user_id": 211141964, "first_name": "Бот", "is_bot": true },
                "recipient": { "chat_type": "dialog", "chat_id": 777, "user_id": 42 },
                "body": { "mid": "mid-1", "seq": 1, "text": "Выбери роль" }
              }
            }
            """;

        var update = JsonSerializer.Deserialize<MaxUpdate>(json, JsonOpts);

        Assert.NotNull(update);
        update.UpdateType.Should().Be("message_callback");
        Assert.NotNull(update.Callback);
        update.Callback!.User!.UserId.Should().Be(42);
        Assert.NotNull(update.Message);
        update.Message!.Sender!.UserId.Should().Be(211141964);
    }
}
