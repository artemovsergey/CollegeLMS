using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/dispatcher/documents")]
[Produces("application/json")]
[Authorize(Roles = "Dispatcher,Admin")]
public class DocumentsController(IDocumentsService service) : ControllerBase
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
}
