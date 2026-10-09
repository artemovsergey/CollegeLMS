using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Персональная рассылка поздравления преподавателям, установившим бота:
/// картинка загружается один раз и отправляется в каждый личный чат.
/// Аудитория — строки user_settings с ролью teacher.
/// Сбой доставки одному чату не прерывает рассылку остальным.
/// </summary>
public class TeacherGreetingSender(
    MaxBotDbContext db,
    MaxApiClient max,
    ILogger<TeacherGreetingSender> logger
)
{
    /// <summary>Результат рассылки: сколько чатов получили поздравление и сколько нет.</summary>
    public record Report(int Recipients, int Sent, int Failed);

    /// <summary>Пауза между отправками — та же, что в дайджесте и практиках.</summary>
    private static readonly TimeSpan Throttle = TimeSpan.FromMilliseconds(500);

    public async Task<Report> SendAsync(byte[] png, string text, CancellationToken ct)
    {
        var chatIds = await db
            .UserSettings.Where(u => u.Role == "teacher")
            .Select(u => u.MaxChatId)
            .Distinct()
            .ToListAsync(ct);

        if (chatIds.Count == 0)
        {
            logger.LogInformation("Рассылка поздравления пропущена: преподавателей в боте нет");
            return new Report(0, 0, 0);
        }

        // Один upload на всю рассылку: payload вложения переиспользуется в каждом чате.
        var upload = await max.GetUploadUrlAsync("image", ct);
        var payload = await max.UploadImageAsync(upload.Url, png, "greeting.png", ct);

        var sent = 0;
        var failed = 0;

        foreach (var chatId in chatIds)
        {
            try
            {
                await max.SendImageAsync(chatId, payload, text, ct);
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Поздравление не доставлено в чат {ChatId}", chatId);
            }

            await Task.Delay(Throttle, ct);
        }

        logger.LogInformation(
            "Поздравление преподавателям: получателей {Recipients}, доставлено {Sent}, сбоев {Failed}",
            chatIds.Count,
            sent,
            failed
        );

        return new Report(chatIds.Count, sent, failed);
    }
}
