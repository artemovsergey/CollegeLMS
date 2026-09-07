using CollegeLMS.MaxBot.Models;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Правила переключения ролей: фильтры студента и преподавателя не пересекаются.</summary>
public static class MaxBotRoleFlow
{
    public static void ApplyRole(UserSettings settings, string role)
    {
        settings.Role = role;
        if (role == "student")
            settings.TeacherId = null;
        else
            settings.GroupId = null;
    }

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

    public static string RoleTogglePayload(string role) =>
        role == "student" ? "role:teacher" : "role:student";

    public static string RoleToggleLabel(string role) =>
        role == "student" ? "🔄 Стать преподавателем" : "🔄 Стать студентом";
}
