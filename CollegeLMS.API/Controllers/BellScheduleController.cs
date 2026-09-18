using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Справочник времени пар (звонков) и большая перемена.</summary>
[ApiController]
[Route("api/bells")]
[Produces("application/json")]
public class BellScheduleController(IBellScheduleService service) : ControllerBase
{
    /// <summary>Справочник звонков.</summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Получить справочник звонков")]
    [SwaggerResponse(200, "Справочник получен", typeof(Result<BellScheduleResponse>))]
    [ProducesResponseType(typeof(Result<BellScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await service.GetAsync(ct);
        return Ok(result);
    }

    /// <summary>Заменить справочник звонков целиком.</summary>
    [HttpPut]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Заменить справочник звонков")]
    [SwaggerResponse(200, "Справочник сохранён", typeof(Result<BellScheduleResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<BellScheduleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(UpdateBellScheduleRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
