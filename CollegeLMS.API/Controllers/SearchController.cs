using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Полнотекстовый поиск по сайту.</summary>
[ApiController]
[Route("api/search")]
[Produces("application/json")]
[EnableRateLimiting("SearchPolicy")]
public class SearchController(ISearchService service) : ControllerBase
{
    /// <summary>Поиск по новостям и статическим страницам.</summary>
    /// <remarks>Ищет совпадения в заголовках и содержимом новостей, а также в названиях статических страниц сайта.</remarks>
    /// <param name="q">Поисковый запрос</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="ct">Токен отмены</param>
    /// <response code="200">Результаты поиска</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Поиск по сайту")]
    [SwaggerResponse(200, "Результаты поиска", typeof(Result<PagedResponse<SearchResponse>>))]
    [SwaggerResponse(500, "Ошибка сервера")]
    [ProducesResponseType(typeof(Result<PagedResponse<SearchResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<PagedResponse<SearchResponse>>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        var result = await service.SearchAsync(q, page, pageSize, ct);
        return Ok(result);
    }
}
