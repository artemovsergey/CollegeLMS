using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Специальные события в расписании (например, «Разговор о важном»).</summary>
[ApiController]
[Route("api/schedule/inserts")]
[Produces("application/json")]
public class ScheduleInsertController(IScheduleInsertService service) : ControllerBase
{
    /// <summary>Список событий с фильтрами по дню недели и курсу.</summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Список событий в расписании")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<ScheduleInsertResponse>>))]
    [ProducesResponseType(typeof(Result<List<ScheduleInsertResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] int? course,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default
    )
    {
        var result = await service.GetAllAsync(dayOfWeek, course, activeOnly, ct);
        return Ok(result);
    }

    /// <summary>Создать событие.</summary>
    [HttpPost]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Создать событие")]
    [SwaggerResponse(200, "Событие создано", typeof(Result<ScheduleInsertResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<ScheduleInsertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(ScheduleInsertRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить событие.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Изменить событие")]
    [SwaggerResponse(200, "Событие обновлено", typeof(Result<ScheduleInsertResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Событие не найдено", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<ScheduleInsertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        ScheduleInsertRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить событие.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить событие")]
    [SwaggerResponse(200, "Событие удалено")]
    [SwaggerResponse(404, "Событие не найдено", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
