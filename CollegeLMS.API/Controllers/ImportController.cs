using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/import")]
[Produces("application/json")]
public class ImportController(IWordPressImportService importService, IWebHostEnvironment env)
    : ControllerBase
{
    [HttpPost("wordpress")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Импортировать данные из WordPress JSON")]
    [SwaggerResponse(200, "Импорт завершён", typeof(Result<ImportResult>))]
    [SwaggerResponse(400, "Файл не найден")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<ImportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ImportResult>>> ImportWordPress(CancellationToken ct)
    {
        var jsonPath = Path.Combine(
            env.ContentRootPath,
            "..",
            "import",
            "wp_data_full.json"
        );
        // Fallback: try /import/ inside Docker container
        if (!System.IO.File.Exists(jsonPath))
        {
            jsonPath = "/import/wp_data_full.json";
        }
        var result = await importService.ImportFromJsonAsync(jsonPath, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
