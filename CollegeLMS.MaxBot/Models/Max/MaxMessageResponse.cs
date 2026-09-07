using System.Text.Json.Serialization;

namespace CollegeLMS.MaxBot.Models.Max;

public record MaxMessageResponse
{
    public MaxMessageEnvelope? Message { get; init; }
}

public record MaxUpdatesResponse
{
    public List<MaxUpdate>? Updates { get; init; }
    public long? Marker { get; init; }
}

public record MaxBotInfo
{
    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    public string Name { get; init; } = "";

    public string? Username { get; init; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; init; }
}
