using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class MaterialFixture
{
    public static Faker<CourseMaterial> CreateFaker() =>
        new Faker<CourseMaterial>()
            .RuleFor(m => m.Id, f => f.Random.Guid())
            .RuleFor(m => m.CourseId, f => f.Random.Guid())
            .RuleFor(m => m.FileName, f => $"{f.Lorem.Word()}.pdf")
            .RuleFor(
                m => m.FilePath,
                f => $"materials/{f.Random.Guid()}/{f.Random.Guid()}_{f.Lorem.Word()}.pdf"
            )
            .RuleFor(m => m.FileSize, f => f.Random.Long(1024, 1048576))
            .RuleFor(m => m.MimeType, f => "application/pdf")
            .RuleFor(m => m.CreatedAt, f => f.Date.Past())
            .RuleFor(m => m.UpdatedAt, f => f.Date.Recent());
}
