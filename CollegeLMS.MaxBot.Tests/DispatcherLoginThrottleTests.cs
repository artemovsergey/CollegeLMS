using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class DispatcherLoginThrottleTests
{
    private static readonly DateTime Now = new(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FiveFailures_BlocksFifteenMinutes()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts; i++)
            throttle.RegisterFailure(42, Now);

        throttle.IsBlocked(42, Now).Should().BeTrue();
        throttle
            .RetryAfter(42, Now)
            .Should()
            .Be(DispatcherLoginThrottle.BlockDuration, "отсчёт до конца блокировки");
    }

    [Fact]
    public void FourFailures_DoNotBlock()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts - 1; i++)
            throttle.RegisterFailure(42, Now);

        throttle.IsBlocked(42, Now).Should().BeFalse();
        throttle.RetryAfter(42, Now).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Success_ResetsCounter()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts - 1; i++)
            throttle.RegisterFailure(42, Now);

        throttle.Reset(42);

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts - 1; i++)
            throttle.RegisterFailure(42, Now);

        throttle.IsBlocked(42, Now).Should().BeFalse();
    }

    [Fact]
    public void Success_AfterBlock_LiftsBlock()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts; i++)
            throttle.RegisterFailure(42, Now);

        throttle.Reset(42);

        throttle.IsBlocked(42, Now).Should().BeFalse();
        throttle.RetryAfter(42, Now).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Block_ExpiresAfterDuration()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts; i++)
            throttle.RegisterFailure(42, Now);

        throttle.IsBlocked(42, Now.AddMinutes(14)).Should().BeTrue();

        var afterBlock = Now.Add(DispatcherLoginThrottle.BlockDuration);
        throttle.IsBlocked(42, afterBlock).Should().BeFalse();
        throttle.RetryAfter(42, afterBlock).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AfterBlockExpiry_FailuresStartOver()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts; i++)
            throttle.RegisterFailure(42, Now);

        var afterBlock = Now.Add(DispatcherLoginThrottle.BlockDuration);
        throttle.RegisterFailure(42, afterBlock);

        throttle.IsBlocked(42, afterBlock).Should().BeFalse();
        throttle.RetryAfter(42, afterBlock).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void DifferentUsers_AreIndependent()
    {
        var throttle = new DispatcherLoginThrottle();

        for (var i = 0; i < DispatcherLoginThrottle.MaxAttempts; i++)
            throttle.RegisterFailure(1, Now);

        throttle.IsBlocked(1, Now).Should().BeTrue();
        throttle.IsBlocked(2, Now).Should().BeFalse();

        throttle.RegisterFailure(2, Now);

        throttle.IsBlocked(1, Now).Should().BeTrue();
        throttle.IsBlocked(2, Now).Should().BeFalse();
    }
}
