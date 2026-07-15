using System.Text.Json;

namespace CollegeLMS.TelegramBot.OpenCode;

public class OpenCodeSseListener : IHostedService, IDisposable
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenCodeSseListener> _logger;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public event Func<SseEvent, Task>? OnEvent;

    public OpenCodeSseListener(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<OpenCodeSseListener> logger
    )
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loopTask = RunLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_loopTask is not null)
                await _loopTask;
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var baseUrl = _config["OpenCode:ServerUrl"] ?? "http://localhost:4096";
                var client = _httpFactory.CreateClient();
                var username = _config["OpenCode:Username"] ?? "opencode";
                var password = _config["OpenCode:Password"] ?? "";

                if (!string.IsNullOrEmpty(password))
                {
                    var cred = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{username}:{password}")
                    );
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", cred);
                }

                var url = $"{baseUrl}/event";
                _logger.LogInformation("Connecting to SSE: {Url}", url);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct
                );

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line is null)
                    {
                        _logger.LogInformation("SSE stream ended, reconnecting...");
                        break;
                    }

                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (line.StartsWith("data: "))
                    {
                        var json = line[6..];
                        try
                        {
                            var evt = JsonSerializer.Deserialize<SseEvent>(json);
                            if (evt is not null)
                            {
                                _logger.LogDebug("SSE event received: {Type}", evt.Type);
                                if (OnEvent is not null)
                                    await OnEvent.Invoke(evt);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogDebug(ex, "Failed to parse SSE event");
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SSE connection lost, reconnecting in 5s...");
                await Task.Delay(5000, ct);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}

public class SseEvent
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("properties")]
    public JsonElement? Properties { get; set; }
}

