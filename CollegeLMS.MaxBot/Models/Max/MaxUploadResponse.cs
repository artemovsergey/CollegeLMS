namespace CollegeLMS.MaxBot.Models.Max;

/// <summary>
/// Ответ POST /uploads — URL для загрузки медиафайла и токен вложения.
/// Для изображений token может прийти в ответе на саму загрузку файла.
/// </summary>
public record MaxUploadResponse
{
    public string Url { get; init; } = "";

    public string? Token { get; init; }
}
