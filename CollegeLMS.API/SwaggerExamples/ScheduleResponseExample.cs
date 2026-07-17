namespace CollegeLMS.API.SwaggerExamples;

public static class ScheduleResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            groupId = Guid.NewGuid(),
            groupName = "ИСП-31",
            teacherId = Guid.NewGuid(),
            teacherName = "Иванов Иван Иванович",
            subject = "Основы программирования",
            room = "301",
            dayOfWeek = 1,
            numberPair = 1,
            startTime = "08:30:00",
            endTime = "10:00:00",
            weeks = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
            lessonType = "Lecture",
        };
}
