using System.Security.Cryptography;
using System.Text;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Проверка секрета вебхука MAX (заголовок <c>X-Max-Bot-Api-Secret</c>).
/// Сравнение выполняется в постоянном времени, чтобы исключить timing-атаки.
/// </summary>
public static class WebhookSecretValidator
{
    /// <summary>
    /// Если <paramref name="expected"/> пуст — проверка отключена (локальная разработка).
    /// </summary>
    public static bool IsValid(string? expected, string? provided)
    {
        if (string.IsNullOrEmpty(expected))
            return true;

        if (string.IsNullOrEmpty(provided))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided)
        );
    }
}
