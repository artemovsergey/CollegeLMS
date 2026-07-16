using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class TestFixture
{
    public static Faker<Test> CreateFaker() =>
        new Faker<Test>()
            .RuleFor(t => t.Id, f => f.Random.Guid())
            .RuleFor(t => t.Title, f => f.Lorem.Sentence(3))
            .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
            .RuleFor(t => t.TimeLimitMinutes, _ => 60)
            .RuleFor(t => t.MaxAttempts, _ => 1)
            .RuleFor(t => t.Type, _ => TestType.SelfStudy)
            .RuleFor(t => t.PassingScore, _ => 60)
            .RuleFor(t => t.CourseId, f => f.Random.Guid())
            .RuleFor(t => t.AutoCheck, _ => true)
            .RuleFor(t => t.ShowCorrectAnswers, _ => false)
            .RuleFor(t => t.ShuffleQuestions, _ => false)
            .RuleFor(t => t.ShuffleOptions, _ => false)
            .RuleFor(t => t.CreatedAt, f => f.Date.Past())
            .RuleFor(t => t.UpdatedAt, f => f.Date.Recent())
            .FinishWith(
                (f, t) =>
                {
                    t.Course = new Course
                    {
                        Id = t.CourseId,
                        Title = f.Lorem.Sentence(2),
                        TeacherId = f.Random.Guid(),
                        GroupId = f.Random.Guid(),
                        Status = CourseStatus.Draft,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                    };
                }
            );
}
