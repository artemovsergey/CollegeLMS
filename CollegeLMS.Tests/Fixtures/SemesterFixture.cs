using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class SemesterFixture
{
    public static Faker<Semester> CreateFaker() =>
        new Faker<Semester>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.Name, f => $"Семестр {f.Random.Number(1, 8)}")
            .RuleFor(s => s.StartDate, f => f.Date.Past(1))
            .RuleFor(s => s.EndDate, f => f.Date.Future(1))
            .RuleFor(s => s.Type, f => f.PickRandom<SemesterType>())
            .RuleFor(s => s.AcademicYear, f => $"{f.Random.Number(2024, 2026)}-{f.Random.Number(2025, 2027)}")
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent());
}
