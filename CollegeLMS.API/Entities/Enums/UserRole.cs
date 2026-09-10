namespace CollegeLMS.API.Entities.Enums;

[Flags]
public enum UserRole
{
    None = 0,
    Admin = 1,
    Teacher = 2,
    Student = 4,
    Dispatcher = 8,
}

public static class UserRoleExtensions
{
    public static bool HasRole(this UserRole value, UserRole role)
    {
        if (role == UserRole.None)
        {
            return value == UserRole.None;
        }

        return (value & role) == role;
    }

    public static List<UserRole> GetRoles(this UserRole value)
    {
        var roles = new List<UserRole>();

        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            if (role != UserRole.None && value.HasRole(role))
            {
                roles.Add(role);
            }
        }

        return roles;
    }
}
