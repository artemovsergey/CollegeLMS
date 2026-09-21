namespace CollegeLMS.MaxBot.Services;

/// <summary>Ограничение частоты неверных попыток входа диспетчера: 5 неудач → блок 15 минут на пользователя.</summary>
public sealed class DispatcherLoginThrottle
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);

    private readonly object _gate = new();
    private readonly Dictionary<long, (int Failures, DateTime? BlockedUntil)> _attempts = [];

    /// <summary>Заблокирован ли пользователь на момент <paramref name="now"/>.</summary>
    public bool IsBlocked(long userId, DateTime now)
    {
        lock (_gate)
        {
            if (!_attempts.TryGetValue(userId, out var entry))
                return false;

            if (entry.BlockedUntil is { } blockedUntil && now < blockedUntil)
                return true;

            _attempts.Remove(userId);
            return false;
        }
    }

    /// <summary>Сколько осталось до конца блокировки; <see cref="TimeSpan.Zero"/>, если блокировки нет.</summary>
    public TimeSpan RetryAfter(long userId, DateTime now)
    {
        lock (_gate)
        {
            if (!_attempts.TryGetValue(userId, out var entry))
                return TimeSpan.Zero;

            if (entry.BlockedUntil is { } blockedUntil && now < blockedUntil)
                return blockedUntil - now;

            return TimeSpan.Zero;
        }
    }

    /// <summary>Учитывает неверную попытку; <see cref="MaxAttempts"/>-я неудача включает блок.</summary>
    public void RegisterFailure(long userId, DateTime now)
    {
        lock (_gate)
        {
            var failures = 1;

            if (_attempts.TryGetValue(userId, out var entry))
            {
                if (entry.BlockedUntil is { } currentBlock && now < currentBlock)
                    return;

                if (entry.BlockedUntil is { } expiredBlock && now >= expiredBlock)
                    failures = 1;
                else
                    failures = entry.Failures + 1;
            }

            DateTime? blockedUntil = failures >= MaxAttempts ? now + BlockDuration : null;
            _attempts[userId] = (failures, blockedUntil);
        }
    }

    /// <summary>Сбрасывает счётчик неудач после успешного входа.</summary>
    public void Reset(long userId)
    {
        lock (_gate)
            _attempts.Remove(userId);
    }
}
