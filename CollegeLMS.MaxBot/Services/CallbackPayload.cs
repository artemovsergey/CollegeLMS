namespace CollegeLMS.MaxBot.Services;

public sealed record CallbackPayload(string Action, string? Param1, string? Param2)
{
    public static CallbackPayload? Parse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var parts = payload.Split(':', 3);
        return new CallbackPayload(
            parts[0],
            parts.Length > 1 ? parts[1] : null,
            parts.Length > 2 ? parts[2] : null
        );
    }
}
