using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/documents")]
[Authorize]
[Produces("application/json")]
public class CourseDocumentController(ICourseDocumentService service) : ControllerBase
{
    /// <summary>Загрузить документ в курс.</summary>
    /// <remarks>Преподаватель может загрузить документ в курс.
    /// Файл сохраняется на сервере, а информация о нём — в базе данных.</remarks>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="file">Файл для загрузки</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Документ загружен</response>
    /// <response code="400">Файл не выбран</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Загрузить документ в курс")]
    [SwaggerResponse(200, "Документ загружен", typeof(Result<CourseDocumentResponse>))]
    [SwaggerResponse(400, "Файл не выбран")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<CourseDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50L * 1024 * 1024)]
    public async Task<ActionResult<Result<CourseDocumentResponse>>> Upload(
        Guid courseId,
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result<CourseDocumentResponse>.Fail("Файл не выбран", 400));

        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UploadAsync(courseId, file, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить список документов курса.</summary>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Список документов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список документов курса")]
    [SwaggerResponse(
        200,
        "Список документов получен",
        typeof(Result<List<CourseDocumentResponse>>)
    )]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<CourseDocumentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<CourseDocumentResponse>>>> GetAll(
        Guid courseId,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(courseId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Скачать файл документа.</summary>
    /// <param name="id">Идентификатор документа</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Файл скачан</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Документ не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("{id:guid}/download")]
    [SwaggerOperation(Summary = "Скачать файл документа")]
    [SwaggerResponse(200, "Файл скачан")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Документ не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        var (stream, fileName, mimeType) = result.Data!;
        return File(stream, mimeType, fileName);
    }

    /// <summary>Удалить документ.</summary>
    /// <param name="id">Идентификатор документа</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Документ удалён</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Документ не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Удалить документ")]
    [SwaggerResponse(200, "Документ удалён", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Документ не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.DeleteAsync(id, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
