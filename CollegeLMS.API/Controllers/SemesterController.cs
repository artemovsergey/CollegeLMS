using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/semesters")]
[Authorize]
[Produces("application/json")]
public class SemesterController(ISemesterService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список семестров")]
    [SwaggerResponse(200, "Список семестров получен", typeof(Result<List<SemesterResponse>>))]
    public async Task<ActionResult<Result<List<SemesterResponse>>>> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить семестр по ID")]
    [SwaggerResponse(200, "Семестр найден", typeof(Result<SemesterResponse>))]
    [SwaggerResponse(404, "Семестр не найден")]
    public async Task<ActionResult<Result<SemesterResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать семестр")]
    [SwaggerResponse(200, "Семестр создан", typeof(Result<SemesterResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    public async Task<ActionResult<Result<SemesterResponse>>> Create(
        CreateSemesterRequest request,
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
    [SwaggerOperation(Summary = "Обновить семестр")]
    [SwaggerResponse(200, "Семестр обновлён", typeof(Result<SemesterResponse>))]
    [SwaggerResponse(404, "Семестр не найден")]
    public async Task<ActionResult<Result<SemesterResponse>>> Update(
        Guid id,
        UpdateSemesterRequest request,
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
    [SwaggerOperation(Summary = "Удалить семестр")]
    [SwaggerResponse(200, "Семестр удалён", typeof(Result))]
    [SwaggerResponse(404, "Семестр не найден")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
