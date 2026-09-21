namespace CollegeLMS.MaxBot.Services;

public static class MiniAppUrlBuilder
{
    public static string Build(
        string baseUrl,
        string route,
        DateTime? date = null,
        Guid? groupId = null,
        Guid? teacherId = null
    )
    {
        var query = new List<string> { $"route={route}" };
        if (date.HasValue)
            query.Add($"date={date:yyyy-MM-dd}");
        if (groupId.HasValue)
            query.Add($"groupId={Uri.EscapeDataString(groupId.Value.ToString())}");
        if (teacherId.HasValue)
            query.Add($"teacherId={Uri.EscapeDataString(teacherId.Value.ToString())}");

        return $"{baseUrl}?{string.Join("&", query)}";
    }

    /// <summary>
    /// Формирует payload для кнопки <c>open_app</c> (допускаются только символы <c>[\w-]</c>):
    /// <c>{route}[-yyyy-MM-dd][-g-{groupId}|-t-{teacherId}]</c>.
    /// Маркер <c>g</c>/<c>t</c> позволяет Mini App отличить группу от преподавателя.
    /// Mini App читает payload из <c>start_param</c> в MAX Bridge.
    /// </summary>
    public static string BuildStartPayload(
        string route,
        DateTime? date = null,
        Guid? groupId = null,
        Guid? teacherId = null
    )
    {
        var payload = route;
        if (date.HasValue)
            payload += $"-{date:yyyy-MM-dd}";

        if (groupId.HasValue)
            payload += $"-g-{groupId.Value}";
        else if (teacherId.HasValue)
            payload += $"-t-{teacherId.Value}";

        return payload;
    }
}
