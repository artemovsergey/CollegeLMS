namespace CollegeLMS.MaxBot.Models.Max;

public record MaxButton
{
    public string Type { get; init; } = "";

    public string Text { get; init; } = "";

    public string? Payload { get; init; }

    public string? Url { get; init; }
}
