using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using CollegeLMS.Shared;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Экзамен приходит из API парой с номером 0 и техническим интервалом
/// 00:00–00:01 (иначе CHECK в БД не проходит). В пользовательском выводе
/// это не должно просочиться: вместо «*0.* … 00:00–00:01» читатель видит
/// «🎓 ЭКЗАМЕН», без номера пары, времени и кабинета.
/// </summary>
public class ExamNotificationFormattingTests
{
    [Fact]
    public void ExamEntryHasNoPairNumberInDigest()
    {
        var entries = new List<ScheduleResponse>
        {
            new()
            {
                LessonType = "Exam",
                NumberPair = 0,
                Subject = "ЭКЗАМЕН",
                Room = "101",
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromMinutes(1),
                DayOfWeek = 1,
            },
        };

        var result = MessageFormatter.FormatDaySchedule(entries, 1, "Группа ИТ-31");

        result.Should().Contain("🎓 ЭКЗАМЕН");
        result.Should().NotContain("*0.*");
        result.Should().NotContain("00:00");
        result.Should().NotContain("00:01");
        result.Should().NotContain("101");
    }

    [Fact]
    public void ChangeVocabularyLabelsExamAsExam()
    {
        ChangeVocabulary.Label("Exam").Should().Be("Экзамен");
    }

    [Fact]
    public void ExamChangeTagHasExamMarker()
    {
        var entries = new List<ScheduleResponse>
        {
            new()
            {
                LessonType = "Exam",
                Subject = "ЭКЗАМЕН",
                ChangeTags = [new ChangeTag { ChangeType = "Exam", Week = 5 }],
            },
        };

        var result = MessageFormatter.FormatDaySchedule(entries, 1, "Группа ИТ-31");

        result.Should().Contain("🟢 ЭКЗАМЕН");
        result.Should().NotContain("🟢 добавлено");
    }

    [Fact]
    public void ExamRevisionDigestHidesPairNumber()
    {
        var revisions = new List<ScheduleRevision>
        {
            new()
            {
                ChangeType = "Exam",
                Subject = "ЭКЗАМЕН",
                GroupName = "ИТ-31",
                DayOfWeek = "Понедельник",
                Week = 5,
                NumberPair = 0,
            },
        };

        var result = MessageFormatter.FormatCorrectionDigest(revisions);

        result.Should().Contain("🎓 ЭКЗАМЕН");
        result.Should().NotContain("пара 0");
        result.Should().Contain("**Экзамен**");
    }
}
