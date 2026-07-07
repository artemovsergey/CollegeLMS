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
            startTime = "10:00:00",
            endTime = "11:30:00",
            lessonType = "Lecture",
        };
}
