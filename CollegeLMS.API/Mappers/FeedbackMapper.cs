using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class FeedbackMapper
{
    public static Feedback ToEntity(this FeedbackRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Message = request.Message,
        };

    public static FeedbackListItemDto ToListItemDto(this Feedback entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            Message = entity.Message,
            CreatedAt = entity.CreatedAt,
        };
}
