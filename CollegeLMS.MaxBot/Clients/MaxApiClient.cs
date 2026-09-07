using System.Net.Http.Json;
using System.Text.Json;
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Clients;

public class MaxApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MaxApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public MaxApiClient(HttpClient http, ILogger<MaxApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<MaxBotInfo?> GetMeAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/me", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MaxBotInfo>(JsonOpts, ct);
    }

    public async Task<MaxUpdatesResponse?> GetUpdatesAsync(
        long? marker = null,
        int timeoutSeconds = 30,
        CancellationToken ct = default
    )
    {
        var url = marker.HasValue
            ? $"/updates?marker={marker}&timeout={timeoutSeconds}"
            : $"/updates?timeout={timeoutSeconds}";
        _logger.LogDebug("Fetching updates: {Url}", url);
        var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "GET /updates returned {Code}: {Body}",
                resp.StatusCode,
                errBody.Length > 200 ? errBody[..200] : errBody
            );
            return null;
        }
        var raw = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Raw updates response: {Raw}", raw.Length > 500 ? raw[..500] : raw);
        var result = JsonSerializer.Deserialize<MaxUpdatesResponse>(raw, JsonOpts);
        _logger.LogInformation(
            "Deserialized: marker={Marker}, updateCount={Count}",
            result?.Marker,
            result?.Updates?.Count ?? 0
        );
        return result;
    }

    public async Task<MaxMessageResponse?> SendMessageAsync(
        long chatId,
        string text,
        string format = "markdown",
        bool notify = true,
        CancellationToken ct = default
    )
    {
        var body = new Dictionary<string, object>
        {
            ["text"] = text,
            ["format"] = format,
            ["notify"] = notify,
        };

        var resp = await _http.PostAsJsonAsync($"/messages?chat_id={chatId}", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MaxMessageResponse>(JsonOpts, ct);
    }

    public async Task<MaxMessageResponse?> SendInlineKeyboardAsync(
        long chatId,
        string text,
        List<List<MaxButton>> buttons,
        string format = "markdown",
        CancellationToken ct = default
    )
    {
        var body = new
        {
            text,
            format,
            attachments = new[] { new { type = "inline_keyboard", payload = new { buttons } } },
        };

        var resp = await _http.PostAsJsonAsync($"/messages?chat_id={chatId}", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MaxMessageResponse>(JsonOpts, ct);
    }

    public async Task AnswerCallbackAsync(
        string callbackId,
        string? text = null,
        CancellationToken ct = default
    )
    {
        Dictionary<string, object> body = text is null
            ? new Dictionary<string, object> { ["notification"] = true }
            : new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object>
                {
                    ["text"] = text,
                    ["attachments"] = Array.Empty<object>(),
                },
            };

        var resp = await _http.PostAsJsonAsync(
            $"/answers?callback_id={Uri.EscapeDataString(callbackId)}",
            body,
            JsonOpts,
            ct
        );
        resp.EnsureSuccessStatusCode();
    }

    public async Task SetCommandsAsync(BotCommand[] commands, CancellationToken ct = default)
    {
        var body = new { commands };
        var resp = await _http.PatchAsJsonAsync("/me/commands", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }
}
