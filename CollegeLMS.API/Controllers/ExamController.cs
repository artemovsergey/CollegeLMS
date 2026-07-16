using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
[Produces("application/json")]
public class ExamController(IExamService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить список экзаменов")]
    [SwaggerResponse(200, "Список экзаменов получен", typeof(Result<List<ExamResponse>>))]
    public async Task<ActionResult<Result<List<ExamResponse>>>> GetAll(
        [FromQuery] Guid? groupId, [FromQuery] Guid? semesterId, [FromQuery] string? type,
        CancellationToken ct)
    {
        var result = await service.GetAllAsync(groupId, semesterId, type, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить экзамен по ID")]
    [SwaggerResponse(200, "Экзамен найден", typeof(Result<ExamResponse>))]
    [SwaggerResponse(404, "Экзамен не найден")]
    public async Task<ActionResult<Result<ExamResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Создать экзамен")]
    [SwaggerResponse(200, "Экзамен создан", typeof(Result<ExamResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    public async Task<ActionResult<Result<ExamResponse>>> Create(CreateExamRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Обновить экзамен")]
    [SwaggerResponse(200, "Экзамен обновлён", typeof(Result<ExamResponse>))]
    [SwaggerResponse(404, "Экзамен не найден")]
    public async Task<ActionResult<Result<ExamResponse>>> Update(Guid id, UpdateExamRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Удалить экзамен")]
    [SwaggerResponse(200, "Экзамен удалён", typeof(Result))]
    [SwaggerResponse(404, "Экзамен не найден")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{examId:guid}/retakes")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Назначить пересдачу")]
    [SwaggerResponse(200, "Пересдача назначена", typeof(Result<RetakeResponse>))]
    [SwaggerResponse(404, "Экзамен или студент не найден")]
    public async Task<ActionResult<Result<RetakeResponse>>> CreateRetake(
        Guid examId, CreateRetakeRequest request, CancellationToken ct)
    {
        var result = await service.CreateRetakeAsync(examId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{examId:guid}/retakes")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Получить список пересдач")]
    [SwaggerResponse(200, "Список пересдач получен", typeof(Result<List<RetakeResponse>>))]
    public async Task<ActionResult<Result<List<RetakeResponse>>>> GetRetakes(Guid examId, CancellationToken ct)
    {
        var result = await service.GetRetakesAsync(examId, ct);
        return Ok(result);
    }

    [HttpPatch("{examId:guid}/retakes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Изменить статус пересдачи")]
    [SwaggerResponse(200, "Статус изменён", typeof(Result<RetakeResponse>))]
    [SwaggerResponse(404, "Пересдача не найдена")]
    public async Task<ActionResult<Result<RetakeResponse>>> UpdateRetakeStatus(
        Guid examId, Guid id, UpdateRetakeStatusRequest request, CancellationToken ct)
    {
        var result = await service.UpdateRetakeStatusAsync(examId, id, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{examId:guid}/retakes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Отменить пересдачу")]
    [SwaggerResponse(200, "Пересдача отменена", typeof(Result))]
    [SwaggerResponse(404, "Пересдача не найдена")]
    public async Task<ActionResult<Result>> DeleteRetake(Guid examId, Guid id, CancellationToken ct)
    {
        var result = await service.DeleteRetakeAsync(examId, id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
