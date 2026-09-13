using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class MiniAppUrlBuilderTests
{
    [Fact]
    public void Build_WithRouteAndDate_ProducesRouteAndDateParams()
    {
        var url = MiniAppUrlBuilder.Build(
            "https://stvcc.tech/max",
            "day",
            new DateTime(2026, 9, 13)
        );

        url.Should().StartWith("https://stvcc.tech/max?route=day&date=2026-09-13");
    }

    [Fact]
    public void Build_WithGroupId_AddsLegacyHint()
    {
        var groupId = Guid.NewGuid();
        var url = MiniAppUrlBuilder.Build(
            "https://stvcc.tech/max",
            "today",
            groupId: groupId
        );

        url.Should().Contain($"route=today").And.Contain($"groupId={groupId}");
    }

    [Fact]
    public void Build_WithTeacherId_AddsLegacyHint()
    {
        var teacherId = Guid.NewGuid();
        var url = MiniAppUrlBuilder.Build(
            "https://stvcc.tech/max",
            "week",
            new DateTime(2026, 9, 13),
            teacherId: teacherId
        );

        url.Should()
            .Contain("route=week")
            .And.Contain("date=2026-09-13")
            .And.Contain($"teacherId={teacherId}");
    }
}