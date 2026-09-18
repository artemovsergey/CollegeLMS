using CollegeLMS.MaxBot.Clients;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Отправка PNG-картинки корректировки расписания в канал Max.
/// Подпись (caption) формирует API CollegeLMS — бот только отправляет.
/// Fail-safe: сбои логируются и не влияют на применение корректировки.
/// </summary>
public class CorrectionImageSender(
    MaxApiClient max,
    IOptions<MaxBotOptions> options,
    ILogger<CorrectionImageSender> logger
)
{
    public async Task SendAsync(byte[] png, string caption, CancellationToken ct)
    {
        try
        {
            if (!long.TryParse(options.Value.CorrectionChannelId, out var chatId))
            {
                logger.LogWarning(
                    "MaxBot:CorrectionChannelId не настроен — картинка корректировки не отправлена."
                );
                return;
            }

            var upload = await max.GetUploadUrlAsync("image", ct);
            var payload = await max.UploadImageAsync(upload.Url, png, "correction.png", ct);
            await max.SendImageAsync(chatId, payload, caption, ct);
            logger.LogInformation("Картинка корректировки отправлена в канал {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Сбой отправки картинки корректировки в канал Max");
        }
    }
}
