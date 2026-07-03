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
[Authorize]
[Produces("application/json")]
public class MaterialController(IMaterialService service) : ControllerBase
{
    /// <summary>Загрузить материал в курс.</summary>
    /// <remarks>Преподаватель может загрузить файл в качестве материала курса.
    /// Файл сохраняется на сервере, а информация о нём — в базе данных.</remarks>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="file">Файл для загрузки</param>
    /// <param name="lectureId">Идентификатор лекции (опционально)</param>
    /// <param name="assignmentId">Идентификатор задания (опционально)</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Материал загружен</response>
    /// <response code="400">Файл не выбран</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost("api/courses/{courseId:guid}/materials")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Загрузить материал в курс")]
    [SwaggerResponse(200, "Материал загружен", typeof(Result<MaterialResponse>))]
    [SwaggerResponse(400, "Файл не выбран")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<MaterialResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<MaterialResponse>>> Upload(
        Guid courseId,
        IFormFile file,
        [FromQuery] Guid? lectureId,
        [FromQuery] Guid? assignmentId,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(Result<MaterialResponse>.Fail("Файл не выбран", 400));

        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UploadAsync(
            courseId,
            file,
            lectureId,
            assignmentId,
            userId,
            role,
            ct
        );
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить список материалов курса.</summary>
    /// <param name="courseId">Идентификатор курса</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Список материалов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Курс не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("api/courses/{courseId:guid}/materials")]
    [SwaggerOperation(Summary = "Получить список материалов курса")]
    [SwaggerResponse(200, "Список материалов получен", typeof(Result<List<MaterialResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<MaterialResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<MaterialResponse>>>> GetByCourse(
        Guid courseId,
        CancellationToken ct
    )
    {
        var result = await service.GetByCourseAsync(courseId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Скачать файл материала.</summary>
    /// <param name="id">Идентификатор материала</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Файл скачан</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Материал не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("api/materials/{id:guid}/download")]
    [SwaggerOperation(Summary = "Скачать файл материала")]
    [SwaggerResponse(200, "Файл скачан")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Материал не найден")]
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

    /// <summary>Удалить материал.</summary>
    /// <param name="id">Идентификатор материала</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Материал удалён</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Материал не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpDelete("api/materials/{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Удалить материал")]
    [SwaggerResponse(200, "Материал удалён", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Материал не найден")]
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
