using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/specialties")]
[Authorize]
[Produces("application/json")]
public class SpecialtyController(ISpecialtyService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список специальностей")]
    [SwaggerResponse(200, "Список специальностей получен", typeof(Result<List<SpecialtyResponse>>))]
    public async Task<ActionResult<Result<List<SpecialtyResponse>>>> GetAll(
        [FromQuery] string? search,
        CancellationToken ct
    )
    {
        var result = await service.GetAllAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить специальность по ID")]
    [SwaggerResponse(200, "Специальность найдена", typeof(Result<SpecialtyResponse>))]
    [SwaggerResponse(404, "Специальность не найдена")]
    public async Task<ActionResult<Result<SpecialtyResponse>>> GetById(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать специальность")]
    [SwaggerResponse(200, "Специальность создана", typeof(Result<SpecialtyResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(409, "Код уже существует")]
    public async Task<ActionResult<Result<SpecialtyResponse>>> Create(
        CreateSpecialtyRequest request,
        CancellationToken ct
    )
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Обновить специальность")]
    [SwaggerResponse(200, "Специальность обновлена", typeof(Result<SpecialtyResponse>))]
    [SwaggerResponse(404, "Специальность не найдена")]
    public async Task<ActionResult<Result<SpecialtyResponse>>> Update(
        Guid id,
        UpdateSpecialtyRequest request,
        CancellationToken ct
    )
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Удалить специальность")]
    [SwaggerResponse(200, "Специальность удалена", typeof(Result))]
    [SwaggerResponse(404, "Специальность не найдена")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
