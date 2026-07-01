using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Управление пользователями системы.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UserController(IUserService service) : ControllerBase
{
    /// <summary>Получить список всех пользователей.</summary>
    /// <remarks>Возвращает отсортированный по FullName список пользователей. Доступно всем авторизованным пользователям.</remarks>
    /// <response code="200">Список пользователей успешно получен</response>
    /// <response code="401">Пользователь не авторизован</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список пользователей")]
    [SwaggerResponse(200, "Список пользователей получен", typeof(Result<List<UserResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<List<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<List<UserResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }
}
