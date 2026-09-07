using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CollegeLMS.MaxBot.Clients;

internal record ResultWrapper<T>
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

public class CollegeLmsApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CollegeLmsApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public CollegeLmsApiClient(HttpClient http, ILogger<CollegeLmsApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<ScheduleResponse>> GetScheduleAsync(
        Guid? groupId = null,
        Guid? teacherId = null,
        int? dayOfWeek = null,
        string? period = null,
        int? week = null,
        CancellationToken ct = default
    )
    {
        var query = new List<string>();
        if (groupId.HasValue)
            query.Add($"groupId={groupId}");
        if (teacherId.HasValue)
            query.Add($"teacherId={teacherId}");
        if (dayOfWeek.HasValue)
            query.Add($"dayOfWeek={dayOfWeek}");
        if (period is not null)
            query.Add($"period={period}");
        if (week.HasValue)
            query.Add($"week={week}");

        var url = query.Count > 0 ? $"/api/schedule?{string.Join("&", query)}" : "/api/schedule";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Schedule API returned {Code}", resp.StatusCode);
                return [];
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<SchedulePageResponse>>(
                json,
                JsonOpts
            );
            return wrapper?.Data?.Items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch schedule");
            return [];
        }
    }

    public async Task<List<GroupResponse>> GetGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/groups", ct);
            if (!resp.IsSuccessStatusCode)
                return [];

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<List<GroupResponse>>>(
                json,
                JsonOpts
            );
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch groups");
            return [];
        }
    }

    public async Task<List<TeacherResponse>> GetTeachersAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/teachers", ct);
            if (!resp.IsSuccessStatusCode)
                return [];

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<List<TeacherResponse>>>(
                json,
                JsonOpts
            );
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch teachers");
            return [];
        }
    }
}
