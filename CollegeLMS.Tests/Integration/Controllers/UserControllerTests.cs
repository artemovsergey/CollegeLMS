using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bogus;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using CollegeLMS.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class UserControllerTests : BaseIntegrationTest
{
    private string GetAdminToken()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Login = "admin",
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
            Login = "student",
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
    public async Task GetAll_ReturnsEmptyList_WhenNoUsers()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<List<UserResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.Empty(body.Data);
    }

    [Fact]
    public async Task GetAll_ReturnsUsers_WhenUsersExist()
    {
        SetAuthHeader(GetAdminToken());

        using var seedScope = Factory.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fakeUsers = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Login, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword("test123"))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.IsActive, _ => true)
            .Generate(5);

        db.Users.AddRange(fakeUsers);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<List<UserResponse>>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.Equal(5, body.Data.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_CreatesUser_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Login = "newuser",
                Email = "newuser@test.ru",
                Password = "password123",
                FullName = "New User",
                Role = UserRole.Student,
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<UserResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("newuser", body.Data!.Login);
        Assert.Equal("newuser@test.ru", body.Data!.Email);
    }

    [Fact]
    public async Task Create_ReturnsForbidden_WhenNotAdmin()
    {
        SetAuthHeader(GetStudentToken());

        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                Login = "newuser",
                Email = "newuser@test.ru",
                Password = "password123",
                FullName = "New User",
                Role = UserRole.Student,
            }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsUser_WhenFound()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = UserFixture.CreateFaker().Generate();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<UserResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(user.Id, body.Data!.Id);
    }

    [Fact]
    public async Task Update_UpdatesUser_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = UserFixture.CreateFaker().Generate();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new UpdateUserRequest
            {
                Login = "updateduser",
                Email = "updated@test.ru",
                FullName = "Updated Name",
                Role = UserRole.Teacher,
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<UserResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("updateduser", body.Data!.Login);
        Assert.Equal("Updated Name", body.Data.FullName);
        Assert.Equal("Teacher", body.Data.Role);
    }

    [Fact]
    public async Task Delete_DeactivatesUser_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = UserFixture.CreateFaker().Generate();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.DeleteAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var db2 = CreateDbContext();
        var deactivated = await db2.Users.FindAsync([user.Id]);
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task ChangeRole_ChangesRole_WhenAdmin()
    {
        SetAuthHeader(GetAdminToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = UserFixture.CreateFaker().Generate();
        user.Role = UserRole.Student;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/api/users/{user.Id}/role",
            new ChangeRoleRequest { Role = UserRole.Dispatcher }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<UserResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("Dispatcher", body.Data!.Role);
    }

    [Fact]
    public async Task ChangeRole_ReturnsForbidden_WhenNotAdmin()
    {
        SetAuthHeader(GetStudentToken());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = UserFixture.CreateFaker().Generate();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/api/users/{user.Id}/role",
            new ChangeRoleRequest { Role = UserRole.Admin }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
