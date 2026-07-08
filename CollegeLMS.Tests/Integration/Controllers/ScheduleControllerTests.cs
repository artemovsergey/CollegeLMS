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
            IsActive = true,
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
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
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
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
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
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 30, 0),
                LessonType = LessonType.Lab.ToString(),
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<ScheduleResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Обновлено", body.Data!.Subject);
    }
}
