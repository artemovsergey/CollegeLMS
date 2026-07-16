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
[Route("api/tests")]
[Authorize]
[Produces("application/json")]
public class TestingController(ITestingService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Получить список тестов")]
    [SwaggerResponse(200, "Список тестов получен", typeof(Result<List<TestResponse>>))]
    [SwaggerResponse(401, "Не авторизован")]
    public async Task<ActionResult<Result<List<TestResponse>>>> GetAll(
        [FromQuery] Guid? courseId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetAllAsync(courseId, userId, role, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Получить тест по ID")]
    [SwaggerResponse(200, "Тест найден", typeof(Result<TestResponse>))]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<TestResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Создать тест")]
    [SwaggerResponse(200, "Тест создан", typeof(Result<TestResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(404, "Курс не найден")]
    public async Task<ActionResult<Result<TestResponse>>> Create(
        CreateTestRequest request,
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
    [SwaggerOperation(Summary = "Обновить тест")]
    [SwaggerResponse(200, "Тест обновлён", typeof(Result<TestResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<TestResponse>>> Update(
        Guid id,
        UpdateTestRequest request,
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
    [SwaggerOperation(Summary = "Удалить тест")]
    [SwaggerResponse(200, "Тест удалён", typeof(Result))]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.DeleteAsync(id, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/questions")]
    [SwaggerOperation(Summary = "Получить вопросы теста")]
    [SwaggerResponse(200, "Список вопросов получен", typeof(Result<List<QuestionResponse>>))]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<List<QuestionResponse>>>> GetQuestions(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetQuestionsAsync(testId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{testId:guid}/questions")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Добавить вопрос в тест")]
    [SwaggerResponse(200, "Вопрос создан", typeof(Result<QuestionResponse>))]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<QuestionResponse>>> CreateQuestion(
        Guid testId,
        CreateQuestionRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.CreateQuestionAsync(testId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{testId:guid}/questions/{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Обновить вопрос")]
    [SwaggerResponse(200, "Вопрос обновлён", typeof(Result<QuestionResponse>))]
    [SwaggerResponse(404, "Тест или вопрос не найден")]
    public async Task<ActionResult<Result<QuestionResponse>>> UpdateQuestion(
        Guid testId,
        Guid id,
        UpdateQuestionRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UpdateQuestionAsync(testId, id, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{testId:guid}/questions/{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Удалить вопрос")]
    [SwaggerResponse(200, "Вопрос удалён", typeof(Result))]
    [SwaggerResponse(404, "Тест или вопрос не найден")]
    public async Task<ActionResult<Result>> DeleteQuestion(
        Guid testId,
        Guid id,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.DeleteQuestionAsync(testId, id, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{testId:guid}/assign")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Назначить тест группе")]
    [SwaggerResponse(200, "Тест назначен", typeof(Result<TestAssignmentResponse>))]
    [SwaggerResponse(404, "Тест или группа не найдены")]
    public async Task<ActionResult<Result<TestAssignmentResponse>>> Assign(
        Guid testId,
        AssignTestRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.AssignTestAsync(testId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/assignments")]
    [SwaggerOperation(Summary = "Получить список назначений теста")]
    [SwaggerResponse(
        200,
        "Список назначений получен",
        typeof(Result<List<TestAssignmentResponse>>)
    )]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<List<TestAssignmentResponse>>>> GetAssignments(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetAssignmentsAsync(testId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{testId:guid}/assign/{assignmentId:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Отменить назначение теста")]
    [SwaggerResponse(200, "Назначение отменено", typeof(Result))]
    [SwaggerResponse(404, "Назначение не найдено")]
    public async Task<ActionResult<Result>> RemoveAssignment(
        Guid testId,
        Guid assignmentId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.RemoveAssignmentAsync(testId, assignmentId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPut("{testId:guid}/settings")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Настроить параметры проверки теста")]
    [SwaggerResponse(200, "Настройки обновлены", typeof(Result<TestResponse>))]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<TestResponse>>> UpdateSettings(
        Guid testId,
        TestSettingsRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.UpdateSettingsAsync(testId, request, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/start")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "Начать тест")]
    [SwaggerResponse(200, "Тест начат", typeof(Result<StartTestResponse>))]
    [SwaggerResponse(400, "Тест не содержит вопросов")]
    [SwaggerResponse(403, "Тест не доступен")]
    [SwaggerResponse(404, "Тест не найден")]
    [SwaggerResponse(409, "Превышено количество попыток")]
    public async Task<ActionResult<Result<StartTestResponse>>> StartTest(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.StartTestAsync(testId, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{testId:guid}/attempt/{attemptId:guid}/submit")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "Отправить ответы на тест")]
    [SwaggerResponse(200, "Ответы приняты", typeof(Result<AttemptResponse>))]
    [SwaggerResponse(400, "Попытка уже завершена")]
    [SwaggerResponse(403, "Не ваша попытка")]
    [SwaggerResponse(404, "Попытка не найдена")]
    [SwaggerResponse(408, "Время вышло")]
    public async Task<ActionResult<Result<AttemptResponse>>> SubmitAnswers(
        Guid testId,
        Guid attemptId,
        SubmitAnswersRequest request,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.SubmitAnswersAsync(testId, attemptId, request, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/attempts")]
    [Authorize(Roles = "Student")]
    [SwaggerOperation(Summary = "Получить мои попытки по тесту")]
    [SwaggerResponse(200, "Список попыток получен", typeof(Result<List<AttemptResponse>>))]
    public async Task<ActionResult<Result<List<AttemptResponse>>>> GetMyAttempts(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.GetMyAttemptsAsync(testId, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/results")]
    [SwaggerOperation(Summary = "Получить результат теста для текущего студента")]
    [SwaggerResponse(200, "Результат получен", typeof(Result<TestResultResponse>))]
    [SwaggerResponse(404, "Результаты не найдены")]
    public async Task<ActionResult<Result<TestResultResponse>>> GetMyResult(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.GetMyResultAsync(testId, userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("~/api/my/test-results")]
    [SwaggerOperation(Summary = "Получить все результаты студента")]
    [SwaggerResponse(200, "Результаты получены", typeof(Result<List<AttemptResponse>>))]
    public async Task<ActionResult<Result<List<AttemptResponse>>>> GetAllMyResults(
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var result = await service.GetAllMyResultsAsync(userId, ct);
        return Ok(result);
    }

    [HttpGet("{testId:guid}/stats")]
    [Authorize(Roles = "Admin,Teacher")]
    [SwaggerOperation(Summary = "Получить статистику по тесту")]
    [SwaggerResponse(200, "Статистика получена", typeof(Result<TestStatsResponse>))]
    [SwaggerResponse(403, "Доступ запрещён")]
    [SwaggerResponse(404, "Тест не найден")]
    public async Task<ActionResult<Result<TestStatsResponse>>> GetStats(
        Guid testId,
        CancellationToken ct
    )
    {
        var userId = User.GetUserId();
        var role = User.GetRole();
        var result = await service.GetStatsAsync(testId, userId, role, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
