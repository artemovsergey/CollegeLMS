using CollegeLMS.MaxBot;
using CollegeLMS.MaxBot.Bot;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Don't kill host on BackgroundService exceptions
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Настройки бота
builder.Services.Configure<MaxBotOptions>(builder.Configuration.GetSection("MaxBot"));
builder.Services.AddSingleton(TimeZoneProvider.Resolve(builder.Configuration["MaxBot:TimeZone"]));

// EF Core
builder.Services.AddDbContext<MaxBotDbContext>(options =>
    options
        .UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly(typeof(MaxBotDbContext).Assembly.FullName)
        )
        .UseSnakeCaseNamingConvention()
);

// Max API client (Russian CA cert not trusted in Docker — bypass SSL)
builder
    .Services.AddHttpClient<MaxApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://platform-api2.max.ru");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                builder.Configuration["MaxBot:AccessToken"] ?? ""
            );
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        }
    );

// CollegeLMS API client
builder.Services.AddHttpClient<CollegeLmsApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["CollegeLmsApi:BaseUrl"] ?? "http://localhost:5000"
    );
});

// Bot services
builder.Services.AddSingleton<MaxBotService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MaxBotService>());

builder.Services.AddSingleton<ScheduleNotifier>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScheduleNotifier>());

var app = builder.Build();

app.UseStaticFiles();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet(
    "/health",
    async (MaxBotDbContext db) =>
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { status = canConnect ? "ok" : "db unreachable" });
    }
);

app.Run();
