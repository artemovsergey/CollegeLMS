using System.Collections.Concurrent;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class DispatcherAuthService(AppDbContext db, ITokenService tokens) : IDispatcherAuthService
{
    private static readonly ConcurrentDictionary<string, AttemptState> Attempts = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    private sealed record AttemptState(int Count, DateTime LockedUntil);

    public async Task<Result<DispatcherLoginResponse>> LoginAsync(
        string password,
        string clientIp,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        var state = Attempts.GetOrAdd(clientIp, new AttemptState(0, DateTime.MinValue));

        if (state.LockedUntil > now)
        {
            var wait = (state.LockedUntil - now).TotalMinutes;
            return Result<DispatcherLoginResponse>.Fail(
                $"Слишком много попыток. Повторите через {Math.Ceiling(wait)} мин",
                429
            );
        }

        var credential = await db.DispatcherCredentials.AsNoTracking().FirstOrDefaultAsync(ct);
        if (credential is null || !BCrypt.Net.BCrypt.Verify(password, credential.PasswordHash))
        {
            if (state.Count + 1 >= MaxAttempts)
            {
                Attempts[clientIp] = new AttemptState(0, now.Add(Lockout));
            }
            else
            {
                Attempts[clientIp] = new AttemptState(state.Count + 1, DateTime.MinValue);
            }

            return Result<DispatcherLoginResponse>.Fail("Неверный пароль диспетчера", 401);
        }

        Attempts[clientIp] = new AttemptState(0, DateTime.MinValue);

        var token = tokens.GenerateCustomToken(
            ["Dispatcher"],
            30,
            $"dispatcher-{Guid.NewGuid():N}"
        );
        return Result<DispatcherLoginResponse>.Ok(
            new DispatcherLoginResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            }
        );
    }
}
