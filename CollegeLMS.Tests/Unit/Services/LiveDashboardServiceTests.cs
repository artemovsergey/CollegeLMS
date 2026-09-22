using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class LiveDashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BellScheduleServiceStub _bells = new();
    private readonly TimeZoneInfo _tz = TimeZoneProvider.Resolve("Europe/Moscow");
    private readonly LiveDashboardService _sut;

    private static readonly DateTime Monday1 = StudyWeek.MondayOf(StudyWeek.SemesterStart);

    public LiveDashboardServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new LiveDashboardService(_db, _bells, _tz);
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Group Group, Teacher Teacher)> SeedGroupAndTeacherAsync(
        string groupName,
        string teacherName
    )
    {
        var group = GroupFixture.CreateFaker().Generate();
        group.Name = groupName;
        var teacher = TeacherFixture.CreateFaker().Generate();
        teacher.User.FullName = teacherName;
        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return (group, teacher);
    }

    private async Task SeedEntryAsync(
        Guid groupId,
        Guid teacherId,
        DayOfWeek day,
        int numberPair,
        params int[] weeks
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                TeacherId = teacherId,
                Subject = "Математика",
                Room = "301",
                DayOfWeek = day,
                NumberPair = numberPair,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(9, 50, 0),
                Weeks = weeks.ToList(),
                LessonType = LessonType.Lecture,
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
        PracticeKind kind,
        string name,
        int? pairCount = null
    )
    {
        var utcNow = DateTime.UtcNow;
        var practice = new Practice
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Name = name,
            GroupId = groupId,
            DateFrom = date.Date,
            DateTo = date.Date,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Teachers =
            [
                new PracticeTeacher
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
            ],
        };

        if (kind == PracticeKind.Up && pairCount is int count)
            practice.Days =
            [
                new PracticeDay
                {
                    Id = Guid.NewGuid(),
                    Date = date.Date,
                    PairCount = count,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
            ];

        _db.Practices.Add(practice);
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

    private async Task SeedWorkingDayAsync(
        DateTime from,
        DateTime to,
        int? substituteDayOfWeek,
        string title
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.WorkingDayOverrides.Add(
            new WorkingDayOverride
            {
                Id = Guid.NewGuid(),
                DateFrom = from.Date,
                DateTo = to.Date,
                SubstituteDayOfWeek = substituteDayOfWeek,
                Title = title,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    private void SetPairTime(
        int numberPair,
        int startHour,
        int startMinute,
        int endHour,
        int endMinute
    ) =>
        _bells.TimeMap[numberPair] = (
            new TimeSpan(startHour, startMinute, 0),
            new TimeSpan(endHour, endMinute, 0)
        );

    [Fact]
    public async Task GetLiveAsync_WithinPair_ReturnsInLessonWithCurrentAndNext()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1;
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 2, 1);
        SetPairTime(1, 8, 30, 9, 50);
        SetPairTime(2, 10, 0, 11, 20);

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue();
        var response = result.Data!;
        response.Week.Should().Be(1);
        response.DayOfWeek.Should().Be((int)DayOfWeek.Monday);
        response.IsWorkingDay.Should().BeTrue();
        response.Counts.InLesson.Should().Be(2);

        var groupStatus = response.Groups.Should().ContainSingle().Which;
        groupStatus.Name.Should().Be("ИС-21");
        groupStatus.Status.Should().Be(LiveLessonStatus.InLesson);
        groupStatus.TotalPairs.Should().Be(2);
        groupStatus.CurrentPair!.NumberPair.Should().Be(1);
        groupStatus.NextPair!.NumberPair.Should().Be(2);
        groupStatus.Entries[0].StartTime.Should().Be(new TimeSpan(8, 30, 0));
        groupStatus.Entries[0].EndTime.Should().Be(new TimeSpan(9, 50, 0));
        groupStatus.Entries[0].TeacherName.Should().Be("Иванов И.И.");

        var teacherStatus = response.Teachers.Should().ContainSingle().Which;
        teacherStatus.Name.Should().Be("Иванов И.И.");
        teacherStatus.Status.Should().Be(LiveLessonStatus.InLesson);
        teacherStatus.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLiveAsync_BeforeFirstPair_ReturnsWaiting()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1;
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        SetPairTime(1, 8, 30, 9, 50);

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(8, 0, 0)),
            CancellationToken.None
        );

        var groupStatus = result.Data!.Groups.Should().ContainSingle().Which;
        groupStatus.Status.Should().Be(LiveLessonStatus.Waiting);
        groupStatus.CurrentPair.Should().BeNull();
        groupStatus.NextPair!.NumberPair.Should().Be(1);
        result.Data.Counts.Waiting.Should().Be(2);
    }

    [Fact]
    public async Task GetLiveAsync_AfterLastPair_ReturnsFinished()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1;
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        SetPairTime(1, 8, 30, 9, 50);

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(12, 0, 0)),
            CancellationToken.None
        );

        var groupStatus = result.Data!.Groups.Should().ContainSingle().Which;
        groupStatus.Status.Should().Be(LiveLessonStatus.Finished);
        groupStatus.CurrentPair.Should().BeNull();
        groupStatus.NextPair.Should().BeNull();
        result.Data.Counts.Finished.Should().Be(2);
    }

    [Fact]
    public async Task GetLiveAsync_NonWorkingDay_ReturnsAllWithoutPairs()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1;
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        await SeedNonWorkingAsync(date, date, "Праздник");

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        var response = result.Data!;
        response.IsWorkingDay.Should().BeFalse();
        response.IsNonWorking.Should().BeTrue();
        response.NonWorkingTitle.Should().Be("Праздник");
        response.Groups.Should().ContainSingle().Which.Status.Should().Be(LiveLessonStatus.NoPairs);
        response
            .Teachers.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(LiveLessonStatus.NoPairs);
        response.Counts.NoPairs.Should().Be(2);
    }

    [Fact]
    public async Task GetLiveAsync_EntityWithoutPairsToday_IsListedAsNoPairs()
    {
        var (busyGroup, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        await SeedGroupAndTeacherAsync("ИС-22", "Петров П.П.");
        var date = Monday1;
        await SeedEntryAsync(busyGroup.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        SetPairTime(1, 8, 30, 9, 50);

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        var response = result.Data!;
        response.IsWorkingDay.Should().BeTrue();
        response.Groups.Should().HaveCount(2);
        response
            .Groups.Should()
            .ContainSingle(g => g.Name == "ИС-22" && g.Status == LiveLessonStatus.NoPairs)
            .Which.Entries.Should()
            .BeEmpty();
        response
            .Teachers.Should()
            .ContainSingle(t => t.Name == "Петров П.П." && t.Status == LiveLessonStatus.NoPairs);
        response.Counts.NoPairs.Should().Be(2);
    }

    [Fact]
    public async Task GetLiveAsync_WorkingDayOverride_UsesSubstituteDayEntries()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var saturday = Monday1.AddDays(5);
        await SeedEntryAsync(group.Id, teacher.Id, DayOfWeek.Monday, 1, 1);
        await SeedWorkingDayAsync(saturday, saturday, substituteDayOfWeek: 1, "Рабочая суббота");
        SetPairTime(1, 8, 30, 9, 50);

        var result = await _sut.GetLiveAsync(
            saturday,
            saturday.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        var response = result.Data!;
        response.IsWorkingDay.Should().BeTrue();
        response.WorkingDayTitle.Should().Be("Рабочая суббота");
        response.DayOfWeek.Should().Be((int)DayOfWeek.Saturday);
        response.Groups.Should().ContainSingle().Which.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task GetLiveAsync_UpPractice_SynthesizesPairsFromBellProfile()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1.AddDays(1);
        await SeedPracticeAsync(group.Id, teacher.Id, date, PracticeKind.Up, "УП 01", pairCount: 3);
        SetPairTime(1, 8, 30, 9, 50);
        SetPairTime(2, 10, 0, 11, 20);
        SetPairTime(3, 11, 30, 12, 50);

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        var groupStatus = result.Data!.Groups.Should().ContainSingle().Which;
        groupStatus.Entries.Should().HaveCount(3);
        groupStatus
            .Entries.Should()
            .OnlyContain(e =>
                e.IsPractice
                && e.PracticeName == "УП 01"
                && e.Subject == "УП 01"
                && e.LessonType == "Practice"
            );
        groupStatus.Entries[0].StartTime.Should().Be(new TimeSpan(8, 30, 0));
        groupStatus.Entries[2].StartTime.Should().Be(new TimeSpan(11, 30, 0));
        groupStatus.Status.Should().Be(LiveLessonStatus.InLesson);
        groupStatus.CurrentPair!.NumberPair.Should().Be(1);

        var teacherStatus = result.Data.Teachers.Should().ContainSingle().Which;
        teacherStatus.Name.Should().Be("Иванов И.И.");
        teacherStatus.TotalPairs.Should().Be(3);
    }

    [Fact]
    public async Task GetLiveAsync_PpPractice_HasNoPairs()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync("ИС-21", "Иванов И.И.");
        var date = Monday1.AddDays(1);
        await SeedPracticeAsync(group.Id, teacher.Id, date, PracticeKind.Pp, "ПП 09");

        var result = await _sut.GetLiveAsync(
            date,
            date.Add(new TimeSpan(9, 0, 0)),
            CancellationToken.None
        );

        var response = result.Data!;
        var groupStatus = response.Groups.Should().ContainSingle().Which;
        groupStatus.Entries.Should().BeEmpty();
        groupStatus.TotalPairs.Should().Be(0);
        groupStatus.Status.Should().Be(LiveLessonStatus.NoPairs);
        response
            .Teachers.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(LiveLessonStatus.NoPairs);
        response.Counts.NoPairs.Should().Be(2);
    }

    [Fact]
    public async Task GetLiveAsync_NoDateAndNoAt_UsesMoscowToday()
    {
        var before = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz).Date;

        var result = await _sut.GetLiveAsync(null, null, CancellationToken.None);

        var after = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz).Date;
        result.IsSuccess.Should().BeTrue();
        result.Data!.Date.Should().BeOneOf(before, after);
    }

    [Fact]
    public async Task GetLiveAsync_NoDate_TargetFollowsMoscowWallClock()
    {
        // 22:00 UTC — уже следующие сутки по МСК, но `at` передаётся как московское время суток.
        var at = new DateTime(2030, 6, 10, 23, 30, 0);

        var result = await _sut.GetLiveAsync(null, at, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Date.Should().Be(new DateTime(2030, 6, 10));
        result.Data.Now.Should().Be(new TimeSpan(23, 30, 0));
    }
}
