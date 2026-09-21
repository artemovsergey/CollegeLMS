using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Рабочие дни (рабочие субботы и переносы).</summary>
[ApiController]
[Route("api/working-days")]
[Produces("application/json")]
[Authorize]
public class WorkingDayController(IWorkingDayService service) : ControllerBase
{
    /// <summary>Список рабочих дней с фильтром по периоду и пагинацией.</summary>
    /// <param name="from">Начало периода (включительно)</param>
    /// <param name="to">Конец периода (включительно)</param>
    /// <param name="page">Номер страницы (с 1)</param>
    /// <param name="pageSize">Размер страницы (1–100)</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Список получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Список рабочих дней")]
    [SwaggerResponse(200, "Список получен", typeof(Result<PagedResponse<WorkingDayResponse>>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(
        typeof(Result<PagedResponse<WorkingDayResponse>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(from, to, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Добавить рабочий день.</summary>
    /// <remarks>Если диапазон пересекается с нерабочим днём, возвращается 409.</remarks>
    /// <param name="request">Период, название и день недели для подмены</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Рабочий день добавлен</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="409">Диапазон пересекается с нерабочим днём</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Добавить рабочий день")]
    [SwaggerResponse(200, "Рабочий день добавлен", typeof(Result<WorkingDayResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Конфликт с нерабочим днём", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<WorkingDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(WorkingDayRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить рабочий день.</summary>
    /// <param name="id">Идентификатор рабочего дня</param>
    /// <param name="request">Новые данные</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Рабочий день обновлён</response>
    /// <response code="400">Некорректные данные</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Рабочий день не найден</response>
    /// <response code="409">Диапазон пересекается с нерабочим днём</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Изменить рабочий день")]
    [SwaggerResponse(200, "Рабочий день обновлён", typeof(Result<WorkingDayResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Рабочий день не найден", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Конфликт с нерабочим днём", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<WorkingDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        WorkingDayRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить рабочий день.</summary>
    /// <param name="id">Идентификатор рабочего дня</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Рабочий день удалён</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Рабочий день не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить рабочий день")]
    [SwaggerResponse(200, "Рабочий день удалён")]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Рабочий день не найден", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
