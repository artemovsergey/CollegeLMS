namespace CollegeLMS.API.Dtos;

public class CreateTeacherRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class UpdateTeacherRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CyclicalCommission { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}
