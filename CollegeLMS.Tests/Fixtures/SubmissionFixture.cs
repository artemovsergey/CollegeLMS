using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class SubmissionFixture
{
    public static Faker<AssignmentSubmission> CreateFaker() =>
        new Faker<AssignmentSubmission>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.AssignmentId, f => f.Random.Guid())
            .RuleFor(s => s.StudentId, f => f.Random.Guid())
            .RuleFor(s => s.FilePath, f => $"submissions/{f.Random.Guid()}/{f.Random.Guid()}_{f.Random.Long()}.bin")
            .RuleFor(s => s.Comment, f => f.Lorem.Sentence())
            .RuleFor(s => s.Score, f => f.Random.Int(0, 100))
            .RuleFor(s => s.SubmittedAt, f => f.Date.Past())
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent());
}
