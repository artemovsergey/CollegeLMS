namespace CollegeLMS.API.SwaggerExamples;

public static class ScheduleHistoryResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            changeType = "Replace",
            appliedAt = "2026-09-08T10:15:00Z",
            appliedByUserId = Guid.NewGuid(),
            groupId = Guid.NewGuid(),
            groupName = "ПО-262",
            teacherId = Guid.NewGuid(),
            teacherName = "Марченко И.А.",
            subject = "Математика",
            room = (string?)null,
            dayOfWeek = 2,
            numberPair = 4,
            week = 2,
            note = "вм. 4 п",
            removedSubject = "Физика",
            removedRoom = "303",
            removedNumberPair = 2,
        };
}