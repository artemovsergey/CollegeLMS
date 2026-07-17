using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class ExamServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly ExamService _sut;

    public ExamServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new ExamService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoExams()
    {
        var result = await _sut.GetAllAsync(null, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsExams_WhenGroupFilter()
    {
        var groupId = Guid.NewGuid();
        var group = new Group
        {
            Id = groupId,
            Name = "ГР-11",
            Course = 1,
        };
        var teacherUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Учитель",
            Email = "t@t.ru",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            IsActive = true,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            CyclicalCommission = "ИТ",
            Position = "П",
        };
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Subject = "Математика",
            GroupId = groupId,
            ExamDate = DateTime.UtcNow.AddDays(30),
            Type = ExamType.Exam,
            TeacherId = teacher.Id,
            SemesterId = semester.Id,
            Status = ExamStatus.Scheduled,
        };
        _db.Users.Add(teacherUser);
        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        _db.Semesters.Add(semester);
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(groupId, null, null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_CreatesExam()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CyclicalCommission = "ИТ",
            Position = "Преподаватель",
        };
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };

        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync();

        var request = new CreateExamRequest
        {
            Subject = "Программирование",
            GroupId = group.Id,
            ExamDate = DateTime.UtcNow.AddDays(60),
            Type = "Exam",
            TeacherId = teacher.Id,
            SemesterId = semester.Id,
        };

        var result = await _sut.CreateAsync(request, default);

        var created = await _db.Exams.FirstOrDefaultAsync(e => e.Subject == "Программирование");
        Assert.NotNull(created);
        created.Subject.Should().Be("Программирование");
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenGroupNotFound()
    {
        var result = await _sut.CreateAsync(
            new CreateExamRequest
            {
                Subject = "Программирование",
                GroupId = Guid.NewGuid(),
                Type = "Exam",
                TeacherId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
            },
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFail_WhenInvalidType()
    {
        var result = await _sut.CreateAsync(
            new CreateExamRequest
            {
                Subject = "Программирование",
                GroupId = Guid.NewGuid(),
                Type = "InvalidType",
                TeacherId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
            },
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExam()
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ГР-11",
            Course = 1,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CyclicalCommission = "ИТ",
            Position = "Преподаватель",
        };
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Семестр",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Type = SemesterType.Autumn,
        };

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Subject = "Старая тема",
            GroupId = group.Id,
            ExamDate = DateTime.UtcNow.AddDays(30),
            Type = ExamType.Exam,
            TeacherId = teacher.Id,
            SemesterId = semester.Id,
            Status = ExamStatus.Scheduled,
        };
        _db.Groups.Add(group);
        _db.Teachers.Add(teacher);
        _db.Semesters.Add(semester);
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateAsync(
            exam.Id,
            new UpdateExamRequest
            {
                Subject = "Обновлённая тема",
                GroupId = group.Id,
                ExamDate = DateTime.UtcNow.AddDays(60),
                Type = "Exam",
                TeacherId = teacher.Id,
                SemesterId = semester.Id,
                Status = "Passed",
            },
            default
        );

        var updated = await _db.Exams.FindAsync([exam.Id]);
        Assert.NotNull(updated);
        updated.Subject.Should().Be("Обновлённая тема");
        updated.Status.Should().Be(ExamStatus.Passed);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.UpdateAsync(
            Guid.NewGuid(),
            new UpdateExamRequest
            {
                Subject = "Тема",
                GroupId = Guid.NewGuid(),
                ExamDate = DateTime.UtcNow,
                Type = "Exam",
                TeacherId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                Status = "Scheduled",
            },
            default
        );

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExam()
    {
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Subject = "Тест",
            GroupId = Guid.NewGuid(),
            ExamDate = DateTime.UtcNow,
            Type = ExamType.Exam,
            TeacherId = Guid.NewGuid(),
            SemesterId = Guid.NewGuid(),
            Status = ExamStatus.Scheduled,
        };
        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(exam.Id, default);

        result.IsSuccess.Should().BeTrue();
        var exists = await _db.Exams.AnyAsync(e => e.Id == exam.Id);
        exists.Should().BeFalse();
    }

}
