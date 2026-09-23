using System.Text.Json;
using CollegeLMS.MaxBot;
using CollegeLMS.MaxBot.Bot;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

builder.Services.AddSingleton<PracticeNotifier>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PracticeNotifier>());

builder.Services.AddScoped<ChangeNotifier>();
builder.Services.AddScoped<CorrectionImageSender>();
builder.Services.AddScoped<InternalProfileService>();

// Очередь апдейтов от вебхука MAX
builder.Services.AddSingleton<MaxUpdateQueue>();

var app = builder.Build();

app.UseStaticFiles();

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
    await db.Database.EnsureCreatedAsync();

    // EnsureCreated не создаёт новую таблицу в существующей БД — идемпотентный raw SQL
    await db.Database.ExecuteSqlRawAsync(
        """
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
            correction_date TIMESTAMPTZ NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_group_name
            ON schedule_revisions (group_name);

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_teacher_name
            ON schedule_revisions (teacher_name);

        CREATE INDEX IF NOT EXISTS ix_schedule_revisions_created_at
            ON schedule_revisions (created_at);

        ALTER TABLE schedule_revisions
            ADD COLUMN IF NOT EXISTS correction_date TIMESTAMPTZ NULL;

        ALTER TABLE user_settings
            ADD COLUMN IF NOT EXISTS notify_time interval NOT NULL DEFAULT INTERVAL '7 hours 30 minutes';

        ALTER TABLE user_settings
            ADD COLUMN IF NOT EXISTS last_notified_on DATE;

        CREATE TABLE IF NOT EXISTS practice_notifications (
            id UUID PRIMARY KEY,
            practice_id UUID NOT NULL,
            event VARCHAR(20) NOT NULL,
            sent_on DATE NOT NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ix_practice_notifications_practice_event
            ON practice_notifications (practice_id, event);
        """
    );
}

app.MapGet(
    "/health",
    async (MaxBotDbContext db) =>
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { status = canConnect ? "ok" : "db unreachable" });
    }
);

// Уведомления об изменениях расписания от API CollegeLMS.
// Fail-safe: любые исключения логируются и не пробрасываются наружу.
app.MapPost(
    "/notify",
    async (NotifyChangeDto[]? changes, IServiceProvider services, CancellationToken ct) =>
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (changes is null || changes.Length == 0)
                return Results.Ok(new { received = 0 });

            var now = DateTime.UtcNow;
            var revisions = changes.Select(c => ScheduleRevision.FromNotify(c, now)).ToList();

            db.ScheduleRevisions.AddRange(revisions);
            await db.SaveChangesAsync(ct);

            var notifier = scope.ServiceProvider.GetRequiredService<ChangeNotifier>();
            await notifier.NotifyAsync(revisions, ct);

            return Results.Ok(new { received = revisions.Count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Сбой обработки /notify");
            return Results.Ok(new { received = 0 });
        }
    }
);

// Картинка корректировки в канал Max от API CollegeLMS (после применения пакета).
// multipart/form-data: file (PNG, имя correction.png) и caption (markdown).
// Fail-safe: любые исключения логируются и не пробрасываются наружу.
app.MapPost(
    "/notify/correction-image",
    async (HttpRequest request, IServiceProvider services, CancellationToken ct) =>
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (!request.HasFormContentType)
                return Results.Ok(new { sent = false });

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFiles("file").FirstOrDefault();
            var caption = form["caption"].ToString();

            if (file is null || file.Length == 0)
            {
                logger.LogWarning("POST /notify/correction-image: файл отсутствует или пуст");
                return Results.Ok(new { sent = false });
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);

            var sender = scope.ServiceProvider.GetRequiredService<CorrectionImageSender>();
            await sender.SendAsync(stream.ToArray(), caption, ct);

            return Results.Ok(new { sent = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Сбой обработки /notify/correction-image");
            return Results.Ok(new { sent = false });
        }
    }
);

// Вебхук MAX: платформа шлёт по одному объекту Update на каждое событие
// (bot_started, message_created, message_callback) и требует ответ 200 в течение 30 с —
// поэтому апдейт кладётся в очередь, а обработка идёт в MaxBotService.
var webhookJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

app.MapPost(
    "/maxbot/webhook",
    async (
        HttpRequest request,
        MaxUpdateQueue queue,
        IOptions<MaxBotOptions> options,
        ILogger<Program> logger,
        CancellationToken ct
    ) =>
    {
        var providedSecret = request.Headers["X-Max-Bot-Api-Secret"].ToString();
        if (!WebhookSecretValidator.IsValid(options.Value.WebhookSecret, providedSecret))
        {
            logger.LogWarning("Webhook: неверный X-Max-Bot-Api-Secret");
            return Results.Unauthorized();
        }

        MaxUpdate? update;
        try
        {
            update = await JsonSerializer.DeserializeAsync<MaxUpdate>(
                request.Body,
                webhookJsonOptions,
                ct
            );
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Webhook: некорректное тело запроса");
            return Results.BadRequest(new { error = "invalid update" });
        }

        if (update is null || string.IsNullOrWhiteSpace(update.UpdateType))
        {
            logger.LogWarning("Webhook: пустой апдейт");
            return Results.BadRequest(new { error = "empty update" });
        }

        await queue.EnqueueAsync(update, ct);
        return Results.Ok(new { ok = true });
    }
);

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
        // Fail-closed: пустой InternalSecret не должен открывать внутренний
        // endpoint — иначе профиль отдаётся без секрета (в отличие от webhook,
        // где пустой секрет допустим для локальной разработки).
        if (string.IsNullOrWhiteSpace(options.Value.InternalSecret))
            return Results.Unauthorized();
        if (!WebhookSecretValidator.IsValid(options.Value.InternalSecret, provided))
            return Results.Unauthorized();

        var profile = await profileService.GetAsync(maxUserId, ct);
        return Results.Ok(profile);
    }
);

app.Run();
