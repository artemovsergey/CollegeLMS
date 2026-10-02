using System.Net;
using System.Net.Http.Headers;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

/// <summary>Панель документов диспетчера: список шаблонов и их скачивание.</summary>
public class DocumentsControllerTests : BaseIntegrationTest
{
    private string GetToken(UserRole role)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{role}@test.ru",
            FullName = role.ToString(),
            PasswordHash = "hash",
            Role = role,
        };
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetTemplates_ListsScheduleDocuments()
    {
        SetAuthHeader(GetToken(UserRole.Dispatcher));

        var response = await Client.GetAsync("/api/dispatcher/documents/templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Расписание.xlsx", body);
    }
}
