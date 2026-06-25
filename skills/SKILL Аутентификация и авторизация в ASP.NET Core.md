# SKILL: Аутентификация и авторизация в ASP.NET Core

## Когда использовать этот скилл

Применяй при задачах:
- JWT Bearer аутентификация (генерация токенов, валидация)
- Keycloak как внешний Identity Provider (OpenID Connect / OAuth 2.0)
- Политики авторизации, роли, Claims
- Refresh Tokens, срок жизни токенов
- Secure endpoints, `[Authorize]`, Policy-based authorization

Источники: `artemovsergey/.NET` (Wiki/Jwt, Wiki/Authorization, Wiki/Claims, Wiki/AuthServices, Wiki/KeyCloak, Wiki/SecurityServices)

---

## 1. JWT — генерация токена

### Конфигурация (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "super-secret-key-minimum-32-characters!",
    "Issuer": "https://myapp.com",
    "Audience": "myapp-api",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

### TokenService

```csharp
public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options) => _settings = options.Value;

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Name,               user.UserName!),
        };

        // Добавляем роли в claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:    _settings.Issuer,
            audience:  _settings.Audience,
            claims:    claims,
            notBefore: DateTime.UtcNow,
            expires:   DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = _settings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = _settings.Audience,
            ValidateLifetime         = false, // специально false для expired токена
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuerSigningKey = true
        };

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, parameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}
```

### Регистрация JWT в Program.cs

```csharp
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var settings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = settings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = settings.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            ValidateIssuerSigningKey = true
        };

        // Для SignalR — токен из query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path        = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
```

---

## 2. Authorization — Политики и Роли

### Policy-based Authorization

```csharp
builder.Services.AddAuthorization(options =>
{
    // Простая политика по роли
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Политика по claim
    options.AddPolicy("PremiumUser", policy =>
        policy.RequireClaim("subscription", "premium", "enterprise"));

    // Комбинированная политика
    options.AddPolicy("BankEmployee", policy =>
        policy.RequireRole("Employee")
              .RequireClaim("department", "banking")
              .RequireAuthenticatedUser());

    // Политика по умолчанию — требовать аутентификацию
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### Resource-based Authorization

```csharp
public class AccountOwnerRequirement : IAuthorizationRequirement { }

public class AccountOwnerHandler
    : AuthorizationHandler<AccountOwnerRequirement, Account>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountOwnerRequirement requirement,
        Account resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (resource.OwnerId == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Регистрация
builder.Services.AddSingleton<IAuthorizationHandler, AccountOwnerHandler>();
```

---

## 3. Claims — получение данных пользователя

```csharp
public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
           ?? throw new UnauthorizedAccessException("User ID claim not found");

    public static string GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
           ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email)
           ?? throw new UnauthorizedAccessException("Email claim not found");

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
        => principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value);

    public static bool IsInAnyRole(this ClaimsPrincipal principal, params string[] roles)
        => roles.Any(principal.IsInRole);
}

// В Minimal API endpoint:
app.MapGet("/api/profile", (HttpContext ctx) =>
{
    var userId = ctx.User.GetUserId();
    var email  = ctx.User.GetEmail();
    return Results.Ok(new { userId, email });
}).RequireAuthorization();
```

---

## 4. Keycloak — интеграция (из ModuleBankApp)

### appsettings.json

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/bankapp",
    "Audience": "bankapp-api",
    "ClientId": "bankapp-api",
    "ClientSecret": "your-client-secret"
  }
}
```

### Регистрация

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority            = builder.Configuration["Keycloak:Authority"];
        options.Audience             = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = builder.Environment.IsProduction();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Keycloak:Audience"],
            RoleClaimType    = "roles" // Keycloak кладёт роли в claim "roles"
        };
    });
```

### Извлечение ролей из Keycloak token

Keycloak кладёт роли в нестандартное место. Нужен маппинг:

```csharp
// Трансформер Claims для Keycloak
public class KeycloakClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Keycloak: realm_access.roles
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
            if (realmAccess.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                }
            }
        }

        return Task.FromResult(principal);
    }
}

// Регистрация
builder.Services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformer>();
```

---

## 5. Refresh Token — схема хранения и ротации

```csharp
public class RefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public async Task<AuthResponse> RefreshAsync(string accessToken, string refreshToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        var userId    = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new UnauthorizedAccessException();

        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow || storedToken.IsRevoked)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // Ротация: отзываем старый, создаём новый
        storedToken.IsRevoked = true;
        var newRefreshToken = new RefreshToken
        {
            UserId    = userId,
            Token     = _tokenService.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        var user  = await _db.Users.FindAsync(userId);
        var roles = await _db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name).ToListAsync();

        return new AuthResponse(
            _tokenService.GenerateAccessToken(user!, roles!),
            newRefreshToken.Token
        );
    }
}
```

---

## 6. Minimal API — защита endpoints

```csharp
// Глобальная защита всего API
app.MapGroup("/api")
   .RequireAuthorization()
   .WithOpenApi();

// Отдельные политики
app.MapPost("/api/admin/users", handler)
   .RequireAuthorization("AdminOnly");

// Анонимный доступ к конкретному endpoint
app.MapGet("/api/health", () => "ok")
   .AllowAnonymous();
```

---

## Checklist

- [ ] `SecretKey` хранится в переменных окружения / User Secrets, НЕ в appsettings.json
- [ ] `ClockSkew = TimeSpan.Zero` для строгой валидации времени
- [ ] Refresh tokens хранятся в БД и ротируются при каждом использовании
- [ ] Политики авторизации определены в `AddAuthorization`, а не инлайн
- [ ] Для SignalR: токен извлекается из `query["access_token"]`
- [ ] В Keycloak настроены правильные `Valid Redirect URIs` и `Web Origins`
- [ ] `RequireHttpsMetadata = false` только в Development
