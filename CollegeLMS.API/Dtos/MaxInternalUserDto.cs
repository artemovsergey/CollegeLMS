namespace CollegeLMS.API.Dtos;

public class MaxInternalUserDto
{
    public bool Found { get; set; }
    public long MaxUserId { get; set; }
    public string Role { get; set; } = "student";
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}
