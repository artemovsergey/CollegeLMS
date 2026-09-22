using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleClaimsContextApiTests : BaseIntegrationTest
{
    private string GuestToken(string role, params Claim[] extra)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokenService.GenerateCustomToken([role], 60, Guid.NewGuid().ToString(), extra);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetContext_GuestTeacher_UsesTeacherIdClaim()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GuestToken("Teacher", new Claim("teacherId", teacher.Id.ToString())));

        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.Equal("Teacher", body!.Data!.Role);
        Assert.Equal(teacher.Id, body.Data.TeacherId);
    }

    [Fact]
    public async Task GetContext_GuestStudent_UsesGroupIdClaim()
    {
        var group = GroupFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Groups.Add(group);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GuestToken("Student", new Claim("groupId", group.Id.ToString())));

        var response = await Client.GetAsync("/api/schedule/context");

        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.Equal("Student", body!.Data!.Role);
        Assert.Equal(group.Id, body.Data.GroupId);
    }

    [Fact]
    public async Task GetJournal_GuestTeacherWithClaim_ReturnsOk()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GuestToken("Teacher", new Claim("teacherId", teacher.Id.ToString())));

        var response = await Client.GetAsync($"/api/schedule/journal?teacherId={teacher.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJournal_GuestTeacherWithoutClaim_ReturnsBadRequest()
    {
        SetAuthHeader(GuestToken("Teacher"));

        var response = await Client.GetAsync("/api/schedule/journal");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
