using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CollegeLMS.Tests.Unit.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService Create() =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = "TestKey_12345678901234567890123456789012",
                        ["Jwt:Issuer"] = "CollegeLMS",
                        ["Jwt:Audience"] = "CollegeLMS.Clients",
                    }
                )
                .Build()
        );

    [Fact]
    public void GenerateCustomToken_AddsExtraClaims()
    {
        var teacherId = Guid.NewGuid();

        var token = Create()
            .GenerateCustomToken(
                ["Teacher"],
                60,
                Guid.NewGuid().ToString(),
                [new Claim("teacherId", teacherId.ToString()), new Claim("max_user_id", "42")]
            );

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "teacherId" && c.Value == teacherId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "max_user_id" && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Teacher");
    }
}
