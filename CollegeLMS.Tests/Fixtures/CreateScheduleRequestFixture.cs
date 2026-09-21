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
            .RuleFor(
                r => r.LessonType,
                f =>
                    f.PickRandom(
                            new[]
                            {
                                LessonType.Lecture,
                                LessonType.Practice,
                                LessonType.Lab,
                                LessonType.Exam,
                            }
                        )
                        .ToString()
            );
}
