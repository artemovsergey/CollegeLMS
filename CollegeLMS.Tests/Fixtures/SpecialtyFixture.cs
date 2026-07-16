using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class SpecialtyFixture
{
    public static Faker<Specialty> CreateFaker() =>
        new Faker<Specialty>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.Code, f => $"09.02.{f.Random.Number(1, 99):D2}")
            .RuleFor(s => s.Name, f => f.Commerce.Department())
            .RuleFor(s => s.Description, f => f.Lorem.Sentence())
            .RuleFor(s => s.Department, f => f.Commerce.Department())
            .RuleFor(s => s.IsDeleted, _ => false)
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent());
}
