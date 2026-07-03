using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class GroupFixture
{
    public static Faker<Group> CreateFaker() =>
        new Faker<Group>()
            .RuleFor(g => g.Id, f => f.Random.Guid())
            .RuleFor(g => g.Name, f => $"ГР-{f.Random.Number(1, 99)}")
            .RuleFor(g => g.Course, f => f.Random.Number(1, 4))
            .RuleFor(g => g.CreatedAt, f => f.Date.Past())
            .RuleFor(g => g.UpdatedAt, f => f.Date.Recent());
}
