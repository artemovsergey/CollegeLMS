using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IUserService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список всех пользователей")]
    [SwaggerResponse(200, "Список пользователей получен")]
    [SwaggerResponse(500, "Ошибка сервера")]
    public async Task<ActionResult<Result<List<UserResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }
}
