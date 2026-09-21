using CollegeLMS.MaxBot.Models.Max;
using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class MaxUpdateQueueTests
{
    [Fact]
    public async Task ReadAllAsync_ReturnsUpdatesInOrder()
    {
        var queue = new MaxUpdateQueue();

        await queue.EnqueueAsync(new MaxUpdate { UpdateType = "first" });
        await queue.EnqueueAsync(new MaxUpdate { UpdateType = "second" });

        await using var enumerator = queue
            .ReadAllAsync(CancellationToken.None)
            .GetAsyncEnumerator();

        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.UpdateType.Should().Be("first");
        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.UpdateType.Should().Be("second");
    }

    [Fact]
    public async Task EnqueueAsync_OverCapacity_DropsOldestUpdate()
    {
        var queue = new MaxUpdateQueue();

        for (var i = 1; i <= 1000; i++)
            await queue.EnqueueAsync(new MaxUpdate { UpdateType = $"update-{i}" });

        await queue.EnqueueAsync(new MaxUpdate { UpdateType = "update-1001" });

        await using var enumerator = queue
            .ReadAllAsync(CancellationToken.None)
            .GetAsyncEnumerator();

        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.UpdateType.Should().Be("update-2");
    }
}
