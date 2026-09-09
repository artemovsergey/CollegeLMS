namespace CollegeLMS.API.SwaggerExamples;

public static class DispatcherDashboardResponseExample
{
    public static object Create() =>
        new
        {
            date = "2026-09-09T00:00:00",
            week = 2,
            dayOfWeek = 3,
            slots = new object[]
            {
                new
                {
                    numberPair = 1,
                    startTime = "09:00:00",
                    endTime = "10:30:00",
                    entries = new object[]
                    {
                        new
                        {
                            groupId = Guid.NewGuid(),
                            groupName = "ПО-262",
                            subject = "Математика",
                            room = "305",
                            startTime = "09:00:00",
                            endTime = "10:30:00",
                            lessonType = "Lecture",
                            changeType = (string?)null,
                        },
                    },
                },
                new
                {
                    numberPair = 2,
                    startTime = "10:40:00",
                    endTime = "12:10:00",
                    entries = new object[]
                    {
                        new
                        {
                            groupId = Guid.NewGuid(),
                            groupName = "ИС-272",
                            subject = "Физика",
                            room = "412",
                            startTime = "10:40:00",
                            endTime = "12:10:00",
                            lessonType = "Practice",
                            changeType = (string?)null,
                        },
                    },
                },
            },
            teachers = new object[]
            {
                new
                {
                    teacherId = Guid.NewGuid(),
                    teacherName = "Марченко И.А.",
                    totalPairs = 2,
                    entries = new object[]
                    {
                        new
                        {
                            groupId = Guid.NewGuid(),
                            groupName = "ПО-262",
                            subject = "Математика",
                            room = "305",
                            startTime = "09:00:00",
                            endTime = "10:30:00",
                            lessonType = "Lecture",
                            changeType = (string?)null,
                        },
                    },
                },
            },
        };
}
