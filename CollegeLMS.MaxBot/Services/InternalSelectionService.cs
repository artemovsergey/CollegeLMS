using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Сохраняет выбор группы/преподавателя, сделанный в мини-приложении, и отправляет
/// в чат экран выбора — ту же клавиатуру, что показывает главное меню бота.
/// </summary>
public class InternalSelectionService(
    MaxBotDbContext db,
    InternalProfileService profileService,
    MaxApiClient max,
    IOptions<MaxBotOptions> options,
    ILogger<InternalSelectionService> logger
)
{
    public async Task<InternalUserProfile> SetAsync(
        long maxUserId,
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        var settings = await db.UserSettings.FirstOrDefaultAsync(x => x.MaxUserId == maxUserId, ct);
        if (settings is null)
        {
            settings = new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = maxUserId,
                MaxChatId = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.UserSettings.Add(settings);
        }

        var changed = groupId.HasValue
            ? settings.GroupId != groupId
            : settings.TeacherId != teacherId;

        if (groupId is { } newGroupId)
            MaxBotRoleFlow.SelectGroup(settings, newGroupId);
        else
            MaxBotRoleFlow.SelectTeacher(settings, teacherId!.Value);

        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var profile = await profileService.GetAsync(maxUserId, ct);

        // Сообщаем только о реальном изменении и только если чат уже известен:
        // выбор мог быть сделан до первого /start пользователя в боте.
        if (changed && settings.MaxChatId > 0)
            await SendMenuAsync(settings, profile, ct);

        return profile;
    }

    private async Task SendMenuAsync(
        UserSettings settings,
        InternalUserProfile profile,
        CancellationToken ct
    )
    {
        var (text, buttons) = BotScreens.MainMenuWithSelection(
            settings.Role,
            settings.GroupId,
            settings.TeacherId,
            profile.GroupName,
            profile.TeacherName,
            options.Value
        );

        try
        {
            await max.SendInlineKeyboardAsync(settings.MaxChatId, text, buttons, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Не удалось отправить сообщение о выборе из мини-приложения в чат {ChatId}",
                settings.MaxChatId
            );
        }
    }
}
