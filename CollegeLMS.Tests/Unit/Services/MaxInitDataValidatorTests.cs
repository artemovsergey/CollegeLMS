using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CollegeLMS.Tests.Unit.Services;

public class MaxInitDataValidatorTests
{
    private const string BotToken = "bot-token-123";
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    private static MaxInitDataValidator Create() =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["MaxAuth:BotToken"] = BotToken }
                )
                .Build(),
            new FixedTimeProvider(Now)
        );

    private static string Build(
        Dictionary<string, string> pairs,
        DateTimeOffset? authDate = null,
        string? hashOverride = null
    )
    {
        pairs["auth_date"] = (authDate ?? Now).ToUnixTimeSeconds().ToString();
        var launch = string.Join(
            "\n",
            pairs.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")
        );
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(BotToken)
        );
        var hash =
            hashOverride
            ?? Convert
                .ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
                .ToLowerInvariant();
        return string.Join(
                "&",
                pairs
                    .OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
            ) + $"&hash={hash}";
    }

    private static Dictionary<string, string> User(long id) =>
        new()
        {
            ["query_id"] = "q1",
            ["user"] = JsonSerializer.Serialize(
                new
                {
                    id,
                    first_name = "Иван",
                    last_name = "Иванов",
                }
            ),
        };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void Validate_ValidSignature_ReturnsPayload()
    {
        var result = Create().Validate(Build(User(42)));

        result.IsSuccess.Should().BeTrue();
        result.Data!.MaxUserId.Should().Be(42);
        result.Data.FullName.Should().Be("Иван Иванов");
    }

    [Fact]
    public void Validate_ExtractsStartParam()
    {
        var pairs = User(42);
        pairs["start_param"] = "day-2026-09-07-g-11111111-1111-1111-1111-111111111111";

        var result = Create().Validate(Build(pairs));

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.StartParam.Should()
            .Be("day-2026-09-07-g-11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Validate_TamperedHash_Fails()
    {
        var result = Create().Validate(Build(User(42), hashOverride: new string('a', 64)));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_DuplicateHash_Fails()
    {
        var initData = Build(User(42)) + "&hash=deadbeef";

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_MissingHash_Fails()
    {
        var initData = Build(User(42));
        initData = initData[..initData.LastIndexOf("&hash=", StringComparison.Ordinal)];

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validate_StaleAuthDate_Fails()
    {
        var result = Create().Validate(Build(User(42), authDate: Now.AddHours(-2)));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Validate_WrongBotTokenSignature_Fails()
    {
        var pairs = User(42);
        pairs["auth_date"] = Now.ToUnixTimeSeconds().ToString();
        var launch = string.Join("\n", pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
        var secret = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes("другой-токен")
        );
        var hash = Convert
            .ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(launch)))
            .ToLowerInvariant();
        var initData =
            string.Join(
                "&",
                pairs.OrderBy(p => p.Key).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")
            ) + $"&hash={hash}";

        var result = Create().Validate(initData);

        result.IsSuccess.Should().BeFalse();
    }
}
