using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Профили звонков: пары и большая перемена с привязкой к дням недели и датам.</summary>
[ApiController]
[Route("api/bells")]
[Produces("application/json")]
public class BellScheduleController(IBellScheduleService service) : ControllerBase
{
    /// <summary>Справочник звонков базового (default) профиля.</summary>
    /// <remarks>Обратная совместимость: возвращает пары и большую перемену базового профиля.</remarks>
    /// <response code="200">Справочник получен</response>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить справочник звонков")]
    [SwaggerResponse(200, "Справочник получен", typeof(Result<BellScheduleResponse>))]
    [ProducesResponseType(typeof(Result<BellScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await service.GetAsync(ct);
        return Ok(result);
    }

    /// <summary>Заменить пары и большую перемену базового (default) профиля.</summary>
    /// <param name="request">Пары и большая перемена</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Справочник сохранён</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    [HttpPut]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Заменить справочник звонков")]
    [SwaggerResponse(200, "Справочник сохранён", typeof(Result<BellScheduleResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<BellScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(UpdateBellScheduleRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Список всех профилей звонков.</summary>
    /// <response code="200">Список получен</response>
    [HttpGet("profiles")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить профили звонков")]
    [SwaggerResponse(200, "Список профилей получен", typeof(Result<List<BellProfileResponse>>))]
    [ProducesResponseType(typeof(Result<List<BellProfileResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfiles(CancellationToken ct)
    {
        var result = await service.GetProfilesAsync(ct);
        return Ok(result);
    }

    /// <summary>Создать профиль звонков.</summary>
    /// <param name="request">Данные профиля: название, дни недели, пары, большая перемена, диапазоны дат</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль создан</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    [HttpPost("profiles")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Создать профиль звонков")]
    [SwaggerResponse(200, "Профиль создан", typeof(Result<BellProfileResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<BellProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProfile(BellProfileRequest request, CancellationToken ct)
    {
        var result = await service.CreateProfileAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить профиль звонков.</summary>
    /// <param name="id">Идентификатор профиля</param>
    /// <param name="request">Новые данные профиля</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль обновлён</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Профиль не найден</response>
    [HttpPut("profiles/{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Изменить профиль звонков")]
    [SwaggerResponse(200, "Профиль обновлён", typeof(Result<BellProfileResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Профиль не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<BellProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        BellProfileRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateProfileAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить профиль звонков.</summary>
    /// <remarks>Базовый (default) профиль удалить нельзя.</remarks>
    /// <param name="id">Идентификатор профиля</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль удалён</response>
    /// <response code="400">Попытка удалить базовый профиль</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Профиль не найден</response>
    [HttpDelete("profiles/{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить профиль звонков")]
    [SwaggerResponse(200, "Профиль удалён", typeof(Result))]
    [SwaggerResponse(400, "Базовый профиль удалить нельзя", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Профиль не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteProfileAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Разрешённый на дату профиль звонков.</summary>
    /// <remarks>Приоритет: диапазон даты → подмена дня недели → день недели → базовый профиль.</remarks>
    /// <param name="date">Дата, для которой нужно определить профиль</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Профиль определён</response>
    [HttpGet("resolved")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Определить профиль звонков на дату")]
    [SwaggerResponse(200, "Профиль определён", typeof(Result<BellProfileResponse>))]
    [ProducesResponseType(typeof(Result<BellProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResolved([FromQuery] DateTime? date, CancellationToken ct)
    {
        var result = await service.GetResolvedAsync(date ?? DateTime.UtcNow, ct);
        return Ok(result);
    }
}
