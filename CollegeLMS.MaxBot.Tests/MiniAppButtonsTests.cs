using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MiniAppButtonsTests
{
    private static readonly MaxBotOptions Options = new() { BotPublicName = "teacher_scc_bot" };

    [Fact]
    public void OpenSchedule_BuildsOpenAppButtonWithPayload()
    {
        var button = MiniAppButtons.OpenSchedule(Options);

        button.Type.Should().Be("open_app");
        button.WebApp.Should().Be("teacher_scc_bot");
        button.Payload.Should().Be("today");
        button.Url.Should().BeNull();
    }

    [Fact]
    public void OpenDay_IncludesDateAndGroupMarker()
    {
        var groupId = Guid.NewGuid();

        var button = MiniAppButtons.OpenDay(Options, new DateTime(2026, 9, 7), groupId, null);

        button.Type.Should().Be("open_app");
        button.Payload.Should().Be($"day-2026-09-07-g-{groupId}");
    }

    [Fact]
    public void OpenDay_PrefersGroupOverTeacher()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var button = MiniAppButtons.OpenDay(Options, new DateTime(2026, 9, 7), groupId, teacherId);

        button.Payload.Should().Be($"day-2026-09-07-g-{groupId}");
    }
}
