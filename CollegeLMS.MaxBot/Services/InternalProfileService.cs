using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Собирает профиль MAX-пользователя из user_settings и имён групп/преподавателей.</summary>
public class InternalProfileService(MaxBotDbContext db, CollegeLmsApiClient api)
{
    public async Task<InternalUserProfile> GetAsync(long maxUserId, CancellationToken ct)
    {
        var settings = await db
            .UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaxUserId == maxUserId, ct);
        if (settings is null)
            return new InternalUserProfile
            {
                Found = false,
                MaxUserId = maxUserId,
                Role = "student",
            };

        string? groupName = null;
        string? teacherName = null;
        if (settings.GroupId.HasValue)
            groupName = (await api.GetGroupsAsync(ct))
                .FirstOrDefault(g => g.Id == settings.GroupId.Value)
                ?.Name;
        if (settings.TeacherId.HasValue)
            teacherName = (await api.GetTeachersAsync(ct))
                .FirstOrDefault(t => t.Id == settings.TeacherId.Value)
                ?.FullName;

        return new InternalUserProfile
        {
            Found = true,
            MaxUserId = maxUserId,
            Role = settings.Role,
            GroupId = settings.GroupId,
            GroupName = groupName,
            TeacherId = settings.TeacherId,
            TeacherName = teacherName,
        };
    }
}
