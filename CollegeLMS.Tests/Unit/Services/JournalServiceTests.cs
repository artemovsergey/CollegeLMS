using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;

namespace CollegeLMS.Tests.Unit.Services;

public class JournalServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ScheduleService _sut;

    public JournalServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var bells = new BellScheduleServiceStub();
        _sut = new ScheduleService(_db, new ScheduleExportService(_db, bells), bells);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Teacher> SeedTeacherAsync()
    {
        var utcNow = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "journal.teacher@collegelms.ru",
            FullName = "Грищук А.В.",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "ЦК",
            Position = "Преподаватель",
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            User = user,
        };
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();
        return teacher;
    }

    private async Task SeedEntryAsync(Guid teacherId, string subject, int pair, List<int> weeks)
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = teacherId,
                Subject = subject,
                Room = "303",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = pair,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                Weeks = weeks,
                LessonType = LessonType.Lecture,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJournalAsync_UnknownTeacher_Returns404()
    {
        var result = await _sut.GetJournalAsync(Guid.NewGuid(), null, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetJournalAsync_GroupBySubjectAndWeek_ReturnsItems()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1, 2]);
        await SeedEntryAsync(teacher.Id, "Физика", 4, [1]);

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        result.IsSuccess.Should().BeTrue();
        var data = result.Data!;
        data.TeacherId.Should().Be(teacher.Id);
        data.TeacherName.Should().Be("Грищук А.В.");
        data.Subjects.Should().HaveCount(2);

        var math = data.Subjects.Single(s => s.Subject == "Математика");
        math.Items.Should().HaveCount(2);
        math.Items[0].Week.Should().Be(1);
        math.Items[1].Week.Should().Be(2);
        math.Items[0].NumberPairs.Should().BeEquivalentTo([2]);
        math.PairCount.Should().Be(2);

        var physics = data.Subjects.Single(s => s.Subject == "Физика");
        physics.Items.Should().ContainSingle();
        physics.Items[0].NumberPairs.Should().BeEquivalentTo([4]);
        physics.PairCount.Should().Be(1);

        data.TotalPairCount.Should().Be(3);
    }

    [Fact]
    public async Task GetJournalAsync_SameSubjectDistinctPairs_AreAggregatedPerWeek()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);
        await SeedEntryAsync(teacher.Id, "Математика", 3, [1]);

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        result.IsSuccess.Should().BeTrue();
        var week1 = result.Data!.Subjects.Single().Items.Single();
        week1.NumberPairs.Should().BeEquivalentTo([2, 3]);
        result.Data!.TotalPairCount.Should().Be(2);
    }

    [Fact]
    public async Task GetJournalAsync_Date_ReflectsDayOfWeek()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [2]);

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        // Вторник 2-й недели: понедельник недели + (DayOfWeek.Tuesday)=1.
        var tuesdayWeek2 = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(7 + 1);
        result.Data!.Subjects.Single().Items.Single().Date.Should().Be(tuesdayWeek2);
    }

    private async Task SeedHistoryAsync(
        Guid teacherId,
        string subject,
        ScheduleChangeType changeType,
        string? note = null,
        int week = 1,
        DayOfWeek day = DayOfWeek.Tuesday
    )
    {
        var utcNow = DateTime.UtcNow;
        _db.ScheduleHistory.Add(
            new ScheduleHistory
            {
                Id = Guid.NewGuid(),
                ChangeType = changeType,
                AppliedAt = utcNow,
                AppliedByUserId = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                TeacherId = teacherId,
                Subject = subject,
                Room = "303",
                DayOfWeek = day,
                NumberPair = 2,
                Week = week,
                Note = note,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            }
        );
        await _db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(ScheduleChangeType.Add, "Add")]
    [InlineData(ScheduleChangeType.Replace, "Replace")]
    [InlineData(ScheduleChangeType.Move, "Move")]
    public async Task GetJournalAsync_History_ReturnsChangeTypeBadge(
        ScheduleChangeType changeType,
        string expected
    )
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);
        await SeedHistoryAsync(teacher.Id, "Математика", changeType, week: 1);

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Single().Items.Single().ChangeTypes.Should().Contain(expected);
    }

    [Fact]
    public async Task GetJournalAsync_RemoveSelfStudy_AddsSelfStudyBadge()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);
        await SeedHistoryAsync(
            teacher.Id,
            "Математика",
            ScheduleChangeType.Remove,
            note: "сам.р.",
            week: 1
        );

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        var changeTypes = result.Data!.Subjects.Single().Items.Single().ChangeTypes;
        changeTypes.Should().Contain("Remove");
        changeTypes.Should().Contain("SelfStudy");
    }

    [Fact]
    public async Task GetJournalAsync_HistoryOtherSubject_DoesNotLeakBadge()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);
        await SeedHistoryAsync(teacher.Id, "Физика", ScheduleChangeType.Add, week: 1);

        var result = await _sut.GetJournalAsync(teacher.Id, null, default);

        result.Data!.Subjects.Single().Items.Single().ChangeTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetJournalAsync_SubjectFilter_ReturnsOnlyMatchingSubject()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);
        await SeedEntryAsync(teacher.Id, "Физика", 4, [1]);

        var result = await _sut.GetJournalAsync(teacher.Id, "Физика", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Should().ContainSingle();
        result.Data.Subjects[0].Subject.Should().Be("Физика");
        result.Data.TotalPairCount.Should().Be(1);
    }

    [Fact]
    public async Task GetJournalAsync_SubjectFilterNoMatch_ReturnsEmptySubjects()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1]);

        var result = await _sut.GetJournalAsync(teacher.Id, "Химия", default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Subjects.Should().BeEmpty();
        result.Data.TotalPairCount.Should().Be(0);
    }
}
