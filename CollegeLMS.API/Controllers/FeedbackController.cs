using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.SwaggerExamples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

[ApiController]
[Route("api/feedback")]
[Produces("application/json")]
public class FeedbackController(IFeedbackService service) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Отправить сообщение обратной связи")]
    [SwaggerResponse(200, "Сообщение отправлено", typeof(Result<FeedbackResponse>))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(429, "Слишком частые запросы")]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<FeedbackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<FeedbackResponse>>> Create(
        FeedbackRequest request,
        CancellationToken ct
    )
    {
        var result = await service.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
