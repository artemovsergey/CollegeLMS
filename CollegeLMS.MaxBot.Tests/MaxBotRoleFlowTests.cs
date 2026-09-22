using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MaxBotRoleFlowTests
{
    private static UserSettings SettingsWithBothEntities() =>
        new()
        {
            Role = "student",
            GroupId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
        };

    [Fact]
    public void RequiresOnboarding_NoSettings_True()
    {
        MaxBotRoleFlow.RequiresOnboarding(null).Should().BeTrue();
    }

    [Fact]
    public void RequiresOnboarding_NoGroupAndNoTeacher_True()
    {
        var settings = new UserSettings { Role = "student" };

        MaxBotRoleFlow.RequiresOnboarding(settings).Should().BeTrue();
    }

    [Fact]
    public void RequiresOnboarding_GroupSelected_False()
    {
        var settings = new UserSettings { Role = "student", GroupId = Guid.NewGuid() };

        MaxBotRoleFlow.RequiresOnboarding(settings).Should().BeFalse();
    }

    [Fact]
    public void RequiresOnboarding_TeacherSelected_False()
    {
        var settings = new UserSettings { Role = "teacher", TeacherId = Guid.NewGuid() };

        MaxBotRoleFlow.RequiresOnboarding(settings).Should().BeFalse();
    }

    [Fact]
    public void SelectGroup_SetsRoleStudentAndClearsTeacherId()
    {
        var settings = SettingsWithBothEntities();
        var groupId = Guid.NewGuid();

        MaxBotRoleFlow.SelectGroup(settings, groupId);

        settings.GroupId.Should().Be(groupId);
        settings.Role.Should().Be("student");
        settings.TeacherId.Should().BeNull();
    }

    [Fact]
    public void SelectTeacher_SetsRoleTeacherAndClearsGroupId()
    {
        var settings = SettingsWithBothEntities();
        var teacherId = Guid.NewGuid();

        MaxBotRoleFlow.SelectTeacher(settings, teacherId);

        settings.TeacherId.Should().Be(teacherId);
        settings.Role.Should().Be("teacher");
        settings.GroupId.Should().BeNull();
    }
}
