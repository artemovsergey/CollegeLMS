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
        await SeedTestsAsync(db);
        await SeedTestQuestionsAsync(db);
        await SeedCourseGroupsAsync(db);
        await LinkTestsToLecturesAsync(db);
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
        if (await db.Courses.CountAsync() >= 14)
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
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-00000000000b"),
                Title = "Системное программирование",
                Description =
                    "МДК 01.04 — основы .NET и C#, ООП, многопоточность, обработка файлов, Entity Framework Core, ASP.NET Core, DI, JWT, REST API",
                TeacherId = teacherMap["teacher@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-41"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-00000000000c"),
                Title = "Разработка мобильных приложений",
                Description =
                    "МДК 01.03 — Kotlin, Jetpack Compose, навигация, ViewModel, Material Design, Retrofit, Room, корутины",
                TeacherId = teacherMap["ivanova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-31"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-00000000000d"),
                Title = "Поддержка и тестирование программных модулей",
                Description =
                    "МДК 01.02 — ручное тестирование, xUnit, Moq, Selenium WebDriver, REST API тестирование, интеграционные тесты, CI/CD",
                TeacherId = teacherMap["sidorov@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-31"].Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-00000000000e"),
                Title = "Мобильные приложения (ИБ)",
                Description =
                    "МДК 01.03 ИБ — Kotlin, Jetpack Compose, навигация, ViewModel, Material Design, Retrofit, SQL, Room для специальности ИБ",
                TeacherId = teacherMap["smirnova@collegelms.ru"].Id,
                GroupId = groupMap["ИСП-41"].Id,
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

        // МДК 01.04 — Системное программирование
        var course11 = await db.Courses.FirstAsync(c => c.Title == "Системное программирование");
        var course12 = await db.Courses.FirstAsync(c =>
            c.Title == "Разработка мобильных приложений"
        );
        var course13 = await db.Courses.FirstAsync(c =>
            c.Title == "Поддержка и тестирование программных модулей"
        );
        var course14 = await db.Courses.FirstAsync(c => c.Title == "Мобильные приложения (ИБ)");

        db.Lectures.AddRange(
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000019"),
                CourseId = course11.Id,
                Title = "Основы .NET и C#",
                Content =
                    "Платформа .NET, CLR, компиляция, типы данных, переменные, условия, циклы, массивы, LINQ.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001a"),
                CourseId = course11.Id,
                Title = "Объектно-ориентированное программирование",
                Content =
                    "Классы, объекты, наследование, полиморфизм, инкапсуляция. Интерфейсы и абстрактные классы.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001b"),
                CourseId = course11.Id,
                Title = "Многопоточность и асинхронность",
                Content =
                    "Task, async/await, Parallel.For, PLINQ, синхронизация потоков (lock, Monitor, Mutex).",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001c"),
                CourseId = course11.Id,
                Title = "Обработка файлов",
                Content =
                    "Работа с файлами, текстовые файлы, Word, Excel, PDF. Параллельная и асинхронная обработка данных.",
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001d"),
                CourseId = course11.Id,
                Title = "Entity Framework Core",
                Content =
                    "ORM для C#, CodeFirst и DatabaseFirst, миграции, CRUD-операции, транзакции, паттерн Unit of Work.",
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001e"),
                CourseId = course11.Id,
                Title = "ASP.NET Core",
                Content =
                    "Контроллеры, MinimalAPI, DI, маршрутизация, JWT аутентификация, ролевая авторизация, FluentValidation.",
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 — Разработка мобильных приложений
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000001f"),
                CourseId = course12.Id,
                Title = "Основы Kotlin",
                Content =
                    "Переменные, функции, условия, null-выражения, коллекции, классы и объекты, корутины.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000020"),
                CourseId = course12.Id,
                Title = "Jetpack Compose",
                Content =
                    "Основы композиции, Image, отладчик, списки, сетки, жизненный цикл активности.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000021"),
                CourseId = course12.Id,
                Title = "Состояния и навигация",
                Content = "Состояние в Compose, навигация между экранами, адаптивная навигация.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000022"),
                CourseId = course12.Id,
                Title = "ViewModel и Material Design",
                Content = "Архитектура MVVM, ViewModel, Material Design компоненты, анимации.",
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000023"),
                CourseId = course12.Id,
                Title = "Retrofit и сетевые запросы",
                Content = "Retrofit, паттерн Repository, работа с REST API, загрузка изображений.",
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000024"),
                CourseId = course12.Id,
                Title = "Локальные данные: SQL и Room",
                Content =
                    "SQL основы, Datastore, Room Persistence Library, чтение и сохранение данных.",
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.02 — Тестирование модулей
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000025"),
                CourseId = course13.Id,
                Title = "Основы тестирования ПО",
                Content =
                    "Жизненный цикл ПО, методологии (Waterfall, Agile, Scrum), тест-планы, тест-кейсы, баг-репорты.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000026"),
                CourseId = course13.Id,
                Title = "Тест-дизайн и документирование",
                Content =
                    "Эквивалентное разделение, граничные значения, чек-листы, отчёты по тестированию.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000027"),
                CourseId = course13.Id,
                Title = "Автотесты на xUnit",
                Content =
                    "Фреймворк xUnit, атрибуты [Fact], [Theory], [InlineData], Assert, фикстуры, параметризованные тесты.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000028"),
                CourseId = course13.Id,
                Title = "Изоляция тестов и Moq",
                Content =
                    "Зачем нужны моки, библиотека Moq, создание mock-объектов, настройка поведения, Verify.",
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000029"),
                CourseId = course13.Id,
                Title = "Selenium WebDriver",
                Content =
                    "Автоматизация веб-UI, локаторы, навигация, ожидания, паттерн Page Object Model.",
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002a"),
                CourseId = course13.Id,
                Title = "REST API тестирование",
                Content =
                    "HTTP и REST, JSON, RestSharp, автотесты для GET/POST/PUT/DELETE, десериализация.",
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002b"),
                CourseId = course13.Id,
                Title = "Интеграционное и нагрузочное тестирование",
                Content =
                    "TestContainers, интеграционные тесты, нагрузочное тестирование k6, CI/CD пайплайны.",
                Order = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 ИБ — Мобильные приложения ИБ
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002c"),
                CourseId = course14.Id,
                Title = "Основы Kotlin",
                Content =
                    "Переменные и функции, условия, коллекции, классы и объекты, лямбда-функции, корутины.",
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002d"),
                CourseId = course14.Id,
                Title = "Jetpack Compose и навигация",
                Content = "Основы композиции, 상태, навигация между экранами, жизненный цикл.",
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002e"),
                CourseId = course14.Id,
                Title = "ViewModel и Material Design",
                Content =
                    "Архитектура MVVM, Material Design компоненты, Retrofit, Repository паттерн.",
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-00000000002f"),
                CourseId = course14.Id,
                Title = "Локальные данные: SQL и Room",
                Content = "SQL, Datastore, Room Persistence Library, чтение и сохранение данных.",
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Lecture
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-000000000030"),
                CourseId = course14.Id,
                Title = "Тестирование и отладка мобильных приложений",
                Content =
                    "JUnit, Espresso, отладчик Android Studio, логирование, профилирование производительности.",
                Order = 5,
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

        // МДК 01.04 — Системное программирование
        var course11 = await db.Courses.FirstAsync(c => c.Title == "Системное программирование");
        var course12 = await db.Courses.FirstAsync(c =>
            c.Title == "Разработка мобильных приложений"
        );
        var course13 = await db.Courses.FirstAsync(c =>
            c.Title == "Поддержка и тестирование программных модулей"
        );
        var course14 = await db.Courses.FirstAsync(c => c.Title == "Мобильные приложения (ИБ)");

        db.Assignments.AddRange(
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000011"),
                CourseId = course11.Id,
                Title = "Компиляция и запуск .NET приложения",
                Description =
                    "Настройка среды разработки, компиляция и запуск первого .NET приложения.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 10,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000012"),
                CourseId = course11.Id,
                Title = "Работа с типами данных и LINQ",
                Description = "Преобразования типов, работа со строками, коллекции, LINQ-запросы.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000013"),
                CourseId = course11.Id,
                Title = "Создание классов и интерфейсов",
                Description =
                    "Реализация наследования, виртуальных методов, интерфейсов и абстрактных классов.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 25,
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000014"),
                CourseId = course11.Id,
                Title = "Многопоточность и Parallel",
                Description =
                    "Создание потоков, синхронизация, Parallel.For, PLINQ, разрешение гонки данных.",
                DueDate = DateTime.UtcNow.AddDays(28),
                MaxScore = 25,
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000015"),
                CourseId = course11.Id,
                Title = "CRUD с Entity Framework Core",
                Description =
                    "Реализация CRUD-операций, миграции, паттерн Repository и Unit of Work.",
                DueDate = DateTime.UtcNow.AddDays(35),
                MaxScore = 30,
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000016"),
                CourseId = course11.Id,
                Title = "REST API с JWT авторизацией",
                Description =
                    "ASP.NET Core контроллеры, JWT токены, ролевая авторизация, FluentValidation.",
                DueDate = DateTime.UtcNow.AddDays(42),
                MaxScore = 30,
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 — Разработка мобильных приложений
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000017"),
                CourseId = course12.Id,
                Title = "Основы Kotlin",
                Description =
                    "Переменные, функции, условия, циклы, коллекции, классы и объекты на Kotlin.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 15,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000018"),
                CourseId = course12.Id,
                Title = "Интерактивные приложения на Compose",
                Description =
                    "Dice Roller, обработка нажатий, списки с прокруткой, установка логотипа.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000019"),
                CourseId = course12.Id,
                Title = "Калькулятор чаевых и навигация",
                Description =
                    "Приложение с состоянием, навигация между экранами, адаптивный макет.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 25,
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001a"),
                CourseId = course12.Id,
                Title = "Список героев на Material Design",
                Description = "Применение Material Design компонентов, ViewModel, анимации.",
                DueDate = DateTime.UtcNow.AddDays(28),
                MaxScore = 25,
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001b"),
                CourseId = course12.Id,
                Title = "Загрузка данных из API",
                Description =
                    "Retrofit, Repository паттерн, загрузка и отображение изображений из интернета.",
                DueDate = DateTime.UtcNow.AddDays(35),
                MaxScore = 25,
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001c"),
                CourseId = course12.Id,
                Title = "Расписание автобусов на Room",
                Description =
                    "Разработка приложения с локальным хранилищем Room, чтение и обновление данных.",
                DueDate = DateTime.UtcNow.AddDays(42),
                MaxScore = 30,
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.02 — Тестирование модулей
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001d"),
                CourseId = course13.Id,
                Title = "Анализ требований и тест-кейсы",
                Description =
                    "Анализ требований к приложению, составление тест-кейсов и баг-репортов.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 15,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001e"),
                CourseId = course13.Id,
                Title = "Юнит-тесты на xUnit",
                Description =
                    "Написание параметризованных тестов, тестирование исключений, работа с фикстурами.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-00000000001f"),
                CourseId = course13.Id,
                Title = "Тесты с Moq и изоляцией",
                Description =
                    "Изоляция зависимостей с помощью Moq, настройка mock-объектов, проверка вызовов.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 20,
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000020"),
                CourseId = course13.Id,
                Title = "UI-тесты на Selenium",
                Description =
                    "Автоматизация сценария входа, паттерн Page Object Model, кросс-браузерное тестирование.",
                DueDate = DateTime.UtcNow.AddDays(28),
                MaxScore = 25,
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000021"),
                CourseId = course13.Id,
                Title = "API-тесты на RestSharp",
                Description =
                    "Автотесты для GET/POST/PUT/DELETE запросов, десериализация JSON, анализ API через Postman.",
                DueDate = DateTime.UtcNow.AddDays(35),
                MaxScore = 25,
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000022"),
                CourseId = course13.Id,
                Title = "Интеграционные тесты и CI/CD",
                Description =
                    "TestContainers, Allure-отчёт, настройка GitHub Actions pipeline для тестов.",
                DueDate = DateTime.UtcNow.AddDays(42),
                MaxScore = 30,
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 ИБ — Мобильные приложения ИБ
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000023"),
                CourseId = course14.Id,
                Title = "Основы Kotlin и корутины",
                Description =
                    "Переменные, функции, коллекции, классы, лямбда-функции, корутины на Kotlin.",
                DueDate = DateTime.UtcNow.AddDays(7),
                MaxScore = 15,
                Order = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000024"),
                CourseId = course14.Id,
                Title = "Интерактивные приложения на Compose",
                Description =
                    "Основы композиции, Dice Roller, обработка нажатий, списки с прокруткой.",
                DueDate = DateTime.UtcNow.AddDays(14),
                MaxScore = 20,
                Order = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000025"),
                CourseId = course14.Id,
                Title = "Навигация и адаптивный макет",
                Description =
                    "Навигация между экранами, корутины в JC, создание адаптивного макета.",
                DueDate = DateTime.UtcNow.AddDays(21),
                MaxScore = 25,
                Order = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000026"),
                CourseId = course14.Id,
                Title = "Список героев на Material Design",
                Description =
                    "ViewModel, Material Design компоненты, разработка приложения Список героев.",
                DueDate = DateTime.UtcNow.AddDays(28),
                MaxScore = 25,
                Order = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000027"),
                CourseId = course14.Id,
                Title = "Загрузка данных из API",
                Description =
                    "Retrofit, Repository паттерн, загрузка и отображение изображений из интернета.",
                DueDate = DateTime.UtcNow.AddDays(35),
                MaxScore = 25,
                Order = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Assignment
            {
                Id = Guid.Parse("e1000000-0000-0000-0000-000000000028"),
                CourseId = course14.Id,
                Title = "Расписание автобусов на Room",
                Description =
                    "SQL, Datastore, Room Persistence Library, приложение Расписание автобусов.",
                DueDate = DateTime.UtcNow.AddDays(42),
                MaxScore = 30,
                Order = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedTestsAsync(AppDbContext db)
    {
        if (await db.Tests.AnyAsync())
            return;

        var course11 = await db.Courses.FirstAsync(c => c.Title == "Системное программирование");
        var course12 = await db.Courses.FirstAsync(c =>
            c.Title == "Разработка мобильных приложений"
        );
        var course13 = await db.Courses.FirstAsync(c =>
            c.Title == "Поддержка и тестирование программных модулей"
        );
        var course14 = await db.Courses.FirstAsync(c => c.Title == "Мобильные приложения (ИБ)");

        db.Tests.AddRange(
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"),
                CourseId = course11.Id,
                Title = "Основы C# и .NET",
                Description = "Проверка знаний основ синтаксиса C#, типов данных, ООП",
                TimeLimitMinutes = 30,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"),
                CourseId = course11.Id,
                Title = "Многопоточность и файлы",
                Description = "Проверка знаний по многопоточности, async/await, обработке файлов",
                TimeLimitMinutes = 25,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                CourseId = course11.Id,
                Title = "EF Core и ASP.NET",
                Description = "Проверка знаний по Entity Framework Core, ASP.NET Core, DI, JWT",
                TimeLimitMinutes = 30,
                MaxAttempts = 2,
                PassingScore = 70,
                Type = TestType.Control,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 — Мобильные приложения
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                CourseId = course12.Id,
                Title = "Основы Kotlin",
                Description =
                    "Проверка знаний синтаксиса Kotlin: переменные, функции, коллекции, классы",
                TimeLimitMinutes = 25,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"),
                CourseId = course12.Id,
                Title = "Jetpack Compose и навигация",
                Description = "Проверка знаний по Compose, состояниям, навигации, ViewModel",
                TimeLimitMinutes = 30,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.02 — Тестирование
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"),
                CourseId = course13.Id,
                Title = "Основы тестирования",
                Description = "Проверка знаний по тест-дизайну, тест-планам, баг-репортам",
                TimeLimitMinutes = 20,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"),
                CourseId = course13.Id,
                Title = "xUnit и Moq",
                Description = "Проверка знаний по юнит-тестированию, изоляции зависимостей",
                TimeLimitMinutes = 25,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"),
                CourseId = course13.Id,
                Title = "Selenium и REST API",
                Description = "Проверка знаний по UI-автоматизации и тестированию API",
                TimeLimitMinutes = 30,
                MaxAttempts = 2,
                PassingScore = 70,
                Type = TestType.Control,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // МДК 01.03 ИБ — Мобильные приложения ИБ
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"),
                CourseId = course14.Id,
                Title = "Основы Kotlin и Compose",
                Description = "Проверка знаний по Kotlin, Jetpack Compose, навигации",
                TimeLimitMinutes = 25,
                MaxAttempts = 3,
                PassingScore = 60,
                Type = TestType.SelfStudy,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Test
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-00000000000a"),
                CourseId = course14.Id,
                Title = "ViewModel, Retrofit и Room",
                Description =
                    "Проверка знаний по архитектуре MVVM, сетевым запросам и локальным данным",
                TimeLimitMinutes = 30,
                MaxAttempts = 2,
                PassingScore = 70,
                Type = TestType.Control,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedTestQuestionsAsync(AppDbContext db)
    {
        if (await db.TestQuestions.AnyAsync())
            return;

        var test1 = await db.Tests.FirstAsync(t => t.Title == "Основы C# и .NET");
        var test2 = await db.Tests.FirstAsync(t => t.Title == "Многопоточность и файлы");
        var test3 = await db.Tests.FirstAsync(t => t.Title == "EF Core и ASP.NET");
        var test4 = await db.Tests.FirstAsync(t => t.Title == "Основы Kotlin");
        var test5 = await db.Tests.FirstAsync(t => t.Title == "Jetpack Compose и навигация");
        var test6 = await db.Tests.FirstAsync(t => t.Title == "Основы тестирования");
        var test7 = await db.Tests.FirstAsync(t => t.Title == "xUnit и Moq");
        var test8 = await db.Tests.FirstAsync(t => t.Title == "Selenium и REST API");
        var test9 = await db.Tests.FirstAsync(t => t.Title == "Основы Kotlin и Compose");
        var test10 = await db.Tests.FirstAsync(t => t.Title == "ViewModel, Retrofit и Room");

        db.TestQuestions.AddRange(
            // Тест 1: Основы C# и .NET
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"),
                TestId = test1.Id,
                Text =
                    "Какой тип данных используется для хранения десятичных чисел с высокой точностью в C#?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "float", "double", "decimal", "int" }
                ),
                CorrectAnswer = "decimal",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"),
                TestId = test1.Id,
                Text = "Какой модификатор доступа делает член доступным только внутри класса?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "public", "private", "protected", "internal" }
                ),
                CorrectAnswer = "private",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"),
                TestId = test1.Id,
                Text = "Что такое CLR в контексте .NET?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Язык программирования",
                        "Среда выполнения",
                        "Библиотека классов",
                        "Текстовый редактор",
                    }
                ),
                CorrectAnswer = "Среда выполнения",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"),
                TestId = test1.Id,
                Text =
                    "Какой метод LINQ возвращает первый элемент коллекции или значение по умолчанию?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "First()", "FirstOrDefault()", "Single()", "Take(1)" }
                ),
                CorrectAnswer = "FirstOrDefault()",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000005"),
                TestId = test1.Id,
                Text = "Какие принципы ООП вы знаете? (выберите все правильные)",
                Type = QuestionType.MultipleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Инкапсуляция", "Наследование", "Компиляция", "Полиморфизм" }
                ),
                CorrectAnswer = "Инкапсуляция,Наследование,Полиморфизм",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000006"),
                TestId = test1.Id,
                Text = "Что вернёт выражение 5 / 2 в C# при целочисленном делении?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "2.5", "2", "3", "2.0" }
                ),
                CorrectAnswer = "2",
                Points = 10,
                OrderIndex = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000007"),
                TestId = test1.Id,
                Text = "Какой коллекции соответствует интерфейс IDictionary?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "List", "Dictionary", "Queue", "Stack" }
                ),
                CorrectAnswer = "Dictionary",
                Points = 10,
                OrderIndex = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 2: Многопоточность и файлы
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000008"),
                TestId = test2.Id,
                Text = "Какой тип используется для запуска асинхронной операции в C#?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Thread", "Task", "Process", "Mutex" }
                ),
                CorrectAnswer = "Task",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000009"),
                TestId = test2.Id,
                Text = "Какой оператор используется для блокировки доступа к общему ресурсу?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "lock", "await", "using", "var" }
                ),
                CorrectAnswer = "lock",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000a"),
                TestId = test2.Id,
                Text = "Что делает метод Parallel.For?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Запускает итерации параллельно",
                        "Создаёт новый поток",
                        "Блокирует поток",
                        "Очищает память",
                    }
                ),
                CorrectAnswer = "Запускает итерации параллельно",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000b"),
                TestId = test2.Id,
                Text = "Какой класс используется для чтения текстовых файлов в .NET?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "StreamReader", "HttpClient", "MemoryStream", "BinaryWriter" }
                ),
                CorrectAnswer = "StreamReader",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000c"),
                TestId = test2.Id,
                Text = "Что такое гонка данных (race condition)?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Одновременный доступ к ресурсу без синхронизации",
                        "Ошибка компиляции",
                        "Переполнение стека",
                        "Отсутствие подключения к БД",
                    }
                ),
                CorrectAnswer = "Одновременный доступ к ресурсу без синхронизации",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 3: EF Core и ASP.NET
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000d"),
                TestId = test3.Id,
                Text = "Что такое Code First в Entity Framework?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Создание БД из моделей кода",
                        "Работа с существующей БД",
                        "Генерация отчётов",
                        "Управление пакетами",
                    }
                ),
                CorrectAnswer = "Создание БД из моделей кода",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000e"),
                TestId = test3.Id,
                Text =
                    "Какой паттерн инверсии зависимостей используется в ASP.NET Core для внедрения сервисов?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Repository", "Dependency Injection", "Singleton", "Observer" }
                ),
                CorrectAnswer = "Dependency Injection",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000000f"),
                TestId = test3.Id,
                Text = "Что такое JWT?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Формат хранения данных",
                        "Токен аутентификации",
                        "Протокол передачи файлов",
                        "База данных",
                    }
                ),
                CorrectAnswer = "Токен аутентификации",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000010"),
                TestId = test3.Id,
                Text = "Какой метод EF Core выполняет миграцию?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "EnsureCreated", "Migrate", "SaveChanges", "FirstOrDefault" }
                ),
                CorrectAnswer = "Migrate",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000011"),
                TestId = test3.Id,
                Text = "Какой HTTP-метод используется для обновления данных?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "GET", "POST", "PUT", "DELETE" }
                ),
                CorrectAnswer = "PUT",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 4: Основы Kotlin
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000012"),
                TestId = test4.Id,
                Text = "Как объявляется переменная в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "var x = 5", "let x = 5", "dim x = 5", "define x = 5" }
                ),
                CorrectAnswer = "var x = 5",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000013"),
                TestId = test4.Id,
                Text = "Что такое корутины в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Потоки",
                        "Легковесные фоновые операции",
                        "Функции высшего порядка",
                        "Коллекции",
                    }
                ),
                CorrectAnswer = "Легковесные фоновые операции",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000014"),
                TestId = test4.Id,
                Text = "Какой коллекции соответствует аналог List в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Array", "ArrayList", "MutableList", "Vector" }
                ),
                CorrectAnswer = "MutableList",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000015"),
                TestId = test4.Id,
                Text = "Какой оператор нулевой безопасности в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(new[] { "??", "?.", "!", "#" }),
                CorrectAnswer = "?.",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000016"),
                TestId = test4.Id,
                Text = "Что такое data class в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Класс для хранения данных с переопределёнными методами",
                        "Абстрактный класс",
                        "Интерфейс",
                        "Перечисление",
                    }
                ),
                CorrectAnswer = "Класс для хранения данных с переопределёнными методами",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 5: Jetpack Compose и навигация
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000017"),
                TestId = test5.Id,
                Text = "Что такое Composable функция в Jetpack Compose?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Обычная функция",
                        "Функция для описания UI",
                        "Функция для работы с сетью",
                        "Функция для БД",
                    }
                ),
                CorrectAnswer = "Функция для описания UI",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000018"),
                TestId = test5.Id,
                Text = "Какой хук используется для хранения состояния в Compose?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "remember", "useState", "stateOf", "mutableStateOf" }
                ),
                CorrectAnswer = "remember",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000019"),
                TestId = test5.Id,
                Text = "Как работает навигация в Jetpack Compose?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Через Activity",
                        "Через NavController и NavHost",
                        "Через Intent",
                        "Через Fragment",
                    }
                ),
                CorrectAnswer = "Через NavController и NavHost",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001a"),
                TestId = test5.Id,
                Text = "Для чего используется ViewModel в архитектуре MVVM?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Для хранения UI-состояния",
                        "Для работы с сетью",
                        "Для отрисовки��",
                        "Для навигации",
                    }
                ),
                CorrectAnswer = "Для хранения UI-состояния",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001b"),
                TestId = test5.Id,
                Text = "Какой компонент отвечает за Material Design в Compose?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Scaffold", "Column", "Row", "Box" }
                ),
                CorrectAnswer = "Scaffold",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 6: Основы тестирования
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001c"),
                TestId = test6.Id,
                Text = "Что такое тест-кейс?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Документ с описанием теста",
                        "Программный код",
                        "Баг-репорт",
                        "Отчёт о тестировании",
                    }
                ),
                CorrectAnswer = "Документ с описанием теста",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001d"),
                TestId = test6.Id,
                Text = "Какая техника тест-дизайна основана на проверке граничных значений?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Эквивалентное разделение",
                        "Граничные значения",
                        "Парное тестирование",
                        "Статический анализ",
                    }
                ),
                CorrectAnswer = "Граничные значения",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001e"),
                TestId = test6.Id,
                Text = "Что такое баг-репорт?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Документальное описание найденного дефекта",
                        "Автоматический тест",
                        "Код программы",
                        "Отчёт о покрытии",
                    }
                ),
                CorrectAnswer = "Документальное описание найденного дефекта",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000001f"),
                TestId = test6.Id,
                Text = "Какие виды тестирования вы знаете? (выберите все правильные)",
                Type = QuestionType.MultipleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "Юнит-тестирование", "Интеграционное", "Ручное", "Компиляция" }
                ),
                CorrectAnswer = "Юнит-тестирование,Интеграционное,Ручное",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000020"),
                TestId = test6.Id,
                Text = "Что такое жизненный цикл ПО (SDLC)?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Процесс разработки от идеи до поддержки",
                        "Только этап тестирования",
                        "Процесс компиляции",
                        "Управление версиями",
                    }
                ),
                CorrectAnswer = "Процесс разработки от идеи до поддержки",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 7: xUnit и Moq
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000021"),
                TestId = test7.Id,
                Text = "Какой атрибут помечает тестовый метод в xUnit?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "[Test]", "[Fact]", "[TestMethod]", "[TestCase]" }
                ),
                CorrectAnswer = "[Fact]",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000022"),
                TestId = test7.Id,
                Text = "Какой атрибут используется для параметризованных тестов в xUnit?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "[Fact]", "[Theory]", "[InlineData]", "[MemberData]" }
                ),
                CorrectAnswer = "[Theory]",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000023"),
                TestId = test7.Id,
                Text = "Для чего используется библиотека Moq?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Для создания mock-объектов",
                        "Для работы с БД",
                        "Для UI-тестирования",
                        "Для логирования",
                    }
                ),
                CorrectAnswer = "Для создания mock-объектов",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000024"),
                TestId = test7.Id,
                Text = "Что проверяет метод Verify() в Moq?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Значение переменной",
                        "Что метод был вызван",
                        "Результат вычисления",
                        "Подключение к сети",
                    }
                ),
                CorrectAnswer = "Что метод был вызван",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000025"),
                TestId = test7.Id,
                Text = "Как проверяется исключение в xUnit?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Assert.Throws<T>()",
                        "Assert.IsType<T>()",
                        "Assert.Null()",
                        "Assert.Contains()",
                    }
                ),
                CorrectAnswer = "Assert.Throws<T>()",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 8: Selenium и REST API
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000026"),
                TestId = test8.Id,
                Text = "Что такое Page Object Model (POM) в Selenium?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Паттерн для структурирования UI-тестов",
                        "Тип локатора",
                        "Метод поиска элемента",
                        "Фреймворк для API-тестов",
                    }
                ),
                CorrectAnswer = "Паттерн для структурирования UI-тестов",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000027"),
                TestId = test8.Id,
                Text = "Какой локатор считается самым надёжным в Selenium?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "XPath", "ID", "CSS Selector", "Name" }
                ),
                CorrectAnswer = "ID",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000028"),
                TestId = test8.Id,
                Text = "Какой HTTP-метод используется для создания нового ресурса в REST API?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "GET", "POST", "PUT", "DELETE" }
                ),
                CorrectAnswer = "POST",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000029"),
                TestId = test8.Id,
                Text = "Что такое Implicit Wait в Selenium?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Глобальное ожидание появления элемента",
                        "Ожидание конкретного условия",
                        "Пауза между запросами",
                        "Ожидание загрузки страницы",
                    }
                ),
                CorrectAnswer = "Глобальное ожидание появления элемента",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002a"),
                TestId = test8.Id,
                Text = "Какой формат данных используется в REST API для обмена данными?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "XML", "JSON", "CSV", "HTML" }
                ),
                CorrectAnswer = "JSON",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 9: Основы Kotlin и Compose (ИБ)
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002b"),
                TestId = test9.Id,
                Text = "Чем отличается val от var в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "val — изменяемая, var — неизменяемая",
                        "val — неизменяемая, var — изменяемая",
                        "Ничем",
                        "val — приватная, var — публичная",
                    }
                ),
                CorrectAnswer = "val — неизменяемая, var — изменяемая",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002c"),
                TestId = test9.Id,
                Text = "Какой оператор используется для безопасного вызова nullable-типа в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "!!", "?.", "as", "is" }
                ),
                CorrectAnswer = "?.",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002d"),
                TestId = test9.Id,
                Text = "Что делает suspend-функция в Kotlin?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Приостанавливает выполнение корутины",
                        "Останавливает поток",
                        "Удаляет переменную",
                        "Очищает память",
                    }
                ),
                CorrectAnswer = "Приостанавливает выполнение корутины",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002e"),
                TestId = test9.Id,
                Text = "Какой аннотацией помечается Composable функция?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "@Composable", "@Override", "@Inject", "@Test" }
                ),
                CorrectAnswer = "@Composable",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-00000000002f"),
                TestId = test9.Id,
                Text = "Какое хранилище данных предоставляет Room в Android?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "SQLite на устройстве", "Firebase", "SharedPreferences", "XML-файлы" }
                ),
                CorrectAnswer = "SQLite на устройстве",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тест 10: ViewModel, Retrofit и Room (ИБ)
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000030"),
                TestId = test10.Id,
                Text = "Какой класс используется для HTTP-запросов через Retrofit?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "HttpURLConnection", "Retrofit (интерфейс)", "OkHttp", "Volley" }
                ),
                CorrectAnswer = "Retrofit (интерфейс)",
                Points = 10,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000031"),
                TestId = test10.Id,
                Text = "Что такое Repository паттерн в архитектуре Android?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Абстракция над источниками данных",
                        "UI-компонент",
                        "Тип навигации",
                        "Атрибут",
                    }
                ),
                CorrectAnswer = "Абстракция над источниками данных",
                Points = 10,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000032"),
                TestId = test10.Id,
                Text = "Для чего используется @Entity аннотация в Room?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Описывает таблицу БД",
                        "Описывает сетевой запрос",
                        "Описывает UI-компонент",
                        "Описывает тест",
                    }
                ),
                CorrectAnswer = "Описывает таблицу БД",
                Points = 10,
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000033"),
                TestId = test10.Id,
                Text = "Что такое LiveData в ViewModel?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[]
                    {
                        "Observable-обёртка для данных",
                        "База данных",
                        "Фреймворк для тестов",
                        "Интерфейс",
                    }
                ),
                CorrectAnswer = "Observable-обёртка для данных",
                Points = 10,
                OrderIndex = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new TestQuestion
            {
                Id = Guid.Parse("b1000000-0000-0000-0000-000000000034"),
                TestId = test10.Id,
                Text = "Какой компонент связывает UI с ViewModel в Compose?",
                Type = QuestionType.SingleChoice,
                Options = System.Text.Json.JsonSerializer.Serialize(
                    new[] { "remember / collectAsState", "Intent", "Fragment", "Adapter" }
                ),
                CorrectAnswer = "remember / collectAsState",
                Points = 10,
                OrderIndex = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedCourseGroupsAsync(AppDbContext db)
    {
        if (await db.CourseGroups.AnyAsync())
            return;

        var course11 = await db.Courses.FirstAsync(c => c.Title == "Системное программирование");
        var course12 = await db.Courses.FirstAsync(c =>
            c.Title == "Разработка мобильных приложений"
        );
        var course13 = await db.Courses.FirstAsync(c =>
            c.Title == "Поддержка и тестирование программных модулей"
        );
        var course14 = await db.Courses.FirstAsync(c => c.Title == "Мобильные приложения (ИБ)");

        var group41 = await db.Groups.FirstAsync(g => g.Name == "ИСП-41");
        var group42 = await db.Groups.FirstAsync(g => g.Name == "ИСП-42");
        var group31 = await db.Groups.FirstAsync(g => g.Name == "ИСП-31");
        var group32 = await db.Groups.FirstAsync(g => g.Name == "ИСП-32");

        db.CourseGroups.AddRange(
            // Системное программирование → ИСП-41, ИСП-42
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                CourseId = course11.Id,
                GroupId = group41.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000002"),
                CourseId = course11.Id,
                GroupId = group42.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Мобильные приложения ИП → ИСП-31, ИСП-32
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000003"),
                CourseId = course12.Id,
                GroupId = group31.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000004"),
                CourseId = course12.Id,
                GroupId = group32.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Тестирование модулей → ИСП-31, ИСП-32
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000005"),
                CourseId = course13.Id,
                GroupId = group31.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000006"),
                CourseId = course13.Id,
                GroupId = group32.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            // Мобильные приложения ИБ → ИСП-41, ИСП-42
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000007"),
                CourseId = course14.Id,
                GroupId = group41.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new CourseGroup
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000008"),
                CourseId = course14.Id,
                GroupId = group42.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task LinkTestsToLecturesAsync(AppDbContext db)
    {
        var tests = await db.Tests.ToListAsync();
        var testByTitle = tests.ToDictionary(t => t.Title);

        var lecturesWithTests = await db
            .Lectures.Where(l =>
                l.Title == "Entity Framework Core"
                || l.Title == "ASP.NET Core"
                || l.Title == "Основы тестирования ПО"
                || l.Title == "Автотесты на xUnit"
                || l.Title == "Selenium WebDriver"
            )
            .ToListAsync();

        foreach (var lecture in lecturesWithTests)
        {
            Test? test = lecture.Title switch
            {
                "Entity Framework Core" => testByTitle.GetValueOrDefault("EF Core и ASP.NET"),
                "ASP.NET Core" => testByTitle.GetValueOrDefault("EF Core и ASP.NET"),
                "Основы тестирования ПО" => testByTitle.GetValueOrDefault("Основы тестирования"),
                "Автотесты на xUnit" => testByTitle.GetValueOrDefault("xUnit и Moq"),
                "Selenium WebDriver" => testByTitle.GetValueOrDefault("Selenium и REST API"),
                _ => null,
            };

            if (test != null)
            {
                lecture.TestId = test.Id;
            }
        }
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
