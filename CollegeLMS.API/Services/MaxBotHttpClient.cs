using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.Services;

/// <summary>
/// HTTP-клиент к боту Max. Отправляет изменения расписания на POST /notify.
/// Fail-safe: недоступность бота не роняет подтверждение корректировки.
/// </summary>
public class MaxBotHttpClient(HttpClient http, ILogger<MaxBotHttpClient> logger)
{
    public async Task SendChangesAsync(
        IReadOnlyList<ScheduleChangeDto> changes,
        CancellationToken ct
    )
    {
        if (changes.Count == 0)
            return;

        try
        {
            var resp = await http.PostAsJsonAsync("/notify", changes.ToList(), ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "MaxBot /notify вернул {Code}: {Body}",
                    resp.StatusCode,
                    body.Length > 200 ? body[..200] : body
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MaxBot недоступен — уведомления об изменениях не отправлены");
        }
    }
}