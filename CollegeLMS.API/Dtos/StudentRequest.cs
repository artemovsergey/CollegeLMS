namespace CollegeLMS.API.Dtos;

public class CreateStudentRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string RecordBookNumber { get; set; } = string.Empty;
}

public class UpdateStudentRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string RecordBookNumber { get; set; } = string.Empty;
}
