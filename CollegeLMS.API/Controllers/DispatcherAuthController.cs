using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/dispatcher")]
[Produces("application/json")]
public class DispatcherAuthController(IDispatcherAuthService service) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [SwaggerOperation(Summary = "Вход диспетчера по паролю — короткий dispatcher-токен")]
    [SwaggerResponse(200, "Токен выдан", typeof(Result<DispatcherLoginResponse>))]
    [SwaggerResponse(401, "Неверный пароль", typeof(ErrorResponse))]
    [SwaggerResponse(429, "Слишком много попыток", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<DispatcherLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(DispatcherLoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await service.LoginAsync(request.Password, ip, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
