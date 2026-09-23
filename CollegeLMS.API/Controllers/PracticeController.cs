using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Практики УП/ПП: управление и импорт графика УП из DOCX.</summary>
[ApiController]
[Route("api/practices")]
[Produces("application/json")]
public class PracticeController(IPracticeService service, IPracticeGraphService graphService)
    : ControllerBase
{
    /// <summary>Список практик с фильтрами и пагинацией.</summary>
    /// <remarks>
    /// Возвращает постраничный список практик. Фильтр по преподавателю выполняется по связи
    /// «практика — преподаватели» (многие-ко-многим). Ответ содержит название практики, список
    /// преподавателей и дни УП с номерами пар.
    /// </remarks>
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
    /// <remarks>
    /// Валидации (400): название не пустое и не длиннее 100 символов; вид определён (УП/ПП);
    /// период задан, дата начала не позже окончания, период в пределах семестра; группа найдена;
    /// минимум один существующий преподаватель; для УП список дней непуст, даты внутри периода,
    /// номера пар 1–8 без дублей. Пересечение периода практик одной группы → 409.
    /// </remarks>
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
    /// <remarks>
    /// Правила такие же, как при создании. Преподаватели и дни УП заменяются целиком;
    /// для ПП список дней игнорируется. 404 — практика не найдена; 409 — пересечение периода.
    /// </remarks>
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

    /// <summary>Превью импорта графика УП из DOCX.</summary>
    /// <remarks>
    /// Разбирает документ «График проведения занятий»: шапку (период, тему, группу) и таблицу
    /// подгрупп (номер подгруппы, тема, кабинет, даты с номерами пар, преподаватель).
    /// </remarks>
    [HttpPost("import/graph/preview")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Превью импорта графика УП из DOCX")]
    [SwaggerResponse(200, "Превью получено", typeof(Result<PracticeGraphPreviewResponse>))]
    [SwaggerResponse(400, "Файл невалиден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeGraphPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewGraphImport(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<PracticeGraphPreviewResponse>.Fail("Файл не выбран", 400));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".docx")
            return BadRequest(
                Result<PracticeGraphPreviewResponse>.Fail("Поддерживается только формат DOCX", 400)
            );

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(
                Result<PracticeGraphPreviewResponse>.Fail(
                    "Файл слишком большой. Максимум 10MB.",
                    400
                )
            );

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        var result = await graphService.PreviewImportAsync(stream, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Подтвердить импорт графика УП (в транзакции).</summary>
    /// <remarks>
    /// Создаёт по одной практике УП на каждую подгруппу: общие тема, группа и период,
    /// свои кабинет, номер подгруппы, преподаватель и дни с номерами пар.
    /// </remarks>
    [HttpPost("import/graph/confirm")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Подтвердить импорт графика УП")]
    [SwaggerResponse(200, "Практики импортированы", typeof(Result<PracticeGraphConfirmResponse>))]
    [SwaggerResponse(400, "Ошибки в данных", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<PracticeGraphConfirmResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmGraphImport(
        PracticeGraphConfirmRequest request,
        CancellationToken ct
    )
    {
        var result = await graphService.ConfirmImportAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Экспорт графика УП группы в DOCX.</summary>
    /// <remarks>
    /// Формирует документ по шаблону: одна строка таблицы на каждую практику УП группы
    /// (подгруппа, тема, кабинет, даты с номерами пар, преподаватель).
    /// </remarks>
    [HttpPost("graph/export")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Экспорт графика УП группы в DOCX")]
    [SwaggerResponse(200, "Файл сформирован", typeof(FileResult))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Практики или шаблон не найдены", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportGraph(
        PracticeGraphExportRequest request,
        CancellationToken ct
    )
    {
        var result = await graphService.ExportAsync(request.GroupId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        var data = result.Data!;
        return File(data.Content, data.ContentType, data.FileName);
    }
}
