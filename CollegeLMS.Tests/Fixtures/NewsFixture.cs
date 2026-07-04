using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class NewsFixture
{
    public static Faker<News> CreateFaker() =>
        new Faker<News>()
            .RuleFor(n => n.Id, f => f.Random.Guid())
            .RuleFor(n => n.Title, f => f.Lorem.Sentence(5))
            .RuleFor(n => n.Content, f => f.Lorem.Paragraphs(3))
            .RuleFor(n => n.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(n => n.IsPublished, true)
            .RuleFor(n => n.PublishedAt, f => f.Date.Past(30))
            .RuleFor(n => n.CreatedAt, f => f.Date.Past(30))
            .RuleFor(n => n.UpdatedAt, f => f.Date.Past(30));
}
