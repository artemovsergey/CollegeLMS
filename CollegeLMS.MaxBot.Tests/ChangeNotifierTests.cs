using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>Выбор получателей уведомлений об изменениях (ChangeNotifier.SelectRecipients).</summary>
public class ChangeNotifierTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    private static readonly Dictionary<Guid, string> GroupNames = new() { [GroupId] = "ПО262" };
    private static readonly Dictionary<Guid, string> TeacherNames = new()
    {
        [TeacherId] = "Петренко В.Б.",
    };

    private static UserSettings Student(bool enabled = true, int[]? days = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = 1,
            MaxChatId = 100,
            Role = "student",
            GroupId = GroupId,
            NotifyEnabled = enabled,
            NotifyDays = days ?? [2],
        };

    private static UserSettings Teacher(bool enabled = true, int[]? days = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = 2,
            MaxChatId = 200,
            Role = "teacher",
            TeacherId = TeacherId,
            NotifyEnabled = enabled,
            NotifyDays = days ?? [2],
        };

    private static ScheduleRevision Rev(
        string? groupName = "ПО262",
        string? teacherName = null,
        string dayName = "Вторник",
        int week = 1
    ) =>
        new()
        {
            Id = 1,
            ForeignId = Guid.NewGuid(),
            ChangeType = "Replace",
            GroupName = groupName ?? "",
            TeacherName = teacherName,
            Subject = "История",
            Room = "301",
            DayOfWeek = dayName,
            Week = week,
            NumberPair = 2,
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public void SelectRecipients_MatchesStudentByGroupName()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Student()],
            GroupNames,
            TeacherNames,
            [Rev()]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 100);
    }

    [Fact]
    public void SelectRecipients_MatchesTeacherByFullName()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Teacher()],
            GroupNames,
            TeacherNames,
            [Rev(teacherName: "Петренко В.Б.")]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 200);
    }

    [Fact]
    public void SelectRecipients_NotifyDisabled_IsExcluded()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Student(enabled: false)],
            GroupNames,
            TeacherNames,
            [Rev()]
        );

        recipients.Should().BeEmpty();
    }

    [Fact]
    public void SelectRecipients_DayNotInNotifyDays_StillMatches()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Student(days: [3])],
            GroupNames,
            TeacherNames,
            [Rev()]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 100);
    }

    [Fact]
    public void SelectRecipients_SundayRevision_StillMatches()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Student(days: [1])],
            GroupNames,
            TeacherNames,
            [Rev(dayName: "Воскресенье")]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 100);
    }

    [Fact]
    public void SelectRecipients_NoNameMatch_ReturnsEmpty()
    {
        var recipients = ChangeNotifier.SelectRecipients(
            [Student()],
            GroupNames,
            TeacherNames,
            [Rev(groupName: "Другая группа")]
        );

        recipients.Should().BeEmpty();
    }

    [Fact]
    public void SelectRecipients_MatchesTeacherByRemovedTeacherName()
    {
        var rev = Rev();
        rev.TeacherName = null;
        rev.RemovedTeacherName = "Петренко В.Б.";

        var recipients = ChangeNotifier.SelectRecipients(
            [Teacher()],
            GroupNames,
            TeacherNames,
            [rev]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 200);
    }

    [Fact]
    public void SelectRecipientsGrouped_TwoSettingsOneChat_ProducesSingleMessage()
    {
        var settings = new List<UserSettings>
        {
            Student(),
            new()
            {
                Id = Guid.NewGuid(),
                MaxUserId = 3,
                MaxChatId = 100,
                Role = "teacher",
                TeacherId = TeacherId,
                NotifyEnabled = true,
                NotifyDays = [2],
            },
        };

        var grouped = ChangeNotifier.SelectRecipientsGrouped(
            settings,
            GroupNames,
            TeacherNames,
            [Rev(teacherName: "Петренко В.Б.")]
        );

        grouped.Should().ContainSingle();
        grouped[0].ChatId.Should().Be(100);
        grouped[0].Revisions.Should().ContainSingle();
    }

    [Fact]
    public void SelectRecipientsGrouped_MultipleRevisions_GroupedIntoOneMessagePerChat()
    {
        var first = Rev();
        var second = Rev();
        second.Id = 2;

        var grouped = ChangeNotifier.SelectRecipientsGrouped(
            [Student()],
            GroupNames,
            TeacherNames,
            [first, second]
        );

        grouped.Should().ContainSingle();
        grouped[0].ChatId.Should().Be(100);
        grouped[0].Revisions.Should().HaveCount(2);
    }

    [Fact]
    public void SelectRecipients_DedupByChatAndRevision()
    {
        var settings = new List<UserSettings>
        {
            Student(),
            new()
            {
                Id = Guid.NewGuid(),
                MaxUserId = 3,
                MaxChatId = 100,
                Role = "teacher",
                TeacherId = TeacherId,
                NotifyEnabled = true,
                NotifyDays = [2],
            },
        };

        var recipients = ChangeNotifier.SelectRecipients(
            settings,
            GroupNames,
            TeacherNames,
            [Rev(teacherName: "Петренко В.Б.")]
        );

        recipients.Should().ContainSingle(x => x.ChatId == 100);
    }

    [Fact]
    public void FormatChangeNotification_ContainsDeepLinkOnDate()
    {
        var rev = Rev(week: 1, dayName: "Вторник");

        var text = MessageFormatter.FormatChangeNotification(rev, "https://stvcc.tech/max");

        text.Should().Contain("📅 Открыть на дату: https://stvcc.tech/max?route=day&date=");
        text.Should().Contain("date=2026-09-01");
    }

    [Fact]
    public void FormatChangeNotification_TuesdaySecondWeekPointsToNextWeek()
    {
        var rev = Rev(week: 2, dayName: "Среда");

        var text = MessageFormatter.FormatChangeNotification(rev, "https://stvcc.tech/max");

        text.Should().Contain("date=2026-09-09");
    }
}
