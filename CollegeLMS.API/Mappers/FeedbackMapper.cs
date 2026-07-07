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
}
