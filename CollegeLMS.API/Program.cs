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
    .AddHealthChecksWithDb(builder.Configuration);

var app = builder.Build();

app.UseExceptionMiddleware();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync();
}

await app.RunAsync();
