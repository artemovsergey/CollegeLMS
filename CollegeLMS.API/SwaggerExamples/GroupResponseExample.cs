namespace CollegeLMS.API.SwaggerExamples;

public static class GroupResponseExample
{
    public static object Create() => new
    {
        id = Guid.NewGuid(),
        name = "ИСП-31",
        course = 3,
        studentCount = 25,
    };
}
