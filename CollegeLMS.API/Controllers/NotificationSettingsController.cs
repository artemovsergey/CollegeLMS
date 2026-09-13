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
[Route("api/notifications/settings")]
[Produces("application/json")]
[Authorize]
public class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Настройки уведомлений текущего пользователя")]
    [SwaggerResponse(200, "Настройки получены", typeof(Result<NotificationSettingsResponse>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NotificationSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await service.GetAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPut]
    [SwaggerOperation(Summary = "Обновить настройки уведомлений")]
    [SwaggerResponse(200, "Настройки обновлены", typeof(Result<NotificationSettingsResponse>))]
    [SwaggerResponse(400, "Некорректные настройки", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NotificationSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        UpdateNotificationSettingsRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(User.GetUserId(), request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
