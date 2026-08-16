namespace CollegeLMS.API.Dtos;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CyclicalCommission { get; set; }
    public string? Category { get; set; }
}
