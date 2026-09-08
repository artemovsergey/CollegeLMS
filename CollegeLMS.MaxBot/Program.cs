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

    // EnsureCreated не создаёт новую таблицу в существующей БД — идемпотентный raw SQL
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS schedule_revisions (
            id BIGSERIAL PRIMARY KEY,
            foreign_id UUID NOT NULL,
            change_type VARCHAR(20) NOT NULL,
            group_name VARCHAR(200) NOT NULL,
            teacher_name VARCHAR(200) NULL,
            subject VARCHAR(300) NOT NULL,
            room VARCHAR(100) NOT NULL,
            day_of_week VARCHAR(20) NOT NULL,
            week INTEGER NOT NULL,
            number_pair INTEGER NOT NULL,
            note VARCHAR(500) NULL,
            removed_subject VARCHAR(300) NULL,
            removed_teacher_name VARCHAR(200) NULL,
            removed_number_pair INTEGER NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_group_name
            ON schedule_revisions (group_name);

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_teacher_name
            ON schedule_revisions (teacher_name);

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_created_at
            ON schedule_revisions (created_at);
        """);
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
