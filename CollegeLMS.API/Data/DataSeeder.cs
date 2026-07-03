using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "admin@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                FullName = "Администратор",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "teacher@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Иванов Иван Иванович",
                Role = UserRole.Teacher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "student@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Петров Пётр Петрович",
                Role = UserRole.Student,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var teacherUser = users.First(u => u.Email == "teacher@collegelms.ru");
        var studentUser = users.First(u => u.Email == "student@collegelms.ru");

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ИСП-31",
            Course = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Groups.Add(group);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = teacherUser.Id,
            Department = "Информационных технологий",
            Position = "Преподаватель",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Teachers.Add(teacher);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUser.Id,
            GroupId = group.Id,
            RecordBookNumber = "ЗК-001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Students.Add(student);

        await db.SaveChangesAsync();

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Основы программирования",
            Description = "Базовый курс по программированию на C#",
            TeacherId = teacher.Id,
            GroupId = group.Id,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Введение в C#",
            Content =
                "Основные понятия языка программирования C#: типы данных, переменные, операторы.",
            Order = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Lectures.Add(lecture);

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Hello World",
            Description = "Написать программу, выводящую 'Hello, World!' в консоль.",
            DueDate = DateTime.UtcNow.AddDays(14),
            MaxScore = 10,
            Order = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Assignments.Add(assignment);

        await db.SaveChangesAsync();
    }
}
