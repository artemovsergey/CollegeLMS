using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Аутентификация и авторизация пользователей.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Вход в систему.</summary>
    /// <remarks>Принимает email и пароль, возвращает JWT токен и данные пользователя.</remarks>
    /// <param name="request">Данные для входа</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Успешный вход — возвращает токен и профиль</response>
    /// <response code="401">Неверный email или пароль</response>
    /// <response code="403">Пользователь деактивирован</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Вход в систему")]
    [SwaggerResponse(200, "Успешный вход", typeof(Result<LoginResponse>))]
    [SwaggerResponse(401, "Неверные учётные данные")]
    [SwaggerResponse(403, "Пользователь деактивирован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<LoginResponse>>> Login(
        LoginRequest request,
        CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить профиль текущего пользователя.</summary>
    /// <remarks>Возвращает данные авторизованного пользователя по JWT токену.</remarks>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль получен</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("profile")]
    [Authorize]
    [SwaggerOperation(Summary = "Получить профиль текущего пользователя")]
    [SwaggerResponse(200, "Профиль получен", typeof(Result<UserResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> Profile(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await authService.GetProfileAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
