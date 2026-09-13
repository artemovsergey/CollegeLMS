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

    public static string BuildScheduleExportXlsxUrl(string baseUrl, Guid? groupId)
    {
        var query = groupId.HasValue ? $"&groupId={groupId}" : "";
        return $"{baseUrl}/api/schedule/export?format=xlsx{query}";
    }
}