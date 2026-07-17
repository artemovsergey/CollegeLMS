namespace CollegeLMS.API.Dtos;

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public TeacherProfileData? TeacherData { get; set; }
    public StudentProfileData? StudentData { get; set; }
}

public class TeacherProfileData
{
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class StudentProfileData
{
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RecordBookNumber { get; set; } = string.Empty;
}
