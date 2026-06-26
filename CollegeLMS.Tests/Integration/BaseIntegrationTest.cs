using System.Text.Json;
using CollegeLMS.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext Db;
    private readonly WebApplicationFactory<Program> _factory;

    protected BaseIntegrationTest()
    {
    _factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Testing");
            builder.ConfigureServices(services =>
            {
                var efServices = services
                    .Where(s => s.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                                s.ServiceType.FullName?.Contains("Npgsql") == true)
                    .ToList();
                foreach (var s in efServices)
                    services.Remove(s);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        });

        Client = _factory.CreateClient();
        var scope = _factory.Services.CreateScope();
        Db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        await _factory.DisposeAsync();
    }

    protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
