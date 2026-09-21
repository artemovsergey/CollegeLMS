using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MaxApiClient(HttpClient http, ILogger<MaxApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет статус ответа MAX API. При неуспехе пишет в лог код и тело ответа
    /// (тело — ключ к диагностике: MAX возвращает причину в JSON) и бросает
    /// <see cref="HttpRequestException"/> с этим телом в сообщении.
    /// </summary>
    private async Task<HttpResponseMessage> EnsureSuccessWithBodyAsync(
        HttpResponseMessage resp,
        string context,
        CancellationToken ct
    )
    {
        if (resp.IsSuccessStatusCode)
            return resp;

        var body = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogError(
            "MAX API {Context} вернул HTTP {Code}: {Body}",
            context,
            (int)resp.StatusCode,
            body.Length > 500 ? body[..500] : body
        );
        throw new HttpRequestException($"MAX API {context}: HTTP {(int)resp.StatusCode}. {body}");
    }

    public async Task<MaxBotInfo?> GetMeAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/me", ct);
        await EnsureSuccessWithBodyAsync(resp, "GET /me", ct);
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

    /// <summary>Список активных подписок на обновления (вебхуков).</summary>
    public async Task<List<MaxSubscription>?> GetSubscriptionsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/subscriptions", ct);
        await EnsureSuccessWithBodyAsync(resp, "GET /subscriptions", ct);
        var result = await resp.Content.ReadFromJsonAsync<MaxSubscriptionsResponse>(JsonOpts, ct);
        return result?.Subscriptions;
    }

    /// <summary>
    /// Создаёт или обновляет подписку на обновления: MAX будет слать их POST-запросом на <paramref name="url"/>.
    /// При <paramref name="secret"/> MAX добавляет заголовок <c>X-Max-Bot-Api-Secret</c>.
    /// </summary>
    public async Task<bool> SubscribeWebhookAsync(
        string url,
        IReadOnlyList<string> updateTypes,
        string? secret = null,
        CancellationToken ct = default
    )
    {
        var body = new Dictionary<string, object> { ["url"] = url, ["update_types"] = updateTypes };
        if (!string.IsNullOrWhiteSpace(secret))
            body["secret"] = secret;

        var resp = await _http.PostAsJsonAsync("/subscriptions", body, JsonOpts, ct);
        await EnsureSuccessWithBodyAsync(resp, "POST /subscriptions", ct);
        var result = await resp.Content.ReadFromJsonAsync<MaxSubscriptionResult>(JsonOpts, ct);
        if (result is { Success: false })
            _logger.LogWarning(
                "MAX POST /subscriptions вернул success=false: {Message}",
                result.Message
            );
        return result?.Success ?? false;
    }

    /// <summary>Удаляет подписку с указанным URL (после этого снова доступен long polling).</summary>
    public async Task<bool> UnsubscribeWebhookAsync(string url, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/subscriptions?url={Uri.EscapeDataString(url)}", ct);
        await EnsureSuccessWithBodyAsync(resp, "DELETE /subscriptions", ct);
        var result = await resp.Content.ReadFromJsonAsync<MaxSubscriptionResult>(JsonOpts, ct);
        return result?.Success ?? false;
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
        await EnsureSuccessWithBodyAsync(resp, $"POST /messages?chat_id={chatId}", ct);
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
        try
        {
            await EnsureSuccessWithBodyAsync(resp, $"POST /messages?chat_id={chatId}", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Не удалось отправить сообщение с клавиатурой в чат {ChatId} — отправляю текст без кнопок",
                chatId
            );
            return await SendMessageAsync(chatId, text, format, ct: ct);
        }
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
        await EnsureSuccessWithBodyAsync(resp, "POST /answers", ct);
    }

    public async Task SetCommandsAsync(BotCommand[] commands, CancellationToken ct = default)
    {
        var body = new { commands };
        var resp = await _http.PatchAsJsonAsync("/me/commands", body, JsonOpts, ct);
        await EnsureSuccessWithBodyAsync(resp, "PATCH /me/commands", ct);
    }

    /// <summary>
    /// Получает URL для загрузки медиафайла: POST /uploads?type={type}.
    /// Ответ в snake_case: { url, token? }.
    /// </summary>
    public async Task<MaxUploadResponse> GetUploadUrlAsync(
        string type,
        CancellationToken ct = default
    )
    {
        var resp = await _http.PostAsync(
            $"/uploads?type={Uri.EscapeDataString(type)}",
            content: null,
            ct
        );
        await EnsureSuccessWithBodyAsync(resp, "POST /uploads", ct);
        return await resp.Content.ReadFromJsonAsync<MaxUploadResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Пустой ответ POST /uploads");
    }

    /// <summary>
    /// Загружает файл по абсолютному URL (multipart, поле data) с заданным Content-Type
    /// и возвращает payload вложения — JSON-объект с token.
    /// </summary>
    public async Task<JsonElement> UploadFileAsync(
        string uploadUrl,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken ct = default
    )
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "data", fileName);

        // uploadUrl — абсолютный адрес CDN, он переопределяет BaseAddress
        var resp = await _http.PostAsync(uploadUrl, form, ct);
        await EnsureSuccessWithBodyAsync(resp, "POST uploadUrl", ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<JsonElement>(raw);
    }

    /// <summary>
    /// Загружает изображение (image/png) — обёртка над <see cref="UploadFileAsync"/>
    /// и возвращает payload вложения — JSON-объект с token.
    /// </summary>
    public Task<JsonElement> UploadImageAsync(
        string uploadUrl,
        byte[] content,
        string fileName,
        CancellationToken ct = default
    ) => UploadFileAsync(uploadUrl, content, fileName, "image/png", ct);

    /// <summary>
    /// Отправляет сообщение с изображением: POST /messages?chat_id={chatId}.
    /// При ошибке attachment.not.ready — пауза 3 с и повтор (до 3 попыток).
    /// </summary>
    public Task<MaxMessageResponse?> SendImageAsync(
        long chatId,
        JsonElement payload,
        string caption,
        CancellationToken ct = default
    )
    {
        var body = new Dictionary<string, object>
        {
            ["text"] = caption,
            ["format"] = "markdown",
            ["notify"] = true,
            ["attachments"] = new[]
            {
                new Dictionary<string, object> { ["type"] = "image", ["payload"] = payload },
            },
        };

        return SendMessageWithRetryAsync(
            chatId,
            body,
            $"Не удалось отправить изображение в чат {chatId}",
            ct
        );
    }

    /// <summary>
    /// Отправляет сообщение с файлом и inline-клавиатурой:
    /// POST /messages?chat_id={chatId}, attachments: [file, inline_keyboard].
    /// При ошибке attachment.not.ready — пауза 3 с и повтор (до 3 попыток).
    /// </summary>
    public Task<MaxMessageResponse?> SendDocumentAsync(
        long chatId,
        JsonElement payload,
        string caption,
        List<List<MaxButton>> buttons,
        CancellationToken ct = default
    )
    {
        var attachments = new List<Dictionary<string, object>>
        {
            new() { ["type"] = "file", ["payload"] = payload },
        };
        if (buttons.Count > 0)
        {
            attachments.Add(new() { ["type"] = "inline_keyboard", ["payload"] = new { buttons } });
        }

        var body = new Dictionary<string, object>
        {
            ["text"] = caption,
            ["format"] = "markdown",
            ["notify"] = true,
            ["attachments"] = attachments,
        };

        return SendMessageWithRetryAsync(
            chatId,
            body,
            $"Не удалось отправить файл в чат {chatId}",
            ct
        );
    }

    /// <summary>
    /// Отправляет сообщение с вложениями. При ошибке attachment.not.ready —
    /// пауза 3 с и повтор до 3 попыток, затем EnsureSuccessStatusCode.
    /// </summary>
    private async Task<MaxMessageResponse?> SendMessageWithRetryAsync(
        long chatId,
        object body,
        string errorMessage,
        CancellationToken ct
    )
    {
        const int maxAttempts = 3;

        for (var attempt = 1; ; attempt++)
        {
            var resp = await _http.PostAsJsonAsync(
                $"/messages?chat_id={chatId}",
                body,
                JsonOpts,
                ct
            );
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<MaxMessageResponse>(JsonOpts, ct);

            var errBody = await resp.Content.ReadAsStringAsync(ct);
            if (
                errBody.Contains("attachment.not.ready", StringComparison.Ordinal)
                && attempt < maxAttempts
            )
            {
                _logger.LogWarning(
                    "Вложение ещё не обработано (попытка {Attempt} из {MaxAttempts}) — повтор через 3 с",
                    attempt,
                    maxAttempts
                );
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                continue;
            }

            // Попытки исчерпаны или ошибка не связана с обработкой вложения
            _logger.LogError(
                "MAX API POST /messages?chat_id={ChatId} вернул HTTP {Code}: {Body}",
                chatId,
                (int)resp.StatusCode,
                errBody.Length > 500 ? errBody[..500] : errBody
            );
            throw new HttpRequestException(
                $"{errorMessage}: HTTP {(int)resp.StatusCode}. {errBody}"
            );
        }
    }
}
