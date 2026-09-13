using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class NotificationTimeRulesTests
{
    [Theory]
    [InlineData(7, 30)]
    [InlineData(7, 45)]
    [InlineData(8, 20)]
    [InlineData(8, 30)]
    public void IsValid_AcceptsBoundariesAndStep(int hours, int minutes)
    {
        NotificationTimeRules.IsValid(TimeSpan.FromMinutes(hours * 60 + minutes)).Should().BeTrue();
    }

    [Theory]
    [InlineData(7, 33)]
    [InlineData(8, 2)]
    [InlineData(6, 30)]
    [InlineData(9, 0)]
    public void IsValid_RejectsOffStepAndOutsideRange(int hours, int minutes)
    {
        NotificationTimeRules.IsValid(TimeSpan.FromMinutes(hours * 60 + minutes)).Should().BeFalse();
    }
}

public class NotificationWindowTests
{
    private readonly TimeSpan _window = TimeSpan.FromMinutes(15);

    [Fact]
    public void IsDue_BeforeWindow_ReturnsFalse()
    {
        NotificationWindow.IsDue(new TimeSpan(7, 29, 0), new TimeSpan(7, 30, 0), _window)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void IsDue_AtWindowStart_ReturnsTrue()
    {
        NotificationWindow.IsDue(new TimeSpan(7, 30, 0), new TimeSpan(7, 30, 0), _window)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDue_InsideWindow_ReturnsTrue()
    {
        NotificationWindow.IsDue(new TimeSpan(7, 40, 0), new TimeSpan(7, 30, 0), _window)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDue_AtWindowEnd_ReturnsTrue()
    {
        NotificationWindow.IsDue(new TimeSpan(7, 45, 0), new TimeSpan(7, 30, 0), _window)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsDue_AfterWindow_ReturnsFalse()
    {
        NotificationWindow.IsDue(new TimeSpan(8, 0, 0), new TimeSpan(7, 30, 0), _window)
            .Should()
            .BeFalse();
    }
}