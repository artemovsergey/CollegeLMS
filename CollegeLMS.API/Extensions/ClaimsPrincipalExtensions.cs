using System.Security.Claims;

namespace CollegeLMS.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)!;

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)!;

    public static Guid? GetMaxGroupId(this ClaimsPrincipal user) =>
        ParseGuid(user.FindFirstValue("groupId"));

    public static Guid? GetMaxTeacherId(this ClaimsPrincipal user) =>
        ParseGuid(user.FindFirstValue("teacherId"));

    public static long? GetMaxUserId(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue("max_user_id"), out var id) ? id : null;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
