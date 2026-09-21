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
        var url = MiniAppUrlBuilder.Build("https://stvcc.tech/max", "today", groupId: groupId);

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

    [Fact]
    public void BuildStartPayload_WithRouteOnly_ReturnsRoute()
    {
        MiniAppUrlBuilder.BuildStartPayload("today").Should().Be("today");
    }

    [Fact]
    public void BuildStartPayload_WithDate_AppendsIsoDate()
    {
        var payload = MiniAppUrlBuilder.BuildStartPayload("day", new DateTime(2026, 9, 22));

        payload.Should().Be("day-2026-09-22");
    }

    [Fact]
    public void BuildStartPayload_WithGroupId_AppendsEntityId()
    {
        var groupId = Guid.NewGuid();

        var payload = MiniAppUrlBuilder.BuildStartPayload(
            "day",
            new DateTime(2026, 9, 22),
            groupId: groupId
        );

        payload.Should().Be($"day-2026-09-22-g-{groupId}");
    }

    [Fact]
    public void BuildStartPayload_WithTeacherId_AppendsEntityId()
    {
        var teacherId = Guid.NewGuid();

        var payload = MiniAppUrlBuilder.BuildStartPayload("week", teacherId: teacherId);

        payload.Should().Be($"week-t-{teacherId}");
    }

    [Fact]
    public void BuildStartPayload_AlwaysMatchesMaxApiPattern()
    {
        var payload = MiniAppUrlBuilder.BuildStartPayload(
            "day",
            new DateTime(2026, 9, 22),
            groupId: Guid.NewGuid()
        );

        payload.Should().MatchRegex("^[\\w-]*$");
    }
}
