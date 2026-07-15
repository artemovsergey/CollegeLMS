using System.Net;
using CollegeLMS.TelegramBot.Bot;
using CollegeLMS.TelegramBot.Models;
using CollegeLMS.TelegramBot.OpenCode;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var telegramToken = builder.Configuration["Telegram:BotToken"] ?? "";
var telegramProxyUrl = builder.Configuration["Telegram:ProxyUrl"] ?? "";
var openCodeUrl = builder.Configuration["OpenCode:ServerUrl"] ?? "http://localhost:4096";
var openCodeUser = builder.Configuration["OpenCode:Username"] ?? "opencode";
var openCodePass = builder.Configuration["OpenCode:Password"] ?? "";

builder.Services.AddHttpClient();

builder.Services.AddHttpClient<OpenCodeClient>(client =>
{
    client.BaseAddress = new Uri(openCodeUrl);
    if (!string.IsNullOrEmpty(openCodePass))
    {
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{openCodeUser}:{openCodePass}")
        );
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }
});

builder.Services.AddSingleton<OpenCodeSseListener>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OpenCodeSseListener>());

builder.Services.AddSingleton<AgentTaskQueue>();
builder.Services.AddSingleton<TelegramBotService>();

var telegramHttpClient = string.IsNullOrEmpty(telegramProxyUrl)
    ? new HttpClient()
    : new HttpClient(
        new HttpClientHandler { Proxy = new WebProxy(telegramProxyUrl), UseProxy = true }
    );
builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(telegramToken, telegramHttpClient)
);

var app = builder.Build();

app.MapGet(
    "/health",
    async (OpenCodeClient oc) =>
    {
        var healthy = await oc.IsHealthyAsync();
        return Results.Ok(new { telegramBot = true, openCode = healthy });
    }
);

app.Run();
