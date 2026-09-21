using System.Threading.Channels;
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>
/// Очередь апдейтов MAX: webhook-эндпоинт кладёт, MaxBotService обрабатывает.
/// Ограниченная — при переполнении вытесняется самый старый апдейт.
/// </summary>
public sealed class MaxUpdateQueue
{
    private readonly Channel<MaxUpdate> _channel = Channel.CreateBounded<MaxUpdate>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest }
    );

    /// <summary>Добавляет апдейт в очередь (не блокирует ответ вебхука).</summary>
    public ValueTask EnqueueAsync(MaxUpdate update, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(update, ct);

    /// <summary>Читает апдейты по мере поступления.</summary>
    public IAsyncEnumerable<MaxUpdate> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}
