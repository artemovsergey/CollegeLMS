using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.SwaggerExamples;

public static class BellProfileExample
{
    public static BellProfileResponse Create() =>
        new()
        {
            Id = Guid.Parse("b3000000-0000-0000-0000-000000000002"),
            Name = "Понедельник",
            IsDefault = false,
            DaysOfWeek = [1],
            Slots =
            [
                new BellSlotResponse
                {
                    Id = Guid.NewGuid(),
                    NumberPair = 1,
                    StartTime = new TimeSpan(9, 10, 0),
                    EndTime = new TimeSpan(10, 40, 0),
                },
                new BellSlotResponse
                {
                    Id = Guid.NewGuid(),
                    NumberPair = 2,
                    StartTime = new TimeSpan(10, 50, 0),
                    EndTime = new TimeSpan(12, 20, 0),
                },
            ],
            BigBreak = null,
            Dates = [],
        };
}
