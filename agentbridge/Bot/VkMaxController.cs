using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AgentBridge.Bot;

[ApiController]
[Route("vk")]
public class VkMaxController : ControllerBase
{
    private readonly VkMaxBotHost _bot;
    private readonly ILogger<VkMaxController> _logger;

    public VkMaxController(VkMaxBotHost bot, ILogger<VkMaxController> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Callback([FromBody] JsonElement body, CancellationToken ct)
    {
        var type = body.TryGetProperty("type", out var t) ? t.GetString() : "unknown";
        _logger.LogInformation("VK callback: {Type}", type);

        if (!_bot.IsEnabled)
            return Content("ok", "text/plain");

        var response = await _bot.HandleCallbackAsync(body, ct);
        return Content(response, "text/plain");
    }
}
