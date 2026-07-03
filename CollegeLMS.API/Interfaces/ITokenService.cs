using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}
