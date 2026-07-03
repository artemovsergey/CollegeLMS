using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class ChangeRoleRequest
{
    public UserRole Role { get; set; }
}
