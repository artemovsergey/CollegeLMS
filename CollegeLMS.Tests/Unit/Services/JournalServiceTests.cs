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
        _sut = new ScheduleService(_db, new ScheduleExportService(_db));
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
        var result = await _sut.GetJournalAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetJournalAsync_GroupBySubjectAndWeek_ReturnsItems()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [1, 2]);
        await SeedEntryAsync(teacher.Id, "Физика", 4, [1]);

        var result = await _sut.GetJournalAsync(teacher.Id, default);

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

        var result = await _sut.GetJournalAsync(teacher.Id, default);

        result.IsSuccess.Should().BeTrue();
        var week1 = result.Data!.Subjects.Single().Items.Single();
        week1.NumberPairs.Should().BeEquivalentTo([2, 3]);
        result.Data!.TotalPairCount.Should().Be(2);
    }

    [Fact]
    public async Task GetJournalAsync_Date_IsMondayOfWeek()
    {
        var teacher = await SeedTeacherAsync();
        await SeedEntryAsync(teacher.Id, "Математика", 2, [2]);

        var result = await _sut.GetJournalAsync(teacher.Id, default);

        var mondayWeek2 = StudyWeek.MondayOf(StudyWeek.SemesterStart).AddDays(7);
        result.Data!.Subjects.Single().Items.Single().Date.Should().Be(mondayWeek2);
    }
}
