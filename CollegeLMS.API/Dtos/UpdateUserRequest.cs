using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
