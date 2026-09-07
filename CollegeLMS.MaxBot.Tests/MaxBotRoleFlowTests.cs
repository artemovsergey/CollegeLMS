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
    public void ApplyRole_Teacher_ClearsGroupId()
    {
        var settings = SettingsWithBothEntities();

        MaxBotRoleFlow.ApplyRole(settings, "teacher");

        settings.Role.Should().Be("teacher");
        settings.TeacherId.Should().NotBeNull();
        settings.GroupId.Should().BeNull();
    }

    [Fact]
    public void ApplyRole_Student_ClearsTeacherId()
    {
        var settings = SettingsWithBothEntities();

        MaxBotRoleFlow.ApplyRole(settings, "student");

        settings.Role.Should().Be("student");
        settings.GroupId.Should().NotBeNull();
        settings.TeacherId.Should().BeNull();
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
