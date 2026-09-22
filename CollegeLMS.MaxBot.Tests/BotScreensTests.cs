using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class BotScreensTests
{
    private static readonly MaxBotOptions Options = new() { BotPublicName = "teacher_scc_bot" };

    [Fact]
    public void MainMenuWithSelection_ForStudent_RendersGroupName()
    {
        var groupId = Guid.NewGuid();

        var (text, buttons) = BotScreens.MainMenuWithSelection(
            "student",
            groupId,
            null,
            "ИС-21",
            null,
            Options
        );

        text.Should().Be("🏠 *Главное меню*\n\nГруппа: ИС-21");
        buttons.Should().HaveCount(2);
        buttons[0].Should().ContainSingle().Which.Type.Should().Be("open_app");
        buttons[0][0].Payload.Should().Be($"today-g-{groupId}");
        buttons[1].Should().ContainSingle().Which.Payload.Should().Be("settings");
    }

    [Fact]
    public void MainMenuWithSelection_ForTeacher_RendersTeacherName()
    {
        var teacherId = Guid.NewGuid();

        var (text, buttons) = BotScreens.MainMenuWithSelection(
            "teacher",
            null,
            teacherId,
            null,
            "Иванов И. И.",
            Options
        );

        text.Should().Be("🏠 *Главное меню*\n\nПреподаватель: Иванов И. И.");
        buttons[0][0].Payload.Should().Be($"today-t-{teacherId}");
    }

    [Fact]
    public void MainMenuWithSelection_UnknownName_UsesFallback()
    {
        var (text, _) = BotScreens.MainMenuWithSelection(
            "student",
            Guid.NewGuid(),
            null,
            null,
            null,
            Options
        );

        text.Should().Contain("Группа: выбрана");
    }
}
