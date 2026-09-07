using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MaxUserResolverTests
{
    [Fact]
    public void ResolveUserId_PrefersCallbackUser_ForMessageCallback()
    {
        var update = new MaxUpdate
        {
            UpdateType = "message_callback",
            Callback = new MaxCallback
            {
                CallbackId = "cb",
                Payload = "role:student",
                User = new MaxSender { UserId = 42 },
            },
            Message = new MaxMessageEnvelope
            {
                Sender = new MaxSender { UserId = 211141964 },
                Recipient = new MaxRecipient { ChatId = 777 },
            },
        };

        var userId = MaxUserResolver.ResolveUserId(update);
        var chatId = MaxUserResolver.ResolveChatId(update);

        userId.Should().Be(42);
        chatId.Should().Be(777);
    }

    [Fact]
    public void ResolveUserId_UsesMessageSender_ForMessageCreated()
    {
        var update = new MaxUpdate
        {
            UpdateType = "message_created",
            Message = new MaxMessageEnvelope
            {
                Sender = new MaxSender { UserId = 42 },
                Recipient = new MaxRecipient { ChatId = 777 },
            },
        };

        var userId = MaxUserResolver.ResolveUserId(update);
        var chatId = MaxUserResolver.ResolveChatId(update);

        userId.Should().Be(42);
        chatId.Should().Be(777);
    }

    [Fact]
    public void ResolveUserId_UsesTopLevelUser_ForBotStarted()
    {
        var update = new MaxUpdate
        {
            UpdateType = "bot_started",
            ChatId = 777,
            User = new MaxSender { UserId = 42 },
        };

        var userId = MaxUserResolver.ResolveUserId(update);
        var chatId = MaxUserResolver.ResolveChatId(update);

        userId.Should().Be(42);
        chatId.Should().Be(777);
    }
}
