using CollegeLMS.API.Dtos;
using CollegeLMS.API.Extensions;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Produces("application/json")]
public class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet("api/teacher/dashboard")]
    [Authorize(Roles = "Teacher")]
    [SwaggerOperation(Summary = "Получить дашборд преподавателя")]
    [SwaggerResponse(200, "Дашборд получен", typeof(Result<TeacherDashboardResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Преподаватель не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<TeacherDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<TeacherDashboardResponse>>> GetTeacherDashboard(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await service.GetTeacherDashboardAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("api/my/dashboard")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "Получить дашборд студента")]
    [SwaggerResponse(200, "Дашборд получен", typeof(Result<StudentDashboardResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Студент не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<StudentDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<StudentDashboardResponse>>> GetStudentDashboard(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await service.GetStudentDashboardAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
