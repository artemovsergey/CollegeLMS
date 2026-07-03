using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class StudentFixture
{
    public static Faker<Student> CreateFaker() =>
        new Faker<Student>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.UserId, (f, s) => s.Id)
            .RuleFor(s => s.GroupId, f => f.Random.Guid())
            .RuleFor(s => s.RecordBookNumber, f => $"ЗК-{f.Random.Number(2023, 2026)}-{f.Random.Number(1, 999):D3}")
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent())
            .FinishWith((f, s) =>
            {
                s.User = new User
                {
                    Id = s.UserId,
                    Email = f.Internet.Email(),
                    FullName = f.Name.FullName(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                    Role = UserRole.Student,
                    IsActive = true,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                };
                s.Group = new Group
                {
                    Id = s.GroupId,
                    Name = $"ГР-{f.Random.Number(1, 99)}",
                    Course = f.Random.Number(1, 4),
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                };
            });
}
