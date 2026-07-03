namespace CollegeLMS.API.SwaggerExamples;

public static class TeacherResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            fullName = "Иванов Иван Иванович",
            email = "ivanov@college.ru",
            department = "Информационных технологий",
            position = "Преподаватель",
        };
}
