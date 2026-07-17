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
            DayOfWeek = (int)entry.DayOfWeek,
            NumberPair = entry.NumberPair,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            Weeks = entry.Weeks,
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
            NumberPair = request.NumberPair,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Weeks = request.Weeks,
            LessonType = Enum.Parse<LessonType>(request.LessonType),
        };
}
