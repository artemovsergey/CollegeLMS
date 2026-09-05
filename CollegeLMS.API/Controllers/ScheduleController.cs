using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/schedule")]
[Produces("application/json")]
public class ScheduleController(IScheduleService service, ScheduleImportService importService) : ControllerBase
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
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? teacherId,
        [FromQuery] string? room,
        [FromQuery] DayOfWeek? dayOfWeek,
        [FromQuery] string? period,
        [FromQuery] int? week,
        [FromQuery] string? view,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        if (view == "calendar")
        {
            var calendarResult = await service.GetCalendarAsync(groupId, teacherId, room, ct);
            return Ok(calendarResult);
        }

        var result = await service.GetAllAsync(
            groupId,
            teacherId,
            room,
            dayOfWeek,
            period,
            week,
            view,
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

    [HttpGet("export")]
    [Authorize(Roles = "Admin,Teacher,Student,Dispatcher")]
    [SwaggerOperation(Summary = "Экспорт расписания в PDF или Excel")]
    [SwaggerResponse(200, "Файл готов к скачиванию")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Нет данных")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? teacherId,
        [FromQuery] string? room,
        [FromQuery] string? period,
        [FromQuery] string format = "pdf",
        [FromQuery] string layout = "grid",
        CancellationToken ct = default
    )
    {
        var fmt = format.ToLower() == "xlsx" ? ExportFormat.Xlsx : ExportFormat.Pdf;
        var lyt = layout.ToLower() == "daycards" ? ExportLayout.DayCards : ExportLayout.Grid;
        var result = await service.ExportScheduleAsync(groupId, teacherId, room, period, fmt, lyt, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return File(result.Data!.FileContent, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("import/preview")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Превью импорта расписания из XLSX")]
    [SwaggerResponse(200, "Превью получено", typeof(Result<SchedulePreviewResponse>))]
    [SwaggerResponse(400, "Ошибка валидации файла")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<SchedulePreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SchedulePreviewResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PreviewImport(
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<SchedulePreviewResponse>.Fail("Файл не выбран", 400));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(Result<SchedulePreviewResponse>.Fail(
                "Поддерживается только формат XLSX", 400));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(Result<SchedulePreviewResponse>.Fail(
                "Файл слишком большой. Максимум 10MB.", 400));

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        var result = await importService.PreviewAsync(stream, ct);
        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(Result<SchedulePreviewResponse>.Ok(result.Preview!));
    }

    [HttpPost("import/confirm")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Подтвердить импорт расписания")]
    [SwaggerResponse(200, "Импорт выполнен", typeof(Result<ConfirmResult>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ConfirmResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmImport(
        ConfirmImportRequest request,
        CancellationToken ct
    )
    {
        var result = await importService.ConfirmAsync(request, ct);
        return Ok(Result<ConfirmResult>.Ok(result));
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
