using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.Services;

/// <summary>
/// HTTP-клиент к боту Max. Отправляет изменения расписания на POST /notify,
/// PNG-картинку корректировки на POST /notify/correction-image и запрашивает
/// профиль MAX-пользователя на GET /maxbot/internal/users/{id}.
/// Fail-safe: недоступность бота не роняет подтверждение корректировки.
/// </summary>
public class MaxBotHttpClient(
    HttpClient http,
    IConfiguration config,
    ILogger<MaxBotHttpClient> logger
)
{
    public async Task<MaxInternalUserDto?> GetInternalUserAsync(
        long maxUserId,
        CancellationToken ct
    )
    {
        try
        {
            var secret = config["MaxBot:InternalSecret"] ?? "";
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/maxbot/internal/users/{maxUserId}"
            );
            if (!string.IsNullOrEmpty(secret))
                request.Headers.Add("X-Internal-Secret", secret);

            var resp = await http.SendAsync(request, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("MaxBot internal users вернул {Code}", resp.StatusCode);
                return null;
            }

            return await resp.Content.ReadFromJsonAsync<MaxInternalUserDto>(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MaxBot недоступен — профиль MAX не получен");
            return null;
        }
    }

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

    public async Task SendCorrectionImageAsync(byte[] png, string caption, CancellationToken ct)
    {
        if (png.Length == 0)
            return;

        try
        {
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(png);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(file, "file", "correction.png");
            content.Add(new StringContent(caption, Encoding.UTF8), "caption");

            var resp = await http.PostAsync("/notify/correction-image", content, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "MaxBot /notify/correction-image вернул {Code}: {Body}",
                    resp.StatusCode,
                    body.Length > 200 ? body[..200] : body
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MaxBot недоступен — картинка корректировки не отправлена");
        }
    }
}
