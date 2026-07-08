using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/schedule")]
[Produces("application/json")]
public class ScheduleController(IScheduleService service) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить расписание с фильтрацией и пагинацией")]
    [SwaggerResponse(200, "Расписание получено", typeof(Result<PagedResponse<ScheduleResponse>>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<PagedResponse<ScheduleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<PagedResponse<ScheduleResponse>>>> GetAll(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? teacherId,
        [FromQuery] string? room,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] string? period,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(
            groupId,
            teacherId,
            room,
            dayOfWeek,
            period,
            page,
            pageSize,
            ct
        );
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить запись расписания по ID")]
    [SwaggerResponse(200, "Запись найдена", typeof(Result<ScheduleResponse>))]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ScheduleResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Создать запись расписания")]
    [SwaggerResponse(200, "Запись создана", typeof(Result<ScheduleResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Группа или преподаватель не найдены")]
    [SwaggerResponse(409, "Конфликт — пересечение расписания")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ScheduleResponse>>> Create(
        CreateScheduleRequest request,
        CancellationToken ct
    )
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Обновить запись расписания")]
    [SwaggerResponse(200, "Запись обновлена", typeof(Result<ScheduleResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Запись не найдена")]
    [SwaggerResponse(409, "Конфликт — пересечение расписания")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ScheduleResponse>>> Update(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить запись расписания")]
    [SwaggerResponse(200, "Запись удалена", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Запись не найдена")]
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
}
