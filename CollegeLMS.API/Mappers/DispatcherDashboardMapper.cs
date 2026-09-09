using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class DispatcherDashboardMapper
{
    public static DispatcherEntry ToDispatcherEntry(this ScheduleEntry e) =>
        new()
        {
            GroupId = e.GroupId,
            GroupName = e.Group?.Name ?? string.Empty,
            Subject = e.Subject,
            Room = e.Room,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            LessonType = e.LessonType.ToString(),
            ChangeType = null,
        };
}
