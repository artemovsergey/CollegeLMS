using AgentBridge.Bot;
using AgentBridge.Models;
using AgentBridge.OpenCode;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var telegramToken = builder.Configuration["Telegram:BotToken"] ?? "";
var openCodeUrl = builder.Configuration["OpenCode:ServerUrl"] ?? "http://localhost:4096";
var openCodeUser = builder.Configuration["OpenCode:Username"] ?? "opencode";
var openCodePass = builder.Configuration["OpenCode:Password"] ?? "";

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// OpenCode client
builder.Services.AddHttpClient<OpenCodeClient>(client =>
{
    client.BaseAddress = new Uri(openCodeUrl);
    if (!string.IsNullOrEmpty(openCodePass))
    {
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{openCodeUser}:{openCodePass}"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }
});

// SSE listener (singleton — holds connection + event handlers)
builder.Services.AddSingleton<OpenCodeSseListener>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OpenCodeSseListener>());

builder.Services.AddSingleton<AgentTaskQueue>();
builder.Services.AddSingleton<MessageRouter>();

// Telegram bot
builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(telegramToken));
builder.Services.AddHostedService<TelegramBotHost>();

var app = builder.Build();

app.MapControllers();

// Health check
app.MapGet("/health", async (OpenCodeClient oc) =>
{
    var healthy = await oc.IsHealthyAsync();
    return Results.Ok(new { bridge = true, openCode = healthy });
});

app.Run();
