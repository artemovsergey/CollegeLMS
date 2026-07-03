using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class AssignmentMapper
{
    public static AssignmentResponse ToDto(this Assignment assignment) =>
        new()
        {
            Id = assignment.Id,
            CourseId = assignment.CourseId,
            Title = assignment.Title,
            Description = assignment.Description,
            DueDate = assignment.DueDate,
            MaxScore = assignment.MaxScore,
            Order = assignment.Order,
            SubmissionCount = assignment.Submissions?.Count ?? 0,
        };
}
