using System.Text.Json.Serialization;

namespace CollegeLMS.MaxBot.Models.Max;

/// <summary>Подписка на обновления MAX (вебхук).</summary>
public record MaxSubscription
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("update_types")]
    public List<string>? UpdateTypes { get; init; }

    [JsonPropertyName("secret")]
    public string? Secret { get; init; }
}

/// <summary>Ответ GET /subscriptions.</summary>
public record MaxSubscriptionsResponse
{
    [JsonPropertyName("subscriptions")]
    public List<MaxSubscription> Subscriptions { get; init; } = [];
}

/// <summary>Ответ POST/DELETE /subscriptions.</summary>
public record MaxSubscriptionResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
