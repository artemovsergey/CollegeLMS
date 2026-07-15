using CollegeLMS.API.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder
    .Services.AddDatabase(builder.Configuration)
    .AddJwt(builder.Configuration)
    .AddSwaggerWithBearer()
    .AddCorsFrontend(builder.Configuration)
    .AddJsonSerializer()
    .AddApplicationServices()
    .AddHealthChecksWithDb(builder.Configuration)
    .AddRateLimit();

var app = builder.Build();

app.UseExceptionMiddleware();
app.UseRateLimiter();
app.UseCors("AllowFrontend");

app.UseSwaggerWithUi();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

await app.MigrateDatabaseAsync();

await app.RunAsync();
