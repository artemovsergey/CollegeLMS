using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class StudentControllerTests : BaseIntegrationTest
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

    private string GetStudentToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var student = new User
        {
            Id = Guid.NewGuid(),
            Email = "student@test.ru",
            FullName = "Student",
            PasswordHash = "hash",
            Role = UserRole.Student,
            IsActive = true,
        };
        return tokenService.GenerateAccessToken(student);
    }

    private void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_ReturnsStudents_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var students = StudentFixture.CreateFaker().Generate(3);
        db.Users.AddRange(students.Select(s => s.User));
        db.Groups.AddRange(students.Select(s => s.Group));
        db.Students.AddRange(students);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/students");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<List<StudentResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(3, body.Data!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsForbidden_WhenNotAdmin()
    {
        SetAuthHeader(GetStudentToken());

        var response = await Client.GetAsync("/api/students");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_CreatesStudent_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ТЕСТ-11",
            Course = 1,
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/students",
            new CreateStudentRequest
            {
                Email = "newstudent@test.ru",
                Password = "test123",
                FullName = "Новый Студент",
                GroupId = group.Id,
                RecordBookNumber = "ЗК-2024-001",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<StudentResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("newstudent@test.ru", body.Data!.Email);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/students");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_TransfersStudent_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var student = StudentFixture.CreateFaker().Generate();
        db.Users.Add(student.User);
        var newGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "НГР-21",
            Course = 2,
        };
        db.Groups.Add(newGroup);
        db.Groups.Add(student.Group);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/students/{student.Id}/transfer"
        )
        {
            Content = JsonContent.Create(
                new TransferStudentRequest { NewGroupId = newGroup.Id, Reason = "Перевод" }
            ),
        };
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTransfers_ReturnsTransfers_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var student = StudentFixture.CreateFaker().Generate();
        db.Users.Add(student.User);
        db.Groups.Add(student.Group);
        db.Students.Add(student);

        var newGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "НГР-21",
            Course = 2,
        };
        db.Groups.Add(newGroup);

        db.TransferRecords.Add(
            new TransferRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                FromGroupId = student.GroupId,
                ToGroupId = newGroup.Id,
                Reason = "Перевод",
            }
        );
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/students/{student.Id}/transfers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
