using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
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

    /// <summary>Сохранение выбора группы или преподавателя из мини-приложения.</summary>
    /// <response code="200">Выбор сохранён, токен обновлён</response>
    /// <response code="400">Указана не ровно одна цель</response>
    /// <response code="401">Токен отсутствует или невалиден</response>
    /// <response code="503">Бот недоступен, выбор не сохранён</response>
    [HttpPost("max/selection")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    [SwaggerOperation(Summary = "Выбор группы или преподавателя из мини-приложения")]
    [SwaggerResponse(200, "Выбор сохранён", typeof(Result<MaxAuthResponse>))]
    [SwaggerResponse(400, "Некорректный выбор", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Нет авторизации", typeof(ErrorResponse))]
    [SwaggerResponse(503, "Бот недоступен", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<MaxAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Result<MaxAuthResponse>>> Select(
        MaxSelectionRequest request,
        CancellationToken ct
    )
    {
        var maxUserId = User.GetMaxUserId();
        if (maxUserId is null)
            return Unauthorized();

        var result = await maxAuthService.SelectAsync(maxUserId.Value, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
