using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class StudentServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly StudentService _sut;

    public StudentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new StudentService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoStudents()
    {
        var result = await _sut.GetAllAsync(null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsStudents_WhenStudentsExist()
    {
        var students = StudentFixture.CreateFaker().Generate(3);
        _db.Users.AddRange(students.Select(s => s.User));
        _db.Groups.AddRange(students.Select(s => s.Group));
        _db.Students.AddRange(students);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(null, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_FiltersByGroupId()
    {
        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();

        var group = new API.Entities.Group
        {
            Id = groupId,
            Name = "ГР-11",
            Course = 1,
        };
        var otherGroup = new API.Entities.Group
        {
            Id = otherGroupId,
            Name = "ГР-22",
            Course = 2,
        };

        var students = new List<API.Entities.Student>();
        for (int i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            students.Add(
                new API.Entities.Student
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    GroupId = groupId,
                    RecordBookNumber = $"ЗК-2024-{i:D3}",
                    User = new User
                    {
                        Id = userId,
                        Email = $"s{i}@t.ru",
                        FullName = $"S{i}",
                        PasswordHash = "h",
                        Role = UserRole.Student,
                        IsActive = true,
                    },
                    Group = group,
                }
            );
        }

        var otherUserId = Guid.NewGuid();
        var other = new API.Entities.Student
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            GroupId = otherGroupId,
            RecordBookNumber = "ЗК-2024-999",
            User = new User
            {
                Id = otherUserId,
                Email = "o@t.ru",
                FullName = "O",
                PasswordHash = "h",
                Role = UserRole.Student,
                IsActive = true,
            },
            Group = otherGroup,
        };

        _db.Users.AddRange(students.Select(s => s.User));
        _db.Users.Add(other.User);
        _db.Groups.Add(group);
        _db.Groups.Add(otherGroup);
        _db.Students.AddRange(students);
        _db.Students.Add(other);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(groupId, default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsStudent_WhenFound()
    {
        var student = StudentFixture.CreateFaker().Generate();
        _db.Users.Add(student.User);
        _db.Groups.Add(student.Group);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(student.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesStudentAndUser()
    {
        var group = new API.Entities.Group
        {
            Id = Guid.NewGuid(),
            Name = "ТЕСТ-11",
            Course = 1,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var request = new CreateStudentRequest
        {
            Email = "student@test.ru",
            Password = "test123",
            FullName = "Пётр Петров",
            GroupId = group.Id,
            RecordBookNumber = "ЗК-2024-001",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Пётр Петров");
        result.Data.Email.Should().Be("student@test.ru");
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDuplicateEmail()
    {
        var student = StudentFixture.CreateFaker().Generate();
        _db.Users.Add(student.User);
        _db.Groups.Add(student.Group);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var request = new CreateStudentRequest
        {
            Email = student.User.Email,
            Password = "test123",
            FullName = "Другой",
            GroupId = student.GroupId,
            RecordBookNumber = "ЗК-9999-999",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Update_UpdatesStudent()
    {
        var student = StudentFixture.CreateFaker().Generate();
        _db.Users.Add(student.User);
        _db.Groups.Add(student.Group);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var request = new UpdateStudentRequest
        {
            Email = "updated@test.ru",
            FullName = "Обновлённый",
            GroupId = student.GroupId,
            RecordBookNumber = "ЗК-2024-999",
        };

        var result = await _sut.UpdateAsync(student.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Обновлённый");
        result.Data.Email.Should().Be("updated@test.ru");
    }

    [Fact]
    public async Task Delete_SoftDeletesStudent()
    {
        var student = StudentFixture.CreateFaker().Generate();
        _db.Users.Add(student.User);
        _db.Groups.Add(student.Group);
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(student.Id, default);

        result.IsSuccess.Should().BeTrue();
        var user = await _db.Users.FindAsync(student.UserId);
        user!.IsActive.Should().BeFalse();
    }
}
