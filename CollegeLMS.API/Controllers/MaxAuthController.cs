using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Вход в API из мини-приложения MAX.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class MaxAuthController(IMaxAuthService maxAuthService) : ControllerBase
{
    /// <summary>Обмен MAX initData на JWT.</summary>
    /// <response code="200">Токен выдан</response>
    /// <response code="401">Невалидный или просроченный initData</response>
    [HttpPost("max")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [SwaggerOperation(Summary = "Вход через MAX мини-приложение")]
    [SwaggerResponse(200, "Токен выдан", typeof(Result<MaxAuthResponse>))]
    [SwaggerResponse(401, "Невалидный initData", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<MaxAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<MaxAuthResponse>>> Login(
        MaxAuthRequest request,
        CancellationToken ct
    )
    {
        var result = await maxAuthService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
