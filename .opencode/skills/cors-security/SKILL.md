---
name: cors-security
description: Configure CORS and security headers for the API
---

# cors-security

Configure CORS policy and security headers for the ASP.NET API.

## CORS config in Program.cs

```csharp
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// later
app.UseCors("AllowFrontend");
```

## Security headers

Add security headers middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

## appsettings.json config

```json
"Cors": {
  "Origins": ["http://localhost:3000"]
}
```

## Environment-specific origins

- Development: `http://localhost:3000`
- Production: Frontend domain URL
