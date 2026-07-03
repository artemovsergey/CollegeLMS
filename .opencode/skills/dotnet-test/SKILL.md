---
name: dotnet-test
description: Create xUnit tests for .NET API — unit tests for services, integration tests for controllers, InMemory EF, Moq, Bogus
---

# dotnet-test

Create unit and integration tests for CollegeLMS .NET API following xUnit conventions with InMemory EF Core, Moq, and Bogus for test data generation.

## Workflow

### 1. Create test project (first time only)

```powershell
dotnet new xunit -n CollegeLMS.Tests -o CollegeLMS.Tests
dotnet add CollegeLMS.Tests reference CollegeLMS.API
dotnet add CollegeLMS.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add CollegeLMS.Tests package Moq
dotnet add CollegeLMS.Tests package Bogus
dotnet add CollegeLMS.Tests package coverlet.msbuild
```

### 2. Project structure

```
CollegeLMS.Tests/
  CollegeLMS.Tests.csproj
  Services/
    {Name}ServiceTests.cs
  Controllers/
    {Name}ControllerTests.cs
  Fixtures/
    TestDbContextFactory.cs
    TestWebApplicationFactory.cs
```

### 3. InMemory DbContext factory

Path: `CollegeLMS.Tests/Fixtures/TestDbContextFactory.cs`

```csharp
using CollegeLMS.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Fixtures;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
```

### 4. WebApplicationFactory for integration tests

Path: `CollegeLMS.Tests/Fixtures/TestWebApplicationFactory.cs`

```csharp
using CollegeLMS.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });
    }
}
```

### 5. Unit tests for services

Path: `CollegeLMS.Tests/Services/{Name}ServiceTests.cs`

```csharp
using CollegeLMS.API.Entities;
using CollegeLMS.Tests.Fixtures;
using Bogus;

namespace CollegeLMS.Tests.Services;

public class {Name}ServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly {Name}Service _sut;

    public {Name}ServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new {Name}Service(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSuccess()
    {
        var entity = new Faker<{Name}>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .Generate();
        _db.Set<{Name}>().Add(entity);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        var request = new Faker<{Action}{Name}>()
            .RuleFor(x => x.{property}, f => f.Lorem.Word())
            .Generate();

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(await _db.Set<{Name}>().AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsSuccess()
    {
        var entity = new Faker<{Name}>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .Generate();
        _db.Set<{Name}>().Add(entity);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(entity.Id);

        Assert.True(result.IsSuccess);
        Assert.False(await _db.Set<{Name}>().AnyAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsNotFound()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }
}
```

### 6. Integration tests for controllers

Path: `CollegeLMS.Tests/Controllers/{Name}ControllerTests.cs`

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.Tests.Fixtures;

namespace CollegeLMS.Tests.Controllers;

public class {Name}ControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public {Name}ControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/{name}");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.Contains("items", content);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/{name}/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOk()
    {
        var request = new {Action}{Name}Request
        {
        };

        var response = await _client.PostAsJsonAsync("/api/{name}", request);

        response.EnsureSuccessStatusCode();
    }
}
```

### 7. Run tests

```powershell
# Run all tests
dotnet test CollegeLMS.Tests --verbosity normal

# Run with code coverage
dotnet test CollegeLMS.Tests --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test CollegeLMS.Tests --filter "FullyQualifiedName~UserServiceTests"

# Run specific test method
dotnet test CollegeLMS.Tests --filter "GetByIdAsync_ExistingId_ReturnsSuccess"
```

## Naming conventions

- Test method: `{Method}_{Scenario}_{Expected}`
- Examples: `GetByIdAsync_NonExistingId_ReturnsNotFound`, `CreateAsync_ValidRequest_ReturnsSuccess`
- One assertion per test (one scenario)
- Test class per service/controller

## Convention rules

- InMemory database for test isolation (new GUID per test class)
- `IDisposable` to clean up DbContext
- Bogus for test data generation
- Arrange-Act-Assert pattern
- `CancellationToken.None` in test calls
- No real database connections in unit tests
- WebApplicationFactory for integration tests (no HTTP server needed)

## Verification

- `dotnet test CollegeLMS.Tests` — all tests pass
- Each service method has at least happy path + error case tests
- Integration tests verify HTTP status codes and response structure
