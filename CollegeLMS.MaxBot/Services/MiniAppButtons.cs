using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Кнопки open_app для перехода в мини-приложение.</summary>
public static class MiniAppButtons
{
    public static MaxButton OpenSchedule(
        MaxBotOptions options,
        Guid? groupId = null,
        Guid? teacherId = null
    ) =>
        new()
        {
            Type = "open_app",
            Text = "📱 Открыть расписание",
            WebApp = options.BotPublicName,
            Payload = MiniAppUrlBuilder.BuildStartPayload("today", null, groupId, teacherId),
        };

    public static MaxButton OpenDay(
        MaxBotOptions options,
        DateTime date,
        Guid? groupId,
        Guid? teacherId
    ) =>
        new()
        {
            Type = "open_app",
            Text = "📅 Открыть день",
            WebApp = options.BotPublicName,
            Payload = MiniAppUrlBuilder.BuildStartPayload("day", date, groupId, teacherId),
        };
}
