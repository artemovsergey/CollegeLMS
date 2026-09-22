using CollegeLMS.MaxBot.Models;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Правила переключения ролей: фильтры студента и преподавателя не пересекаются.</summary>
public static class MaxBotRoleFlow
{
    /// <summary>Онбординг нужен, если группа и преподаватель не выбраны.</summary>
    public static bool RequiresOnboarding(UserSettings? settings) =>
        settings is null || (settings.GroupId is null && settings.TeacherId is null);

    public static void SelectGroup(UserSettings settings, Guid groupId)
    {
        settings.Role = "student";
        settings.GroupId = groupId;
        settings.TeacherId = null;
    }

    public static void SelectTeacher(UserSettings settings, Guid teacherId)
    {
        settings.Role = "teacher";
        settings.TeacherId = teacherId;
        settings.GroupId = null;
    }
}
