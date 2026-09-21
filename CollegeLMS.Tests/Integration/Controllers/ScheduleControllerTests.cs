using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleControllerTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private string GetAdminToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.ru",
            FullName = "Admin",
            PasswordHash = "hash",
            Role = UserRole.Admin,
        };
        return tokenService.GenerateAccessToken(admin);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T?> DeserializeWithEnumsAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    [Fact]
    public async Task GetAll_ReturnsList_WhenNoAuth()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(entries);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/schedule");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleResponse>>>(
            response
        );
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(3, body.Data!.Items.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByGroupId()
    {
        var groupId = Guid.NewGuid();
        var entries = ScheduleEntryFixture.CreateFaker().Generate(3);
        entries[0].GroupId = groupId;
        entries[0].Group!.Id = groupId;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(entries);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/schedule?groupId={groupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleResponse>>>(
            response
        );
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!.Items);
    }

    [Fact]
    public async Task GetSubjects_ReturnsDistinctSubjects_WhenAnonymous()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    Subject = "Математика",
                    Room = "301",
                    DayOfWeek = DayOfWeek.Monday,
                    NumberPair = 1,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Weeks = new List<int> { 1 },
                    LessonType = LessonType.Lecture,
                },
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    Subject = "МАТЕМАТИКА",
                    Room = "302",
                    DayOfWeek = DayOfWeek.Tuesday,
                    NumberPair = 2,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Weeks = new List<int> { 1 },
                    LessonType = LessonType.Practice,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/schedule/subjects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<SubjectsResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Contains("Математика", body.Data!.Subjects);
        Assert.Single(body.Data.Subjects);
    }

    [Fact]
    public async Task GetSubjects_FiltersByQuery_IgnoreCase()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    Subject = "Математика",
                    Room = "301",
                    DayOfWeek = DayOfWeek.Monday,
                    NumberPair = 1,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Weeks = new List<int> { 1 },
                    LessonType = LessonType.Lecture,
                },
                new ScheduleEntry
                {
                    Id = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    Subject = "Литература",
                    Room = "401",
                    DayOfWeek = DayOfWeek.Tuesday,
                    NumberPair = 1,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Weeks = new List<int> { 1 },
                    LessonType = LessonType.Lecture,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/schedule/subjects?q=%D0%9C%D0%B0%D1%82");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<SubjectsResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        var subject = Assert.Single(body.Data!.Subjects);
        Assert.Equal("Математика", subject);
    }

    [Fact]
    public async Task GetById_ReturnsEntry_WhenFound()
    {
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/schedule/{entry.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(entry.Id, body.Data!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var response = await Client.GetAsync($"/api/schedule/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesEntry_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        var groupId = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Groups.Add(
                new Group
                {
                    Id = groupId,
                    Name = "ГР-11",
                    Course = 1,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync(
            "/api/schedule",
            new CreateScheduleRequest
            {
                GroupId = groupId,
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new() { 1 },
                LessonType = LessonType.Lecture.ToString(),
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Математика", body.Data!.Subject);
    }

    [Fact]
    public async Task Post_ReturnsUnauthorized_WhenNoAuth()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/schedule",
            new CreateScheduleRequest
            {
                GroupId = Guid.NewGuid(),
                Subject = "Математика",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                NumberPair = 1,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                Weeks = new() { 1 },
                LessonType = LessonType.Lecture.ToString(),
            }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesEntry_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        var groupId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Groups.Add(
                new Group
                {
                    Id = groupId,
                    Name = "ГР-11",
                    Course = 1,
                }
            );
            db.ScheduleEntries.Add(
                new ScheduleEntry
                {
                    Id = entryId,
                    GroupId = groupId,
                    Subject = "Старая тема",
                    Room = "101",
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    LessonType = LessonType.Lecture,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.PutAsJsonAsync(
            $"/api/schedule/{entryId}",
            new UpdateScheduleRequest
            {
                GroupId = groupId,
                Subject = "Обновлено",
                Room = "402",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 1,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                Weeks = new() { 1 },
                LessonType = LessonType.Lab.ToString(),
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Обновлено", body.Data!.Subject);
    }

    [Fact]
    public async Task GetAll_DayView_ReturnsStructuredDay()
    {
        var groupId = Guid.NewGuid();
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        entry.GroupId = groupId;
        entry.Group!.Id = groupId;
        entry.DayOfWeek = DayOfWeek.Tuesday;
        entry.NumberPair = 1;
        entry.Weeks = new List<int> { 1 };

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync(
            $"/api/schedule?view=day&date=2026-09-01&groupId={groupId}"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"isSunday\":false").And.Contain("\"entries\"");
    }

    [Fact]
    public async Task GetAll_WeekView_ReturnsSixDays()
    {
        var response = await Client.GetAsync("/api/schedule?view=week&week=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleWeekViewResponse>>(response);
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Data!.Week.Should().Be(1);
        body.Data.Days.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetAll_SemesterView_WithoutFilter_Returns400()
    {
        var response = await Client.GetAsync("/api/schedule?view=semester");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_CalendarView_ReturnsMonthGrid()
    {
        var response = await Client.GetAsync("/api/schedule?view=calendar&month=2026-09");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleMonthViewResponse>>(response);
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Data!.Month.Should().Be(9);
        body.Data.Days.Should().HaveCount(30);
    }

    [Fact]
    public async Task GetAll_DayView_InvalidDate_Returns400WithRussianMessage()
    {
        var response = await Client.GetAsync("/api/schedule?view=day&date=21-09-2026");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Неверный формат даты.");
    }

    [Fact]
    public async Task GetAll_UnknownView_FallsBackToPagedList()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(2);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(entries);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/schedule?view=unknown");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleResponse>>>(
            response
        );
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Export_Day_ReturnsXlsxWithFile3Name()
    {
        var groupId = Guid.NewGuid();
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        entry.GroupId = groupId;
        entry.Group!.Id = groupId;
        entry.DayOfWeek = DayOfWeek.Tuesday;
        entry.NumberPair = 1;
        entry.Weeks = new List<int> { 1 };

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync(
            $"/api/schedule/export?scope=day&date=2026-09-01&groupId={groupId}&format=xlsx"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response
            .Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var disposition = Uri.UnescapeDataString(
            response.Content.Headers.GetValues("Content-Disposition").Single()
        );
        disposition
            .Should()
            .MatchRegex(@"Расписание_день_\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}\.xlsx");
    }

    [Fact]
    public async Task Export_InvalidDate_Returns400WithRussianMessage()
    {
        var response = await Client.GetAsync("/api/schedule/export?scope=day&date=21-09-2026");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Неверный формат даты.");
    }

    [Fact]
    public async Task Export_Day_NonWorking_Returns400WithTitle()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NonWorkingDays.Add(
                new NonWorkingDay
                {
                    Id = Guid.NewGuid(),
                    DateFrom = new DateTime(2026, 9, 7),
                    DateTo = new DateTime(2026, 9, 7),
                    Title = "Праздник",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync(
            "/api/schedule/export?scope=day&date=2026-09-07&format=xlsx"
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Нерабочий день: Праздник");
    }

    [Fact]
    public async Task Export_Week_Empty_Returns404()
    {
        var response = await Client.GetAsync("/api/schedule/export?scope=week&week=1&format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Export_UnknownScope_Returns400()
    {
        var response = await Client.GetAsync("/api/schedule/export?scope=year&format=xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Укажите scope: day, week или semester.");
    }

    [Fact]
    public async Task Export_LegacyNoScope_ReturnsXlsxWithFile3Name()
    {
        var groupId = Guid.NewGuid();
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        entry.GroupId = groupId;
        entry.Group!.Id = groupId;
        entry.DayOfWeek = DayOfWeek.Tuesday;
        entry.NumberPair = 1;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.Add(entry);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/schedule/export?format=xlsx&groupId={groupId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var disposition = Uri.UnescapeDataString(
            response.Content.Headers.GetValues("Content-Disposition").Single()
        );
        disposition.Should().MatchRegex(@"Расписание_\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}\.xlsx");
    }
}
