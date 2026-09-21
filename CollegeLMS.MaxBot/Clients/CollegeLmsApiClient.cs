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

    public async Task<string?> DispatcherLoginAsync(string password, CancellationToken ct)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync(
                "/api/dispatcher/login",
                new { password },
                JsonOpts,
                ct
            );
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Dispatcher login returned {Code}", resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<DispatcherLoginResponse>>(
                json,
                JsonOpts
            );
            return wrapper?.Data?.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login dispatcher");
            return null;
        }
    }

    public async Task<ScheduleMetaDto?> GetScheduleMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/api/schedule/meta", ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<ScheduleMetaDto>>(
                json,
                JsonOpts
            );
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch schedule meta");
            return null;
        }
    }

    /// <summary>
    /// Получает день расписания через серверный вид view=day (слои практик, вставок и пар).
    /// null — ошибка запроса или невалидный ответ (отличается от валидного пустого дня).
    /// </summary>
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

    public async Task<CorrectionConfirmResponse?> ConfirmCorrectionAsync(
        CorrectionEntryDto entry,
        string token,
        CancellationToken ct
    )
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/schedule/correction/confirm"
            )
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { entries = new[] { entry } }, JsonOpts),
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token
            );
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            var resp = await _http.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Correction confirm returned {Code}: {Body}",
                    resp.StatusCode,
                    json
                );
                return null;
            }

            var wrapper = JsonSerializer.Deserialize<ResultWrapper<CorrectionConfirmResponse>>(
                json,
                JsonOpts
            );
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm correction");
            return null;
        }
    }

    public async Task<CorrectionBatchDto?> CreateCorrectionBatchAsync(
        DateTime correctionDate,
        string token,
        CancellationToken ct
    )
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/schedule/correction/batches"
            )
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { correctionDate }, JsonOpts),
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token
            );

            var resp = await _http.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Correction batch creation returned {Code}: {Body}",
                    resp.StatusCode,
                    json
                );
                return null;
            }

            var wrapper = JsonSerializer.Deserialize<ResultWrapper<CorrectionBatchDto>>(
                json,
                JsonOpts
            );
            return wrapper is { IsSuccess: true } ? wrapper.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create correction batch");
            return null;
        }
    }

    public async Task<bool> AddCorrectionPositionAsync(
        Guid batchId,
        CreateCorrectionPositionDto position,
        string token,
        CancellationToken ct
    )
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/schedule/correction/batches/{batchId}/positions"
            )
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(position, JsonOpts),
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token
            );

            var resp = await _http.SendAsync(request, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Adding correction position returned {Code}", resp.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add correction position");
            return false;
        }
    }

    public async Task<CorrectionBatchApplyResponse?> ApplyCorrectionBatchAsync(
        Guid batchId,
        string token,
        string idempotencyKey,
        CancellationToken ct
    )
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/schedule/correction/batches/{batchId}/apply"
            );
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token
            );
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            var resp = await _http.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Correction batch apply returned {Code}: {Body}",
                    resp.StatusCode,
                    json
                );
                return null;
            }

            var wrapper = JsonSerializer.Deserialize<ResultWrapper<CorrectionBatchApplyResponse>>(
                json,
                JsonOpts
            );
            return wrapper is { IsSuccess: true } ? wrapper.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply correction batch");
            return null;
        }
    }

    public async Task<byte[]?> GetScheduleXlsxAsync(
        Guid? groupId,
        string token,
        CancellationToken ct
    )
    {
        var url = groupId.HasValue
            ? $"/api/schedule/export?format=xlsx&groupId={groupId}"
            : "/api/schedule/export?format=xlsx";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token
            );
            var resp = await _http.SendAsync(request, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Schedule export returned {Code}", resp.StatusCode);
                return null;
            }

            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export schedule");
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

    public async Task<List<ScheduleInsertDto>> GetInsertsAsync(
        int dayOfWeek,
        int? course = null,
        CancellationToken ct = default
    )
    {
        var url = $"/api/schedule/inserts?dayOfWeek={dayOfWeek}&activeOnly=true";
        if (course.HasValue)
            url += $"&course={course.Value}";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inserts API returned {Code}", resp.StatusCode);
                return [];
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var wrapper = JsonSerializer.Deserialize<ResultWrapper<List<ScheduleInsertDto>>>(
                json,
                JsonOpts
            );
            return wrapper?.Data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch schedule inserts");
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
