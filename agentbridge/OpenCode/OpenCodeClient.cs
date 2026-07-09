using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentBridge.OpenCode;

public class OpenCodeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenCodeClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OpenCodeClient(HttpClient http, ILogger<OpenCodeClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("/global/health", ct);
            if (!resp.IsSuccessStatusCode) return false;
            var health = await resp.Content.ReadFromJsonAsync<HealthResponse>(JsonOpts, ct);
            return health?.Healthy == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenCode health check failed");
            return false;
        }
    }

    public async Task<SessionResponse?> CreateSessionAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/session", new { }, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SessionResponse>(JsonOpts, ct);
    }

    public async Task<MessageResponse?> SendPromptAsync(
        string sessionId,
        string prompt,
        string? model = null,
        CancellationToken ct = default)
    {
        var parts = new[] { new { type = "text", text = prompt } };
        object body = model is not null
            ? new { parts, model = ParseModel(model) }
            : new { parts };

        var resp = await _http.PostAsJsonAsync($"/session/{sessionId}/message", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<MessageResponse>(JsonOpts, ct);
    }

    public async Task SendPromptFireAndForgetAsync(
        string sessionId,
        string prompt,
        string? model = null,
        CancellationToken ct = default)
    {
        var parts = new[] { new { type = "text", text = prompt } };
        object body = model is not null
            ? new { parts, model = ParseModel(model) }
            : new { parts };

        var resp = await _http.PostAsJsonAsync($"/session/{sessionId}/prompt_async", body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    private static object ParseModel(string model)
    {
        var parts = model.Split('/', 2);
        return parts.Length == 2
            ? new { modelID = parts[1], providerID = parts[0] }
            : new { modelID = model, providerID = "opencode" };
    }

    public async Task AbortSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"/session/{sessionId}/abort", null, ct);
        _logger.LogInformation("Abort session {SessionId}: {Status}", sessionId, resp.StatusCode);
    }

    public async Task<List<SessionStatusResponse>?> GetSessionStatusAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/session/status", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<List<SessionStatusResponse>>(JsonOpts, ct);
    }

    public async Task RespondToPermissionAsync(
        string sessionId,
        string permissionId,
        bool allow,
        bool remember = false,
        CancellationToken ct = default)
    {
        var body = new { response = allow ? "allow" : "deny", remember };
        var resp = await _http.PostAsJsonAsync(
            $"/session/{sessionId}/permissions/{permissionId}", body, JsonOpts, ct);
        _logger.LogInformation("Permission {PermissionId} → {Response} (status {Status})",
            permissionId, allow ? "allow" : "deny", resp.StatusCode);
    }

    public async Task<MessageResponse?> GetMessageAsync(
        string sessionId,
        string messageId,
        CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/session/{sessionId}/message/{messageId}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<MessageResponse>(JsonOpts, ct);
    }

    public async Task<List<MessageResponse>?> GetMessagesAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/session/{sessionId}/message", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOpts, ct);
    }

    public async Task<ProviderResponse?> GetProvidersAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/provider", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ProviderResponse>(JsonOpts, ct);
    }
}

// --- DTOs ---

public record HealthResponse(
    [property: JsonPropertyName("healthy")] bool Healthy,
    [property: JsonPropertyName("version")] string? Version);

public record SessionResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string? Title);

public record MessageResponse(
    [property: JsonPropertyName("info")] MessageInfo Info,
    [property: JsonPropertyName("parts")] List<MessagePart> Parts);

public record MessageInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role")] string Role);

public record MessagePart(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text);

public record SessionStatusResponse(
    [property: JsonPropertyName("sessionID")] string SessionId,
    [property: JsonPropertyName("status")] string Status);

public record ProviderResponse(
    [property: JsonPropertyName("all")] List<ProviderInfo> Providers,
    [property: JsonPropertyName("connected")] List<string> Connected);

public record ProviderInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("models")] Dictionary<string, ModelInfo> Models);

public record ModelInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name);
