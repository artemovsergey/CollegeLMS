using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class DashboardFixture
{
    public static Faker<Teacher> CreateTeacherFaker() =>
        new Faker<Teacher>()
            .RuleFor(t => t.Id, f => f.Random.Guid())
            .RuleFor(t => t.UserId, (f, t) => t.Id)
            .RuleFor(t => t.CyclicalCommission, f => f.Commerce.Department())
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
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                    };
                }
            );

    public static Faker<Student> CreateStudentFaker() =>
        new Faker<Student>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.UserId, (f, s) => s.Id)
            .RuleFor(s => s.RecordBookNumber, f => f.Random.AlphaNumeric(8))
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent())
            .FinishWith(
                (f, s) =>
                {
                    s.User = new User
                    {
                        Id = s.UserId,
                        Email = f.Internet.Email(),
                        FullName = f.Name.FullName(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                        Role = UserRole.Student,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                    };
                }
            );

    public static Faker<Group> CreateGroupFaker() =>
        new Faker<Group>()
            .RuleFor(g => g.Id, f => f.Random.Guid())
            .RuleFor(g => g.Name, f => $"ГР-{f.Random.Number(10, 99)}")
            .RuleFor(g => g.Course, f => f.Random.Number(1, 4))
            .RuleFor(g => g.CreatedAt, f => f.Date.Past())
            .RuleFor(g => g.UpdatedAt, f => f.Date.Recent());

    public static Faker<Course> CreateCourseFaker() =>
        new Faker<Course>()
            .RuleFor(c => c.Id, f => f.Random.Guid())
            .RuleFor(c => c.Title, f => f.Lorem.Word())
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.Status, CourseStatus.Active)
            .RuleFor(c => c.CreatedAt, f => f.Date.Past())
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent());
}
