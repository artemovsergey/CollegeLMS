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
        CancellationToken ct
    )
    {
        var result = await authService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить профиль текущего пользователя.</summary>
    /// <remarks>Возвращает данные авторизованного пользователя по JWT токену с роль-специфичными полями (цикловая комиссия для преподавателя, группа для студента).</remarks>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль получен</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("profile")]
    [Authorize]
    [SwaggerOperation(Summary = "Получить профиль текущего пользователя")]
    [SwaggerResponse(200, "Профиль получен", typeof(Result<ProfileResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ProfileResponse>>> Profile(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await authService.GetProfileAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Обновить профиль текущего пользователя.</summary>
    /// <remarks>Позволяет изменить ФИО и email.</remarks>
    /// <param name="request">Новые данные профиля</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль обновлён</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="409">Email уже занят</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpPut("profile")]
    [Authorize]
    [SwaggerOperation(Summary = "Обновить профиль текущего пользователя")]
    [SwaggerResponse(200, "Профиль обновлён", typeof(Result<ProfileResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(409, "Email уже занят")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ProfileResponse>>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await authService.UpdateProfileAsync(userId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    [SwaggerOperation(Summary = "Сменить пароль")]
    [SwaggerResponse(200, "Пароль изменён", typeof(Result))]
    [SwaggerResponse(400, "Неверный старый пароль")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await authService.ChangePasswordAsync(userId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
