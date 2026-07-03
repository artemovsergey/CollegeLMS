using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class TeacherFixture
{
    public static Faker<Teacher> CreateFaker() =>
        new Faker<Teacher>()
            .RuleFor(t => t.Id, f => f.Random.Guid())
            .RuleFor(t => t.UserId, (f, t) => t.Id)
            .RuleFor(t => t.Department, f => f.Commerce.Department())
            .RuleFor(t => t.Position, f => f.Name.JobTitle())
            .RuleFor(t => t.CreatedAt, f => f.Date.Past())
            .RuleFor(t => t.UpdatedAt, f => f.Date.Recent())
            .FinishWith(
                (f, t) =>
                {
                    t.User = new User
                    {
                        Id = t.UserId,
                        Email = f.Internet.Email(),
                        FullName = f.Name.FullName(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                        Role = UserRole.Teacher,
                        IsActive = true,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                    };
                }
            );
}
