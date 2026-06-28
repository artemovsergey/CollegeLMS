using Bogus;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.Tests.Fixtures;

public static class UserFixture
{
    public static Faker<User> CreateFaker() =>
        new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.PasswordHash, _ => BCrypt.Net.BCrypt.HashPassword("test123"))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past())
            .RuleFor(u => u.UpdatedAt, f => f.Date.Recent());
}
