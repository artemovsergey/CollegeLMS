namespace CollegeLMS.API.Dtos;

public class TeacherResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}
