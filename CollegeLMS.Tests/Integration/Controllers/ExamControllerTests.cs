using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ExamControllerTests : BaseIntegrationTest
{
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

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync("/api/exams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<ExamResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync($"/api/exams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenValid()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var group = new Group { Id = Guid.NewGuid(), Name = "ГР-11", Course = 1 };
        var teacherUser = new User { Id = Guid.NewGuid(), FullName = "Учитель", Email = "t@t.ru", PasswordHash = "hash", Role = UserRole.Teacher, IsActive = true };
        var teacher = new Teacher { Id = Guid.NewGuid(), UserId = teacherUser.Id, Department = "ИТ", Position = "П" };
        var semester = new Semester { Id = Guid.NewGuid(), Name = "Семестр", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6), Type = SemesterType.Autumn };
        db.Users.Add(teacherUser);
        db.Groups.Add(group);
        db.Teachers.Add(teacher);
        db.Semesters.Add(semester);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync("/api/exams", new CreateExamRequest
        {
            Subject = "Экзамен",
            GroupId = group.Id,
            ExamDate = DateTime.UtcNow.AddDays(30),
            Type = "Exam",
            TeacherId = teacher.Id,
            SemesterId = semester.Id,
        });

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected OK got {response.StatusCode}: {body}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
