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
[Route("api/favorites")]
[Produces("application/json")]
[Authorize]
public class FavoritesController(IFavoritesService service) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Список избранного текущего пользователя")]
    [SwaggerResponse(200, "Список получен", typeof(Result<List<FavoriteResponse>>))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<List<FavoriteResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await service.GetAllAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Добавить группу или преподавателя в избранное")]
    [SwaggerResponse(200, "Добавлено", typeof(Result<FavoriteResponse>))]
    [SwaggerResponse(400, "Некорректные данные", typeof(ErrorResponse))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Объект не найден", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<FavoriteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(AddFavoriteRequest request, CancellationToken ct)
    {
        var result = await service.AddAsync(User.GetUserId(), request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Удалить запись избранного")]
    [SwaggerResponse(200, "Удалено", typeof(Result))]
    [SwaggerResponse(401, "Не авторизован", typeof(ErrorResponse))]
    [SwaggerResponse(404, "Не найдено", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(User.GetUserId(), id, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
