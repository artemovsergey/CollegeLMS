namespace CollegeLMS.TelegramBot.OpenCode;

public class OpenCodeSession
{
    private readonly OpenCodeClient _client;
    private readonly ILogger<OpenCodeSession> _logger;

    public OpenCodeSession(OpenCodeClient client, ILogger<OpenCodeSession> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(string sessionId, string? response)> RunAsync(
        string prompt,
        string? model = null,
        CancellationToken ct = default
    )
    {
        var session = await _client.CreateSessionAsync(ct);
        if (session is null)
            throw new InvalidOperationException("Failed to create OpenCode session");

        _logger.LogInformation("Created session {SessionId}", session.Id);

        var message = await _client.SendPromptAsync(session.Id, prompt, model, ct);
        if (message is null)
            throw new InvalidOperationException("No response from OpenCode");

        var text = string.Join(
            "\n",
            message.Parts.Where(p => p.Type == "text" && p.Text is not null).Select(p => p.Text)
        );

        return (session.Id, text);
    }

    public async Task AbortAsync(string sessionId, CancellationToken ct = default)
    {
        await _client.AbortSessionAsync(sessionId, ct);
    }
}
