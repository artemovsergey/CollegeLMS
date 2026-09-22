namespace CollegeLMS.MaxBot.Models;

/// <summary>Профиль MAX-пользователя для внутреннего endpoint (внутри сервиса бота).</summary>
public record InternalUserProfile
{
    public bool Found { get; init; }
    public long MaxUserId { get; init; }
    public string Role { get; init; } = "student";
    public Guid? GroupId { get; init; }
    public string? GroupName { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
}
