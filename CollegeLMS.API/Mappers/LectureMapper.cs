using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class LectureMapper
{
    public static LectureResponse ToDto(this Lecture lecture) => new()
    {
        Id = lecture.Id,
        CourseId = lecture.CourseId,
        Title = lecture.Title,
        Content = lecture.Content,
        Order = lecture.Order,
    };
}
