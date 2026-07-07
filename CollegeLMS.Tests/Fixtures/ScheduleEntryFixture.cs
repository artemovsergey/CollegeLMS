using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class ScheduleEntryFixture
{
    public static Faker<ScheduleEntry> CreateFaker() =>
        new Faker<ScheduleEntry>()
            .RuleFor(s => s.Id, f => f.Random.Guid())
            .RuleFor(s => s.GroupId, f => f.Random.Guid())
            .RuleFor(s => s.TeacherId, f => f.Random.Guid())
            .RuleFor(s => s.Subject, f => f.Lorem.Word())
            .RuleFor(s => s.Room, f => $"{f.Random.Number(100, 500)}")
            .RuleFor(s => s.DayOfWeek, f => f.PickRandom<DayOfWeek>())
            .RuleFor(s => s.StartTime, f => new TimeSpan(f.Random.Number(8, 12), 0, 0))
            .RuleFor(s => s.EndTime, (f, s) => s.StartTime.Add(new TimeSpan(1, 30, 0)))
            .RuleFor(s => s.LessonType, f => f.PickRandom<LessonType>())
            .RuleFor(s => s.CreatedAt, f => f.Date.Past())
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent())
            .FinishWith(
                (f, s) =>
                {
                    s.Group = new Group
                    {
                        Id = s.GroupId,
                        Name = $"ГР-{f.Random.Number(1, 99)}",
                        Course = f.Random.Number(1, 4),
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                    };
                    s.Teacher = new Teacher
                    {
                        Id = s.TeacherId!.Value,
                        UserId = f.Random.Guid(),
                        Department = f.Commerce.Department(),
                        Position = f.Name.JobTitle(),
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
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
                }
            );
}
