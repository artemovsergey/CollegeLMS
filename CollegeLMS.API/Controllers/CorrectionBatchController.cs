using System.Security.Claims;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>
/// Пакеты корректировок: пакеты и позиции, импорт XLSX, экспорт, применение.
/// </summary>
[ApiController]
[Route("api/schedule/correction/batches")]
[Produces("application/json")]
[Authorize(Roles = "Dispatcher,Admin")]
public class CorrectionBatchController(ICorrectionBatchService service) : ControllerBase
{
    /// <summary>Создать пакет корректировки по дате (вычисляет неделю и день недели).</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Создать пакет корректировки")]
    [SwaggerResponse(200, "Пакет создан", typeof(Result<CorrectionBatchResponse>))]
    [SwaggerResponse(400, "Некорректная дата", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBatch(
        CreateCorrectionBatchRequest request,
        CancellationToken ct
    )
    {
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var result = await service.CreateBatchAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Список пакетов корректировок (по умолчанию — подготовленные).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Список пакетов корректировок")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<CorrectionBatchResponse>>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<List<CorrectionBatchResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBatches(
        [FromQuery] CorrectionBatchStatus? status,
        CancellationToken ct
    )
    {
        var result = await service.GetBatchesAsync(status, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Пакет с позициями.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить пакет корректировки")]
    [SwaggerResponse(200, "Пакет получен", typeof(Result<CorrectionBatchResponse>))]
    [SwaggerResponse(404, "Пакет не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionBatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid id, CancellationToken ct)
    {
        var result = await service.GetBatchAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить черновой пакет.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить пакет корректировки")]
    [SwaggerResponse(200, "Пакет удалён")]
    [SwaggerResponse(404, "Пакет не найден", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пакет не в статусе «Подготовлен»", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBatch(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteBatchAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Добавить позицию в пакет.</summary>
    [HttpPost("{id:guid}/positions")]
    [SwaggerOperation(Summary = "Добавить позицию корректировки")]
    [SwaggerResponse(200, "Позиция добавлена", typeof(Result<CorrectionPositionResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Пакет не найден", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пакет не в статусе «Подготовлен»", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionPositionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPosition(
        Guid id,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    )
    {
        var result = await service.AddPositionAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Редактировать позицию.</summary>
    [HttpPut("{id:guid}/positions/{positionId:guid}")]
    [SwaggerOperation(Summary = "Редактировать позицию корректировки")]
    [SwaggerResponse(200, "Позиция обновлена", typeof(Result<CorrectionPositionResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Пакет или позиция не найдены", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пакет не в статусе «Подготовлен»", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionPositionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePosition(
        Guid id,
        Guid positionId,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdatePositionAsync(id, positionId, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить позицию.</summary>
    [HttpDelete("{id:guid}/positions/{positionId:guid}")]
    [SwaggerOperation(Summary = "Удалить позицию корректировки")]
    [SwaggerResponse(200, "Позиция удалена")]
    [SwaggerResponse(404, "Пакет или позиция не найдены", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пакет не в статусе «Подготовлен»", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePosition(Guid id, Guid positionId, CancellationToken ct)
    {
        var result = await service.DeletePositionAsync(id, positionId, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Импорт XLSX: создаёт пакет из файла (или возвращает детальные ошибки).</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Импорт корректировок из XLSX")]
    [SwaggerResponse(
        200,
        "Позиции импортированы или ошибки валидации",
        typeof(Result<CorrectionImportResponse>)
    )]
    [SwaggerResponse(400, "Файл невалиден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionImportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<CorrectionImportResponse>.Fail("Файл не выбран", 400));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(
                Result<CorrectionImportResponse>.Fail("Поддерживается только формат XLSX", 400)
            );

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(
                Result<CorrectionImportResponse>.Fail("Файл слишком большой. Максимум 10MB.", 400)
            );

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        var result = await service.ImportAsync(stream, userId, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Скачать XLSX с текущими позициями пакета.</summary>
    [HttpPost("{id:guid}/export")]
    [SwaggerOperation(Summary = "Экспорт пакета корректировки в XLSX")]
    [SwaggerResponse(200, "Файл XLSX")]
    [SwaggerResponse(404, "Пакет не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        var result = await service.ExportAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>Применить все позиции пакета (с подтверждением на клиенте).</summary>
    [HttpPost("{id:guid}/apply")]
    [SwaggerOperation(Summary = "Применить пакет корректировки")]
    [SwaggerResponse(200, "Корректировки применены", typeof(Result<CorrectionApplyResult>))]
    [SwaggerResponse(400, "Ошибка валидации", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Пакет не найден", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пакет уже применён или отменён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<CorrectionApplyResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(Guid id, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            idempotencyKey = Guid.NewGuid().ToString();

        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var appliedByUserId);

        var result = await service.ApplyAsync(id, idempotencyKey, appliedByUserId, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
