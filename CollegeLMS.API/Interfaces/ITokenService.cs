using System.Security.Claims;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateCustomToken(
        IReadOnlyCollection<string> roles,
        int lifetimeMinutes,
        string nameIdentifier,
        IEnumerable<Claim>? extraClaims = null
    );
}
