using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace CollegeLMS.Tests.Unit.Services;

public class ScheduleViewServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BellScheduleServiceStub _bells = new();
    private readonly ScheduleViewService _sut;

    private static readonly DateTime Monday1 = StudyWeek.MondayOf(StudyWeek.SemesterStart); // понедельник недели 1

    public ScheduleViewServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ScheduleViewService(
            _db,
            _bells,
            new PracticeService(_db),
            new ScheduleInsertService(_db)
        );
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Group Group, Teacher Teacher)> SeedGroupAndTeacherAsync(int course = 2)
    {
        var group = GroupFixture.CreateFaker().Generate();
        group.Course = course;
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return (group, teacher);
    }

    private async Task SeedEntryAsync(
        Guid groupId,
        Guid? teacherId,
        DayOfWeek day,
        int numberPair,
        params int[] weeks
    )
    {
        var entry = ScheduleEntryFixture.CreateFaker().Generate();
        entry.GroupId = groupId;
        entry.TeacherId = teacherId;
        entry.Group = null;
        entry.Teacher = null;
        entry.DayOfWeek = day;
        entry.NumberPair = numberPair;
        entry.Weeks = weeks.ToList();
        entry.StartTime = new TimeSpan(8, 30, 0);
        entry.EndTime = new TimeSpan(9, 50, 0);
        _db.ScheduleEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    private async Task SeedInsertAsync(DayOfWeek day, int? course, bool active = true)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleInserts.Add(
            new ScheduleInsert
            {
                Id = Guid.NewGuid(),
                Title = "Разговор о важном",
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(9, 0, 0),
                Course = course,
                IsActive = active,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedPracticeAsync(
        Guid groupId,
        Guid teacherId,
        DateTime date,
        PracticeKind kind = PracticeKind.Pp
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.Practices.Add(
            new Practice
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                GroupId = groupId,
                TeacherId = teacherId,
                DateFrom = date.Date,
                DateTo = date.Date,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedNonWorkingAsync(DateTime from, DateTime to, string title)
    {
        var utcNow = DateTime.UtcNow;
        _db.NonWorkingDays.Add(
            new NonWorkingDay
            {
                Id = Guid.NewGuid(),
                DateFrom = from.Date,
                DateTo = to.Date,
                Title = title,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedHistoryAsync(
        Guid groupId,
        DayOfWeek day,
        int numberPair,
        int week,
        ScheduleChangeType type
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = type,
                AppliedAt = utcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = groupId,
                DayOfWeek = day,
                NumberPair = numberPair,
                Week = week,
                Subject = "Математика",
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDayAsync_Sunday_ReturnsIsSundayAndEmpty()
    {
        var result = await _sut.GetDayAsync(
            null,
            null,
            null,
            Monday1.AddDays(6),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        result.Data!.IsSunday.Should().BeTrue();
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
        result.Data.Practices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_NonWorking_ReturnsTitleAndEmpty()
    {
        var date = Monday1.AddDays(2); // среда недели 1
        await SeedNonWorkingAsync(date, date, "День народного единства");
        await SeedEntryAsync(Guid.NewGuid(), null, date.DayOfWeek, 1, 1);
        await SeedInsertAsync(date.DayOfWeek, null);

        var result = await _sut.GetDayAsync(null, null, null, date, CancellationToken.None);

        result.Data!.IsNonWorking.Should().BeTrue();
        result.Data.NonWorkingTitle.Should().Be("День народного единства");
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
        result.Data.Practices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_Practice_SuppressesEntriesAndInserts()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        var date = Monday1.AddDays(1);
        await SeedPracticeAsync(group.Id, teacher.Id, date);
        await SeedEntryAsync(group.Id, teacher.Id, date.DayOfWeek, 1, 1);
        await SeedInsertAsync(date.DayOfWeek, null);

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Practices.Should().HaveCount(1);
        result.Data.Practices[0].Kind.Should().Be(PracticeKind.Pp);
        result.Data.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDayAsync_Inserts_FiltersByCourseAndActive()
    {
        var (group, _) = await SeedGroupAndTeacherAsync(course: 2);
        var date = Monday1.AddDays(1);
        await SeedInsertAsync(date.DayOfWeek, null, active: true); // общая — попадает
        await SeedInsertAsync(date.DayOfWeek, 2, active: true); // курс группы — попадает
        await SeedInsertAsync(date.DayOfWeek, 3, active: true); // другой курс — нет
        await SeedInsertAsync(date.DayOfWeek, 2, active: false); // неактивная — нет

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Inserts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDayAsync_Entries_ApplyBellTimesAndTags()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        var date = Monday1.AddDays(1);
        await SeedEntryAsync(group.Id, teacher.Id, date.DayOfWeek, 1, 1, 2);
        await SeedHistoryAsync(group.Id, date.DayOfWeek, 1, 1, ScheduleChangeType.Add);
        _bells.TimeMap[1] = (new TimeSpan(10, 0, 0), new TimeSpan(11, 20, 0));

        var result = await _sut.GetDayAsync(group.Id, null, null, date, CancellationToken.None);

        result.Data!.Entries.Should().HaveCount(1);
        result.Data.Entries[0].StartTime.Should().Be(new TimeSpan(10, 0, 0));
        result.Data.Entries[0].EndTime.Should().Be(new TimeSpan(11, 20, 0));
        result
            .Data.Entries[0]
            .ChangeTags.Should()
            .ContainSingle(t => t.ChangeType == ScheduleChangeType.Add);
    }

    [Fact]
    public async Task GetDayAsync_PracticeServiceFails_PropagatesError()
    {
        var practices = new Mock<IPracticeService>();
        practices
            .Setup(p =>
                p.GetAllAsync(
                    It.IsAny<Guid?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<PracticeKind?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<PagedResponse<PracticeResponse>>.Fail("Сбой практик", 500));
        var sut = new ScheduleViewService(
            _db,
            _bells,
            practices.Object,
            new ScheduleInsertService(_db)
        );

        var result = await sut.GetDayAsync(null, null, null, Monday1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.ErrorMessage.Should().Be("Сбой практик");
    }

    [Fact]
    public async Task GetDayAsync_InsertServiceFails_PropagatesError()
    {
        var inserts = new Mock<IScheduleInsertService>();
        inserts
            .Setup(i =>
                i.GetAllAsync(
                    It.IsAny<DayOfWeek?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<List<ScheduleInsertResponse>>.Fail("Сбой вставок", 500));
        var sut = new ScheduleViewService(_db, _bells, new PracticeService(_db), inserts.Object);

        var result = await sut.GetDayAsync(null, null, null, Monday1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.ErrorMessage.Should().Be("Сбой вставок");
    }

    [Fact]
    public async Task GetDayAsync_Week_ComputedFromDate()
    {
        var result = await _sut.GetDayAsync(
            null,
            null,
            null,
            Monday1.AddDays(7),
            CancellationToken.None
        );

        result.Data!.Week.Should().Be(2);
    }

    [Fact]
    public async Task GetDayAsync_OutOfSemester_EntriesAndInsertsEmpty()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        await SeedInsertAsync(DayOfWeek.Monday, null);

        var result = await _sut.GetDayAsync(
            group.Id,
            null,
            null,
            new DateTime(2026, 8, 24),
            CancellationToken.None
        ); // понедельник до семестра

        result.Data!.Entries.Should().BeEmpty();
        result.Data.Inserts.Should().BeEmpty();
        result.Data.Practices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeekAsync_ReturnsSixDaysMondayToSaturday()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Wednesday, 2, 1);

        var result = await _sut.GetWeekAsync(group.Id, null, null, 1, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Week.Should().Be(1);
        result.Data.WeekStart.Should().Be(Monday1);
        result.Data.Days.Should().HaveCount(6); // Пн–Сб
        result.Data.Days[0].Date.Should().Be(Monday1);
        result.Data.Days[2].Entries.Should().ContainSingle(); // среда
        result.Data.Days[5].Date.DayOfWeek.Should().Be(DayOfWeek.Saturday);
    }

    [Fact]
    public async Task GetWeekAsync_ClampsWeekToSemester()
    {
        var result = await _sut.GetWeekAsync(null, null, null, 99, null, CancellationToken.None);

        result.Data!.Week.Should().Be(StudyWeek.TotalWeeks);
    }

    [Fact]
    public async Task GetMonthAsync_MarksSundayAndNonWorkingWithPairCount()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Tuesday, 1, 1); // 01.09.2026 — вторник недели 1
        await SeedNonWorkingAsync(new DateTime(2026, 9, 7), new DateTime(2026, 9, 7), "Праздник");

        var result = await _sut.GetMonthAsync(
            group.Id,
            null,
            null,
            "2026-09",
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var days = result.Data!.Days;
        days.Should().HaveCount(30);
        var sunday = days.Single(d => d.Date == new DateTime(2026, 9, 6));
        sunday.IsSunday.Should().BeTrue();
        sunday.PairCount.Should().Be(0);
        var holiday = days.Single(d => d.Date == new DateTime(2026, 9, 7));
        holiday.IsNonWorking.Should().BeTrue();
        holiday.NonWorkingTitle.Should().Be("Праздник");
        days.Single(d => d.Date == new DateTime(2026, 9, 1)).PairCount.Should().Be(1);
        days.Should().OnlyContain(d => !d.IsOutOfSemester);
    }

    [Fact]
    public async Task GetMonthAsync_AugustBoundary_Week1MondayInSemester()
    {
        var result = await _sut.GetMonthAsync(null, null, null, "2026-08", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result
            .Data!.Days.Single(d => d.Date == new DateTime(2026, 8, 28))
            .IsOutOfSemester.Should()
            .BeTrue();
        result
            .Data.Days.Single(d => d.Date == new DateTime(2026, 8, 31))
            .IsOutOfSemester.Should()
            .BeFalse();
    }

    [Fact]
    public async Task GetMonthAsync_PreSemesterDate_PairCountZero()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Tuesday, 1, 1); // 01.09.2026 — вторник недели 1

        var result = await _sut.GetMonthAsync(
            group.Id,
            null,
            null,
            "2026-08",
            CancellationToken.None
        );

        result.Data!.Days.Single(d => d.Date == new DateTime(2026, 8, 25)).PairCount.Should().Be(0);
        result
            .Data.Days.Single(d => d.Date == new DateTime(2026, 8, 25))
            .IsOutOfSemester.Should()
            .BeTrue();
    }

    [Fact]
    public async Task GetMonthAsync_PracticeKindAndOutOfSemester()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedPracticeAsync(group.Id, teacher.Id, new DateTime(2026, 12, 15), PracticeKind.Up);

        var result = await _sut.GetMonthAsync(
            group.Id,
            null,
            null,
            "2026-12",
            CancellationToken.None
        );

        var practiceDay = result.Data!.Days.Single(d => d.Date == new DateTime(2026, 12, 15));
        practiceDay.PracticeKinds.Should().ContainSingle().Which.Should().Be(PracticeKind.Up);
        practiceDay.PairCount.Should().Be(0);
        // семестр: 31.08.2026 + 16 недель → конец 20.12.2026
        result
            .Data.Days.Single(d => d.Date == new DateTime(2026, 12, 21))
            .IsOutOfSemester.Should()
            .BeTrue();
    }

    [Fact]
    public async Task GetMonthAsync_InvalidMonth_Returns400()
    {
        var result = await _sut.GetMonthAsync(null, null, null, "2026-13", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetSemesterAsync_RequiresExactlyOneFilter()
    {
        var none = await _sut.GetSemesterAsync(null, null, CancellationToken.None);
        none.IsSuccess.Should().BeFalse();
        none.StatusCode.Should().Be(400);
        none.ErrorMessage.Should().Contain("одну группу или одного преподавателя");

        var (group, teacher) = await SeedGroupAndTeacherAsync();
        var both = await _sut.GetSemesterAsync(group.Id, teacher.Id, CancellationToken.None);
        both.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetSemesterAsync_ReturnsTotalWeeksRowsWithSixDays()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);

        var result = await _sut.GetSemesterAsync(group.Id, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalWeeks.Should().Be(StudyWeek.TotalWeeks);
        result.Data.Weeks.Should().HaveCount(StudyWeek.TotalWeeks);
        result.Data.Weeks.Should().OnlyContain(w => w.Days.Count == 6);
        result.Data.Weeks[0].Days[0].Entries.Should().ContainSingle();
    }
}
