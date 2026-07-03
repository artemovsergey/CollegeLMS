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
            GroupId = course.GroupId,
            GroupName = course.Group?.Name ?? string.Empty,
            Status = course.Status.ToString(),
            LectureCount = course.Lectures?.Count ?? 0,
            AssignmentCount = course.Assignments?.Count ?? 0,
        };
}
