using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MessageFormatterTests
{
    private static List<ScheduleResponse> Entries() =>
        [
            new ScheduleResponse
            {
                DayOfWeek = 1,
                NumberPair = 1,
                Subject = "Математика",
                Room = "405",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 30, 0),
                TeacherName = "Иванов И.И.",
                LessonType = "Lecture",
            },
        ];

    [Fact]
    public void FormatDaySchedule_IncludesDateInHeader()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Entries(),
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
        text.Should().Contain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_ShowGroup_IncludesGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_WithoutShowGroup_OmitsGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И."
        );

        text.Should().NotContain("ИС-21");
    }

    [Fact]
    public void FormatWeekSchedule_ShowGroup_IncludesGroupName()
    {
        var entries = new List<ScheduleResponse> { Entries()[0] with { GroupName = "ИС-21" } };

        var text = MessageFormatter.FormatWeekSchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_Empty_ShowsHoliday()
    {
        var text = MessageFormatter.FormatDaySchedule([], new DateTime(2026, 9, 7), "Группа 101");

        text.Should().Contain("Расписания нет — выходной!");
    }

    [Fact]
    public void FormatWeekSchedule_HeaderHasDateRange()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(Entries(), weekStart, "Группа 101");

        text.Should().Contain("07.09–13.09");
        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
    }

    [Fact]
    public void FormatWeekSchedule_SundayEntries_HeaderUsesIndex7()
    {
        var weekStart = new DateTime(2026, 9, 7);
        var entries = new List<ScheduleResponse> { Entries()[0] with { DayOfWeek = 0 } };

        var text = MessageFormatter.FormatWeekSchedule(entries, weekStart, "Группа 101");

        text.Should().Contain("Воскресенье, 13.09");
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    public void DayIndex_MapsApiDayToLabelIndex(int apiDay, int expected)
    {
        MessageFormatter.DayIndex(apiDay).Should().Be(expected);
    }

    [Fact]
    public void DateForWeekDay_MapsApiDayToDate()
    {
        var weekStart = new DateTime(2026, 9, 7);

        MessageFormatter.DateForWeekDay(weekStart, 1).Should().Be(new DateTime(2026, 9, 7));
        MessageFormatter.DateForWeekDay(weekStart, 0).Should().Be(new DateTime(2026, 9, 13));
    }

    [Fact]
    public void FormatWeekSchedule_PairNumbers_AreBoldNotOrderedList()
    {
        var entries = new List<ScheduleResponse>();
        for (var i = 1; i <= 7; i++)
            entries.Add(Entries()[0] with { NumberPair = i, Subject = $"Предмет{i}" });

        var text = MessageFormatter.FormatWeekSchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("*7.* 📖 Предмет7");
        text.Should().NotMatch(@"  \d+\.");
    }

    [Fact]
    public void FormatDaySchedule_ChangeTags_ShowsMarkers()
    {
        var entries = new List<ScheduleResponse>
        {
            Entries()[0] with
            {
                ChangeTags =
                [
                    new ChangeTag
                    {
                        ChangeType = "Move",
                        Week = 1,
                        RemovedNumberPair = 2,
                    },
                ],
            },
        };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("🔄 перенос с пары 2");
    }

    [Fact]
    public void FormatDaySchedule_HasHorizontalRuleBetweenSlots()
    {
        var entries = new List<ScheduleResponse>
        {
            Entries()[0],
            Entries()[0] with
            {
                NumberPair = 2,
                Subject = "Физика",
                StartTime = new TimeSpan(10, 50, 0),
            },
        };

        var text = MessageFormatter.FormatDaySchedule(
            entries,
            new DateTime(2026, 9, 7),
            "Группа 101"
        );

        text.Should().Contain("────────");
        text.Should().Contain("*1.* 📖 Математика");
        text.Should().Contain("*2.* 📖 Физика");
    }

    private static ScheduleRevision Revision(string changeType = "Replace") =>
        new()
        {
            Id = 1,
            ForeignId = Guid.NewGuid(),
            ChangeType = changeType,
            GroupName = "ПО262",
            TeacherName = "Петренко В.Б.",
            Subject = "История",
            Room = "301",
            DayOfWeek = "Вторник",
            Week = 1,
            NumberPair = 2,
            Note = "вм.4 п",
            CreatedAt = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc),
        };

    [Theory]
    [InlineData("Add", "добавлена")]
    [InlineData("Remove", "снята")]
    [InlineData("Replace", "замена")]
    [InlineData("", "изменена")]
    public void FormatChangeNotificationTitle_TitlesByType(string changeType, string expected)
    {
        MessageFormatter.FormatChangeNotificationTitle(changeType).Should().Be(expected);
    }

    [Fact]
    public void FormatChangeNotification_ContainsMetaHeaderAndFields()
    {
        var text = MessageFormatter.FormatChangeNotification(Revision());

        text.Should().Contain("🔔 *Изменение в расписании*");
        text.Should().Contain("ПО262 · Вторник · Нед. 1 · Пара 2");
        text.Should().Contain("📖 История — замена");
        text.Should().Contain("👨‍🏫 Преподаватель: Петренко В.Б.");
        text.Should().Contain("📝 Примечание: вм.4 п");
    }

    [Fact]
    public void FormatChangeNotification_WithoutNoteAndTeacher_HidesOptionalLines()
    {
        var r = Revision();
        r.TeacherName = null;
        r.Note = null;

        var text = MessageFormatter.FormatChangeNotification(r);

        text.Should().Contain("📖 История — замена");
        text.Should().NotContain("👨‍🏫");
        text.Should().NotContain("📝");
    }

    [Fact]
    public void FormatMyChanges_Empty_ShowsEmptyState()
    {
        var text = MessageFormatter.FormatMyChanges([], 0);

        text.Should().Contain("Изменений пока нет.");
    }

    private static ScheduleRevision CreateRevision(int index) =>
        new()
        {
            Id = index,
            ForeignId = Guid.NewGuid(),
            ChangeType = "Replace",
            GroupName = "ПО262",
            TeacherName = "Петренко В.Б.",
            Subject = "История",
            Room = "301",
            DayOfWeek = "Вторник",
            Week = 1,
            NumberPair = 2,
            CreatedAt = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc).AddMinutes(index),
        };

    [Fact]
    public void FormatMyChanges_FirstPage_ListsIndexedItems()
    {
        var items = Enumerable.Range(1, 20).Select(CreateRevision).ToList();

        var text = MessageFormatter.FormatMyChanges(items, 0);

        text.Should().Contain("стр. 1");
        text.Should().Contain("1. ПО262 · Вторник · Нед. 1 · Пара 2");
        text.Should().Contain("20. ПО262 · Вторник · Нед. 1 · Пара 2");
        text.Should().Contain("📖 История (замена)");
    }

    [Fact]
    public void FormatMyChanges_SecondPage_StartsIndexAt21()
    {
        var items = Enumerable.Range(1, 25).Select(CreateRevision).Skip(20).Take(5).ToList();

        var text = MessageFormatter.FormatMyChanges(items, 1);

        text.Should().Contain("стр. 2");
        text.Should().Contain("21. ПО262 · Вторник · Нед. 1 · Пара 2");
        text.Should().Contain("25. ПО262 · Вторник · Нед. 1 · Пара 2");
    }
}
