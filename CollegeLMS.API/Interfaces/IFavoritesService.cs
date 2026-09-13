using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IFavoritesService
{
    Task<Result<List<FavoriteResponse>>> GetAllAsync(Guid userId, CancellationToken ct);
    Task<Result<FavoriteResponse>> AddAsync(
        Guid userId,
        AddFavoriteRequest request,
        CancellationToken ct
    );
    Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}