namespace CollegeLMS.MaxBot.Models.Max;

public record BotCommand
{
    public string Name { get; init; } = "";

    public string? Description { get; init; }
}
