using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class AssignmentFixture
{
    public static Faker<Assignment> CreateFaker() =>
        new Faker<Assignment>()
            .RuleFor(a => a.Id, f => f.Random.Guid())
            .RuleFor(a => a.CourseId, f => f.Random.Guid())
            .RuleFor(a => a.Title, f => f.Lorem.Sentence(4))
            .RuleFor(a => a.Description, f => f.Lorem.Paragraph())
            .RuleFor(a => a.DueDate, f => f.Date.Future())
            .RuleFor(a => a.MaxScore, f => f.Random.Int(10, 100))
            .RuleFor(a => a.Order, f => f.Random.Number(1, 10))
            .RuleFor(a => a.CreatedAt, f => f.Date.Past())
            .RuleFor(a => a.UpdatedAt, f => f.Date.Recent());
}
