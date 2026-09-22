using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class MessageFormatterTests
{
    private static ScheduleResponse Pair(
        int numberPair = 1,
        string subject = "Математика",
        string room = "405",
        string? teacherName = "Иванов И.И.",
        string lessonType = "Lecture",
        string groupName = "",
        TimeSpan? start = null,
        TimeSpan? end = null,
        List<ChangeTag>? changeTags = null
    ) =>
        new()
        {
            DayOfWeek = 1,
            NumberPair = numberPair,
            Subject = subject,
            Room = room,
            StartTime = start ?? new TimeSpan(9, 0, 0),
            EndTime = end ?? new TimeSpan(10, 30, 0),
            TeacherName = teacherName,
            LessonType = lessonType,
            GroupName = groupName,
            ChangeTags = changeTags ?? [],
        };

    private static ScheduleInsertDto Insert(string title, int hour, int minute) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            DayOfWeek = 1,
            StartTime = new TimeSpan(hour, minute, 0),
            EndTime = new TimeSpan(hour, minute + 30, 0),
            IsActive = true,
        };

    private static PracticeDto Practice() =>
        new()
        {
            Id = Guid.NewGuid(),
            Kind = "Up",
            GroupId = Guid.NewGuid(),
            GroupName = "ПО-262",
            TeacherId = Guid.NewGuid(),
            TeacherName = "Марченко И.А.",
            DateFrom = new DateTime(2026, 9, 7),
            DateTo = new DateTime(2026, 9, 11),
        };

    private static ScheduleDayViewDto Day(
        DateTime date,
        List<ScheduleResponse>? entries = null,
        bool isSunday = false,
        bool isNonWorking = false,
        string? nonWorkingTitle = null,
        List<PracticeDto>? practices = null,
        List<ScheduleInsertDto>? inserts = null,
        BigBreakDto? bigBreak = null
    ) =>
        new()
        {
            Date = date,
            Week = 1,
            DayOfWeek = MessageFormatter.ToApiDay(date.DayOfWeek),
            IsSunday = isSunday,
            IsNonWorking = isNonWorking,
            NonWorkingTitle = nonWorkingTitle,
            Practices = practices ?? [],
            Inserts = inserts ?? [],
            Entries = entries ?? [],
            BigBreak = bigBreak,
        };

    private static ScheduleWeekViewDto Week(DateTime weekStart, params ScheduleDayViewDto[] days) =>
        new()
        {
            Week = 1,
            WeekStart = weekStart,
            Days = [.. days],
        };

    [Fact]
    public void FormatDaySchedule_IncludesDateInHeader()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair()]),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
        text.Should().Contain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_ShowGroup_IncludesGroupName()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair(groupName: "ИС-21")]),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_WithoutShowGroup_OmitsGroupName()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair(groupName: "ИС-21")]),
            "Иванов И.И.",
            showGroup: false
        );

        text.Should().NotContain("ИС-21");
    }

    [Fact]
    public void FormatDaySchedule_EmptyEntries_ShowsNoPairs()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7)),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("Пар нет.");
        text.Should().NotContain("выходной!");
    }

    [Fact]
    public void FormatDaySchedule_BigBreak_NotShown()
    {
        var bigBreak = new BigBreakDto
        {
            Id = Guid.NewGuid(),
            AfterPair = 2,
            StartTime = new TimeSpan(11, 0, 0),
            EndTime = new TimeSpan(11, 20, 0),
        };

        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair()], bigBreak: bigBreak),
            "Группа 101",
            showGroup: false
        );

        text.Should().NotContain("Большая перемена");
        text.Should().NotContain("11:00–11:20");
    }

    [Fact]
    public void FormatDaySchedule_BigBreakNotShown_WhenNoPairs()
    {
        var bigBreak = new BigBreakDto
        {
            Id = Guid.NewGuid(),
            AfterPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 20, 0),
        };

        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), bigBreak: bigBreak),
            "Группа 101",
            showGroup: false
        );

        text.Should().NotContain("Большая перемена");
    }

    [Fact]
    public void FormatWeekSchedule_BigBreak_NotShown()
    {
        var bigBreak = new BigBreakDto
        {
            Id = Guid.NewGuid(),
            AfterPair = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 20, 0),
        };
        var day = Day(new DateTime(2026, 9, 7), [Pair()], bigBreak: bigBreak);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(new DateTime(2026, 9, 7), day),
            "Группа 101",
            showGroup: false
        );

        text.Should().NotContain("Большая перемена");
        text.Should().NotContain("09:00–09:20");
    }

    [Fact]
    public void FormatDaySchedule_Sunday_ShowsHoliday()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 13), isSunday: true),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("Воскресенье, 13.09");
        text.Should().Contain("Расписания нет — выходной!");
    }

    [Fact]
    public void FormatDaySchedule_NonWorking_ShowsTitle()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), isNonWorking: true, nonWorkingTitle: "Праздник"),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("🎉 Нерабочий день: Праздник");
        text.Should().NotContain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_Practice_ReplacesPairs()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair()], practices: [Practice()]),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("🎓 *Практика*");
        text.Should().Contain("УП: ПО-262");
        text.Should().NotContain("Математика");
    }

    [Fact]
    public void FormatDaySchedule_Inserts_RenderedBeforePairs()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), [Pair()], inserts: [Insert("Линейка", 8, 0)]),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("08:00–08:30 Линейка");
        text.IndexOf("08:00–08:30 Линейка").Should().BeLessThan(text.IndexOf("*1.* 📖 Математика"));
    }

    [Fact]
    public void FormatDaySchedule_EmptyEntriesWithInserts_ShowsInsertAndNoPairs()
    {
        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), inserts: [Insert("Линейка", 8, 0)]),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("08:00–08:30 Линейка");
        text.Should().Contain("Пар нет.");
        text.Should().NotContain("выходной!");
    }

    [Fact]
    public void FormatDaySchedule_ChangeTags_ShowsMarkers()
    {
        var entries = new List<ScheduleResponse>
        {
            Pair(
                changeTags:
                [
                    new ChangeTag
                    {
                        ChangeType = "Move",
                        Week = 1,
                        RemovedNumberPair = 2,
                    },
                ]
            ),
        };

        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), entries),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("🔄 перенос с пары 2");
    }

    [Fact]
    public void FormatDaySchedule_HasHorizontalRuleBetweenSlots()
    {
        var entries = new List<ScheduleResponse>
        {
            Pair(),
            Pair(
                numberPair: 2,
                subject: "Физика",
                start: new TimeSpan(10, 50, 0),
                end: new TimeSpan(12, 20, 0)
            ),
        };

        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), entries),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("────────");
        text.Should().Contain("*1.* 📖 Математика");
        text.Should().Contain("*2.* 📖 Физика");
    }

    [Fact]
    public void FormatDaySchedule_AfternoonTime_Uses24HourFormat()
    {
        var entries = new List<ScheduleResponse>
        {
            Pair(start: new TimeSpan(15, 5, 0), end: new TimeSpan(16, 25, 0)),
        };

        var text = MessageFormatter.FormatDaySchedule(
            Day(new DateTime(2026, 9, 7), entries),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("15:05–16:25");
        text.Should().NotContain("03:05");
    }

    [Fact]
    public void FormatWeekSchedule_HeaderHasDateRange()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(weekStart, [Pair()])),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("Неделя 1 · 07.09–13.09");
        text.Should().Contain("Понедельник, 07.09");
        text.Should().Contain("Группа 101");
    }

    [Fact]
    public void FormatWeekSchedule_ShowGroup_IncludesGroupName()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(weekStart, [Pair(groupName: "ИС-21")])),
            "Иванов И.И.",
            showGroup: true
        );

        text.Should().Contain("ИС-21");
    }

    [Fact]
    public void FormatWeekSchedule_NonWorkingDay_MarkedInDayBlock()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(
                weekStart,
                Day(weekStart, [Pair()]),
                Day(weekStart.AddDays(1), isNonWorking: true, nonWorkingTitle: "Праздник")
            ),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("*Вторник, 08.09*");
        text.Should().Contain("🎉 Нерабочий день: Праздник");
    }

    [Fact]
    public void FormatWeekSchedule_PracticeDay_NoPairs()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(weekStart, [Pair()], practices: [Practice()])),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("🎓 *Практика*");
        text.Should().Contain("УП: ПО-262");
        text.Should().NotContain("Математика");
    }

    [Fact]
    public void FormatWeekSchedule_EmptyDay_ShowsNoPairs()
    {
        var weekStart = new DateTime(2026, 9, 7);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(weekStart)),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("Пар нет.");
    }

    [Fact]
    public void FormatWeekSchedule_SundayEntries_HeaderUsesIndex7()
    {
        var weekStart = new DateTime(2026, 9, 7);
        var sunday = weekStart.AddDays(6);

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(sunday, [Pair()], isSunday: true)),
            "Группа 101",
            showGroup: false
        );

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
        var weekStart = new DateTime(2026, 9, 7);
        var entries = new List<ScheduleResponse>();
        for (var i = 1; i <= 7; i++)
            entries.Add(Pair(numberPair: i, subject: $"Предмет{i}"));

        var text = MessageFormatter.FormatWeekSchedule(
            Week(weekStart, Day(weekStart, entries)),
            "Группа 101",
            showGroup: false
        );

        text.Should().Contain("*7.* 📖 Предмет7");
        text.Should().NotMatch(@"  \d+\.");
    }

    [Fact]
    public void FormatPracticeLine_Up_IncludesPeriod()
    {
        var practice = new PracticeDto
        {
            Id = Guid.NewGuid(),
            Kind = "Up",
            GroupName = "ПО-262",
            TeacherName = "Марченко И.А.",
            DateFrom = new DateTime(2026, 9, 7),
            DateTo = new DateTime(2026, 9, 11),
        };

        var line = MessageFormatter.FormatPracticeLine(practice);

        line.Should().Be("УП: ПО-262 · Марченко И.А. (с 07.09.2026 по 11.09.2026)");
    }

    [Fact]
    public void FormatPracticeLine_Pp_IncludesPeriod()
    {
        var practice = new PracticeDto
        {
            Id = Guid.NewGuid(),
            Kind = "Pp",
            GroupName = "ПО-263",
            TeacherName = "Петренко В.Б.",
            DateFrom = new DateTime(2026, 10, 1),
            DateTo = new DateTime(2026, 10, 12),
        };

        var line = MessageFormatter.FormatPracticeLine(practice);

        line.Should().Be("ПП: ПО-263 · Петренко В.Б. (с 01.10.2026 по 12.10.2026)");
    }

    [Fact]
    public void FormatPracticeStarted_IncludesNameKindGroupAndPeriod()
    {
        var practice = new PracticeDto
        {
            Id = Guid.NewGuid(),
            Kind = "Up",
            Name = "УП 01",
            GroupName = "ПО-262",
            Teachers = [new PracticeTeacherDto { Id = Guid.NewGuid(), FullName = "Марченко И.А." }],
            DateFrom = new DateTime(2026, 9, 7),
            DateTo = new DateTime(2026, 9, 11),
        };

        var text = MessageFormatter.FormatPracticeStarted(practice);

        text.Should().Contain("Началась практика").And.Contain("УП 01");
        text.Should().Contain("УП: ПО-262 · Марченко И.А.");
        text.Should().Contain("(с 07.09.2026 по 11.09.2026)");
    }

    [Fact]
    public void FormatPracticeFinished_UsesFallbackNameAndLegacyTeacher()
    {
        var practice = new PracticeDto
        {
            Id = Guid.NewGuid(),
            Kind = "Pp",
            GroupName = "ПО-263",
            TeacherName = "Петренко В.Б.",
            DateFrom = new DateTime(2026, 10, 1),
            DateTo = new DateTime(2026, 10, 12),
        };

        var text = MessageFormatter.FormatPracticeFinished(practice);

        text.Should().Contain("Закончилась практика").And.Contain("ПП");
        text.Should().Contain("ПП: ПО-263 · Петренко В.Б.");
        text.Should().Contain("(с 01.10.2026 по 12.10.2026)");
    }

    [Fact]
    public void FormatPracticeLine_NewContract_UsesTeachersList()
    {
        var practice = new PracticeDto
        {
            Id = Guid.NewGuid(),
            Kind = "Up",
            Name = "УП 01",
            GroupName = "ПО-262",
            TeacherIds = [Guid.NewGuid(), Guid.NewGuid()],
            Teachers =
            [
                new PracticeTeacherDto { Id = Guid.NewGuid(), FullName = "Марченко И.А." },
                new PracticeTeacherDto { Id = Guid.NewGuid(), Name = "Сидоров С.С." },
            ],
            DateFrom = new DateTime(2026, 9, 7),
            DateTo = new DateTime(2026, 9, 11),
        };

        var line = MessageFormatter.FormatPracticeLine(practice);

        line.Should().Be("УП: ПО-262 · Марченко И.А., Сидоров С.С. (с 07.09.2026 по 11.09.2026)");
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
    public void FormatCorrectionDigest_RendersWebStyleCard()
    {
        var text = MessageFormatter.FormatCorrectionDigest([Revision()], TimeZoneInfo.Utc);

        text.Should().Contain("🔔 **Изменения в расписании**");
        text.Should().Contain("**Замена**");
        text.Should().Contain("📅 1 сентября 2026");
        text.Should().Contain("Вторник, 1-я неделя");
        text.Should().Contain("🏫 **ПО262**");
        text.Should().Contain("🕐 пара 2");
        text.Should().Contain("📍 ауд. 301");
        text.Should().Contain("📖 **История**");
        text.Should().Contain("👤 Петренко В.Б.");
        text.Should().Contain("📝 Примечание: вм.4 п");
        text.Should().NotContain("http");
    }

    [Fact]
    public void FormatCorrectionDigest_ReplaceWithRemovedSubject_ShowsStrikethrough()
    {
        var revision = Revision();
        revision.ChangeType = "Move";
        revision.RemovedSubject = "Математика";
        revision.RemovedNumberPair = 2;
        revision.NumberPair = 3;

        var text = MessageFormatter.FormatCorrectionDigest([revision], TimeZoneInfo.Utc);

        text.Should().Contain("**Перенос**");
        text.Should().Contain("🕐 пара 2 → 3");
        text.Should().Contain("📖 ~~Математика~~ → **История**");
    }

    [Fact]
    public void FormatCorrectionDigest_DoesNotShowAppliedAt()
    {
        var text = MessageFormatter.FormatCorrectionDigest([Revision()], TimeZoneInfo.Utc);

        text.Should().NotContain("Применено");
    }

    [Fact]
    public void FormatCorrectionDigest_SelfStudyNote_ShowsSelfStudyBadge()
    {
        var revision = Revision(changeType: "Remove");
        revision.Note = "сам.р.";

        var text = MessageFormatter.FormatCorrectionDigest([revision], TimeZoneInfo.Utc);

        text.Should().Contain("🟣 Сам.р.");
        text.Should().Contain("📖 **История**");
    }

    [Fact]
    public void FormatCorrectionDigest_WithoutSelfStudy_HidesSelfStudyBadge()
    {
        var text = MessageFormatter.FormatCorrectionDigest([Revision()], TimeZoneInfo.Utc);

        text.Should().NotContain("Сам.р.");
    }

    [Fact]
    public void FormatChangeNotification_ContainsMetaHeaderAndFields()
    {
        var text = MessageFormatter.FormatChangeNotification(Revision(), "https://stvcc.tech/max");

        text.Should().Contain("🔔 *Изменение в расписании*");
        text.Should().Contain("ПО262 · Вторник · Нед. 1 · Пара 2");
        text.Should().Contain("📖 История — замена");
        text.Should().Contain("👨‍🏫 Преподаватель: Петренко В.Б.");
        text.Should().Contain("📝 Примечание: вм.4 п");
        text.Should().Contain("route=day&date=");
    }

    [Fact]
    public void FormatChangeNotification_WithoutNoteAndTeacher_HidesOptionalLines()
    {
        var r = Revision();
        r.TeacherName = null;
        r.Note = null;

        var text = MessageFormatter.FormatChangeNotification(r, "https://stvcc.tech/max");

        text.Should().Contain("📖 История — замена");
        text.Should().NotContain("👨‍🏫");
        text.Should().NotContain("📝");
    }
}
