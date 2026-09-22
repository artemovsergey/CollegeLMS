namespace CollegeLMS.MaxBot;

public class MaxBotOptions
{
    public string AccessToken { get; set; } = "";

    public int NotifyHour { get; set; } = 7;

    public int NotifyMinute { get; set; } = 30;

    public string TimeZone { get; set; } = "Europe/Moscow";

    public string MiniAppUrl { get; set; } = "https://stvcc.tech/max";

    /// <summary>Идентификатор канала Max для публикации картинки корректировки.</summary>
    public string CorrectionChannelId { get; set; } = "";

    /// <summary>Публичный HTTPS-адрес вебхука MAX. Пусто — используется long polling.</summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>Секрет вебхука: MAX присылает его в заголовке X-Max-Bot-Api-Secret.</summary>
    public string WebhookSecret { get; set; } = "";

    /// <summary>Публичное имя бота — поле web_app у кнопок open_app.</summary>
    public string BotPublicName { get; set; } = "";

    /// <summary>Секрет внутреннего endpoint профиля (заголовок X-Internal-Secret).</summary>
    public string InternalSecret { get; set; } = "";
}
