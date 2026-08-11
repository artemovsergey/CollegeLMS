using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace CollegeLMS.API.Services;

public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
        var issuer = config["Jwt:Issuer"] ?? "CollegeLMS";
        var audience = config["Jwt:Audience"] ?? "CollegeLMS.Clients";
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
