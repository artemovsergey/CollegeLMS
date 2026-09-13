using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class FavoriteMapper
{
    public static FavoriteResponse ToDto(this Favorite favorite) =>
        new()
        {
            Id = favorite.Id,
            TargetType = favorite.TargetType,
            TargetId = favorite.TargetId,
        };
}