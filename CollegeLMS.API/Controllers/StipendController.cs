using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/stipend")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class StipendController(IStipendService service) : ControllerBase
{
    [HttpPost("generate")]
    [SwaggerOperation(Summary = "Сформировать стипендиальный список")]
    [SwaggerResponse(200, "Список сформирован", typeof(Result<StipendListDetailResponse>))]
    [SwaggerResponse(404, "Семестр не найден")]
    public async Task<ActionResult<Result<StipendListDetailResponse>>> Generate(
        [FromQuery] Guid semesterId,
        CancellationToken ct
    )
    {
        var result = await service.GenerateAsync(semesterId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("lists")]
    [SwaggerOperation(Summary = "Получить список стипендиальных ведомостей")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<StipendListResponse>>))]
    public async Task<ActionResult<Result<List<StipendListResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("lists/{id:guid}")]
    [SwaggerOperation(Summary = "Получить детали ведомости")]
    [SwaggerResponse(200, "Ведомость найдена", typeof(Result<StipendListDetailResponse>))]
    [SwaggerResponse(404, "Ведомость не найдена")]
    public async Task<ActionResult<Result<StipendListDetailResponse>>> GetById(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("lists/{id:guid}")]
    [SwaggerOperation(Summary = "Удалить ведомость")]
    [SwaggerResponse(200, "Ведомость удалена", typeof(Result))]
    [SwaggerResponse(404, "Ведомость не найдена")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
