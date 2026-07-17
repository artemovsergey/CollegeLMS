using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class CourseMapper
{
    public static CourseResponse ToDto(this Course course) =>
        new()
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
            TeacherName = course.Teacher?.User?.FullName ?? string.Empty,
            GroupNames = string.Join(", ", course.CourseGroups?.Select(cg => cg.Group?.Name ?? "") ?? []),
            Status = course.Status.ToString(),
            LectureCount = course.Lectures?.Count ?? 0,
            AssignmentCount = course.Assignments?.Count ?? 0,
        };
}
