using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class CourseFixture
{
    public static Faker<Course> CreateFaker() =>
        new Faker<Course>()
            .RuleFor(c => c.Id, f => f.Random.Guid())
            .RuleFor(c => c.Title, f => f.Lorem.Sentence(3))
            .RuleFor(c => c.Description, f => f.Lorem.Paragraph())
            .RuleFor(c => c.TeacherId, f => f.Random.Guid())
            .RuleFor(c => c.GroupId, f => f.Random.Guid())
            .RuleFor(c => c.Status, f => f.PickRandom<CourseStatus>())
            .RuleFor(c => c.CreatedAt, f => f.Date.Past())
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent())
            .FinishWith((f, c) =>
            {
                c.Teacher = new Teacher
                {
                    Id = c.TeacherId,
                    UserId = f.Random.Guid(),
                    Department = f.Commerce.Department(),
                    Position = f.Name.JobTitle(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    User = new User
                    {
                        Id = f.Random.Guid(),
                        Email = f.Internet.Email(),
                        FullName = f.Name.FullName(),
                        PasswordHash = "hash",
                        Role = UserRole.Teacher,
                        IsActive = true,
                    },
                };
                c.Group = new Group
                {
                    Id = c.GroupId,
                    Name = $"ГР-{f.Random.Number(1, 99)}",
                    Course = f.Random.Number(1, 4),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                };
            });
}
