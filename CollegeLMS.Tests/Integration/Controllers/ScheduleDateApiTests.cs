using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleDateApiTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private string GetTeacherToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.ru",
            FullName = "Преподаватель",
            Login = "teacher",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
        };
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<T?> DeserializeWithEnumsAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    [Fact]
    public async Task GetMeta_ReturnsSemesterCalendar()
    {
        var response = await Client.GetAsync("/api/schedule/meta");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleMetaResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(new DateTime(2026, 9, 1), body.Data!.SemesterStart);
        Assert.Equal(16, body.Data.TotalWeeks);
        Assert.True(body.Data.CurrentWeek >= 1);
    }

    [Fact]
    public async Task GetAll_WithDate_ReturnsOkPage()
    {
        var response = await Client.GetAsync("/api/schedule?date=2026-09-07");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<PagedResponse<ScheduleResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data!.Items);
    }

    [Fact]
    public async Task GetAll_WithDate_FiltersByWeekAndDay()
    {
        var entries = ScheduleEntryFixture.CreateFaker().Generate(4);
        entries[0].Weeks = [1];
        entries[0].DayOfWeek = DayOfWeek.Monday;
        entries[1].Weeks = [2];
        entries[1].DayOfWeek = DayOfWeek.Monday;
        entries[2].Weeks = [2];
        entries[2].DayOfWeek = DayOfWeek.Tuesday;
        entries[3].Weeks = [2];
        entries[3].DayOfWeek = DayOfWeek.Monday;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScheduleEntries.AddRange(entries);
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/schedule?date=2026-09-07");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleResponse>>>(
            response
        );
        Assert.True(body!.IsSuccess);
        Assert.Equal(2, body.Data!.Items.Count);
    }

    [Fact]
    public async Task Search_AllowsAnonymous()
    {
        var response = await Client.GetAsync("/api/schedule/search?q=pos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsMatchingGroup()
    {
        var group = GroupFixture.CreateFaker().Generate();
        group.Name = "КОЛ-101";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Groups.Add(group);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GetTeacherToken());
        var response = await Client.GetAsync("/api/schedule/search?q=кол");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleSearchResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!.Groups);
        Assert.Equal("КОЛ-101", body.Data.Groups[0].Name);
    }

    [Fact]
    public async Task Search_ReturnsMatchingTeacher()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        teacher.User.FullName = "Иванов Иван Иванович";
        teacher.Position = "Преподаватель";

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GetTeacherToken());
        var response = await Client.GetAsync("/api/schedule/search?q=иванов");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleSearchResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!.Teachers);
        Assert.Equal("Иванов Иван Иванович", body.Data.Teachers[0].FullName);
    }
}
