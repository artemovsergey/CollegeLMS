using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Управление пользователями системы.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UserController(IUserService service) : ControllerBase
{
    /// <summary>Получить список всех пользователей.</summary>
    /// <remarks>Возвращает отсортированный по FullName список пользователей. Доступно авторизованным пользователям.</remarks>
    /// <response code="200">Список пользователей успешно получен</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список пользователей")]
    [SwaggerResponse(200, "Список пользователей получен", typeof(Result<List<UserResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<UserResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>Получить пользователя по ID.</summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Пользователь найден</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить пользователя по ID")]
    [SwaggerResponse(200, "Пользователь найден", typeof(Result<UserResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Создать нового пользователя.</summary>
    /// <remarks>Только для администраторов. Пароль задаётся вручную.</remarks>
    /// <param name="request">Данные нового пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Пользователь создан</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён (требуется роль Admin)</response>
    /// <response code="409">Пользователь с таким email уже существует</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать пользователя")]
    [SwaggerResponse(200, "Пользователь создан", typeof(Result<UserResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(409, "Конфликт — email уже существует")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> Create(
        CreateUserRequest request,
        CancellationToken ct
    )
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Обновить данные пользователя.</summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="request">Новые данные</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Пользователь обновлён</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён (требуется роль Admin)</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="409">Конфликт — email уже существует</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Обновить пользователя")]
    [SwaggerResponse(200, "Пользователь обновлён", typeof(Result<UserResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(409, "Конфликт — email уже существует")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Деактивировать пользователя (мягкое удаление).</summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Пользователь деактивирован</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён (требуется роль Admin)</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Деактивировать пользователя")]
    [SwaggerResponse(200, "Пользователь деактивирован", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Изменить роль пользователя.</summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="request">Новая роль</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Роль изменена</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён (требуется роль Admin)</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPatch("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Изменить роль пользователя")]
    [SwaggerResponse(200, "Роль изменена", typeof(Result<UserResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> ChangeRole(
        Guid id,
        ChangeRoleRequest request,
        CancellationToken ct
    )
    {
        var result = await service.ChangeRoleAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Заблокировать/разблокировать пользователя")]
    [SwaggerResponse(200, "Статус изменён", typeof(Result<UserResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<UserResponse>>> ToggleActive(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await service.ToggleActiveAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
