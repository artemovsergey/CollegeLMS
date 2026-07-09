using System.Text.Json;
using System.Text.Json.Serialization;
using AgentBridge.Models;

namespace AgentBridge.Bot;

public class VkMaxBotHost
{
    private readonly MessageRouter _router;
    private readonly IConfiguration _config;
    private readonly ILogger<VkMaxBotHost> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _accessToken;
    private readonly int _groupId;

    public VkMaxBotHost(
        MessageRouter router,
        IConfiguration config,
        ILogger<VkMaxBotHost> logger,
        IHttpClientFactory httpFactory)
    {
        _router = router;
        _config = config;
        _logger = logger;
        _httpFactory = httpFactory;
        _accessToken = config["VkMax:AccessToken"] ?? "";
        _groupId = config.GetValue<int>("VkMax:GroupId");
    }

    public bool IsEnabled => !string.IsNullOrEmpty(_accessToken);

    public async Task<string> HandleCallbackAsync(JsonElement body, CancellationToken ct = default)
    {
        var type = body.GetProperty("type").GetString();

        if (type == "confirmation")
        {
            return _config["VkMax:ConfirmationCode"] ?? "";
        }

        if (type == "message_new")
        {
            var messageObj = body.GetProperty("object").GetProperty("message");
            var text = messageObj.GetProperty("text").GetString() ?? "";
            var peerId = messageObj.GetProperty("peer_id").GetInt64();

            _logger.LogInformation("VK Max message: peer={PeerId}, text={Text}", peerId, text);

            var response = await _router.HandleCommandAsync(text, peerId, "vkmax");
            await SendMessageAsync(peerId, response, ct);
        }

        if (type == "message_event")
        {
            var evt = body.GetProperty("object");
            var payload = evt.TryGetProperty("payload", out var p) ? p.GetString() : null;
            var peerId = evt.GetProperty("peer_id").GetInt64();
            var conversationMessageId = evt.GetProperty("conversation_message_id").GetInt32();

            _logger.LogInformation("VK Max callback: peer={PeerId}, payload={Payload}", peerId, payload);

            if (payload is not null)
            {
                await _router.HandlePermissionResponseAsync(payload, payload.StartsWith("allow:"));
            }
        }

        return "ok";
    }

    public async Task SendMessageAsync(long peerId, string text, CancellationToken ct = default)
    {
        if (!IsEnabled) return;

        var client = _httpFactory.CreateClient();
        var url = $"https://api.vk.com/method/messages.send?" +
                  $"access_token={_accessToken}" +
                  $"&peer_id={peerId}" +
                  $"&message={Uri.EscapeDataString(text)}" +
                  $"&random_id={Random.Shared.Next()}" +
                  $"&v=5.199";

        var resp = await client.PostAsync(url, null, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("VK Max send failed: {Status}", resp.StatusCode);
        }
    }

    public async Task SendKeyboardMessageAsync(
        long peerId,
        string text,
        string permissionId,
        CancellationToken ct = default)
    {
        if (!IsEnabled) return;

        var keyboard = new
        {
            one_time = true,
            buttons = new[]
            {
                new[]
                {
                    new
                    {
                        action = new
                        {
                            type = "callback",
                            label = "Разрешить",
                            payload = JsonSerializer.Serialize($"allow:{permissionId}")
                        },
                        color = "positive"
                    },
                    new
                    {
                        action = new
                        {
                            type = "callback",
                            label = "Отказать",
                            payload = JsonSerializer.Serialize($"deny:{permissionId}")
                        },
                        color = "negative"
                    }
                }
            }
        };

        var client = _httpFactory.CreateClient();
        var keyboardJson = JsonSerializer.Serialize(keyboard);
        var url = $"https://api.vk.com/method/messages.send?" +
                  $"access_token={_accessToken}" +
                  $"&peer_id={peerId}" +
                  $"&message={Uri.EscapeDataString(text)}" +
                  $"&keyboard={Uri.EscapeDataString(keyboardJson)}" +
                  $"&random_id={Random.Shared.Next()}" +
                  $"&v=5.199";

        var resp = await client.PostAsync(url, null, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("VK Max keyboard send failed: {Status}", resp.StatusCode);
        }
    }
}
