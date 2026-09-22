using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Экраны бота, которые отправляются не только из MaxBotService
/// (например, сервисом выбора группы/преподавателя из мини-приложения).
/// </summary>
public static class BotScreens
{
    /// <summary>Главное меню с заданным выбором: «Группа: …» или «Преподаватель: …».</summary>
    public static (string Text, List<List<MaxButton>> Buttons) MainMenuWithSelection(
        string role,
        Guid? groupId,
        Guid? teacherId,
        string? groupName,
        string? teacherName,
        MaxBotOptions options
    )
    {
        var entity =
            role == "student"
                ? $"Группа: {groupName ?? "выбрана"}"
                : $"Преподаватель: {teacherName ?? "выбран"}";

        var buttons = new List<List<MaxButton>>
        {
            new() { MiniAppButtons.OpenSchedule(options, groupId, teacherId) },
            new List<MaxButton>
            {
                new()
                {
                    Type = "callback",
                    Text = "⚙️ Настройки",
                    Payload = "settings",
                },
            },
        };

        return ($"🏠 *Главное меню*\n\n{entity}", buttons);
    }
}
