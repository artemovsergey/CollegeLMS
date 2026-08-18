using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class LessonMapper
{
    public static LessonResponse ToDto(this Lesson lesson) =>
        new()
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Content = lesson.Content,
            Order = lesson.Order,
            Kind = lesson.Kind.ToString(),
            IsCurrent = lesson.IsCurrent,
            TestId = lesson.TestId,
            TestTitle = lesson.Test?.Title,
        };
}
