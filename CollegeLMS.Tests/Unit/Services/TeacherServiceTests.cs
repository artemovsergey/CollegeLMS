using CollegeLMS.API.Dtos;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.Tests.Unit.Services;

public class TeacherServiceTests : IDisposable
{
    private readonly API.Data.AppDbContext _db;
    private readonly TeacherService _sut;

    public TeacherServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new TeacherService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTeachers()
    {
        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsTeachers_WhenTeachersExist()
    {
        var teachers = TeacherFixture.CreateFaker().Generate(3);
        _db.Users.AddRange(teachers.Select(t => t.User));
        _db.Teachers.AddRange(teachers);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(default);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsTeacher_WhenFound()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(teacher.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(teacher.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_CreatesTeacherAndUser()
    {
        var request = new CreateTeacherRequest
        {
            Email = "teacher@test.ru",
            Password = "test123",
            FullName = "Иван Иванов",
            Department = "ИТ",
            Position = "Преподаватель",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Иван Иванов");
        result.Data.Email.Should().Be("teacher@test.ru");
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenDuplicateEmail()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var request = new CreateTeacherRequest
        {
            Email = teacher.User.Email,
            Password = "test123",
            FullName = "Другой",
            Department = "ИТ",
            Position = "Преподаватель",
        };

        var result = await _sut.CreateAsync(request, default);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Update_UpdatesTeacher()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var request = new UpdateTeacherRequest
        {
            Email = "updated@test.ru",
            FullName = "Обновлённый",
            Department = "Новая кафедра",
            Position = "Старший преподаватель",
        };

        var result = await _sut.UpdateAsync(teacher.Id, request, default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Обновлённый");
        result.Data.Email.Should().Be("updated@test.ru");
    }

    [Fact]
    public async Task Delete_SoftDeletesTeacher()
    {
        var teacher = TeacherFixture.CreateFaker().Generate();
        _db.Users.Add(teacher.User);
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(teacher.Id, default);

        result.IsSuccess.Should().BeTrue();
        var user = await _db.Users.FindAsync(teacher.UserId);
        user!.IsActive.Should().BeFalse();
    }
}
