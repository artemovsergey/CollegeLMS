using System.Text.Json.Serialization;

namespace CollegeLMS.MaxBot.Models.Max;

public record MaxUpdate
{
    [JsonPropertyName("update_type")]
    public string UpdateType { get; init; } = "";

    public long Timestamp { get; init; }

    /// <summary>Присутствует у событий типа bot_started / chat-событий.</summary>
    [JsonPropertyName("chat_id")]
    public long? ChatId { get; init; }

    public MaxSender? User { get; init; }

    public MaxMessageEnvelope? Message { get; init; }

    public MaxCallback? Callback { get; init; }
}

public record MaxCallback
{
    public long Timestamp { get; init; }

    [JsonPropertyName("callback_id")]
    public string CallbackId { get; init; } = "";

    public string? Payload { get; init; }

    public MaxSender? User { get; init; }
}

public record MaxMessageEnvelope
{
    public MaxRecipient? Recipient { get; init; }
    public MaxSender? Sender { get; init; }
    public MaxBody? Body { get; init; }
}

public record MaxRecipient
{
    [JsonPropertyName("chat_type")]
    public string ChatType { get; init; } = "";

    [JsonPropertyName("chat_id")]
    public long ChatId { get; init; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; init; }
}

public record MaxSender
{
    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("is_bot")]
    public bool IsBot { get; init; }

    public string? Name { get; init; }
}

public record MaxBody
{
    public string? Mid { get; init; }
    public long? Seq { get; init; }
    public string? Text { get; init; }
}
