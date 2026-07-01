using System.Text.Json;
using CollegeLMS.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected AppDbContext Db = null!;
    protected readonly WebApplicationFactory<Program> Factory;
    private readonly string _dbName;

    protected BaseIntegrationTest()
    {
        _dbName = $"TestDb_{Guid.NewGuid()}";
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Testing");
            builder.ConfigureServices(services =>
            {
                var efServices = services
                    .Where(s =>
                        s.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                        || s.ServiceType.FullName?.Contains("Npgsql") == true
                    )
                    .ToList();
                foreach (var s in efServices)
                    services.Remove(s);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName)
                );
            });
        });

        Client = Factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }

    protected AppDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}
