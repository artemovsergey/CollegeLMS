using System.Security.Claims;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>
/// Корректировка расписания: превью изменений из файла, применение и история.
/// </summary>
[ApiController]
[Route("api/schedule")]
[Produces("application/json")]
[Authorize(Roles = "Dispatcher,Admin")]
public class ScheduleCorrectionController(IScheduleCorrectionService service) : ControllerBase
{
    /// <summary>
    /// Превью корректировок из файла «Корректировка.xlsx».
    /// </summary>
    /// <remarks>
    /// Принимает multipart/form-data файл. Возвращает разобранные операции
    /// (добавление, снятие, замена) и ошибки трёх уровней: structure, data, logic.
    /// </remarks>
    /// <response code="200">Превью корректировок получено</response>
    /// <response code="400">Файл невалиден или содержит ошибки</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost("correction/preview")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Превью корректировок расписания из XLSX")]
    [SwaggerResponse(200, "Превью получено", typeof(Result<CorrectionPreviewResponse>))]
    [SwaggerResponse(400, "Ошибка валидации файла", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PreviewCorrection(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<CorrectionPreviewResponse>.Fail("Файл не выбран", 400));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(
                Result<CorrectionPreviewResponse>.Fail("Поддерживается только формат XLSX", 400)
            );

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(
                Result<CorrectionPreviewResponse>.Fail("Файл слишком большой. Максимум 10MB.", 400)
            );

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        var result = await service.PreviewAsync(stream, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("correction/export")]
    public async Task<IActionResult> ExportManualCorrection(
        ManualCorrectionExportRequest request,
        CancellationToken ct
    )
    {
        var result = await service.ExportManualAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Применить корректировки расписания.
    /// </summary>
    /// <remarks>
    /// Применяет список операций из превью в одной транзакции
    /// и сохраняет историю изменений. После фиксации уведомляет бота Max (fail-safe).
    /// </remarks>
    /// <response code="200">Корректировки применены</response>
    /// <response code="400">Некорректные данные для применения</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Группа или занятие не найдены</response>
    /// <response code="409">Ключ идемпотентности уже использован</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost("correction/confirm")]
    [SwaggerOperation(Summary = "Подтвердить корректировки расписания")]
    [SwaggerResponse(200, "Корректировки применены", typeof(Result<CorrectionConfirmResult>))]
    [SwaggerResponse(400, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Группа или занятие не найдены", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Ключ идемпотентности уже использован", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionConfirmResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmCorrection(
        CorrectionConfirmRequest request,
        CancellationToken ct
    )
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(
                Result<CorrectionConfirmResult>.Fail("Заголовок Idempotency-Key обязателен.", 400)
            );

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var appliedByUserId);

        var result = await service.ConfirmAsync(request, idempotencyKey, appliedByUserId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// История корректировок расписания с пагинацией и фильтрами.
    /// </summary>
    /// <response code="200">История получена</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("history")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "История корректировок расписания")]
    [SwaggerResponse(
        200,
        "История получена",
        typeof(Result<PagedResponse<ScheduleHistoryResponse>>)
    )]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(
        typeof(Result<PagedResponse<ScheduleHistoryResponse>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? teacherId,
        [FromQuery] int? week,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ScheduleChangeType? changeType,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        var result = await service.GetHistoryAsync(
            groupId,
            teacherId,
            week,
            date,
            from,
            to,
            changeType,
            page,
            pageSize,
            ct
        );
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    /// <summary>
    /// Расписание группы на дату для флоу корректировки.
    /// </summary>
    /// <remarks>
    /// Возвращает эффективные пары дня: базовое расписание, применённые изменения
    /// и неприменённые позиции пакета (если передан batchId).
    /// </remarks>
    /// <response code="200">Расписание дня получено</response>
    /// <response code="404">Группа не найдена</response>
    [HttpGet("correction/day")]
    [SwaggerOperation(Summary = "Расписание группы на дату для корректировки")]
    [SwaggerResponse(200, "Расписание дня получено", typeof(Result<CorrectionDayResponse>))]
    [SwaggerResponse(404, "Группа не найдена", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCorrectionDay(
        [FromQuery] Guid groupId,
        [FromQuery] DateTime date,
        [FromQuery] Guid? batchId,
        CancellationToken ct
    )
    {
        var result = await service.GetDayAsync(groupId, date, batchId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }
}
