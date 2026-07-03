namespace CollegeLMS.API.SwaggerExamples;

public static class StudentResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        fullName = "Петров Пётр Петрович",
        email = "petrov@college.ru",
        groupName = "ИСП-31",
        recordBookNumber = "ЗК-2024-001",
    };
}
