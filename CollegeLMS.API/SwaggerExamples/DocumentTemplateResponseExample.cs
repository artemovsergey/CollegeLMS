namespace CollegeLMS.API.SwaggerExamples;

public static class DocumentTemplateResponseExample
{
    public static object Create() =>
        new object[]
        {
            new
            {
                fileName = "Расписание.xlsx",
                name = "Расписание",
                description = "Шаблон расписания для импорта",
                size = 83193,
            },
            new
            {
                fileName = "Корректировка.xlsx",
                name = "Корректировка",
                description = "Шаблон корректировки расписания для импорта",
                size = 9862,
            },
        };
}
