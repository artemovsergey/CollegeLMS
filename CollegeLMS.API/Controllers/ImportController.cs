using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/import")]
[Produces("application/json")]
public class ImportController(
    IWordPressImportService importService,
    IWebHostEnvironment env,
    IConfiguration config
) : ControllerBase
{
    [HttpPost("wordpress")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Импортировать данные из WordPress JSON")]
    [SwaggerResponse(200, "Импорт запущен", typeof(Result<string>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public ActionResult<Result<string>> ImportWordPress()
    {
        var jsonPath = Path.Combine(env.ContentRootPath, "..", "import", "wp_data_full.json");
        if (!System.IO.File.Exists(jsonPath))
            jsonPath = "/import/wp_data_full.json";

        string importId = null!;
        importId = importService.StartImport(async ct =>
        {
            var result = await importService.ImportFromJsonAsync(jsonPath, ct, importId);
            var progress = importService.GetImportProgress(importId);
            if (progress != null && result.IsSuccess)
            {
                progress.Result = result.Data;
            }
        });

        return Ok(Result<string>.Ok(importId));
    }

    [HttpPost("wordpress/rest")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Импортировать данные через WordPress REST API")]
    [SwaggerResponse(200, "Импорт запущен", typeof(Result<string>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(502, "Ошибка подключения к WordPress")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public ActionResult<Result<string>> ImportWordPressRest()
    {
        var baseUrl = config.GetValue<string>("WordPress:BaseUrl") ?? "https://stvcc.ru";

        string importId = null!;
        importId = importService.StartImport(async ct =>
        {
            var result = await importService.ImportFromRestApiAsync(baseUrl, ct, importId);
            var progress = importService.GetImportProgress(importId);
            if (progress != null && result.IsSuccess)
            {
                progress.Result = result.Data;
            }
        });

        return Ok(Result<string>.Ok(importId));
    }

    [HttpPost("wordpress/stop/{importId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Остановить импорт")]
    [SwaggerResponse(200, "Импорт остановлен", typeof(Result<bool>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Импорт не найден")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<Result<bool>> StopImport(string importId)
    {
        var progress = importService.GetImportProgress(importId);
        if (progress == null)
            return NotFound(Result<bool>.Fail("Импорт не найден", 404));

        importService.StopImport(importId);
        return Ok(Result<bool>.Ok(true));
    }

    [HttpGet("wordpress/status/{importId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить статус импорта")]
    [SwaggerResponse(200, "Статус импорта", typeof(Result<ImportProgressDto>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Импорт не найден")]
    [ProducesResponseType(typeof(Result<ImportProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<Result<ImportProgressDto>> GetImportStatus(string importId)
    {
        var progress = importService.GetImportProgress(importId);
        if (progress == null)
            return NotFound(Result<ImportProgressDto>.Fail("Импорт не найден", 404));

        return Ok(Result<ImportProgressDto>.Ok(progress));
    }
}
