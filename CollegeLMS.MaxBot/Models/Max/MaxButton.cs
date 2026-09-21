namespace CollegeLMS.MaxBot.Models.Max;

public record MaxButton
{
    public string Type { get; init; } = "";

    public string Text { get; init; } = "";

    public string? Payload { get; init; }

    public string? Url { get; init; }

    /// <summary>Публичное имя бота — обязательно для кнопок типа <c>open_app</c>.</summary>
    public string? WebApp { get; init; }
}
