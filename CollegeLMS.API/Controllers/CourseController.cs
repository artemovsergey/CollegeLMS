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
[Route("api/courses")]
[Authorize]
[Produces("application/json")]
public class CourseController(ICourseService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список курсов")]
    [SwaggerResponse(200, "Список курсов получен", typeof(Result<List<CourseResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<CourseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<CourseResponse>>>> GetAll(
        [FromQuery] Guid? teacherId,
        [FromQuery] Guid? groupId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetAllAsync(teacherId, groupId, userId, role, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить курс по ID")]
    [SwaggerResponse(200, "Курс найден", typeof(Result<CourseResponse>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<CourseResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Создать курс")]
    [SwaggerResponse(200, "Курс создан", typeof(Result<CourseResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Группа не найдена")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<CourseResponse>>> Create(
        CreateCourseRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.CreateAsync(request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Обновить курс")]
    [SwaggerResponse(200, "Курс обновлён", typeof(Result<CourseResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<CourseResponse>>> Update(
        Guid id,
        UpdateCourseRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UpdateAsync(id, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Удалить курс")]
    [SwaggerResponse(200, "Курс удалён", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Курс не найден")]
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

    [HttpPost("{courseId:guid}/groups")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Назначить группы курсу")]
    [SwaggerResponse(200, "Группы назначены", typeof(Result))]
    public async Task<ActionResult<Result>> AssignGroups(
        Guid courseId,
        AssignGroupsRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.AssignGroupsAsync(courseId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{courseId:guid}/groups")]
    [SwaggerOperation(Summary = "Получить группы курса")]
    [SwaggerResponse(200, "Список групп получен", typeof(Result<List<CourseGroupResponse>>))]
    public async Task<ActionResult<Result<List<CourseGroupResponse>>>> GetGroups(
        Guid courseId,
        CancellationToken ct
    )
    {
        var result = await service.GetCourseGroupsAsync(courseId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{courseId:guid}/groups/{groupId:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Отвязать группу от курса")]
    [SwaggerResponse(200, "Группа отвязана", typeof(Result))]
    public async Task<ActionResult<Result>> RemoveGroup(
        Guid courseId,
        Guid groupId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.RemoveGroupAsync(courseId, groupId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}

[ApiController]
[Route("api/my/courses")]
[Authorize]
[Produces("application/json")]
public class MyCourseController(ICourseService service) : ControllerBase
{
    [HttpGet("{id:guid}/progress")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "Получить прогресс по курсу")]
    [SwaggerResponse(200, "Прогресс получен", typeof(Result<CourseProgressResponse>))]
    public async Task<ActionResult<Result<CourseProgressResponse>>> GetProgress(
        Guid id,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.GetProgressAsync(id, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
