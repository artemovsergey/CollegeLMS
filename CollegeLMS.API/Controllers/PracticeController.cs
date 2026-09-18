using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Практики УП/ПП: управление и импорт из XLSX.</summary>
[ApiController]
[Route("api/practices")]
[Produces("application/json")]
public class PracticeController(IPracticeService service) : ControllerBase
{
    /// <summary>Список практик с фильтрами и пагинацией.</summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Список практик")]
    [SwaggerResponse(200, "Список получен", typeof(Result<PagedResponse<PracticeResponse>>))]
    [ProducesResponseType(typeof(Result<PagedResponse<PracticeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? groupId,
        [FromQuery] Guid? teacherId,
        [FromQuery] PracticeKind? kind,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(
            groupId,
            teacherId,
            kind,
            from,
            to,
            page,
            pageSize,
            ct
        );
        return Ok(result);
    }

    /// <summary>Создать практику.</summary>
    [HttpPost]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Создать практику")]
    [SwaggerResponse(200, "Практика создана", typeof(Result<PracticeResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пересечение практик группы", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(PracticeRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить практику.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Изменить практику")]
    [SwaggerResponse(200, "Практика обновлена", typeof(Result<PracticeResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Практика не найдена", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Пересечение практик группы", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, PracticeRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить практику.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить практику")]
    [SwaggerResponse(200, "Практика удалена")]
    [SwaggerResponse(404, "Практика не найдена", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Превью импорта практик из XLSX.</summary>
    [HttpPost("import/preview")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Превью импорта практик из XLSX")]
    [SwaggerResponse(200, "Превью получено", typeof(Result<PracticeImportPreviewResponse>))]
    [SwaggerResponse(400, "Файл невалиден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeImportPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewImport(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<PracticeImportPreviewResponse>.Fail("Файл не выбран", 400));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(
                Result<PracticeImportPreviewResponse>.Fail("Поддерживается только формат XLSX", 400)
            );

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(
                Result<PracticeImportPreviewResponse>.Fail(
                    "Файл слишком большой. Максимум 10MB.",
                    400
                )
            );

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        var result = await service.PreviewImportAsync(stream, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Подтвердить импорт практик (в транзакции).</summary>
    [HttpPost("import/confirm")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Подтвердить импорт практик")]
    [SwaggerResponse(200, "Практики импортированы", typeof(Result<PracticeImportConfirmResponse>))]
    [SwaggerResponse(400, "Ошибки в строках", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeImportConfirmResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmImport(
        PracticeImportConfirmRequest request,
        CancellationToken ct
    )
    {
        var result = await service.ConfirmImportAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
