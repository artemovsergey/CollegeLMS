namespace CollegeLMS.API.Dtos;

public class MaxAuthRequest
{
    public string InitData { get; set; } = string.Empty;
}

public class MaxAuthProfile
{
    public long MaxUserId { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = "Other";
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}

public class MaxAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public MaxAuthProfile Profile { get; set; } = new();
}
