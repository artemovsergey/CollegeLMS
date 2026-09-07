using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Извлекает идентификатор пользователя и чата из события MAX.
/// Внимание: в событии message_callback поле message — это исходящее
/// сообщение бота (sender = сам бот), поэтому приоритет у callback.user.
/// </summary>
public static class MaxUserResolver
{
    public static long ResolveUserId(MaxUpdate update)
    {
        if (update.UpdateType == "message_callback")
            return update.Callback?.User?.UserId
                ?? update.Message?.Sender?.UserId
                ?? update.User?.UserId
                ?? 0;

        return update.Message?.Sender?.UserId ?? update.User?.UserId ?? 0;
    }

    public static long ResolveChatId(MaxUpdate update)
    {
        return update.Message?.Recipient?.ChatId ?? update.ChatId ?? 0;
    }
}
