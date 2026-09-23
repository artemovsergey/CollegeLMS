# Поток данных бот MAX ↔ мини-приложение ↔ API — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Связать бот MAX и мини-приложение: бот открывает мини-апп кнопками `open_app`, API проверяет MAX `initData`, выдаёт JWT с claims, а `/context` и `/journal` умеют работать с гостем без CRM-записи.

**Architecture:** Бот остаётся источником профиля (`user_settings`) и отдаёт его по внутреннему endpoint с секретом. API анонимно принимает `initData`, проверяет подпись MAX, забирает профиль у бота и выдаёт обычный JWT с дополнительными claims (`max_user_id`, `role`, `groupId`/`teacherId`). Мини-приложение при наличии MAX Bridge логинится этим путём; `ScheduleController` при отсутствии CRM-записи строит контекст из доверенных claims.

**Tech Stack:** .NET 10 (ASP.NET Core minimal API + контроллеры, EF Core, xUnit, FluentAssertions), Next.js 14 (App Router, TypeScript, axios), Playwright, Docker Compose, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-22-bot-miniapp-data-flow-design.md`

## Global Constraints

- Целевой фреймворк: .NET 10; тесты — xUnit + FluentAssertions; бот-тесты в `CollegeLMS.MaxBot.Tests`, API-тесты в `CollegeLMS.Tests`.
- Данные, комментарии, документация и сообщения об ошибках — на русском.
- Никаких try-catch в контроллерах/сервисах API без необходимости; исключения ловит middleware. Внешние HTTP-вызовы бота — fail-safe (catch + log), как в существующем `MaxBotHttpClient`.
- `Result<T>` (namespace `CollegeLMS.API.Response`) для всех ответов API; `CancellationToken ct` на всех async-методах; file-scoped namespaces; primary constructor DI; ручные мапперы.
- Роли: бот хранит `user_settings.Role` в нижнем регистре (`student`/`teacher`); API отдаёт `Student`/`Teacher`/`Other` (тип `MaxRole`), claim `role` — PascalCase.
- Алгоритм проверки MAX `initData` — строго по спеке: `secret_key = HMAC-SHA256(key="WebAppData", message=BOT_TOKEN)`, `signature = hex(HMAC-SHA256(secret_key, launch_params))`, `launch_params` = пары `key=value`, отсортированные по ключу (a→z), `hash` исключён, значения URL-декодированы, соединены `\n` (0x0A); ровно один `hash`; `auth_date` не старше 1 часа.
- Форматирование: перед коммитом прогонять `dotnet csharpier format .`.
- Команды сборки/тестов: `dotnet build CollegeLMS.slnx`, `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}`.

## Review Focus

Классы входов/сбоев, которые спека подразумевает, но которые легко упустить (каждый закрыт тестом в своей задаче):

1. `initData` с **дублирующимся `hash`** (или без него) — должен быть отклонён, а не «взят первый/последний» (Task 3).
2. `auth_date` старше 1 часа (повторное использование) — 401, даже при корректной подписи (Task 3, Task 6).
3. **Бот недоступен** при запросе профиля — API всё равно отдаёт 200 с `role: "Other"` и валидным токеном (Task 6).
4. **Гость-преподаватель без claim `teacherId`** — журнал недоступен (400), а не 500/403-сюрприз (Task 7).
5. **Браузер без MAX Bridge** (нет `window.WebApp`) — мини-апп не дёргает `/api/auth/max` и 401 внутри `/max` **не** редиректит на `/login` (Task 10).

---

### Task 1: JWT с дополнительными claims (API)

**Files:**
- Modify: `CollegeLMS.API/Interfaces/ITokenService.cs`
- Modify: `CollegeLMS.API/Services/JwtTokenService.cs:46-77`
- Test: `CollegeLMS.Tests/Unit/Services/JwtTokenServiceTests.cs` (create)

**Interfaces:**
- Consumes: —
- Produces: `ITokenService.GenerateCustomToken(IReadOnlyCollection<string> roles, int lifetimeMinutes, string nameIdentifier, IEnumerable<Claim>? extraClaims = null)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CollegeLMS.Tests.Unit.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService Create() =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = "TestKey_12345678901234567890123456789012",
                        ["Jwt:Issuer"] = "CollegeLMS",
                        ["Jwt:Audience"] = "CollegeLMS.Clients",
                    }
                )
                .Build()
        );

    [Fact]
    public void GenerateCustomToken_AddsExtraClaims()
    {
        var teacherId = Guid.NewGuid();

        var token = Create()
            .GenerateCustomToken(
                ["Teacher"],
                60,
                Guid.NewGuid().ToString(),
                [
                    new Claim("teacherId", teacherId.ToString()),
                    new Claim("max_user_id", "42"),
                ]
            );

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "teacherId" && c.Value == teacherId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "max_user_id" && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Teacher");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~JwtTokenServiceTests`
Expected: FAIL — `GenerateCustomToken` не принимает 4-й аргумент.

- [ ] **Step 3: Write minimal implementation**

`ITokenService.cs` — добавь параметр и using:

```csharp
using System.Security.Claims;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateCustomToken(
        IReadOnlyCollection<string> roles,
        int lifetimeMinutes,
        string nameIdentifier,
        IEnumerable<Claim>? extraClaims = null
    );
}
```

`JwtTokenService.cs` — добавь параметр и `AddRange`:

```csharp
    public string GenerateCustomToken(
        IReadOnlyCollection<string> roles,
        int lifetimeMinutes,
        string nameIdentifier,
        IEnumerable<Claim>? extraClaims = null
    )
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nameIdentifier),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        if (extraClaims is not null)
            claims.AddRange(extraClaims);
```

Остальное тело метода не меняется.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~JwtTokenServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Interfaces/ITokenService.cs CollegeLMS.API/Services/JwtTokenService.cs CollegeLMS.Tests/Unit/Services/JwtTokenServiceTests.cs
git commit -m "feat: дополнительные claims в GenerateCustomToken"
```

---

### Task 2: Хелперы чтения claims (API)

**Files:**
- Modify: `CollegeLMS.API/Extensions/ClaimsPrincipalExtensions.cs`
- Test: `CollegeLMS.Tests/Unit/Extensions/ClaimsPrincipalExtensionsTests.cs` (create)

**Interfaces:**
- Consumes: —
- Produces: `Guid? GetMaxGroupId(this ClaimsPrincipal)`, `Guid? GetMaxTeacherId(this ClaimsPrincipal)`, `long? GetMaxUserId(this ClaimsPrincipal)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Security.Claims;
using CollegeLMS.API.Extensions;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void GetMaxGroupId_ReadsGuidClaim()
    {
        var id = Guid.NewGuid();
        Principal(new Claim("groupId", id.ToString())).GetMaxGroupId().Should().Be(id);
    }

    [Fact]
    public void GetMaxTeacherId_MissingClaim_ReturnsNull()
    {
        Principal().GetMaxTeacherId().Should().BeNull();
    }

    [Fact]
    public void GetMaxUserId_ReadsLongClaim()
    {
        Principal(new Claim("max_user_id", "777")).GetMaxUserId().Should().Be(777);
    }

    [Fact]
    public void GetMaxUserId_NonNumeric_ReturnsNull()
    {
        Principal(new Claim("max_user_id", "abc")).GetMaxUserId().Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ClaimsPrincipalExtensionsTests`
Expected: FAIL — методов нет.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Security.Claims;

namespace CollegeLMS.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)!;

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)!;

    public static Guid? GetMaxGroupId(this ClaimsPrincipal user) =>
        ParseGuid(user.FindFirstValue("groupId"));

    public static Guid? GetMaxTeacherId(this ClaimsPrincipal user) =>
        ParseGuid(user.FindFirstValue("teacherId"));

    public static long? GetMaxUserId(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue("max_user_id"), out var id) ? id : null;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ClaimsPrincipalExtensionsTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Extensions/ClaimsPrincipalExtensions.cs CollegeLMS.Tests/Unit/Extensions/ClaimsPrincipalExtensionsTests.cs
git commit -m "feat: хелперы чтения claims MAX (groupId/teacherId/max_user_id)"
```

---

### Task 3: Проверка `initData` (API)

**Files:**
- Create: `CollegeLMS.API/Services/MaxInitDataValidator.cs`
- Create: `CollegeLMS.API/Dtos/MaxInitDataPayload.cs`
- Test: `CollegeLMS.Tests/Unit/Services/MaxInitDataValidatorTests.cs` (create)

**Interfaces:**
- Consumes: —
- Produces:
  - `class MaxInitDataPayload { long MaxUserId; string FullName; string? StartParam; }`
  - `class MaxInitDataValidator(IConfiguration config, TimeProvider timeProvider)` с методом `Result<MaxInitDataPayload> Validate(string? initData)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CollegeLMS.Tests.Unit.Services;

public class MaxInitDataValidatorTests
{
    private const string BotToken = "bot-token-123";
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    private static MaxInitDataValidator Create() =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["MaxAuth:BotToken"] = BotToken })
                .Build(),
            new FixedTimeProvider(Now)
        );

    private static string Build(
        Dictionary<string, string> pairs,
        DateTimeOffset? authDate = null,
        string? hashOverride = null
    )
    {
        pairs["auth_date"] = (authDate ?? Now).ToUnixTimeSeconds().ToString();
        var launch = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")
        );
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(BotToken)
        );
        var hash =
            hashOverride
            ?? Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
                .ToLowerInvariant();
        return string.Join(
                "&",
                pairs.OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
            )
            + $"&hash={hash}";
    }

    private static Dictionary<string, string> User(long id) =>
        new()
        {
            ["query_id"] = "q1",
            ["user"] = JsonSerializer.Serialize(new { id, first_name = "Иван", last_name = "Иванов" }),
        };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void Validate_ValidSignature_ReturnsPayload()
    {
        var result = Create().Validate(Build(User(42)));

        result.IsSuccess.Should().BeTrue();
        result.Data!.MaxUserId.Should().Be(42);
        result.Data.FullName.Should().Be("Иван Иванов");
    }

    [Fact]
    public void Validate_ExtractsStartParam()
    {
        var pairs = User(42);
        pairs["start_param"] = "day-2026-09-07-g-11111111-1111-1111-1111-111111111111";

        var result = Create().Validate(Build(pairs));

        result.IsSuccess.Should().BeTrue();
        result.Data!.StartParam.Should().Be("day-2026-09-07-g-11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Validate_TamperedHash_Fails()
    {
        var result = Create().Validate(Build(User(42), hashOverride: new string('a', 64)));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_DuplicateHash_Fails()
    {
        var initData = Build(User(42)) + "&hash=deadbeef";

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_MissingHash_Fails()
    {
        var initData = Build(User(42));
        initData = initData[..initData.LastIndexOf("&hash=", StringComparison.Ordinal)];

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validate_StaleAuthDate_Fails()
    {
        var result = Create().Validate(Build(User(42), authDate: Now.AddHours(-2)));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_WrongBotTokenSignature_Fails()
    {
        var pairs = User(42);
        pairs["auth_date"] = Now.ToUnixTimeSeconds().ToString();
        var launch = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")
        );
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes("другой-токен")
        );
        var hash = Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
            .ToLowerInvariant();
        var initData =
            string.Join("&", pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))
            + $"&hash={hash}";

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxInitDataValidatorTests`
Expected: FAIL — тип `MaxInitDataValidator` не найден.

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.API/Dtos/MaxInitDataPayload.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class MaxInitDataPayload
{
    public long MaxUserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? StartParam { get; init; }
}
```

`CollegeLMS.API/Services/MaxInitDataValidator.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

/// <summary>
/// Проверка подписи MAX WebAppData. Алгоритм: dev.max.ru/docs/webapps/validation.
/// </summary>
public class MaxInitDataValidator(IConfiguration config, TimeProvider timeProvider)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    public Result<MaxInitDataPayload> Validate(string? initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
            return Result<MaxInitDataPayload>.Fail("initData пуст.", 401);

        var pairs = new List<KeyValuePair<string, string>>();
        string? hash = null;
        foreach (var chunk in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = chunk.IndexOf('=');
            if (idx <= 0)
                continue;
            var key = chunk[..idx];
            var value = Uri.UnescapeDataString(chunk[(idx + 1)..]);
            if (key == "hash")
            {
                if (hash is not null)
                    return Result<MaxInitDataPayload>.Fail("initData: дублирующийся hash.", 401);
                hash = value;
                continue;
            }
            pairs.Add(new(key, value));
        }

        if (hash is null)
            return Result<MaxInitDataPayload>.Fail("initData: отсутствует hash.", 401);

        var botToken = config["MaxAuth:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return Result<MaxInitDataPayload>.Fail("Проверка MAX не настроена.", 401);

        var launchParams = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")
        );

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(botToken)
        );
        var signature = Convert
            .ToHexString(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(launchParams)))
            .ToLowerInvariant();

        if (
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(hash.ToLowerInvariant())
            )
        )
            return Result<MaxInitDataPayload>.Fail("initData: подпись не совпадает.", 401);

        var authDateRaw = pairs.FirstOrDefault(p => p.Key == "auth_date").Value;
        if (!long.TryParse(authDateRaw, out var authDateUnix))
            return Result<MaxInitDataPayload>.Fail("initData: нет auth_date.", 401);

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        if (timeProvider.GetUtcNow() - authDate > MaxAge)
            return Result<MaxInitDataPayload>.Fail("initData просрочен.", 401);

        var userRaw = pairs.FirstOrDefault(p => p.Key == "user").Value;
        if (string.IsNullOrWhiteSpace(userRaw))
            return Result<MaxInitDataPayload>.Fail("initData: нет user.", 401);

        long userId;
        string? firstName = null;
        string? lastName = null;
        try
        {
            using var doc = JsonDocument.Parse(userRaw);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt64(out userId))
                return Result<MaxInitDataPayload>.Fail("initData: нет user.id.", 401);
            if (root.TryGetProperty("first_name", out var fn))
                firstName = fn.GetString();
            if (root.TryGetProperty("last_name", out var ln))
                lastName = ln.GetString();
        }
        catch (JsonException)
        {
            return Result<MaxInitDataPayload>.Fail("initData: некорректный user.", 401);
        }

        var startParam = pairs.FirstOrDefault(p => p.Key == "start_param").Value;

        return Result<MaxInitDataPayload>.Ok(
            new MaxInitDataPayload
            {
                MaxUserId = userId,
                FullName = string.Join(
                    " ",
                    new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s))
                ),
                StartParam = string.IsNullOrWhiteSpace(startParam) ? null : startParam,
            }
        );
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxInitDataValidatorTests`
Expected: PASS (7 тестов).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Services/MaxInitDataValidator.cs CollegeLMS.API/Dtos/MaxInitDataPayload.cs CollegeLMS.Tests/Unit/Services/MaxInitDataValidatorTests.cs
git commit -m "feat: проверка подписи MAX initData"
```

---

### Task 4: Внутренний endpoint профиля в боте

**Files:**
- Modify: `CollegeLMS.MaxBot/MaxBotOptions.cs`
- Create: `CollegeLMS.MaxBot/Models/InternalUserProfile.cs`
- Create: `CollegeLMS.MaxBot/Services/InternalProfileService.cs`
- Modify: `CollegeLMS.MaxBot/Program.cs:21-22` (DI), `:126-137` (убрать DDL `bot_favorites`), после `:283` (endpoint)
- Test: `CollegeLMS.MaxBot.Tests/InternalProfileServiceTests.cs` (create)

**Interfaces:**
- Consumes: `CollegeLmsApiClient.GetGroupsAsync(ct)`, `GetTeachersAsync(ct)`; `WebhookSecretValidator.IsValid(expected, provided)`.
- Produces: `record InternalUserProfile { bool Found; long MaxUserId; string Role; Guid? GroupId; string? GroupName; Guid? TeacherId; string? TeacherName; }`; `class InternalProfileService(MaxBotDbContext db, CollegeLmsApiClient api)` с `Task<InternalUserProfile> GetAsync(long maxUserId, CancellationToken ct)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

public class InternalProfileServiceTests
{
    private sealed class StubHandler(Func<string, string> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var url = request.RequestUri!.ToString();
            var body = respond(url.Contains("/api/groups") ? "groups" : "teachers");
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private static CollegeLmsApiClient BuildApi(string groups, string teachers) =>
        new(
            new HttpClient(new StubHandler(kind => kind == "groups" ? groups : teachers))
            {
                BaseAddress = new Uri("http://api.unit.test"),
            },
            NullLogger<CollegeLmsApiClient>.Instance
        );

    private static MaxBotDbContext BuildDb() =>
        new(
            new DbContextOptionsBuilder<MaxBotDbContext>()
                .UseInMemoryDatabase($"maxbot_{Guid.NewGuid()}")
                .Options
        );

    [Fact]
    public async Task GetAsync_ReturnsProfileWithResolvedNames()
    {
        var groupId = Guid.NewGuid();
        await using var db = BuildDb();
        db.UserSettings.Add(
            new UserSettings
            {
                Id = Guid.NewGuid(),
                MaxUserId = 42,
                MaxChatId = 42,
                Role = "student",
                GroupId = groupId,
            }
        );
        await db.SaveChangesAsync();

        var api = BuildApi(
            $$"""{"isSuccess":true,"data":[{"id":"{{groupId}}","name":"ИС-21-1"}]}""",
            """{"isSuccess":true,"data":[]}"""
        );
        var service = new InternalProfileService(db, api);

        var profile = await service.GetAsync(42, CancellationToken.None);

        profile.Found.Should().BeTrue();
        profile.Role.Should().Be("student");
        profile.GroupId.Should().Be(groupId);
        profile.GroupName.Should().Be("ИС-21-1");
    }

    [Fact]
    public async Task GetAsync_UnknownUser_ReturnsNotFound()
    {
        await using var db = BuildDb();
        var api = BuildApi("""{"isSuccess":true,"data":[]}""", """{"isSuccess":true,"data":[]}""");
        var service = new InternalProfileService(db, api);

        var profile = await service.GetAsync(999, CancellationToken.None);

        profile.Found.Should().BeFalse();
        profile.Role.Should().Be("student");
    }

    [Fact]
    public void InternalSecret_GuardRejectsWrongValue()
    {
        WebhookSecretValidator.IsValid("secret", "other").Should().BeFalse();
        WebhookSecretValidator.IsValid("secret", "secret").Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~InternalProfileServiceTests`
Expected: FAIL — `InternalProfileService` не найден.

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.MaxBot/Models/InternalUserProfile.cs`:

```csharp
namespace CollegeLMS.MaxBot.Models;

/// <summary>Профиль MAX-пользователя для внутреннего endpoint (внутри сервиса бота).</summary>
public record InternalUserProfile
{
    public bool Found { get; init; }
    public long MaxUserId { get; init; }
    public string Role { get; init; } = "student";
    public Guid? GroupId { get; init; }
    public string? GroupName { get; init; }
    public Guid? TeacherId { get; init; }
    public string? TeacherName { get; init; }
}
```

`CollegeLMS.MaxBot/Services/InternalProfileService.cs`:

```csharp
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Собирает профиль MAX-пользователя из user_settings и имён групп/преподавателей.</summary>
public class InternalProfileService(MaxBotDbContext db, CollegeLmsApiClient api)
{
    public async Task<InternalUserProfile> GetAsync(long maxUserId, CancellationToken ct)
    {
        var settings = await db
            .UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaxUserId == maxUserId, ct);
        if (settings is null)
            return new InternalUserProfile
            {
                Found = false,
                MaxUserId = maxUserId,
                Role = "student",
            };

        string? groupName = null;
        string? teacherName = null;
        if (settings.GroupId.HasValue)
            groupName = (await api.GetGroupsAsync(ct))
                .FirstOrDefault(g => g.Id == settings.GroupId.Value)
                ?.Name;
        if (settings.TeacherId.HasValue)
            teacherName = (await api.GetTeachersAsync(ct))
                .FirstOrDefault(t => t.Id == settings.TeacherId.Value)
                ?.FullName;

        return new InternalUserProfile
        {
            Found = true,
            MaxUserId = maxUserId,
            Role = settings.Role,
            GroupId = settings.GroupId,
            GroupName = groupName,
            TeacherId = settings.TeacherId,
            TeacherName = teacherName,
        };
    }
}
```

`MaxBotOptions.cs` — добавь в конец класса:

```csharp
    /// <summary>Публичное имя бота — поле web_app у кнопок open_app.</summary>
    public string BotPublicName { get; set; } = "";

    /// <summary>Секрет внутреннего endpoint профиля (заголовок X-Internal-Secret).</summary>
    public string InternalSecret { get; set; } = "";
```

`Program.cs` — регистрация сервиса (рядом с другими scoped):

```csharp
builder.Services.AddScoped<InternalProfileService>();
```

`Program.cs` — удали блок DDL `bot_favorites` (строки с `CREATE TABLE IF NOT EXISTS bot_favorites` и `CREATE UNIQUE INDEX IF NOT EXISTS ix_bot_favorites_user_type_target`).

`Program.cs` — добавь endpoint после `/maxbot/webhook` (перед `app.Run();`):

```csharp
// Внутренний профиль MAX-пользователя для CollegeLMS API (проверка подписи initData).
app.MapGet(
    "/maxbot/internal/users/{maxUserId:long}",
    async (
        long maxUserId,
        HttpRequest request,
        InternalProfileService profileService,
        IOptions<MaxBotOptions> options,
        CancellationToken ct
    ) =>
    {
        var provided = request.Headers["X-Internal-Secret"].ToString();
        if (!WebhookSecretValidator.IsValid(options.Value.InternalSecret, provided))
            return Results.Unauthorized();

        var profile = await profileService.GetAsync(maxUserId, ct);
        return Results.Ok(profile);
    }
);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~InternalProfileServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.MaxBot/MaxBotOptions.cs CollegeLMS.MaxBot/Models/InternalUserProfile.cs CollegeLMS.MaxBot/Services/InternalProfileService.cs CollegeLMS.MaxBot/Program.cs CollegeLMS.MaxBot.Tests/InternalProfileServiceTests.cs
git commit -m "feat: внутренний endpoint профиля MAX-пользователя в боте"
```

---

### Task 5: Клиент профиля на стороне API

**Files:**
- Create: `CollegeLMS.API/Dtos/MaxInternalUserDto.cs`
- Modify: `CollegeLMS.API/Services/MaxBotHttpClient.cs:1-12`
- Test: `CollegeLMS.Tests/Unit/Services/MaxBotHttpClientTests.cs` (create)

**Interfaces:**
- Consumes: `MaxBot:InternalSecret`, `MaxBot:BaseUrl`.
- Produces: `Task<MaxInternalUserDto?> GetInternalUserAsync(long maxUserId, CancellationToken ct)`; `class MaxInternalUserDto { bool Found; long MaxUserId; string Role; Guid? GroupId; string? GroupName; Guid? TeacherId; string? TeacherName; }`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Text;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

public class MaxBotHttpClientTests
{
    private sealed class CapturingHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public string? Secret { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Last = request;
            Secret = request.Headers.TryGetValues("X-Internal-Secret", out var v)
                ? v.First()
                : null;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private static MaxBotHttpClient Build(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://maxbot.unit.test") },
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["MaxBot:InternalSecret"] = "s3cr3t" }
                )
                .Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );

    [Fact]
    public async Task GetInternalUserAsync_SendsSecretAndParses()
    {
        var groupId = Guid.NewGuid();
        var handler = new CapturingHandler(
            $$"""{"found":true,"maxUserId":42,"role":"student","groupId":"{{groupId}}","groupName":"ИС-21-1","teacherId":null,"teacherName":null}"""
        );

        var dto = await Build(handler).GetInternalUserAsync(42, CancellationToken.None);

        handler.Last!.RequestUri!.AbsolutePath.Should().Be("/maxbot/internal/users/42");
        handler.Secret.Should().Be("s3cr3t");
        dto!.Found.Should().BeTrue();
        dto.GroupName.Should().Be("ИС-21-1");
    }

    [Fact]
    public async Task GetInternalUserAsync_BotError_ReturnsNull()
    {
        var handler = new CapturingHandler("{}");
        // переиспользуем, но вернём 500
        var errorHandler = new StubHandler(HttpStatusCode.InternalServerError, "{}");
        var dto = await Build(errorHandler).GetInternalUserAsync(42, CancellationToken.None);

        dto.Should().BeNull();
        _ = handler;
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxBotHttpClientTests`
Expected: FAIL — нет конструктора с 3 аргументами / нет `GetInternalUserAsync`.

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.API/Dtos/MaxInternalUserDto.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class MaxInternalUserDto
{
    public bool Found { get; set; }
    public long MaxUserId { get; set; }
    public string Role { get; set; } = "student";
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}
```

`MaxBotHttpClient.cs` — замени usings и primary constructor, добавь метод:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CollegeLMS.API.Dtos;

namespace CollegeLMS.API.Services;

public class MaxBotHttpClient(
    HttpClient http,
    IConfiguration config,
    ILogger<MaxBotHttpClient> logger
)
{
    public async Task<MaxInternalUserDto?> GetInternalUserAsync(
        long maxUserId,
        CancellationToken ct
    )
    {
        try
        {
            var secret = config["MaxBot:InternalSecret"] ?? "";
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/maxbot/internal/users/{maxUserId}"
            );
            if (!string.IsNullOrEmpty(secret))
                request.Headers.Add("X-Internal-Secret", secret);

            var resp = await http.SendAsync(request, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("MaxBot internal users вернул {Code}", resp.StatusCode);
                return null;
            }

            return await resp.Content.ReadFromJsonAsync<MaxInternalUserDto>(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MaxBot недоступен — профиль MAX не получен");
            return null;
        }
    }
```

Существующие методы `SendChangesAsync`/`SendCorrectionImageAsync` остаются без изменений. Проверь, что нет других вызовов `new MaxBotHttpClient(` (grep) — если есть, обнови.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxBotHttpClientTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Dtos/MaxInternalUserDto.cs CollegeLMS.API/Services/MaxBotHttpClient.cs CollegeLMS.Tests/Unit/Services/MaxBotHttpClientTests.cs
git commit -m "feat: запрос профиля MAX у бота из API"
```

---

### Task 6: `POST /api/auth/max` (API)

**Files:**
- Create: `CollegeLMS.API/Dtos/MaxAuthDtos.cs`
- Create: `CollegeLMS.API/Interfaces/IMaxAuthService.cs`
- Create: `CollegeLMS.API/Services/MaxAuthService.cs`
- Create: `CollegeLMS.API/Controllers/MaxAuthController.cs`
- Modify: `CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs` (регистрация в `AddApplicationServices`)
- Test: `CollegeLMS.Tests/Integration/Controllers/MaxAuthApiTests.cs` (create)

**Interfaces:**
- Consumes: `MaxInitDataValidator.Validate`, `MaxBotHttpClient.GetInternalUserAsync`, `ITokenService.GenerateCustomToken`.
- Produces: `POST /api/auth/max` → `Result<MaxAuthResponse>`; `class MaxAuthRequest { string InitData }`, `class MaxAuthProfile { long MaxUserId; string? FullName; string Role; Guid? GroupId; string? GroupName; Guid? TeacherId; string? TeacherName }`, `class MaxAuthResponse { string Token; MaxAuthProfile Profile }`; `interface IMaxAuthService { Task<Result<MaxAuthResponse>> LoginAsync(MaxAuthRequest request, CancellationToken ct) }`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class MaxAuthApiTests : BaseIntegrationTest
{
    private const string BotToken = "test-bot-token";
    private const long MaxUserId = 555000111;

    private sealed class BotStub(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var json = JsonSerializer.Serialize(
                new MaxInternalUserDto
                {
                    Found = true,
                    MaxUserId = MaxUserId,
                    Role = "student",
                    GroupId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GroupName = "ИС-21-1",
                }
            );
            return Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );
        }
    }

    private static string BuildInitData(long userId, DateTimeOffset authDate)
    {
        var pairs = new Dictionary<string, string>
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "q1",
            ["start_param"] = "today",
            ["user"] = JsonSerializer.Serialize(
                new
                {
                    id = userId,
                    first_name = "Иван",
                    last_name = "Иванов",
                }
            ),
        };
        var launch = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")
        );
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(BotToken)
        );
        var hash = Convert
            .ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
            .ToLowerInvariant();
        return string.Join(
                "&",
                pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
            )
            + $"&hash={hash}";
    }

    private HttpClient ClientWithBot(HttpStatusCode status)
    {
        var factory = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MaxAuth:BotToken", BotToken);
            builder.ConfigureServices(services =>
                services
                    .AddHttpClient<MaxBotHttpClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new BotStub(status))
            );
        });
        return factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidInitData_ReturnsTokenAndProfile()
    {
        var client = ClientWithBot(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.False(string.IsNullOrEmpty(body.Data!.Token));
        Assert.Equal("Student", body.Data.Profile.Role);
        Assert.Equal("ИС-21-1", body.Data.Profile.GroupName);
        Assert.Equal(MaxUserId, body.Data.Profile.MaxUserId);
    }

    [Fact]
    public async Task Login_BotUnavailable_StillReturnsTokenAsGuest()
    {
        var client = ClientWithBot(HttpStatusCode.InternalServerError);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<MaxAuthResponse>>(response);
        Assert.True(body!.IsSuccess);
        Assert.False(string.IsNullOrEmpty(body.Data!.Token));
        Assert.Equal("Other", body.Data.Profile.Role);
    }

    [Fact]
    public async Task Login_TamperedHash_Returns401()
    {
        var client = ClientWithBot(HttpStatusCode.OK);
        var initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow) + "tampered";

        var response = await client.PostAsJsonAsync("/api/auth/max", new { initData });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_StaleAuthDate_Returns401()
    {
        var client = ClientWithBot(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/api/auth/max",
            new { initData = BuildInitData(MaxUserId, DateTimeOffset.UtcNow.AddHours(-2)) }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxAuthApiTests`
Expected: FAIL — 404 на `/api/auth/max`.

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.API/Dtos/MaxAuthDtos.cs`:

```csharp
namespace CollegeLMS.API.Dtos;

public class MaxAuthRequest
{
    public string InitData { get; set; } = string.Empty;
}

public class MaxAuthProfile
{
    public long MaxUserId { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = "Other";
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}

public class MaxAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public MaxAuthProfile Profile { get; set; } = new();
}
```

`CollegeLMS.API/Interfaces/IMaxAuthService.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Interfaces;

public interface IMaxAuthService
{
    Task<Result<MaxAuthResponse>> LoginAsync(MaxAuthRequest request, CancellationToken ct);
}
```

`CollegeLMS.API/Services/MaxAuthService.cs`:

```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

public class MaxAuthService(
    MaxInitDataValidator validator,
    MaxBotHttpClient botClient,
    ITokenService tokenService
) : IMaxAuthService
{
    private const int TokenLifetimeMinutes = 1440;

    public async Task<Result<MaxAuthResponse>> LoginAsync(
        MaxAuthRequest request,
        CancellationToken ct
    )
    {
        var validation = validator.Validate(request.InitData);
        if (!validation.IsSuccess)
            return Result<MaxAuthResponse>.Fail(
                validation.ErrorMessage ?? "Невалидный initData.",
                401
            );

        var payload = validation.Data!;
        var botProfile = await botClient.GetInternalUserAsync(payload.MaxUserId, ct);

        var role = MapRole(botProfile?.Role);
        var profile = new MaxAuthProfile
        {
            MaxUserId = payload.MaxUserId,
            FullName = payload.FullName,
            Role = role,
            GroupId = botProfile?.GroupId,
            GroupName = botProfile?.GroupName,
            TeacherId = botProfile?.TeacherId,
            TeacherName = botProfile?.TeacherName,
        };

        var claims = new List<Claim> { new("max_user_id", payload.MaxUserId.ToString()) };
        if (profile.GroupId.HasValue)
            claims.Add(new Claim("groupId", profile.GroupId.Value.ToString()));
        if (profile.TeacherId.HasValue)
            claims.Add(new Claim("teacherId", profile.TeacherId.Value.ToString()));

        var roles = role == "Other" ? Array.Empty<string>() : new[] { role };
        var token = tokenService.GenerateCustomToken(
            roles,
            TokenLifetimeMinutes,
            SyntheticUserId(payload.MaxUserId).ToString(),
            claims
        );

        return Result<MaxAuthResponse>.Ok(new MaxAuthResponse { Token = token, Profile = profile });
    }

    private static string MapRole(string? botRole) =>
        botRole?.ToLowerInvariant() switch
        {
            "student" => "Student",
            "teacher" => "Teacher",
            _ => "Other",
        };

    private static Guid SyntheticUserId(long maxUserId)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"max:{maxUserId}"));
        return new Guid(bytes);
    }
}
```

`CollegeLMS.API/Controllers/MaxAuthController.cs`:

```csharp
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace CollegeLMS.API.Controllers;

/// <summary>Вход в API из мини-приложения MAX.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class MaxAuthController(IMaxAuthService maxAuthService) : ControllerBase
{
    /// <summary>Обмен MAX initData на JWT.</summary>
    /// <response code="200">Токен выдан</response>
    /// <response code="401">Невалидный или просроченный initData</response>
    [HttpPost("max")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [SwaggerOperation(Summary = "Вход через MAX мини-приложение")]
    [SwaggerResponse(200, "Токен выдан", typeof(Result<MaxAuthResponse>))]
    [SwaggerResponse(401, "Невалидный initData", typeof(ErrorResponse))]
    [SwaggerResponse(500, "Ошибка сервера", typeof(ErrorResponse))]
    [ProducesResponseType(typeof(Result<MaxAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<MaxAuthResponse>>> Login(
        MaxAuthRequest request,
        CancellationToken ct
    )
    {
        var result = await maxAuthService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
```

`ServiceCollectionExtensions.cs` — в `AddApplicationServices` добавь регистрации:

```csharp
services.AddScoped<MaxInitDataValidator>();
services.AddScoped<IMaxAuthService, MaxAuthService>();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MaxAuthApiTests`
Expected: PASS (4 теста).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.API/Dtos/MaxAuthDtos.cs CollegeLMS.API/Interfaces/IMaxAuthService.cs CollegeLMS.API/Services/MaxAuthService.cs CollegeLMS.API/Controllers/MaxAuthController.cs CollegeLMS.API/Extensions/ServiceCollectionExtensions.cs CollegeLMS.Tests/Integration/Controllers/MaxAuthApiTests.cs
git commit -m "feat: POST /api/auth/max — обмен initData на JWT"
```

---

### Task 7: Claims-aware контекст и журнал (API)

**Files:**
- Modify: `CollegeLMS.API/Interfaces/IScheduleService.cs` (после `GetContextAsync`, строки 22-25)
- Modify: `CollegeLMS.API/Services/ScheduleService.cs` (после `GetContextAsync`, строка 160)
- Modify: `CollegeLMS.API/Controllers/ScheduleController.cs:173-241`
- Test: `CollegeLMS.Tests/Integration/Controllers/ScheduleClaimsContextApiTests.cs` (create)

**Interfaces:**
- Consumes: `ClaimsPrincipalExtensions.GetMaxGroupId/GetMaxTeacherId`.
- Produces: `IScheduleService.GetContextByTargetAsync(Guid? groupId, Guid? teacherId, CancellationToken ct)`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleClaimsContextApiTests : BaseIntegrationTest
{
    private string GuestToken(string role, params Claim[] extra)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokenService.GenerateCustomToken([role], 60, Guid.NewGuid().ToString(), extra);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetContext_GuestTeacher_UsesTeacherIdClaim()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(
            GuestToken("Teacher", new Claim("teacherId", teacher.Id.ToString()))
        );

        var response = await Client.GetAsync("/api/schedule/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.Equal("Teacher", body!.Data!.Role);
        Assert.Equal(teacher.Id, body.Data.TeacherId);
    }

    [Fact]
    public async Task GetContext_GuestStudent_UsesGroupIdClaim()
    {
        var group = GroupFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Groups.Add(group);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GuestToken("Student", new Claim("groupId", group.Id.ToString())));

        var response = await Client.GetAsync("/api/schedule/context");

        var body = await DeserializeAsync<Result<ScheduleContextResponse>>(response);
        Assert.Equal("Student", body!.Data!.Role);
        Assert.Equal(group.Id, body.Data.GroupId);
    }

    [Fact]
    public async Task GetJournal_GuestTeacherWithClaim_ReturnsOk()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GuestToken("Teacher", new Claim("teacherId", teacher.Id.ToString())));

        var response = await Client.GetAsync($"/api/schedule/journal?teacherId={teacher.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJournal_GuestTeacherWithoutClaim_ReturnsBadRequest()
    {
        SetAuthHeader(GuestToken("Teacher"));

        var response = await Client.GetAsync("/api/schedule/journal");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ScheduleClaimsContextApiTests`
Expected: FAIL — гостевой контекст возвращает `Other`, журнал — 403/400 не по ожиданию.

- [ ] **Step 3: Write minimal implementation**

`IScheduleService.cs` — добавь после `GetContextAsync`:

```csharp
    Task<Result<ScheduleContextResponse>> GetContextByTargetAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct = default
    );
```

`ScheduleService.cs` — добавь после `GetContextAsync` (строка 160):

```csharp
    public async Task<Result<ScheduleContextResponse>> GetContextByTargetAsync(
        Guid? groupId,
        Guid? teacherId,
        CancellationToken ct
    )
    {
        if (teacherId.HasValue)
        {
            var teacher = await db
                .Teachers.AsNoTracking()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == teacherId.Value, ct);
            if (teacher is not null)
                return Result<ScheduleContextResponse>.Ok(
                    new ScheduleContextResponse
                    {
                        TeacherId = teacher.Id,
                        TeacherName = teacher.User.FullName,
                        Role = "Teacher",
                    }
                );
        }

        if (groupId.HasValue)
        {
            var group = await db
                .Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == groupId.Value, ct);
            if (group is not null)
                return Result<ScheduleContextResponse>.Ok(
                    new ScheduleContextResponse
                    {
                        GroupId = group.Id,
                        GroupName = group.Name,
                        Role = "Student",
                    }
                );
        }

        return Result<ScheduleContextResponse>.Ok(new ScheduleContextResponse { Role = "Other" });
    }
```

`ScheduleController.cs` — добавь приватный хелпер (например, после конструктора/полей, перед первым экшеном):

```csharp
    private async Task<Result<ScheduleContextResponse>> ResolveContextAsync(CancellationToken ct)
    {
        var result = await service.GetContextAsync(User.GetUserId(), ct);
        if (result.Data?.Role == "Other")
        {
            var groupId = User.GetMaxGroupId();
            var teacherId = User.GetMaxTeacherId();
            if (groupId.HasValue || teacherId.HasValue)
                return await service.GetContextByTargetAsync(groupId, teacherId, ct);
        }
        return result;
    }
```

В `GetContext` замени тело:

```csharp
        var result = await ResolveContextAsync(ct);
        return Ok(result);
```

В `GetJournal` замени оба обращения `await service.GetContextAsync(User.GetUserId(), ct)` на `await ResolveContextAsync(ct)`:

```csharp
        if (teacherId.HasValue && !User.IsInRole("Admin") && !User.IsInRole("Dispatcher"))
        {
            var context = await ResolveContextAsync(ct);
            if (!context.IsSuccess || context.Data!.TeacherId != teacherId)
                return Forbid();
        }

        var effectiveTeacherId = teacherId ?? (await ResolveContextAsync(ct)).Data?.TeacherId;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ScheduleClaimsContextApiTests`
Expected: PASS (4 теста).

- [ ] **Step 5: Run regression on существующие тесты контекста**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ScheduleContextApiTests`
Expected: PASS (CRM-путь не сломан).

- [ ] **Step 6: Commit**

```bash
git add CollegeLMS.API/Interfaces/IScheduleService.cs CollegeLMS.API/Services/ScheduleService.cs CollegeLMS.API/Controllers/ScheduleController.cs CollegeLMS.Tests/Integration/Controllers/ScheduleClaimsContextApiTests.cs
git commit -m "feat: claims-aware контекст и журнал расписания"
```

---

### Task 8: Кнопки `open_app`, чистка уведомления (бот)

**Files:**
- Create: `CollegeLMS.MaxBot/Services/MiniAppButtons.cs`
- Modify: `CollegeLMS.MaxBot/Services/MessageFormatter.cs:414` (убрать «Применено»), `:488` (сделать `DateForRevision` public)
- Modify: `CollegeLMS.MaxBot/Services/ChangeNotifier.cs:32-76`
- Test: `CollegeLMS.MaxBot.Tests/MiniAppButtonsTests.cs` (create)
- Modify: `CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs:599-647`
- Modify: `CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs` (при необходимости — компиляция)

**Interfaces:**
- Consumes: `MiniAppUrlBuilder.BuildStartPayload`, `MaxBotOptions.BotPublicName`.
- Produces: `static MaxButton MiniAppButtons.OpenSchedule(MaxBotOptions options)`, `static MaxButton MiniAppButtons.OpenDay(MaxBotOptions options, DateTime date, Guid? groupId, Guid? teacherId)`; `MessageFormatter.DateForRevision(ScheduleRevision)` — public; `ChangeNotifier.SelectRecipientsGrouped(...)` возвращает `List<(long ChatId, Guid? GroupId, Guid? TeacherId, List<ScheduleRevision> Revisions)>`.

- [ ] **Step 1: Write the failing test**

```csharp
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MiniAppButtonsTests
{
    private static readonly MaxBotOptions Options = new() { BotPublicName = "teacher_scc_bot" };

    [Fact]
    public void OpenSchedule_BuildsOpenAppButtonWithPayload()
    {
        var button = MiniAppButtons.OpenSchedule(Options);

        button.Type.Should().Be("open_app");
        button.WebApp.Should().Be("teacher_scc_bot");
        button.Payload.Should().Be("today");
        button.Url.Should().BeNull();
    }

    [Fact]
    public void OpenDay_IncludesDateAndGroupMarker()
    {
        var groupId = Guid.NewGuid();

        var button = MiniAppButtons.OpenDay(Options, new DateTime(2026, 9, 7), groupId, null);

        button.Type.Should().Be("open_app");
        button.Payload.Should().Be($"day-2026-09-07-g-{groupId}");
    }

    [Fact]
    public void OpenDay_PrefersGroupOverTeacher()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var button = MiniAppButtons.OpenDay(Options, new DateTime(2026, 9, 7), groupId, teacherId);

        button.Payload.Should().Be($"day-2026-09-07-g-{groupId}");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MiniAppButtonsTests`
Expected: FAIL — `MiniAppButtons` не найден.

- [ ] **Step 3: Write minimal implementation**

`CollegeLMS.MaxBot/Services/MiniAppButtons.cs`:

```csharp
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Кнопки open_app для перехода в мини-приложение.</summary>
public static class MiniAppButtons
{
    public static MaxButton OpenSchedule(MaxBotOptions options) =>
        new()
        {
            Type = "open_app",
            Text = "📱 Открыть расписание",
            WebApp = options.BotPublicName,
            Payload = MiniAppUrlBuilder.BuildStartPayload("today"),
        };

    public static MaxButton OpenDay(
        MaxBotOptions options,
        DateTime date,
        Guid? groupId,
        Guid? teacherId
    ) =>
        new()
        {
            Type = "open_app",
            Text = "📅 Открыть день",
            WebApp = options.BotPublicName,
            Payload = MiniAppUrlBuilder.BuildStartPayload("day", date, groupId, teacherId),
        };
}
```

`MessageFormatter.cs`:
- В `FormatRevisionCard` удали строку `sb.Append($"✅ Применено: {FormatAppliedAt(r.CreatedAt, timeZone)}");` (строка 414). Метод `FormatAppliedAt` можно оставить (используется тестами? если нет — удалить вместе с ним; проверь grep `FormatAppliedAt`).
- У `DateForRevision` замени `private static DateTime?` на `public static DateTime?` (строка 488).

`ChangeNotifier.cs` — обнови группировку и отправку:

```csharp
    public async Task NotifyAsync(List<ScheduleRevision> revisions, CancellationToken ct)
    {
        if (revisions.Count == 0)
            return;

        var groupNames = (await _api.GetGroupsAsync(ct)).ToDictionary(g => g.Id, g => g.Name);
        var teacherNames = (await _api.GetTeachersAsync(ct)).ToDictionary(
            t => t.Id,
            t => t.FullName
        );
        var settings = await _db.UserSettings.Where(u => u.NotifyEnabled).ToListAsync(ct);

        var recipients = SelectRecipientsGrouped(settings, groupNames, teacherNames, revisions);

        foreach (var (chatId, groupId, teacherId, recipientRevisions) in recipients)
        {
            try
            {
                var text = MessageFormatter.FormatCorrectionDigest(recipientRevisions, _timeZone);
                var buttons = BuildDayButtons(groupId, teacherId, recipientRevisions);
                await _max.SendInlineKeyboardAsync(chatId, text, buttons, ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить уведомление в чат {ChatId}", chatId);
            }
        }
    }

    private static List<List<MaxButton>> BuildDayButtons(
        Guid? groupId,
        Guid? teacherId,
        List<ScheduleRevision> revisions
    )
    {
        var dates = revisions
            .Select(MessageFormatter.DateForRevision)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .Distinct()
            .OrderBy(d => d)
            .Take(6)
            .ToList();

        var rows = new List<List<MaxButton>>();
        for (var i = 0; i < dates.Count; i += 3)
            rows.Add(
                dates
                    .Skip(i)
                    .Take(3)
                    .Select(d => MiniAppButtons.OpenDay(_options, d, groupId, teacherId))
                    .ToList()
            );
        return rows;
    }
```

Обнови группировку:

```csharp
    public static List<(long ChatId, Guid? GroupId, Guid? TeacherId, List<ScheduleRevision> Revisions)> SelectRecipientsGrouped(
        List<UserSettings> settings,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, string> teacherNames,
        List<ScheduleRevision> revisions
    )
    {
        var flat = SelectRecipients(settings, groupNames, teacherNames, revisions);
        var settingsByChat = settings
            .GroupBy(s => s.MaxChatId)
            .ToDictionary(g => g.Key, g => g.First());

        return flat
            .GroupBy(x => x.ChatId)
            .Select(g =>
            {
                var s = settingsByChat[g.Key];
                return (g.Key, s.GroupId, s.TeacherId, g.Select(x => x.Revision).ToList());
            })
            .ToList();
    }
```

Конструктор `ChangeNotifier` — добавь `IOptions<MaxBotOptions> options` и поле `private readonly MaxBotOptions _options = options.Value;`. Добавь usings: `CollegeLMS.MaxBot.Models.Max`, `Microsoft.Extensions.Options`.

`CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs`:
- Удали утверждение `text.Should().Contain("✅ Применено: 08.09.2026 10:00");` (строка 614).
- Замени тест `FormatCorrectionDigest_AppliedAt_UsesTimeZone` (635-647) на проверку отсутствия строки:

```csharp
    [Fact]
    public void FormatCorrectionDigest_DoesNotShowAppliedAt()
    {
        var text = MessageFormatter.FormatCorrectionDigest([Revision()], TimeZoneInfo.Utc);

        text.Should().NotContain("Применено");
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MiniAppButtonsTests`
Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~MessageFormatterTests`
Run: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~ChangeNotifierTests`
Expected: PASS (тесты `SelectRecipientsGrouped_*` компилируются благодаря именам кортежа `ChatId`/`Revisions`).

- [ ] **Step 5: Commit**

```bash
git add CollegeLMS.MaxBot/Services/MiniAppButtons.cs CollegeLMS.MaxBot/Services/MessageFormatter.cs CollegeLMS.MaxBot/Services/ChangeNotifier.cs CollegeLMS.MaxBot.Tests/MiniAppButtonsTests.cs CollegeLMS.MaxBot.Tests/MessageFormatterTests.cs CollegeLMS.MaxBot.Tests/ChangeNotifierTests.cs
git commit -m "feat: open_app кнопка дня в уведомлениях, чистка карточки изменения"
```

---

### Task 9: Кнопка мини-аппа в меню и настройках бота

**Files:**
- Modify: `CollegeLMS.MaxBot/Bot/MaxBotService.cs:747-855` (`ShowMainMenuAsync`, `ShowSettingsAsync`)

**Interfaces:**
- Consumes: `MiniAppButtons.OpenSchedule`, `_options.BotPublicName`.
- Produces: пункт меню «📱 Открыть расписание» (open_app).

- [ ] **Step 1: Добавь кнопку в главное меню**

В `ShowMainMenuAsync` (строка ~747) перед `SendInlineKeyboardAsync` добавь к существующему списку строк кнопок строку с мини-аппом. Пример (сохрани текущие кнопки):

```csharp
        var buttons = new List<List<MaxButton>>
        {
            new() { new MaxButton { Type = "open_app", Text = "📱 Открыть расписание", WebApp = _options.BotPublicName, Payload = MiniAppUrlBuilder.BuildStartPayload("today") } },
            new() { new MaxButton { Type = "callback", Text = "🔔 Уведомления", Payload = "notifications" } },
            new() { new MaxButton { Type = "callback", Text = "⚙️ Настройки", Payload = "settings" } },
        };
```

(Если в существующем коде строки уже сгруппированы — вставь строку с `MiniAppButtons.OpenSchedule(_options)` первой, не дублируя callback-кнопки.)

- [ ] **Step 2: Добавь кнопку на экран настроек**

В `ShowSettingsAsync` (строка ~801) добавь строку с мини-аппом перед кнопками роли:

```csharp
        var buttons = new List<List<MaxButton>>
        {
            new() { MiniAppButtons.OpenSchedule(_options) },
            new() { new MaxButton { Type = "callback", Text = "🎓 Студент", Payload = "settings:student" } },
            new() { new MaxButton { Type = "callback", Text = "👨‍🏫 Преподаватель", Payload = "settings:teacher" } },
            new() { new MaxButton { Type = "callback", Text = "🔙 Меню", Payload = "menu" } },
        };
```

- [ ] **Step 3: Собери проект**

Run: `dotnet build CollegeLMS.slnx`
Expected: 0 ошибок.

- [ ] **Step 4: Commit**

```bash
git add CollegeLMS.MaxBot/Bot/MaxBotService.cs
git commit -m "feat: кнопка открытия мини-приложения в меню и настройках бота"
```

---

### Task 10: Мини-приложение — вход через MAX

**Files:**
- Create: `CollegeLMS.Next/api/auth.ts`
- Modify: `CollegeLMS.Next/lib/max-context.tsx:106-149`
- Modify: `CollegeLMS.Next/lib/api.ts:35-40`
- Modify: `CollegeLMS.Next/e2e/max-miniapp.spec.ts:162-176` (мок journal) и добавить тесты

**Interfaces:**
- Consumes: `POST /api/auth/max` → `{ token, profile }`.
- Produces: `loginWithMax(initData: string): Promise<MaxAuthResponse>`.

- [ ] **Step 1: Создай клиент авторизации**

`CollegeLMS.Next/api/auth.ts`:

```ts
import api from "@/lib/api"

export interface MaxAuthProfile {
  maxUserId: number
  fullName?: string | null
  role: "Student" | "Teacher" | "Other"
  groupId?: string | null
  groupName?: string | null
  teacherId?: string | null
  teacherName?: string | null
}

export interface MaxAuthResponse {
  token: string
  profile: MaxAuthProfile
}

interface ResultEnvelope<T> {
  isSuccess: boolean
  data: T | null
  errorMessage?: string | null
}

export async function loginWithMax(initData: string): Promise<MaxAuthResponse> {
  const res = await api.post<ResultEnvelope<MaxAuthResponse>>("/api/auth/max", { initData })
  if (!res.data.isSuccess || !res.data.data) {
    throw new Error(res.data.errorMessage ?? "Не удалось войти через MAX")
  }
  return res.data.data
}
```

- [ ] **Step 2: Встрой вход через MAX в контекст**

`lib/max-context.tsx` — добавь импорт `import { loginWithMax } from "@/api/auth"` и замени начало `reload` (строки 106-115):

```ts
  const reload = useCallback(() => {
    setLoading(true)

    const initData = (
      window as unknown as { WebApp?: { initData?: string } }
    ).WebApp?.initData

    if (initData) {
      loginWithMax(initData)
        .then((res) => {
          localStorage.setItem("token", res.token)
          setIsAuthed(true)
          setProfile({
            id: String(res.profile.maxUserId),
            fullName: res.profile.fullName ?? null,
            role: res.profile.role,
            teacherId: res.profile.teacherId ?? null,
            teacherName: res.profile.teacherName ?? null,
            groupId: res.profile.groupId ?? null,
            groupName: res.profile.groupName ?? null,
          })
          const own: ViewContext = {}
          if (res.profile.groupId) own.groupId = res.profile.groupId
          if (res.profile.groupName) own.groupName = res.profile.groupName
          if (res.profile.teacherId) own.teacherId = res.profile.teacherId
          if (res.profile.teacherName) own.teacherName = res.profile.teacherName
          setViewContextState((prev) => {
            const next = Object.keys(prev).length === 0 ? own : prev
            storeViewContext(next)
            return next
          })
        })
        .catch(() => {
          setIsAuthed(false)
          setProfile(null)
        })
        .finally(() => setLoading(false))
      return
    }

    const token =
      typeof window !== "undefined" ? localStorage.getItem("token") : null
    if (!token) {
      setIsAuthed(false)
      setProfile(null)
      setLoading(false)
      return
    }
```

Остальное тело `reload` (запрос `/api/schedule/context`) не меняется.

- [ ] **Step 3: Не выкидывать гостя из `/max`**

`lib/api.ts` — в обработчике 401 (строки 35-40):

```ts
      if (status === 401) {
        localStorage.removeItem("token")
        localStorage.removeItem("user")
        const path = window.location.pathname
        if (!path.startsWith("/login") && !path.startsWith("/max")) {
          window.location.href = "/login"
        }
      }
```

- [ ] **Step 4: Обнови e2e — мок журнала и новые тесты**

В `e2e/max-miniapp.spec.ts` в `beforeEach` добавь ветку journal в route-обработчик (после `/history`):

```ts
      if (url.includes("/journal")) return route.fulfill(inlineJson(ok({ subjects: [], entries: [] })))
```

Добавь в конец `test.describe`:

```ts
  test("MAX initData: гость входит и видит расписание", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "auth_date=1&user=%7B%22id%22%3A1%7D&hash=stub",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            token: "max-jwt",
            profile: {
              maxUserId: 1,
              fullName: "Гость",
              role: "Student",
              groupId: "g1",
              groupName: "ПО262",
              teacherId: null,
              teacherName: null,
            },
          }),
        ),
      ),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByText("Расписание")).toBeVisible()
  })

  test("MAX initData: роль Teacher показывает вкладку Журнал", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "auth_date=1&user=%7B%22id%22%3A2%7D&hash=stub",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            token: "max-jwt",
            profile: {
              maxUserId: 2,
              fullName: "Преподаватель",
              role: "Teacher",
              groupId: null,
              groupName: null,
              teacherId: "t1",
              teacherName: "Петренко В.Б.",
            },
          }),
        ),
      ),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar").getByText("Журнал")).toBeVisible()
  })

  test("MAX initData: ошибка входа не выкидывает на /login", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "bad",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill({ status: 401, contentType: "application/json", body: JSON.stringify({ isSuccess: false, errorMessage: "bad", statusCode: 401 }) }),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page).toHaveURL(/\/max\//)
  })
```

- [ ] **Step 5: Собери и прогони линт/e2e**

Run: `cd CollegeLMS.Next && npm run build`
Run: `cd CollegeLMS.Next && npm run lint`
Run: `cd CollegeLMS.Next && npx playwright test e2e/max-miniapp.spec.ts`
Expected: build и lint без ошибок; все e2e-тесты зелёные.

- [ ] **Step 6: Commit**

```bash
git add CollegeLMS.Next/api/auth.ts CollegeLMS.Next/lib/max-context.tsx CollegeLMS.Next/lib/api.ts CollegeLMS.Next/e2e/max-miniapp.spec.ts
git commit -m "feat: вход мини-приложения через MAX initData"
```

---

### Task 11: Конфигурация, деплой, README

**Files:**
- Modify: `CollegeLMS.API/appsettings.json`
- Modify: `docker-compose.yml:70-75` (api env), `:152-163` (maxbot env)
- Modify: `.github/workflows/deploy.yml:8-13`, `:34`, `:59-75`
- Modify: `CollegeLMS.MaxBot/README.md:255-258`

- [ ] **Step 1: appsettings.json**

Добавь секции (рядом с существующим `MaxBot`):

```json
  "MaxAuth": {
    "BotToken": ""
  },
  "MaxBot": {
    "BaseUrl": "http://localhost:8080",
    "InternalSecret": ""
  },
```

(Сохрани уже существующее значение `MaxBot:BaseUrl`, не дублируй ключ.)

- [ ] **Step 2: docker-compose.yml**

В сервис `api` (после `MaxBot__BaseUrl`):

```yaml
      MaxBot__InternalSecret: ${MAXBOT_INTERNAL_SECRET:-}
      MaxAuth__BotToken: ${MAX_BOT_TOKEN:-}
```

В сервис `maxbot` (после `MaxBot__WebhookSecret`):

```yaml
      MaxBot__BotPublicName: ${MAX_BOT_PUBLIC_NAME:-}
      MaxBot__InternalSecret: ${MAXBOT_INTERNAL_SECRET:-}
```

- [ ] **Step 3: deploy.yml**

В блок `env:` (строки 8-13) добавь:

```yaml
  MAXBOT_INTERNAL_SECRET: ${{ secrets.MAXBOT_INTERNAL_SECRET }}
  MAX_BOT_PUBLIC_NAME: ${{ secrets.MAX_BOT_PUBLIC_NAME }}
```

В `envs:` (строка 34) допиши в конец: `,MAXBOT_INTERNAL_SECRET,MAX_BOT_PUBLIC_NAME`.

В блок формирования `.env` (строки 59-75) добавь строки:

```bash
              echo "MAXBOT_INTERNAL_SECRET=${MAXBOT_INTERNAL_SECRET}"
              echo "MAX_BOT_PUBLIC_NAME=${MAX_BOT_PUBLIC_NAME}"
```

- [ ] **Step 4: README**

Замени блок строк 255-258 `CollegeLMS.MaxBot/README.md` на корректное описание:

```markdown
Кнопки «📱 Открыть расписание» и «📅 Открыть день» — типа `open_app` с полем
`web_app` (публичное имя бота, `MaxBot:BotPublicName`) и payload вида
`{route}[-yyyy-MM-dd][-g-<groupId>|-t-<teacherId>]`. Мини-приложение читает payload
из `start_param` MAX Bridge (`window.WebApp.initDataUnsafe.start_param`) и
навигирует на нужный экран.
```

- [ ] **Step 5: Собери проект**

Run: `dotnet build CollegeLMS.slnx`
Expected: 0 ошибок.

- [ ] **Step 6: Commit**

```bash
git add CollegeLMS.API/appsettings.json docker-compose.yml .github/workflows/deploy.yml CollegeLMS.MaxBot/README.md
git commit -m "chore: конфигурация MAX-аутентификации и деплой"
```

---

## Self-Review

**Spec coverage:**
- §1 «Бот: реальные кнопки open_app» → Task 8 (`MiniAppButtons`, `ChangeNotifier`, `MessageFormatter`), Task 9 (меню/настройки).
- §2 «Бот: внутренний endpoint профиля» → Task 4.
- §3 «API: POST /api/auth/max» → Task 1 (claims), Task 3 (валидатор), Task 5 (клиент бота), Task 6 (сервис+контроллер).
- §4 «API: claims-aware контекст и журнал» → Task 2 (хелперы), Task 7.
- §5 «Мини-приложение» → Task 10.
- §6 «Вычистка» (`bot_favorites` DDL, README) → Task 4 (DDL), Task 11 (README).
- Конфигурация → Task 11.
- Тесты → задачи 1-8, 10.

**Placeholder scan:** плейсхолдеров нет; каждый шаг содержит конкретный код или команду.

**Type consistency:** `MaxAuthProfile`/`MaxAuthResponse`/`MaxInternalUserDto`/`InternalUserProfile` согласованы; имена claims `max_user_id`/`groupId`/`teacherId` совпадают в Task 2, 6, 7; `SelectRecipientsGrouped` — новая арность кортежа используется и в тестах (именованные элементы `ChatId`/`Revisions`).

**Review Focus:** каждый из 5 пунктов закрыт тестом — дубль `hash` (Task 3), старый `auth_date` (Task 3, Task 6), недоступный бот (Task 6), гость-преподаватель без `teacherId` (Task 7), отсутствие MAX Bridge и 401 в `/max` (Task 10).
