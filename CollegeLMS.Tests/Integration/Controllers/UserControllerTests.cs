using System.Net;
using Bogus;
using CollegeLMS.API.Data;
using Microsoft.Extensions.DependencyInjection;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;

namespace CollegeLMS.Tests.Integration.Controllers;

public class UserControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoUsers()
    {
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
        using var seedScope = Factory.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fakeUsers = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword("test123"))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
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
}
