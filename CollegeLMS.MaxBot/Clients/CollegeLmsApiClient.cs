using System.Globalization;
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

    public async Task<ScheduleDayViewDto?> GetDayViewAsync(
        DateTime? date,
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        var query = new List<string> { "view=day" };
        if (date.HasValue)
            query.Add($"date={date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        if (groupId.HasValue)
            query.Add($"groupId={groupId.Value}");
        if (teacherId.HasValue)
            query.Add($"teacherId={teacherId.Value}");

        try
        {
            var resp = await _http.GetAsync($"/api/schedule?{string.Join("&", query)}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Schedule day view API returned {Code}", resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<ScheduleDayViewDto>>(
                json,
                JsonOpts
            );
            return wrapper is { IsSuccess: true } ? wrapper.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch schedule day view");
            return null;
        }
    }

    /// <summary>
    /// Получает неделю расписания через серверный вид view=week (дни Пн–Сб со слоями).
    /// null — ошибка запроса или невалидный ответ (отличается от валидной пустой недели).
    /// </summary>
    public async Task<ScheduleWeekViewDto?> GetWeekViewAsync(
        int? week,
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        var query = new List<string> { "view=week" };
        if (week.HasValue)
            query.Add($"week={week.Value}");
        if (groupId.HasValue)
            query.Add($"groupId={groupId.Value}");
        if (teacherId.HasValue)
            query.Add($"teacherId={teacherId.Value}");

        try
        {
            var resp = await _http.GetAsync($"/api/schedule?{string.Join("&", query)}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Schedule week view API returned {Code}", resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<ScheduleWeekViewDto>>(
                json,
                JsonOpts
            );
            return wrapper is { IsSuccess: true } ? wrapper.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch schedule week view");
            return null;
        }
    }

    public async Task<List<NonWorkingDayDto>> GetNonWorkingDaysAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        var url =
            $"/api/non-working-days?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&page=1&pageSize=100";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Non-working days API returned {Code}", resp.StatusCode);
                return [];
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<
                ResultWrapper<PagedResponse<NonWorkingDayDto>>
            >(json, JsonOpts);
            return wrapper?.Data?.Items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch non-working days");
            return [];
        }
    }

    public async Task<List<PracticeDto>> GetPracticesAsync(
        DateOnly from,
        DateOnly to,
        Guid? groupId = null,
        Guid? teacherId = null,
        CancellationToken ct = default
    )
    {
        var url = $"/api/practices?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&page=1&pageSize=100";
        if (groupId.HasValue)
            url += $"&groupId={groupId.Value}";
        if (teacherId.HasValue)
            url += $"&teacherId={teacherId.Value}";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Practices API returned {Code}", resp.StatusCode);
                return [];
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<PagedResponse<PracticeDto>>>(
                json,
                JsonOpts
            );
            return wrapper?.Data?.Items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch practices");
            return [];
        }
    }
}
