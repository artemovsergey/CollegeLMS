namespace CollegeLMS.API.SwaggerExamples;

public static class NewsResponseExample
{
    public static object Create() =>
        new
        {
            id = Guid.NewGuid(),
            title = "День открытых дверей в колледже связи",
            content = "<p>Приглашаем всех желающих на день открытых дверей...</p>",
            imageUrl = "https://example.com/images/event.jpg",
            categoryId = Guid.NewGuid(),
            categoryName = "Мероприятия",
            isPublished = true,
            publishedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            createdByName = "Иванов Иван Иванович",
        };
}
