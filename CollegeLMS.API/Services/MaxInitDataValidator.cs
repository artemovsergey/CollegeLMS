using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.Services;

/// <summary>
/// Проверка подписи MAX WebAppData. Алгоритм: dev.max.ru/docs/webapps/validation.
/// </summary>
public class MaxInitDataValidator(IConfiguration config, TimeProvider timeProvider)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    public Result<MaxInitDataPayload> Validate(string? initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
            return Result<MaxInitDataPayload>.Fail("initData пуст.", 401);

        var pairs = new List<KeyValuePair<string, string>>();
        string? hash = null;
        foreach (var chunk in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = chunk.IndexOf('=');
            if (idx <= 0)
                continue;
            var key = chunk[..idx];
            var value = Uri.UnescapeDataString(chunk[(idx + 1)..]);
            if (key == "hash")
            {
                if (hash is not null)
                    return Result<MaxInitDataPayload>.Fail("initData: дублирующийся hash.", 401);
                hash = value;
                continue;
            }
            pairs.Add(new(key, value));
        }

        if (hash is null)
            return Result<MaxInitDataPayload>.Fail("initData: отсутствует hash.", 401);

        var botToken = config["MaxAuth:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return Result<MaxInitDataPayload>.Fail("Проверка MAX не настроена.", 401);

        var launchParams = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")
        );

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(botToken)
        );
        var signature = Convert
            .ToHexString(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(launchParams)))
            .ToLowerInvariant();

        if (
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(hash.ToLowerInvariant())
            )
        )
            return Result<MaxInitDataPayload>.Fail("initData: подпись не совпадает.", 401);

        var authDateRaw = pairs.FirstOrDefault(p => p.Key == "auth_date").Value;
        if (!long.TryParse(authDateRaw, out var authDateUnix))
            return Result<MaxInitDataPayload>.Fail("initData: нет auth_date.", 401);

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        if (timeProvider.GetUtcNow() - authDate > MaxAge)
            return Result<MaxInitDataPayload>.Fail("initData просрочен.", 401);

        var userRaw = pairs.FirstOrDefault(p => p.Key == "user").Value;
        if (string.IsNullOrWhiteSpace(userRaw))
            return Result<MaxInitDataPayload>.Fail("initData: нет user.", 401);

        long userId;
        string? firstName = null;
        string? lastName = null;
        try
        {
            using var doc = JsonDocument.Parse(userRaw);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt64(out userId))
                return Result<MaxInitDataPayload>.Fail("initData: нет user.id.", 401);
            if (root.TryGetProperty("first_name", out var fn))
                firstName = fn.GetString();
            if (root.TryGetProperty("last_name", out var ln))
                lastName = ln.GetString();
        }
        catch (JsonException)
        {
            return Result<MaxInitDataPayload>.Fail("initData: некорректный user.", 401);
        }

        var startParam = pairs.FirstOrDefault(p => p.Key == "start_param").Value;

        return Result<MaxInitDataPayload>.Ok(
            new MaxInitDataPayload
            {
                MaxUserId = userId,
                FullName = string.Join(
                    " ",
                    new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s))
                ),
                StartParam = string.IsNullOrWhiteSpace(startParam) ? null : startParam,
            }
        );
    }
}
