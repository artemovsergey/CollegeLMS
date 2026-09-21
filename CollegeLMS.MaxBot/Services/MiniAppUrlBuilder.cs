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
    /// <c>{route}[-yyyy-MM-dd][-{group|teacher guid}]</c>.
    /// Mini App читает его из <c>start_param</c> в MAX Bridge.
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

        var entityId = groupId ?? teacherId;
        if (entityId.HasValue)
            payload += $"-{entityId.Value}";

        return payload;
    }

    public static string BuildScheduleExportXlsxUrl(string baseUrl, Guid? groupId)
    {
        var query = groupId.HasValue ? $"&groupId={groupId}" : "";
        return $"{baseUrl}/api/schedule/export?format=xlsx{query}";
    }
}
