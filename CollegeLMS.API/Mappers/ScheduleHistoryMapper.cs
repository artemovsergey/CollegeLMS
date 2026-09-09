using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class ScheduleHistoryMapper
{
    public static ScheduleHistoryResponse ToDto(this ScheduleHistory h) =>
        new()
        {
            Id = h.Id,
            ChangeType = h.ChangeType,
            AppliedAt = h.AppliedAt,
            AppliedByUserId = h.AppliedByUserId,
            GroupId = h.GroupId,
            GroupName = h.Group?.Name ?? string.Empty,
            TeacherId = h.TeacherId,
            TeacherName = h.Teacher?.User?.FullName,
            Subject = h.Subject,
            Room = h.Room,
            DayOfWeek = h.DayOfWeek,
            NumberPair = h.NumberPair,
            Week = h.Week,
            Note = h.Note,
            RemovedSubject = h.RemovedSubject,
            RemovedRoom = h.RemovedRoom,
            RemovedNumberPair = h.RemovedNumberPair,
        };
}
