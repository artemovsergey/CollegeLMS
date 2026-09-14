namespace CollegeLMS.API.SwaggerExamples;

public static class SubjectsResponseExample
{
    public static object Create() =>
        new
        {
            subjects = new[]
            {
                "Математика",
                "Физика",
                "Информатика",
                "История",
            },
        };
}