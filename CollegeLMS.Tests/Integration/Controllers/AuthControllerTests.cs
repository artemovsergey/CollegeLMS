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

public class AuthControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsValid()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "testuser",
            Email = "test@test.ru",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Login = "testuser", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<LoginResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data);
        Assert.NotEmpty(body.Data.Token);
        Assert.Equal("testuser", body.Data.User.Login);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "testuser",
            Email = "test@test.ru",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Login = "testuser", Password = "wrong-password" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Login = "nonexistent", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsForbidden_WhenUserDeactivated()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "testuser",
            Email = "test@test.ru",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Student,
            IsActive = false,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Login = "testuser", Password = "password123" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_ReturnsUser_WhenAuthenticated()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "profileuser",
            Email = "profile@test.ru",
            FullName = "Profile User",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/auth/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result<UserResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(user.Email, body.Data!.Email);
    }

    [Fact]
    public async Task Profile_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.GetAsync("/api/auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsOk_WhenOldPasswordCorrect()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "changepwuser",
            Email = "changepw@test.ru",
            FullName = "Change PW User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password"),
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest { OldPassword = "old-password", NewPassword = "new-password" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeAsync<Result>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_ReturnsBadRequest_WhenOldPasswordWrong()
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = "changepwuser2",
            Email = "changepw2@test.ru",
            FullName = "Change PW User 2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            Role = UserRole.Student,
            IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateAccessToken(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest { OldPassword = "wrong-password", NewPassword = "new-password" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsUnauthorized_WhenNoToken()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
