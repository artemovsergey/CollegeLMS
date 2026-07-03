---
name: redis-session
description: Configure Redis for session caching using StackExchangeRedis and IDistributedCache
---

# redis-session

Configure Redis as a distributed cache for session storage.

## NuGet packages

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

## Program.cs registration

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    options.InstanceName = "CollegeLMS";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

## appsettings.json

```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

## Usage

```csharp
public class {Name}Service(IDistributedCache cache)
{
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        return await cache.GetStringAsync(key, ct);
    }

    public async Task SetAsync(string key, string value, CancellationToken ct)
    {
        await cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        }, ct);
    }
}
```

## Verification

- Redis container runs (`docker compose up -d redis`)
- `redis-cli ping` returns PONG
- Cache read/write operations succeed without errors
