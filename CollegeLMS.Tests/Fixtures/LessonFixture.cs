using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class LessonFixture
{
    public static Faker<Lesson> CreateFaker() =>
        new Faker<Lesson>()
            .RuleFor(l => l.Id, f => f.Random.Guid())
            .RuleFor(l => l.CourseId, f => f.Random.Guid())
            .RuleFor(l => l.Title, f => f.Lorem.Sentence(4))
            .RuleFor(l => l.Content, f => f.Lorem.Paragraphs(3))
            .RuleFor(l => l.Order, f => f.Random.Number(1, 20))
            .RuleFor(l => l.Kind, f => f.PickRandom<LessonKind>())
            .RuleFor(l => l.CreatedAt, f => f.Date.Past())
            .RuleFor(l => l.UpdatedAt, f => f.Date.Recent());
}
