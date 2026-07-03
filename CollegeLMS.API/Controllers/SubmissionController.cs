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
public class SubmissionController(ISubmissionService service) : ControllerBase
{
    /// <summary>Отправить ответ на задание.</summary>
    /// <remarks>Студент может отправить файл в качестве ответа на задание курса, на который он зачислен.
    /// Если ответ уже существует, он будет обновлён.</remarks>
    /// <param name="assignmentId">Идентификатор задания</param>
    /// <param name="request">Данные для отправки</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Ответ отправлен</response>
    /// <response code="400">Ошибка валидации</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён (студент не зачислен)</response>
    /// <response code="404">Задание не найдено</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPost("api/assignments/{assignmentId:guid}/submit")]
    [Authorize(Roles = "Admin,Student")]
    [SwaggerOperation(Summary = "Отправить ответ на задание")]
    [SwaggerResponse(200, "Ответ отправлен", typeof(Result<SubmissionResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Задание не найдено")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<SubmissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<SubmissionResponse>>> Submit(Guid assignmentId, SubmitAssignmentRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await service.SubmitAsync(assignmentId, request, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить список ответов на задание.</summary>
    /// <remarks>Преподаватель может просматривать все ответы студентов на задание своего курса.</remarks>
    /// <param name="assignmentId">Идентификатор задания</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Список ответов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Задание не найдено</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("api/assignments/{assignmentId:guid}/submissions")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Получить список ответов на задание")]
    [SwaggerResponse(200, "Список ответов получен", typeof(Result<List<SubmissionResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Задание не найдено")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<SubmissionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<SubmissionResponse>>>> GetSubmissions(Guid assignmentId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetSubmissionsAsync(assignmentId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Оценить ответ студента.</summary>
    /// <remarks>Преподаватель может выставить оценку за ответ студента.
    /// Оценка не может превышать максимальный балл, указанный в задании.</remarks>
    /// <param name="submissionId">Идентификатор ответа</param>
    /// <param name="request">Данные для оценки</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Ответ оценён</response>
    /// <response code="400">Оценка превышает максимальный балл</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="403">Доступ запрещён</response>
    /// <response code="404">Ответ не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpPatch("api/submissions/{submissionId:guid}/grade")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Оценить ответ студента")]
    [SwaggerResponse(200, "Ответ оценён", typeof(Result<SubmissionResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Ответ не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<SubmissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<SubmissionResponse>>> Grade(Guid submissionId, GradeSubmissionRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GradeAsync(submissionId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>Получить мои ответы.</summary>
    /// <remarks>Студент может просматривать свои отправленные ответы на все задания.</remarks>
    /// <response code="200">Список ответов получен</response>
    /// <response code="401">Не авторизован</response>
    /// <response code="404">Студент не найден</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet("api/my/submissions")]
    [Authorize(Roles = "Admin,Student")]
    [SwaggerOperation(Summary = "Получить мои ответы")]
    [SwaggerResponse(200, "Список ответов получен", typeof(Result<List<SubmissionResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Студент не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<SubmissionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<SubmissionResponse>>>> GetMySubmissions(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await service.GetMySubmissionsAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
