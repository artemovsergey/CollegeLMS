using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/dispatcher/documents")]
[Produces("application/json")]
[Authorize(Roles = "Dispatcher,Admin")]
public class DocumentsController(
    IDocumentsService service,
    IScheduleMatrixExportService matrixExport
) : ControllerBase
{
    [HttpGet("templates")]
    [SwaggerOperation(Summary = "Получить список шаблонов документов")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<DocumentTemplateResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [ProducesResponseType(typeof(Result<List<DocumentTemplateResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<List<DocumentTemplateResponse>>>> GetTemplates(
        CancellationToken ct
    )
    {
        var result = await service.GetTemplatesAsync(ct);
        return Ok(result);
    }

    [HttpGet("templates/{fileName}/download")]
    [SwaggerOperation(Summary = "Скачать шаблон документа")]
    [SwaggerResponse(200, "Файл скачан")]
    [SwaggerResponse(404, "Шаблон не найден")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName, CancellationToken ct)
    {
        var result = await service.DownloadAsync(fileName, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Выгрузить итоговое расписание в формате файла импорта «Расписание.xlsx».
    /// </summary>
    /// <remarks>
    /// Собирается из `schedule_entries`, то есть уже с учётом применённых корректировок.
    /// Структура совпадает с импортом `Расписание.xlsx` (названия групп в строке 5,
    /// дни в столбце A, номера пар в столбце B), поэтому файл можно загрузить обратно.
    /// Имя — `Расписание_ddMMyy_HHmm.xlsx`.
    /// </remarks>
    /// <response code="200">Файл сформирован</response>
    /// <response code="400">В системе нет групп или расписание не загружено</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    [HttpGet("schedule.xlsx")]
    [SwaggerOperation(Summary = "Скачать итоговое расписание в формате XLSX")]
    [SwaggerResponse(200, "Файл сформирован", typeof(FileResult))]
    [SwaggerResponse(400, "Расписание выгружать нечего", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadSchedule(CancellationToken ct)
    {
        var result = await matrixExport.ExportAsync(ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }
}
