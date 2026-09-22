using System.Security.Claims;
using CollegeLMS.API.Extensions;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void GetMaxGroupId_ReadsGuidClaim()
    {
        var id = Guid.NewGuid();
        Principal(new Claim("groupId", id.ToString())).GetMaxGroupId().Should().Be(id);
    }

    [Fact]
    public void GetMaxTeacherId_MissingClaim_ReturnsNull()
    {
        Principal().GetMaxTeacherId().Should().BeNull();
    }

    [Fact]
    public void GetMaxUserId_ReadsLongClaim()
    {
        Principal(new Claim("max_user_id", "777")).GetMaxUserId().Should().Be(777);
    }

    [Fact]
    public void GetMaxUserId_NonNumeric_ReturnsNull()
    {
        Principal(new Claim("max_user_id", "abc")).GetMaxUserId().Should().BeNull();
    }
}
