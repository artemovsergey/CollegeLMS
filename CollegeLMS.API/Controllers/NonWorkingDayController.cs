using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Нерабочие даты (праздники и выходные).</summary>
[ApiController]
[Route("api/non-working-days")]
[Produces("application/json")]
[Authorize]
public class NonWorkingDayController(INonWorkingDayService service) : ControllerBase
{
    /// <summary>Список нерабочих дат с фильтром по периоду и пагинацией.</summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Список нерабочих дат")]
    [SwaggerResponse(200, "Список получен", typeof(Result<PagedResponse<NonWorkingDayResponse>>))]
    [ProducesResponseType(
        typeof(Result<PagedResponse<NonWorkingDayResponse>>),
        StatusCodes.Status200OK
    )]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(from, to, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Добавить нерабочую дату.</summary>
    [HttpPost]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Добавить нерабочую дату")]
    [SwaggerResponse(200, "Дата добавлена", typeof(Result<NonWorkingDayResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(403, "Доступ запрещён", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NonWorkingDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(NonWorkingDayRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Изменить нерабочую дату.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Изменить нерабочую дату")]
    [SwaggerResponse(200, "Дата обновлена", typeof(Result<NonWorkingDayResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Дата не найдена", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<NonWorkingDayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        NonWorkingDayRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить нерабочую дату.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [SwaggerOperation(Summary = "Удалить нерабочую дату")]
    [SwaggerResponse(200, "Дата удалена")]
    [SwaggerResponse(404, "Дата не найдена", typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
