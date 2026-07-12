using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedUsersAsync(db);
        await SeedGroupsAsync(db);
        await SeedTeachersAsync(db);
        await SeedStudentsAsync(db);
        await SeedCoursesAsync(db);
        await SeedLecturesAsync(db);
        await SeedAssignmentsAsync(db);
        await SeedScheduleEntriesAsync(db);
        await SeedNewsCategoriesAsync(db);
        await SeedNewsAsync(db);
        await SeedAssignmentSubmissionsAsync(db);
        await SeedCourseMaterialsAsync(db);
        await SeedFeedbacksAsync(db);
    }

    private static async Task SeedUsersAsync(AppDbContext db)
    {
        var users = new List<User>
        {
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"),
                Email = "admin@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                FullName = "Администратор Системы",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"),
                Email = "teacher@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Иванов Иван Иванович",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                Email = "student@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Петров Пётр Петрович",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                Email = "dispatcher@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("dispatcher"),
                FullName = "Диспетчер Учебной Части",
                Role = UserRole.Dispatcher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"),
                Email = "ivanova@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Иванова Елена Алексеевна",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"),
                Email = "sidorov@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Сидоров Дмитрий Владимирович",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"),
                Email = "smirnova@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Смирнова Анна Сергеевна",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"),
                Email = "kozlov@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Козлов Андрей Николаевич",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"),
                Email = "popova@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Попова Мария Викторовна",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-00000000000a"),
                Email = "volkov@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Волков Сергей Петрович",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-00000000000b"),
                Email = "morozova@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Морозова Татьяна Дмитриевна",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-00000000000c"),
                Email = "novikov@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Новиков Алексей Игоревич",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-00000000000d"),
                Email = "zaytseva@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher"),
                FullName = "Зайцева Ольга Сергеевна",
                Role = UserRole.Teacher,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        var students = new List<User>
        {
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000001"),
                Email = "student01@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Алексеев Дмитрий Сергеевич",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000002"),
                Email = "student02@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Белова Екатерина Андреевна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000003"),
                Email = "student03@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Васильев Максим Олегович",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000004"),
                Email = "student04@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Григорьева Алина Денисовна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000005"),
                Email = "student05@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Дмитриев Кирилл Евгеньевич",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000006"),
                Email = "student06@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Егорова Виктория Павловна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000007"),
                Email = "student07@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Жукова Полина Игоревна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000008"),
                Email = "student08@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Зайцев Артём Викторович",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000009"),
                Email = "student09@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Исаева Наталья Романовна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000010"),
                Email = "student10@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Кузнецов Тимур Алексеевич",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000011"),
                Email = "student11@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Лебедева Ксения Дмитриевна",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a2000000-0000-0000-0000-000000000012"),
                Email = "student12@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student"),
                FullName = "Михайлов Иван Алексеевич",
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        users.AddRange(students);

        foreach (var user in users)
        {
            user.Login = user.Email.Split('@')[0];
            if (!await db.Users.AnyAsync(u => u.Login == user.Login))
                db.Users.Add(user);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedGroupsAsync(AppDbContext db)
    {
        if (await db.Groups.CountAsync() >= 10)
            return;

        var groups = new List<Group>
        {
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"),
                Name = "ИСП-31",
                Course = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"),
                Name = "ИСП-32",
                Course = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"),
                Name = "ИСП-41",
                Course = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"),
                Name = "ИСП-11",
                Course = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000005"),
                Name = "ИСП-12",
                Course = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000006"),
                Name = "ИСП-21",
                Course = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000007"),
                Name = "ИСП-22",
                Course = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000008"),
                Name = "ИСП-33",
                Course = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000009"),
                Name = "ИСП-42",
                Course = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000a"),
                Name = "ИСП-13",
                Course = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        foreach (var group in groups)
        {
            if (!await db.Groups.AnyAsync(g => g.Name == group.Name))
                db.Groups.Add(group);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedTeachersAsync(AppDbContext db)
    {
        if (await db.Teachers.CountAsync() >= 10)
            return;

        var teacherEmails = new[]
        {
            "teacher@collegelms.ru",
            "ivanova@collegelms.ru",
            "sidorov@collegelms.ru",
            "smirnova@collegelms.ru",
            "kozlov@collegelms.ru",
            "popova@collegelms.ru",
            "volkov@collegelms.ru",
            "morozova@collegelms.ru",
            "novikov@collegelms.ru",
            "zaytseva@collegelms.ru",
        };

        var departments = new[]
        {
            "Информационных технологий",
            "Математики и естественных наук",
            "Иностранных языков",
            "Информационных технологий",
            "Физической культуры",
            "Экономики и права",
            "Информационных технологий",
            "Математики и естественных наук",
            "Гуманитарных дисциплин",
            "Иностранных языков",
        };

        var positions = new[]
        {
            "Преподаватель высшей категории",
            "Старший преподаватель",
            "Преподаватель",
            "Преподаватель",
            "Старший преподаватель",
            "Преподаватель",
            "Преподаватель высшей категории",
            "Старший преподаватель",
            "Преподаватель",
            "Преподаватель",
        };

        for (int i = 0; i < teacherEmails.Length; i++)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == teacherEmails[i]);
            if (user == null)
                continue;

            var existing = await db.Teachers.AnyAsync(t => t.UserId == user.Id);
            if (existing)
                continue;

            db.Teachers.Add(
                new Teacher
                {
                    Id = Guid.Parse($"b2000000-0000-0000-0000-00000000000{i + 1:x}"),
                    UserId = user.Id,
                    Department = departments[i],
                    Position = positions[i],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentsAsync(AppDbContext db)
    {
        if (await db.Students.AnyAsync())
            return;

        var groupNames = new[] { "ИСП-31", "ИСП-32", "ИСП-41", "ИСП-11" };
        var groups = await db.Groups.Where(g => groupNames.Contains(g.Name)).ToListAsync();
        var groupMap = groups.ToDictionary(g => g.Name);

        var studentUsers = await db
            .Users.Where(u => u.Role == UserRole.Student)
            .OrderBy(u => u.Email)
            .ToListAsync();

        var studentRecords = new List<Student>
        {
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000001"),
                UserId = studentUsers[0].Id,
                GroupId = groupMap["ИСП-31"].Id,
                RecordBookNumber = "ЗК-2024-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000002"),
                UserId = studentUsers[1].Id,
                GroupId = groupMap["ИСП-31"].Id,
                RecordBookNumber = "ЗК-2024-002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000003"),
                UserId = studentUsers[2].Id,
                GroupId = groupMap["ИСП-31"].Id,
                RecordBookNumber = "ЗК-2024-003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000004"),
                UserId = studentUsers[3].Id,
                GroupId = groupMap["ИСП-31"].Id,
                RecordBookNumber = "ЗК-2024-004",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000005"),
                UserId = studentUsers[4].Id,
                GroupId = groupMap["ИСП-32"].Id,
                RecordBookNumber = "ЗК-2024-005",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000006"),
                UserId = studentUsers[5].Id,
                GroupId = groupMap["ИСП-32"].Id,
                RecordBookNumber = "ЗК-2024-006",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000007"),
                UserId = studentUsers[6].Id,
                GroupId = groupMap["ИСП-32"].Id,
                RecordBookNumber = "ЗК-2024-007",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000008"),
                UserId = studentUsers[7].Id,
                GroupId = groupMap["ИСП-32"].Id,
                RecordBookNumber = "ЗК-2024-008",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000009"),
                UserId = studentUsers[8].Id,
                GroupId = groupMap["ИСП-41"].Id,
                RecordBookNumber = "ЗК-2023-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000010"),
                UserId = studentUsers[9].Id,
                GroupId = groupMap["ИСП-41"].Id,
                RecordBookNumber = "ЗК-2023-002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000011"),
                UserId = studentUsers[10].Id,
                GroupId = groupMap["ИСП-41"].Id,
                RecordBookNumber = "ЗК-2023-003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000012"),
                UserId = studentUsers[11].Id,
                GroupId = groupMap["ИСП-41"].Id,
                RecordBookNumber = "ЗК-2023-004",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        var petrov = await db.Users.FirstAsync(u => u.Email == "student@collegelms.ru");
        studentRecords.Add(
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000013"),
                UserId = petrov.Id,
                GroupId = groupMap["ИСП-11"].Id,
                RecordBookNumber = "ЗК-2025-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );

        db.Students.AddRange(studentRecords);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCoursesAsync(AppDbContext db)
    {
        if (await db.Courses.CountAsync() >= 10)
            return;

        var allTeachers = await db.Teachers.Include(t => t.User).ToListAsync();
        var allGroups = await db.Groups.ToListAsync();
        var teacherMap = allTeachers.ToDictionary(t => t.User!.Email);
        var groupMap = allGroups.ToDictionary(g => g.Name);

        var courses = new List<Course>
        {
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                Title = "Основы программирования",
                Description =
                    "Базовый курс по программированию на C#: типы данных, управляющие конструкции, ООП",
                TeacherId = teacherMap["teacher@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-31"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000002"),
                Title = "Математика",
                Description =
                    "Высшая математика для IT-специальностей: линейная алгебра, матанализ",
                TeacherId = teacherMap["ivanova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-31"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000003"),
                Title = "Базы данных",
                Description = "Проектирование и работа с реляционными базами данных, SQL",
                TeacherId = teacherMap["teacher@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-32"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000004"),
                Title = "Английский язык",
                Description = "Профессиональный английский для IT-специалистов",
                TeacherId = teacherMap["sidorov@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-31"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000005"),
                Title = "Web-разработка",
                Description = "Современная веб-разработка: HTML, CSS, JavaScript, React",
                TeacherId = teacherMap["smirnova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-41"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000006"),
                Title = "Операционные системы",
                Description = "Изучение архитектуры ОС, процессов, памяти, файловых систем",
                TeacherId = teacherMap["ivanova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-41"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000007"),
                Title = "Информатика",
                Description = "Основы информатики для первокурсников",
                TeacherId = teacherMap["smirnova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-11"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000008"),
                Title = "Компьютерные сети",
                Description = "Основы сетевых технологий, OSI, TCP/IP, маршрутизация",
                TeacherId = teacherMap["teacher@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-32"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000009"),
                Title = "Экономика",
                Description = "Основы экономической теории для IT-специалистов",
                TeacherId = teacherMap["popova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-21"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-00000000000a"),
                Title = "История",
                Description = "История России и всеобщая история",
                TeacherId = teacherMap["novikov@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-12"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        foreach (var course in courses)
        {
            if (
                !await db.Courses.AnyAsync(c =>
                    c.Title == course.Title && c.GroupId == course.GroupId
                )
            )
                db.Courses.Add(course);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedLecturesAsync(AppDbContext db)
    {
        if (await db.Lectures.AnyAsync())
            return;

        var course1 = await db.Courses.FirstAsync(c => c.Title == "Основы программирования");
        var course2 = await db.Courses.FirstAsync(c => c.Title == "Математика");
        var course3 = await db.Courses.FirstAsync(c => c.Title == "Базы данных");
        var course4 = await db.Courses.FirstAsync(c => c.Title == "Английский язык");
        var course5 = await db.Courses.FirstAsync(c => c.Title == "Web-разработка");
        var course6 = await db.Courses.FirstAsync(c => c.Title == "Операционные системы");

        db.Lectures.AddRange(
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000001"),
                CourseId = course1.Id,
                Title = "Введение в C#",
                Content =
                    "История языка C#, платформа .NET, установка инструментов. Первая программа: Hello, World!",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000002"),
                CourseId = course1.Id,
                Title = "Типы данных и переменные",
                Content =
                    "Целочисленные, вещественные, строковые, логические типы. Объявление переменных, приведение типов.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000003"),
                CourseId = course1.Id,
                Title = "Управляющие конструкции",
                Content =
                    "Условные операторы if/switch, циклы for/while/foreach, операторы break/continue.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000004"),
                CourseId = course1.Id,
                Title = "Массивы и коллекции",
                Content = "Одномерные и многомерные массивы. List, Dictionary, Queue, Stack.",
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000005"),
                CourseId = course1.Id,
                Title = "Объектно-ориентированное программирование",
                Content =
                    "Классы, объекты, наследование, полиморфизм, инкапсуляция. Интерфейсы и абстрактные классы.",
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000006"),
                CourseId = course2.Id,
                Title = "Линейная алгебра: матрицы",
                Content =
                    "Определение матриц, операции над матрицами, определитель, обратная матрица.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000007"),
                CourseId = course2.Id,
                Title = "Пределы и непрерывность",
                Content =
                    "Предел функции, свойства пределов, непрерывность функции в точке и на отрезке.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000008"),
                CourseId = course2.Id,
                Title = "Производные функции",
                Content =
                    "Определение производной, правила дифференцирования, производные сложных функций.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000009"),
                CourseId = course3.Id,
                Title = "Введение в базы данных",
                Content =
                    "Понятие БД, СУБД, реляционная модель, нормализация. Знакомство с PostgreSQL.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000010"),
                CourseId = course3.Id,
                Title = "SQL: SELECT и JOIN",
                Content =
                    "Основы SELECT, WHERE, GROUP BY, HAVING. INNER/LEFT/RIGHT JOIN, подзапросы.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000011"),
                CourseId = course3.Id,
                Title = "Индексы и оптимизация",
                Content =
                    "Типы индексов, план запроса, анализ производительности, EXPLAIN ANALYZE.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000012"),
                CourseId = course4.Id,
                Title = "IT Vocabulary",
                Content =
                    "Basic IT terminology: hardware, software, networking, programming languages.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000013"),
                CourseId = course4.Id,
                Title = "Technical Writing",
                Content =
                    "Writing documentation, emails, reports in English. Grammar and style for technical communication.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000014"),
                CourseId = course5.Id,
                Title = "HTML и CSS",
                Content =
                    "Семантическая вёрстка, Flexbox, Grid, адаптивный дизайн, CSS-переменные.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000015"),
                CourseId = course5.Id,
                Title = "JavaScript основы",
                Content = "Переменные, функции, замыкания, прототипы, async/await, работа с DOM.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000016"),
                CourseId = course5.Id,
                Title = "React: компоненты и состояние",
                Content =
                    "JSX, функциональные компоненты, хуки (useState, useEffect, useContext), роутинг.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000017"),
                CourseId = course6.Id,
                Title = "Архитектура ОС",
                Content =
                    "Ядро и модули, режимы ядра/пользователя, системные вызовы, микроядерная архитектура.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000018"),
                CourseId = course6.Id,
                Title = "Управление процессами",
                Content = "Процессы и потоки, планирование, синхронизация, взаимные блокировки.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedAssignmentsAsync(AppDbContext db)
    {
        if (await db.Assignments.AnyAsync())
            return;

        var course1 = await db.Courses.FirstAsync(c => c.Title == "Основы программирования");
        var course2 = await db.Courses.FirstAsync(c => c.Title == "Математика");
        var course3 = await db.Courses.FirstAsync(c => c.Title == "Базы данных");
        var course4 = await db.Courses.FirstAsync(c => c.Title == "Английский язык");
        var course5 = await db.Courses.FirstAsync(c => c.Title == "Web-разработка");

        db.Assignments.AddRange(
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000001"),
                CourseId = course1.Id,
                Title = "Hello, World!",
                Description =
                    "Написать программу, выводящую 'Hello, World!' в консоль. Закрепить навыки работы с IDE.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 10,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000002"),
                CourseId = course1.Id,
                Title = "Калькулятор",
                Description =
                    "Реализовать консольный калькулятор с поддержкой базовых операций (+, -, *, /).",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000003"),
                CourseId = course1.Id,
                Title = "Учёт студентов",
                Description =
                    "Разработать консольное приложение для учёта студентов с возможностью добавления, удаления и поиска.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 30,
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000004"),
                CourseId = course2.Id,
                Title = "Операции с матрицами",
                Description =
                    "Реализовать операции сложения, умножения и нахождения определителя матриц 3x3.",
                DueDate = DateTime.UtcNow.AddDays(10),
                MaxScore = 15,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000005"),
                CourseId = course2.Id,
                Title = "Производные функций",
                Description = "Решить 20 задач на нахождение производных различных функций.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000006"),
                CourseId = course3.Id,
                Title = "Проектирование БД",
                Description =
                    "Спроектировать схему БД для интернет-магазина. Выделить сущности, связи, нормализовать до 3НФ.",
                DueDate = DateTime.UtcNow.AddDays(10),
                MaxScore = 25,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000007"),
                CourseId = course3.Id,
                Title = "SQL запросы",
                Description =
                    "Написать 15 SQL запросов различной сложности к схеме интернет-магазина.",
                DueDate = DateTime.UtcNow.AddDays(17),
                MaxScore = 25,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000008"),
                CourseId = course4.Id,
                Title = "IT Vocabulary Test",
                Description =
                    "Составить глоссарий из 50 IT-терминов на английском с переводом и примерами.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 15,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000009"),
                CourseId = course5.Id,
                Title = "Вёрстка landing page",
                Description =
                    "Сверстать landing page по макету с использованием HTML, CSS (Flexbox/Grid), адаптивная вёрстка.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 30,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000010"),
                CourseId = course5.Id,
                Title = "ToDo App на React",
                Description =
                    "Реализовать ToDo-приложение на React с добавлением, удалением, фильтрацией задач.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 40,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedScheduleEntriesAsync(AppDbContext db)
    {
        if (await db.ScheduleEntries.AnyAsync())
            return;

        var group31 = await db.Groups.FirstAsync(g => g.Name == "ИСП-31");
        var group32 = await db.Groups.FirstAsync(g => g.Name == "ИСП-32");
        var group41 = await db.Groups.FirstAsync(g => g.Name == "ИСП-41");
        var group11 = await db.Groups.FirstAsync(g => g.Name == "ИСП-11");

        var teacher1 = await db.Teachers.FirstAsync(t => t.User!.Email == "teacher@collegelms.ru");
        var teacher2 = await db.Teachers.FirstAsync(t => t.User!.Email == "ivanova@collegelms.ru");
        var teacher3 = await db.Teachers.FirstAsync(t => t.User!.Email == "sidorov@collegelms.ru");
        var teacher4 = await db.Teachers.FirstAsync(t => t.User!.Email == "smirnova@collegelms.ru");
        var teacher5 = await db.Teachers.FirstAsync(t => t.User!.Email == "kozlov@collegelms.ru");

        var entries = new List<ScheduleEntry>
        {
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000001"),
                GroupId = group31.Id,
                TeacherId = teacher1.Id,
                Subject = "Основы программирования",
                Room = "301",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000002"),
                GroupId = group31.Id,
                TeacherId = teacher1.Id,
                Subject = "Основы программирования",
                Room = "302",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000003"),
                GroupId = group31.Id,
                TeacherId = teacher5.Id,
                Subject = "Физическая культура",
                Room = "Спортзал",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(12, 0, 0),
                EndTime = new TimeSpan(13, 30, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000004"),
                GroupId = group31.Id,
                TeacherId = teacher2.Id,
                Subject = "Математика",
                Room = "205",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000005"),
                GroupId = group31.Id,
                TeacherId = teacher2.Id,
                Subject = "Математика",
                Room = "205",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000006"),
                GroupId = group31.Id,
                TeacherId = teacher3.Id,
                Subject = "Английский язык",
                Room = "410",
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000007"),
                GroupId = group31.Id,
                TeacherId = teacher1.Id,
                Subject = "Основы программирования",
                Room = "301",
                DayOfWeek = DayOfWeek.Thursday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000008"),
                GroupId = group32.Id,
                TeacherId = teacher1.Id,
                Subject = "Базы данных",
                Room = "310",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000009"),
                GroupId = group32.Id,
                TeacherId = teacher1.Id,
                Subject = "Базы данных",
                Room = "310",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(12, 0, 0),
                EndTime = new TimeSpan(13, 30, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000010"),
                GroupId = group32.Id,
                TeacherId = teacher4.Id,
                Subject = "Компьютерные сети",
                Room = "315",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000011"),
                GroupId = group32.Id,
                TeacherId = teacher4.Id,
                Subject = "Компьютерные сети",
                Room = "315",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000012"),
                GroupId = group32.Id,
                TeacherId = teacher5.Id,
                Subject = "Физическая культура",
                Room = "Спортзал",
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000013"),
                GroupId = group41.Id,
                TeacherId = teacher4.Id,
                Subject = "Web-разработка",
                Room = "320",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000014"),
                GroupId = group41.Id,
                TeacherId = teacher4.Id,
                Subject = "Web-разработка",
                Room = "320",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000015"),
                GroupId = group41.Id,
                TeacherId = teacher2.Id,
                Subject = "Операционные системы",
                Room = "305",
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000016"),
                GroupId = group41.Id,
                TeacherId = teacher2.Id,
                Subject = "Операционные системы",
                Room = "305",
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000017"),
                GroupId = group11.Id,
                TeacherId = teacher4.Id,
                Subject = "Информатика",
                Room = "101",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Lecture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000018"),
                GroupId = group11.Id,
                TeacherId = teacher4.Id,
                Subject = "Информатика",
                Room = "101",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Lab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000019"),
                GroupId = group11.Id,
                TeacherId = teacher5.Id,
                Subject = "Физическая культура",
                Room = "Спортзал",
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(10, 15, 0),
                EndTime = new TimeSpan(11, 45, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f1000000-0000-0000-0000-000000000020"),
                GroupId = group11.Id,
                TeacherId = teacher3.Id,
                Subject = "Английский язык",
                Room = "110",
                DayOfWeek = DayOfWeek.Thursday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                LessonType = LessonType.Practice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        db.ScheduleEntries.AddRange(entries);
        await db.SaveChangesAsync();
    }

    private static async Task SeedNewsCategoriesAsync(AppDbContext db)
    {
        if (await db.NewsCategories.CountAsync() >= 10)
            return;

        var categories = new List<NewsCategory>
        {
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000001"),
                Name = "Объявления",
                Slug = "announcements",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000002"),
                Name = "Мероприятия",
                Slug = "events",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000003"),
                Name = "Достижения",
                Slug = "achievements",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000004"),
                Name = "Важная информация",
                Slug = "important",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000005"),
                Name = "Спорт",
                Slug = "sport",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000006"),
                Name = "Культура",
                Slug = "culture",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000007"),
                Name = "Наука",
                Slug = "science",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000008"),
                Name = "Вакансии",
                Slug = "jobs",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-000000000009"),
                Name = "Студсовет",
                Slug = "student-council",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f2000000-0000-0000-0000-00000000000a"),
                Name = "Библиотека",
                Slug = "library",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        foreach (var cat in categories)
        {
            if (!await db.NewsCategories.AnyAsync(c => c.Slug == cat.Slug))
                db.NewsCategories.Add(cat);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedNewsAsync(AppDbContext db)
    {
        if (await db.News.CountAsync() >= 10)
            return;

        var admin = await db.Users.FirstAsync(u => u.Email == "admin@collegelms.ru");

        var newsList = new List<News>
        {
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000001"),
                Title = "Начало учебного года 2026-2027",
                Slug = "nachalo-uchebnogo-goda-2026-2027",
                Content =
                    "Уважаемые студенты и преподаватели! Поздравляем вас с началом нового учебного года. "
                    + "Торжественная линейка состоится 1 сентября в 10:00 в актовом зале. Расписание занятий "
                    + "будет опубликовано на сайте до 28 августа.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-10),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000002"),
                Title = "День открытых дверей",
                Slug = "den-otkrytyh-dverej",
                Content =
                    "Приглашаем абитуриентов и их родителей на День открытых дверей. "
                    + "В программе: презентация специальностей, экскурсия по колледжу, мастер-классы. "
                    + "Ждём вас 15 октября в 11:00 в главном корпусе.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-5),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000003"),
                Title = "Победа в хакатоне «Цифровой прорыв»",
                Slug = "pobeda-v-hakatone-cifrovoj-proryv",
                Content =
                    "Студенты группы ИСП-41 заняли I место в региональном хакатоне «Цифровой прорыв»! "
                    + "Команда разработала сервис для мониторинга экологической обстановки. Поздравляем!",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000004"),
                Title = "Изменение в расписании",
                Slug = "izmenenie-v-raspisanii",
                Content =
                    "Уважаемые студенты! Обратите внимание на изменения в расписании на следующую неделю. "
                    + "Занятия по математике в группе ИСП-31 переносятся со вторника на среду. "
                    + "Актуальное расписание доступно в разделе «Расписание».",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000005"),
                Title = "Конкурс «Лучший студент года»",
                Slug = "konkurs-luchshij-student-goda",
                Content =
                    "Объявляется приём заявок на ежегодный конкурс «Лучший студент года». "
                    + "К участию приглашаются студенты всех курсов. Заявки принимаются до 1 ноября. "
                    + "Подробности в студенческом отделе (кабинет 201).",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000006"),
                Title = "Спартакиада колледжа 2026",
                Slug = "spartakiada-kolledzha-2026",
                Content =
                    "С 20 по 30 ноября пройдёт ежегодная спартакиада колледжа. "
                    + "Соревнования по футболу, волейболу, баскетболу и лёгкой атлетике. "
                    + "Приглашаются все желающие. Заявки от групп принимаются в спортклубе до 15 ноября.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(1),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000007"),
                Title = "Вечер поэзии «Осенний ритм»",
                Slug = "vecher-poezii-osenij-ritm",
                Content =
                    "Литературный клуб приглашает на вечер поэзии «Осенний ритм». "
                    + "В программе: чтение стихов классиков и современных авторов, музыкальные номера. "
                    + "Мероприятие пройдёт 25 октября в актовом зале. Начало в 17:00.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(2),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000008"),
                Title = "Научно-практическая конференция",
                Slug = "nauchno-prakticheskaya-konferenciya",
                Content =
                    "Приглашаем студентов принять участие в ежегодной научно-практической конференции. "
                    + "Тематика: информационные технологии, математика, экономика. "
                    + "Приём тезисов до 1 декабря. Лучшие работы будут опубликованы в сборнике.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(3),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-000000000009"),
                Title = "Стажировка в IT-компаниях",
                Slug = "stazhirovka-v-it-kompaniyah",
                Content =
                    "Центр карьеры приглашает студентов 3-4 курсов на стажировку в IT-компании города. "
                    + "Доступны направления: разработка, тестирование, DevOps, аналитика. "
                    + "Подробности и регистрация в кабинете 305 до 10 декабря.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(4),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("f3000000-0000-0000-0000-00000000000a"),
                Title = "Работа студенческого совета в новом семестре",
                Slug = "rabota-studencheskogo-soveta",
                Content =
                    "Состоялось первое заседание студенческого совета в новом семестре. "
                    + "Избраны председатели комитетов, утверждён план мероприятий на семестр. "
                    + "Следующее собрание — 15 ноября в 15:00 в кабинете 410.",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(5),
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        // Assign categories
        var catAnnouncements = await db.NewsCategories.FirstOrDefaultAsync(c =>
            c.Slug == "announcements"
        );
        var catEvents = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "events");
        var catAchievements = await db.NewsCategories.FirstOrDefaultAsync(c =>
            c.Slug == "achievements"
        );
        var catImportant = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "important");
        var catSport = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "sport");
        var catCulture = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "culture");
        var catScience = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "science");
        var catJobs = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "jobs");
        var catStudentCouncil = await db.NewsCategories.FirstOrDefaultAsync(c =>
            c.Slug == "student-council"
        );

        newsList[0].CategoryId = catImportant?.Id;
        newsList[1].CategoryId = catEvents?.Id;
        newsList[2].CategoryId = catAchievements?.Id;
        newsList[3].CategoryId = catAnnouncements?.Id;
        newsList[4].CategoryId = catEvents?.Id;
        newsList[5].CategoryId = catSport?.Id;
        newsList[6].CategoryId = catCulture?.Id;
        newsList[7].CategoryId = catScience?.Id;
        newsList[8].CategoryId = catJobs?.Id;
        newsList[9].CategoryId = catStudentCouncil?.Id;

        foreach (var news in newsList)
        {
            if (!await db.News.AnyAsync(n => n.Slug == news.Slug))
                db.News.Add(news);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedAssignmentSubmissionsAsync(AppDbContext db)
    {
        if (await db.AssignmentSubmissions.AnyAsync())
            return;

        var assignments = await db.Assignments.OrderBy(a => a.Title).Take(5).ToListAsync();
        var students = await db.Students.OrderBy(s => s.RecordBookNumber).Take(10).ToListAsync();

        var submissions = new List<AssignmentSubmission>();
        for (int i = 0; i < 10; i++)
        {
            var assignment = assignments[i % assignments.Count];
            var student = students[i % students.Count];
            int? score = i % 3 == 0 ? null : Random.Shared.Next(5, assignment.MaxScore + 1);

            submissions.Add(
                new AssignmentSubmission
                {
                    Id = Guid.Parse($"e2000000-0000-0000-0000-00000000000{i + 1:x}"),
                    AssignmentId = assignment.Id,
                    StudentId = student.Id,
                    FilePath =
                        $"/uploads/assignments/{assignment.Id}/{student.Id}/submission_{i + 1}.pdf",
                    Comment =
                        score == null
                            ? null
                            : $"Работа сдана вовремя. Оценка: {score}/{assignment.MaxScore}",
                    Score = score,
                    SubmittedAt = DateTime.UtcNow.AddDays(-7 + i),
                    CreatedAt = DateTime.UtcNow.AddDays(-7 + i),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7 + i),
                }
            );
        }

        db.AssignmentSubmissions.AddRange(submissions);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCourseMaterialsAsync(AppDbContext db)
    {
        if (await db.CourseMaterials.AnyAsync())
            return;

        var courses = await db.Courses.OrderBy(c => c.Title).ToListAsync();
        if (courses.Count < 5)
            return;

        var materials = new List<CourseMaterial>
        {
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000001"),
                CourseId = courses[0].Id,
                FileName = "Лекция 1. Введение в C#.pdf",
                FilePath = "/uploads/materials/csharp/lecture1.pdf",
                FileSize = 2_456_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000002"),
                CourseId = courses[0].Id,
                FileName = "Лекция 2. Типы данных.pptx",
                FilePath = "/uploads/materials/csharp/lecture2.pptx",
                FileSize = 3_120_000,
                MimeType =
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000003"),
                CourseId = courses[0].Id,
                FileName = "Практическое задание 1.docx",
                FilePath = "/uploads/materials/csharp/practice1.docx",
                FileSize = 890_000,
                MimeType =
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000004"),
                CourseId = courses[1].Id,
                FileName = "Матрицы. Теория и примеры.pdf",
                FilePath = "/uploads/materials/math/matrices.pdf",
                FileSize = 1_890_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000005"),
                CourseId = courses[1].Id,
                FileName = "Задачи по производным.pdf",
                FilePath = "/uploads/materials/math/derivatives.pdf",
                FileSize = 2_100_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000006"),
                CourseId = courses[2].Id,
                FileName = "SQL. Базовые запросы.pdf",
                FilePath = "/uploads/materials/db/sql-basics.pdf",
                FileSize = 1_560_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000007"),
                CourseId = courses[2].Id,
                FileName = "Схема интернет-магазина.png",
                FilePath = "/uploads/materials/db/shop-schema.png",
                FileSize = 450_000,
                MimeType = "image/png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000008"),
                CourseId = courses[3].Id,
                FileName = "IT Vocabulary. Список терминов.pdf",
                FilePath = "/uploads/materials/english/vocabulary.pdf",
                FileSize = 780_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-000000000009"),
                CourseId = courses[4].Id,
                FileName = "Макет landing page.fig",
                FilePath = "/uploads/materials/web/landing.fig",
                FileSize = 5_200_000,
                MimeType = "application/octet-stream",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c2000000-0000-0000-0000-00000000000a"),
                CourseId = courses[4].Id,
                FileName = "React. Шпаргалка.pdf",
                FilePath = "/uploads/materials/web/react-cheatsheet.pdf",
                FileSize = 1_340_000,
                MimeType = "application/pdf",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        db.CourseMaterials.AddRange(materials);
        await db.SaveChangesAsync();
    }

    private static async Task SeedFeedbacksAsync(AppDbContext db)
    {
        if (await db.Feedbacks.AnyAsync())
            return;

        var feedbacks = new List<Feedback>
        {
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000001"),
                Name = "Иванов Иван",
                Email = "ivanov@example.com",
                Message =
                    "Отличный сайт! Очень удобно смотреть расписание онлайн. Спасибо разработчикам.",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000002"),
                Name = "Петрова Анна",
                Email = "petrova@example.com",
                Message =
                    "Хотелось бы видеть больше информации о предстоящих мероприятиях. А так всё нравится.",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000003"),
                Name = "Сидоров Пётр",
                Email = "sidorov@example.com",
                Message = "Не работает поиск по новостям. Когда исправите?",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000004"),
                Name = "Кузнецова Елена",
                Email = "kuznetsova@example.com",
                Message =
                    "Очень удобно, что можно смотреть оценки в личном кабинете. Ребёнок доволен.",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000005"),
                Name = "Михайлов Дмитрий",
                Email = "mikhailov@example.com",
                Message = "Предлагаю добавить тёмную тему. Глаза устают от белого фона.",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000006"),
                Name = "Алексеева София",
                Email = "alekseeva@example.com",
                Message =
                    "Спасибо за возможность быстрой связи с преподавателем через платформу. Очень удобно!",
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000007"),
                Name = "Григорьев Максим",
                Email = "grigoriev@example.com",
                Message =
                    "Когда появится мобильное приложение? Было бы удобно смотреть расписание с телефона.",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000008"),
                Name = "Белова Татьяна",
                Email = "belova@example.com",
                Message =
                    "Отличный портал! Всё интуитивно понятно. Особенно нравится раздел с новостями.",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-000000000009"),
                Name = "Волков Андрей",
                Email = "volkov@example.com",
                Message =
                    "Не хватает календаря событий. Было бы удобно видеть все мероприятия в календаре.",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new()
            {
                Id = Guid.Parse("f4000000-0000-0000-0000-00000000000a"),
                Name = "Зайцева Мария",
                Email = "zaytseva@example.com",
                Message = "Всё работает отлично! Спасибо за оперативное обновление расписания.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        db.Feedbacks.AddRange(feedbacks);
        await db.SaveChangesAsync();
    }
}
