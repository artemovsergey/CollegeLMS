using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Mappers;

public static class ScheduleMapper
{
    public static ScheduleResponse ToDto(this ScheduleEntry entry) =>
        new()
        {
            Id = entry.Id,
            GroupId = entry.GroupId,
            GroupName = entry.Group?.Name ?? string.Empty,
            TeacherId = entry.TeacherId,
            TeacherName = entry.Teacher?.User?.FullName,
            Subject = entry.Subject,
            Room = entry.Room,
            DayOfWeek = entry.DayOfWeek,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            LessonType = entry.LessonType.ToString(),
        };

    public static ScheduleEntry ToEntity(this CreateScheduleRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            TeacherId = request.TeacherId,
            Subject = request.Subject,
            Room = request.Room,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            LessonType = Enum.Parse<LessonType>(request.LessonType),
        };
}
