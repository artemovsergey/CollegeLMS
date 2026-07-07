using Bogus;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class CreateScheduleRequestFixture
{
    public static Faker<CreateScheduleRequest> CreateFaker() =>
        new Faker<CreateScheduleRequest>()
            .RuleFor(r => r.GroupId, f => f.Random.Guid())
            .RuleFor(r => r.TeacherId, f => f.Random.Guid())
            .RuleFor(r => r.Subject, f => f.Lorem.Word())
            .RuleFor(r => r.Room, f => $"{f.Random.Number(100, 500)}")
            .RuleFor(r => r.DayOfWeek, f => f.PickRandom<DayOfWeek>())
            .RuleFor(r => r.StartTime, f => new TimeSpan(f.Random.Number(8, 12), 0, 0))
            .RuleFor(r => r.EndTime, (f, r) => r.StartTime.Add(new TimeSpan(1, 30, 0)))
            .RuleFor(r => r.LessonType, f => f.PickRandom<LessonType>().ToString());
}
