namespace CollegeLMS.API.SwaggerExamples;

public static class CorrectionPreviewResponseExample
{
    public static object Create() =>
        new
        {
            correctionDate = "2026-09-08T00:00:00",
            week = 2,
            dayOfWeek = 2,
            totalEntries = 2,
            entries = new object[]
            {
                new
                {
                    row = 7,
                    groupId = Guid.NewGuid(),
                    groupName = "ПО-262",
                    changeType = "Replace",
                    dayOfWeek = 2,
                    week = 2,
                    numberPair = 4,
                    subject = "Математика",
                    teacherId = Guid.NewGuid(),
                    teacherName = "Марченко И.А.",
                    removedSubject = "Физика",
                    removedTeacherId = Guid.NewGuid(),
                    removedTeacherName = "Иванов Д.А.",
                    removedNumberPair = 2,
                    note = "вм. 4 п",
                },
                new
                {
                    row = 8,
                    groupId = Guid.NewGuid(),
                    groupName = "ПО-262",
                    changeType = "Remove",
                    dayOfWeek = 3,
                    week = 2,
                    numberPair = 5,
                    subject = (string?)null,
                    teacherId = (Guid?)null,
                    teacherName = (string?)null,
                    removedSubject = "История",
                    removedTeacherId = Guid.NewGuid(),
                    removedTeacherName = "Сидорова Е.В.",
                    removedNumberPair = 5,
                    note = "",
                },
            },
            errors = new object[]
            {
                new
                {
                    row = 10,
                    column = 4,
                    level = "logic",
                    message = "Строка 10: на Вторник 2-й неделе, пара 3 уже занята.",
                },
            },
        };
}