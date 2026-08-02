using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>
/// Проверка доступности внешних источников данных.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController(IStvccHealthService stvccHealthService) : ControllerBase
{
    /// <summary>
    /// Проверить доступность источника данных stvcc.ru.
    /// </summary>
    /// <remarks>
    /// Временный эндпоинт для разработки: фронтенд показывает плашку,
    /// когда stvcc.ru недоступен. Убрать после перехода с stvcc.ru.
    /// </remarks>
    /// <response code="200">Результат проверки доступности</response>
    [HttpGet("stvcc")]
    [SwaggerOperation(Summary = "Проверить доступность stvcc.ru")]
    [SwaggerResponse(200, "Результат проверки доступности", typeof(Result<StvccHealthDto>))]
    [ProducesResponseType(typeof(Result<StvccHealthDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<StvccHealthDto>>> CheckStvcc(CancellationToken ct)
    {
        return Ok(await stvccHealthService.CheckAsync(ct));
    }
}
