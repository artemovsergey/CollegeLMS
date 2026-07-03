---
name: testing-xunit
description: Create xUnit test project with WebApplicationFactory, Bogus fixtures, Controller/Service tests, and coverage
---

# testing-xunit

Set up and write tests for CollegeLMS API using xUnit + WebApplicationFactory + Bogus + Coverlet.

## Workflow

### 1. Create test project

```powershell
# From repo root
dotnet new xunit -n CollegeLMS.Tests -o CollegeLMS.Tests
cd CollegeLMS.Tests

dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
dotnet add package Bogus
dotnet add package coverlet.msbuild
dotnet add package FluentAssertions
dotnet add package Testcontainers.PostgreSql

cd ..
dotnet add CollegeLMS.Tests/CollegeLMS.Tests.csproj reference CollegeLMS.API/CollegeLMS.csproj
```

### 2. Directory structure

```
CollegeLMS.Tests/
  Integration/
    BaseIntegrationTest.cs
    Controllers/
      AuthControllerTests.cs
      {Name}ControllerTests.cs
  Unit/
    Services/
      {Name}ServiceTests.cs
  Fixtures/
    {Name}Fixture.cs
```

### 3. BaseIntegrationTest

**`Integration/BaseIntegrationTest.cs`:**

```csharp
using System.Text.Json;
using CollegeLMS.API.Data;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(AppDbContext));
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

    protected async Task<Result<T>?> GetResultAsync<T>(HttpResponseMessage response)
    {
        return await DeserializeAsync<Result<T>>(response);
    }
}
```

### 4. Controller test pattern

**`Integration/Controllers/{Name}ControllerTests.cs`:**

```csharp
using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;

namespace CollegeLMS.Tests.Integration.Controllers;

public class {Name}ControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task GetAll_ReturnsOk_WithList()
    {
        var response = await Client.GetAsync("/api/{name}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeAsync<Result<List<{Name}Response>>>(response);
        Assert.NotNull(result);
        Assert.True(result!.IsSuccess);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var response = await Client.GetAsync($"/api/{{name}}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsOk_WithCreatedItem()
    {
        var request = new Create{Name}Request
        {
            // set required properties
        };

        var response = await Client.PostAsJsonAsync("/api/{name}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeAsync<Result<{Name}Response>>(response);
        Assert.NotNull(result);
        Assert.True(result!.IsSuccess);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenInvalid()
    {
        var request = new Create{Name}Request(); // missing required fields

        var response = await Client.PostAsJsonAsync("/api/{name}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

### 5. Service test pattern (with Bogus)

**`Fixtures/{Name}Fixture.cs`:**

```csharp
using Bogus;
using CollegeLMS.API.Entities;

namespace CollegeLMS.Tests.Fixtures;

public static class {Name}Fixture
{
    public static Faker<{Name}> CreateFaker() =>
        new Faker<{Name}>()
            .RuleFor(x => x.Id, f => f.Random.Guid())
            .RuleFor(x => x.{Property}, f => f.Lorem.Word())
            .RuleFor(x => x.CreatedAt, f => f.Date.Past())
            .RuleFor(x => x.UpdatedAt, f => f.Date.Recent());
}
```

### 6. Running tests

```powershell
# From repo root
dotnet test CollegeLMS.Tests --verbosity normal

# With coverage
dotnet test CollegeLMS.Tests /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:ExcludeByAttribute="CompilerGenerated"

# With coverage + report
dotnet test CollegeLMS.Tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:CollegeLMS.Tests/TestResults/coverage.opencover.xml -targetdir:CollegeLMS.Tests/TestResults/Reports -reporttypes:Html
```

### 7. GitHub Actions test job reference

```yaml
test:
  runs-on: ubuntu-latest
  services:
    postgres:
      image: postgres:16-alpine
      env:
        POSTGRES_USER: postgres
        POSTGRES_PASSWORD: postgres
        POSTGRES_DB: collegelms
      ports: ["5432:5432"]
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: "10.0.x" }
    - run: dotnet restore
    - run: dotnet build --no-restore
    - run: dotnet test --no-build /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:ExcludeByAttribute="CompilerGenerated"
```

## Convention rules

- Use `BaseIntegrationTest` for all controller/integration tests
- Use InMemory database for unit/service tests (isolated per test)
- One test class per controller/service
- Test naming: `{Method}_Returns{Expected}_When{Condition}`
- Arrange → Act → Assert with empty lines between
- Integration tests cover: 200 OK, 400 BadRequest, 401 Unauthorized, 404 NotFound
- Use Bogus `Faker<T>` for entity fixtures
- Skip tests that require real Postgres with `[Fact(Skip = "Requires DB")]`

## Verification checklist

- [ ] `dotnet test` — all tests green
- [ ] Coverage report generated (minimum 60% line coverage in MVP)
- [ ] Each controller has at least: GetAll, GetById, Create, Delete tests
- [ ] Auth endpoints tested: login success + invalid credentials
- [ ] Pagination/filter endpoints tested for edge cases
