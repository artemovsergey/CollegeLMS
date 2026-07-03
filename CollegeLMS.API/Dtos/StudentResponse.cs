namespace CollegeLMS.API.Dtos;

public class StudentResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RecordBookNumber { get; set; } = string.Empty;
}
