using System.Net;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleViewControllerTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private async Task<T?> DeserializeWithEnumsAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    [Fact]
    public async Task DayResponseContainsExamsSeparatelyFromEntries()
    {
        // 2026-09-01 — вторник первой недели семестра, обычный рабочий день.
        var target = new DateTime(2026, 9, 1);
        var groupId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Groups.Add(
                new Group
                {
                    Id = groupId,
                    Name = "ПО-261",
                    Course = 1,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            db.ScheduleEntries.AddRange(
                // Экзамен ровно таким его создаёт движок корректировок.
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    Subject = "ЭКЗАМЕН",
                    DayOfWeek = target.DayOfWeek,
                    NumberPair = 0,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromMinutes(1),
                    Weeks = [1],
                    LessonType = LessonType.Exam,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
                // Обычная пара — чтобы Entries был непустым и проверка была осмысленной.
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    Subject = "Математика",
                    Room = "301",
                    DayOfWeek = target.DayOfWeek,
                    NumberPair = 1,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Weeks = [1],
                    LessonType = LessonType.Lecture,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync(
            $"/api/schedule?view=day&date=2026-09-01&groupId={groupId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleDayViewResponse>>(response);
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();

        var exams = body.Data!.Exams;
        exams.Should().HaveCount(1);
        exams[0].LessonType.Should().Be(nameof(LessonType.Exam));
        exams[0].NumberPair.Should().Be(0);
        exams[0].Subject.Should().Be("ЭКЗАМЕН");
        exams[0].StartTime.Should().Be(TimeSpan.Zero);
        exams[0].EndTime.Should().Be(TimeSpan.Zero);

        body.Data!.Entries.Should().NotBeEmpty();
        body.Data.Entries.Should()
            .OnlyContain(e => e.NumberPair >= 1 && e.LessonType != nameof(LessonType.Exam));
    }
}
