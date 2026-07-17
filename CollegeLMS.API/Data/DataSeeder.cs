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
        await SeedReactCourseAsync(db);
        await SeedScheduleEntriesAsync(db);
        await SeedNewsCategoriesAsync(db);
        await SeedNewsAsync(db);
        await ImportWordPressDataAsync(db);
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

        var commissions = new[]
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
                    CyclicalCommission = commissions[i],
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
        var groupIds = groupNames.Select(n => groupMap[n].Id).ToList();

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
                GroupId = groupIds[0],
                RecordBookNumber = "ЗК-2024-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000002"),
                UserId = studentUsers[1].Id,
                GroupId = groupIds[0],
                RecordBookNumber = "ЗК-2024-002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000003"),
                UserId = studentUsers[2].Id,
                GroupId = groupIds[0],
                RecordBookNumber = "ЗК-2024-003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000004"),
                UserId = studentUsers[3].Id,
                GroupId = groupIds[1],
                RecordBookNumber = "ЗК-2024-004",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000005"),
                UserId = studentUsers[4].Id,
                GroupId = groupIds[1],
                RecordBookNumber = "ЗК-2024-005",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000006"),
                UserId = studentUsers[5].Id,
                GroupId = groupIds[1],
                RecordBookNumber = "ЗК-2024-006",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000007"),
                UserId = studentUsers[6].Id,
                GroupId = groupIds[2],
                RecordBookNumber = "ЗК-2024-007",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000008"),
                UserId = studentUsers[7].Id,
                GroupId = groupIds[2],
                RecordBookNumber = "ЗК-2024-008",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000009"),
                UserId = studentUsers[8].Id,
                GroupId = groupIds[2],
                RecordBookNumber = "ЗК-2023-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000010"),
                UserId = studentUsers[9].Id,
                GroupId = groupIds[1],
                RecordBookNumber = "ЗК-2023-002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000011"),
                UserId = studentUsers[10].Id,
                GroupId = groupIds[0],
                RecordBookNumber = "ЗК-2023-003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000012"),
                UserId = studentUsers[11].Id,
                GroupId = groupIds[3],
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
                GroupId = groupIds[3],
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
        var teacherMap = allTeachers.ToDictionary(t => t.User!.Email);

        var courses = new List<Course>
        {
            new()
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                Title = "Основы программирования",
                Description =
                    "Базовый курс по программированию на C#: типы данных, управляющие конструкции, ООП",
                TeacherId = teacherMap["teacher@collegelms.ru"].Id,
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
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        foreach (var course in courses)
        {
            if (
                !await db.Courses.AnyAsync(c =>
                    c.Title == course.Title
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

    private static async Task SeedReactCourseAsync(AppDbContext db)
    {
        if (await db.Courses.AnyAsync(c => c.Title == "React-разработка (МДК 01.05)"))
            return;

        var teacher = await db.Teachers.Include(t => t.User).FirstAsync(t =>
            t.User!.Email == "smirnova@collegelms.ru"
        );
        var group31 = await db.Groups.FirstAsync(g => g.Name == "ИСП-31");
        var group32 = await db.Groups.FirstAsync(g => g.Name == "ИСП-32");

        // ── Course ──
        var course = new Course
        {
            Id = Guid.Parse("c5000000-0000-0000-0000-000000000001"),
            Title = "React-разработка (МДК 01.05)",
            Description = "МДК 01.05 — React: компоненты, хуки, роутинг, Redux, тестирование, Next.js",
            TeacherId = teacher.Id,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        // ── 25 Lectures ──
        var lecturesData = new (string Title, string Content)[]
        {
            ("Введение в React",
                "История React, виртуальный DOM, создание проекта через Vite и CRA, структура проекта."),
            ("JSX и рендеринг",
                "Синтаксис JSX, выражения в JSX, рендеринг элементов, обновление отрендеренных элементов."),
            ("Компоненты и пропсы",
                "Функциональные и классовые компоненты, пропсы, композиция компонентов, PropTypes."),
            ("Состояние и useState",
                "Хук useState, правило хуков, множественное состояние, подъём состояния."),
            ("Обработка событий",
                "Синтетические события, обработчики, передача аргументов, отмена события по умолчанию."),
            ("Условный рендеринг",
                "if/else в JSX, тернарный оператор, логический оператор &&, условный return."),
            ("Списки и ключи",
                "Рендеринг списков через map, ключи, их назначение, правильный выбор ключа."),
            ("Формы и управляемые компоненты",
                "Управляемые и неуправляемые компоненты, text input, select, checkbox, radio."),
            ("useEffect и побочные эффекты",
                "Хук useEffect, зависимости, очистка эффектов, fetch-запросы в useEffect."),
            ("useRef и работа с DOM",
                "Хук useRef, доступ к DOM-элементам, хранение мутируемых значений, forwardRef."),
            ("Контекст и useContext",
                "React Context API, createContext, Provider, useContext, когда использовать контекст."),
            ("useReducer и сложное состояние",
                "Хук useReducer, редьюсеры, экшены, сравнение с useState, Middleware."),
            ("Кастомные хуки",
                "Создание собственных хуков, композиция хуков, примеры useLocalStorage, useDebounce."),
            ("Мемоизация (useMemo, useCallback)",
                "useMemo для вычислений, useCallback для функций, React.memo, профилирование."),
            ("Роутинг с React Router",
                "BrowserRouter, Routes, Route, Link, NavLink, вложенные маршруты, index route."),
            ("Параметры маршрутов и навигация",
                "useParams, useNavigate, URL-параметры, query-параметры, защищённые маршруты."),
            ("Работа с API (fetch/axios)",
                "HTTP-запросы, async/await, отмена запросов, Axios vs fetch, перехватчики."),
            ("Состояние загрузки и обработка ошибок",
                "Загрузка, спиннеры, скелетоны, ErrorBoundary, try/catch в асинхронных операциях."),
            ("Управление формой и валидация",
                "Библиотеки React Hook Form и Formik, валидация (Yup, Zod), кастомные валидаторы."),
            ("Redux Toolkit основы",
                "createSlice, configureStore, Provider, useSelector, useDispatch, devtools."),
            ("Redux Toolkit: асинхронные действия",
                "createAsyncThunk, extraReducers, обработка loading/fulfilled/rejected."),
            ("Тестирование React-компонентов (Jest, RTL)",
                "Jest, React Testing Library, render, screen, fireEvent, тестирование хуков."),
            ("Оптимизация производительности",
                "React DevTools Profiler, code splitting (lazy, Suspense), виртуализация списков."),
            ("Сборка и деплой",
                "Vite production build, CI/CD, Docker, Vercel/Netlify деплой, переменные окружения."),
            ("Обзор Next.js",
                "SSR, SSG, App Router, серверные компоненты, layout, middleware, API routes."),
        };

        var lectures = new List<Lecture>();
        for (int i = 0; i < lecturesData.Length; i++)
        {
            lectures.Add(new Lecture
            {
                Id = Guid.Parse($"d5000000-0000-0000-0000-{i + 1:x12}"),
                CourseId = course.Id,
                Title = lecturesData[i].Title,
                Content = lecturesData[i].Content,
                Order = i + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        db.Lectures.AddRange(lectures);
        await db.SaveChangesAsync();

        // ── 33 Assignments ──
        var assignmentsData = new (string Title, string Description, int MaxScore)[]
        {
            ("Настройка проекта Vite",
                "Создать React-приложение через Vite. Настроить ESLint, Prettier. Запустить dev-сервер. Создать компонент App с приветствием.", 10),
            ("JSX-компонент ProfileCard",
                "Создать компонент ProfileCard, который принимает пропсы name, role, avatarUrl и рендерит карточку пользователя с использованием JSX.", 15),
            ("Композиция компонентов Layout",
                "Создать компоненты Header, Sidebar, Main, Footer. Скомпоновать их в Layout." +
                " Header принимает title, Sidebar — items[], Main — children.", 20),
            ("Счётчик с разными шагами",
                "Создать счётчик с кнопками +1, -1, +5, -5, сброс. Отображать историю последних 5 значений. Использовать useState.", 20),
            ("Список задач (Todo)",
                "Реализовать Todo-приложение: добавление, удаление, отметка выполнения, фильтрация (все/активные/выполненные).", 30),
            ("Список задач с React Router",
                "Добавить в Todo-приложение страницы: / (список задач), /stats (статистика), /about. Использовать React Router.", 25),
            ("Форма регистрации с валидацией",
                "Создать форму регистрации: имя, email, пароль, подтверждение пароля. Валидация на стороне клиента (React Hook Form + Yup).", 25),
            ("Форма с динамическими полями",
                "Форма создания опроса: динамические поля вопросов, варианты ответов, тип вопроса (single/multiple).", 25),
            ("Таймер с управлением",
                "Создать таймер обратного отсчёта: ввод времени, старт, пауза, сброс. Использовать useEffect и useRef.", 20),
            ("Погодное приложение",
                "Приложение погоды: поиск города, отображение температуры, влажности, ветра. Использовать OpenWeatherMap API.", 30),
            ("Поиск пользователей GitHub",
                "Поиск пользователей GitHub по логину. Отображать аватар, имя, количество репозиториев. Использовать GitHub API.", 25),
            ("Кастомный хук useLocalStorage",
                "Реализовать хук useLocalStorage, который синхронизирует состояние с localStorage. Использовать в Todo-приложении.", 20),
            ("Кастомный хук useDebounce",
                "Реализовать хук useDebounce для поиска с задержкой. Применить в поиске GitHub-пользователей.", 20),
            ("Контекст темы (светлая/тёмная)",
                "Создать ThemeContext с Provider. Кнопка переключения темы. Применить тёмную тему через CSS-переменные.", 20),
            ("Корзина интернет-магазина",
                "Создать корзину товаров с useReducer: добавление, удаление, изменение количества, подсчёт суммы.", 30),
            ("Список товаров с пагинацией",
                "Список товаров из API (dummyjson) с пагинацией, загрузкой, обработкой ошибок.", 25),
            ("Бесконечная лента (infinite scroll)",
                "Реализовать бесконечную ленту постов с Intersection Observer и загрузкой страниц.", 25),
            ("Модальное окно с порталом",
                "Создать компонент Modal с ReactDOM.createPortal. Клавиша Escape закрывает модалку.", 20),
            ("Аккордеон/FAQ",
                "Создать аккордеон: раскрытие/закрытие секций, возможность открыть несколько или одну.", 15),
            ("Вкладки (Tabs)",
                "Создать компонент Tabs: переключение контента по вкладкам, анимация переключения.", 15),
            ("Redux: список постов",
                "Подключить Redux Toolkit. Создать slice для постов: fetchPosts через createAsyncThunk, отображение списка.", 25),
            ("Redux: корзина и избранное",
                "Создать два slice: cart и favourites. Товары добавляются в корзину и избранное, состояние глобальное.", 30),
            ("Тестирование компонентов",
                "Написать тесты для компонентов ProfileCard, TodoList, Counter с Jest и React Testing Library.", 25),
            ("Тестирование Redux slice",
                "Написать unit-тесты для createSlice и createAsyncThunk: начальное состояние, редьюсеры, middleware.", 20),
            ("Lazy loading страниц",
                "Разделить приложение на lazy-загружаемые страницы через React.lazy и Suspense.", 15),
            ("Скелетоны загрузки",
                "Реализовать компонент Skeleton для состояний загрузки. Анимировать shimmer-эффект.", 15),
            ("Файловый менеджер",
                "Создать компонент файлового менеджера: отображение папок/файлов, навигация, загрузка файлов.", 30),
            ("Чат-компонент",
                "Реализовать простой чат: отправка сообщений, отображение сообщений собеседника, WebSocket-имитация.", 30),
            ("Графики и аналитика",
                "Интегрировать Recharts: график посещаемости, круговая диаграмма распределения, линейный график прогресса.", 25),
            ("Drag and Drop (dnd-kit)",
                "Реализовать перетаскивание элементов списка с @dnd-kit/core. Переупорядочивание задач.", 25),
            ("Календарь событий",
                "Создать календарь: отображение месяца, переключение, добавление/удаление событий.", 30),
            ("Next.js: серверный рендеринг",
                "Перенести приложение на Next.js App Router. Использовать серверные компоненты, layout, SSR.", 25),
            ("Next.js: API routes и middleware",
                "Реализовать Next.js API routes для CRUD. Middleware для аутентификации. ISR для страницы постов.", 25),
        };

        var assignments = new List<Assignment>();
        for (int i = 0; i < assignmentsData.Length; i++)
        {
            assignments.Add(new Assignment
            {
                Id = Guid.Parse($"e5000000-0000-0000-0000-{i + 1:x12}"),
                CourseId = course.Id,
                Title = assignmentsData[i].Title,
                Description = assignmentsData[i].Description,
                DueDate = DateTime.UtcNow.AddDays(7 * (i + 1)),
                MaxScore = assignmentsData[i].MaxScore,
                Order = i + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        db.Assignments.AddRange(assignments);
        await db.SaveChangesAsync();

        // ── 25 Tests (10 questions each = 250 questions) ──
        var testsData = new (string Title, string Description, int Minutes, int Passing, TestType Type)[]
        {
            ("Введение в React", "JSX, Virtual DOM, создание проекта", 10, 60, TestType.SelfStudy),
            ("Компоненты и пропсы", "Функциональные компоненты, пропсы, композиция", 10, 60, TestType.SelfStudy),
            ("useState и состояние", "Хук состояния, подъём состояния, правило хуков", 10, 60, TestType.SelfStudy),
            ("Обработка событий", "Синтетические события, обработчики, аргументы", 10, 60, TestType.SelfStudy),
            ("Условный рендеринг и списки", "if/else, тернарник, map, ключи", 10, 60, TestType.SelfStudy),
            ("Формы в React", "Управляемые компоненты, select, checkbox", 10, 60, TestType.SelfStudy),
            ("useEffect", "Побочные эффекты, зависимости, очистка", 10, 60, TestType.SelfStudy),
            ("useRef и DOM", "Ref, forwardRef, мутируемые значения", 10, 60, TestType.SelfStudy),
            ("Контекст и useContext", "Context API, Provider, Consumer", 10, 60, TestType.SelfStudy),
            ("useReducer", "Редьюсеры, экшены, сложное состояние", 10, 60, TestType.SelfStudy),
            ("Кастомные хуки", "Создание хуков, композиция, примеры", 10, 60, TestType.SelfStudy),
            ("Мемоизация", "useMemo, useCallback, React.memo", 10, 60, TestType.SelfStudy),
            ("React Router", "BrowserRouter, Routes, Route, Link", 10, 60, TestType.SelfStudy),
            ("Навигация и параметры", "useParams, useNavigate, query params", 10, 60, TestType.SelfStudy),
            ("Работа с API", "fetch, axios, async/await, отмена", 10, 60, TestType.SelfStudy),
            ("Загрузка и ошибки", "ErrorBoundary, спиннеры, скелетоны", 10, 60, TestType.SelfStudy),
            ("React Hook Form", "Управление формой, валидация, Yup/Zod", 10, 60, TestType.SelfStudy),
            ("Redux Toolkit", "createSlice, configureStore, useSelector", 15, 70, TestType.Control),
            ("Redux Async", "createAsyncThunk, extraReducers", 15, 70, TestType.Control),
            ("Тестирование", "Jest, RTL, render, screen, fireEvent", 15, 70, TestType.Control),
            ("Оптимизация", "lazy, Suspense, Profiler, code splitting", 10, 60, TestType.SelfStudy),
            ("Сборка и деплой", "Vite, CI/CD, Docker, Vercel", 10, 60, TestType.SelfStudy),
            ("Next.js основы", "App Router, SSR, SSG, серверные компоненты", 15, 70, TestType.Control),
            ("Next.js роутинг", "layout, middleware, API routes, ISR", 15, 70, TestType.Control),
            ("DnD и анимации", "dnd-kit, framer-motion, переходы CSS", 10, 60, TestType.SelfStudy),
        };

        var tests = new List<Test>();
        for (int i = 0; i < testsData.Length; i++)
        {
            tests.Add(new Test
            {
                Id = Guid.Parse($"f5000000-0000-0000-0000-{i + 1:x12}"),
                CourseId = course.Id,
                Title = testsData[i].Title,
                Description = testsData[i].Description,
                TimeLimitMinutes = testsData[i].Minutes,
                MaxAttempts = 3,
                PassingScore = testsData[i].Passing,
                Type = testsData[i].Type,
                AutoCheck = true,
                ShuffleQuestions = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        db.Tests.AddRange(tests);
        await db.SaveChangesAsync();

        var savedTests = await db.Tests.Where(t => t.CourseId == course.Id).OrderBy(t => t.Title).ToListAsync();

        // ── 250 Test Questions (10 per test) ──
        var allQuestions = new List<TestQuestion>();
        int qCounter = 0;

        void AddQ(string text, string[] options, string correct, QuestionType qType = QuestionType.SingleChoice, int points = 10)
        {
            qCounter++;
            allQuestions.Add(new TestQuestion
            {
                Id = Guid.Parse($"b5000000-0000-0000-0000-{qCounter:x8}0001"),
                TestId = savedTests[(qCounter - 1) / 10].Id,
                Text = text,
                Type = qType,
                Options = System.Text.Json.JsonSerializer.Serialize(options),
                CorrectAnswer = correct,
                Points = points,
                OrderIndex = (qCounter - 1) % 10 + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        // Questions (250 total, 10 per test)
        // Test 1: Введение в React
        AddQ("Что такое виртуальный DOM?", ["Объектная модель документа браузера", "Легковесная копия реального DOM в памяти", "Библиотека для работы с DOM", "Фреймворк для серверного рендеринга"], "Легковесная копия реального DOM в памяти");
        AddQ("Какая команда создаёт React-приложение через Vite?", ["npx create-react-app my-app", "npm init react-app my-app", "npm create vite@latest my-app -- --template react", "npx react-new my-app"], "npm create vite@latest my-app -- --template react");
        AddQ("Что возвращает функция-компонент React?", ["Объект JavaScript", "Строку HTML", "React-элемент (JSX)", "Виртуальный DOM-узел"], "React-элемент (JSX)");
        AddQ("Какой файл является точкой входа в CRA-приложении?", ["index.html", "App.js", "main.jsx", "index.js"], "index.js");
        AddQ("Какая технология позволяет писать HTML-подобный код внутри JavaScript?", ["TypeScript", "Babel", "JSX", "ESLint"], "JSX");
        AddQ("Что такое React?", ["Фреймворк для создания UI", "Библиотека для создания пользовательских интерфейсов", "Язык программирования", "База данных"], "Библиотека для создания пользовательских интерфейсов");
        AddQ("Какой хук используется для обновления состояния компонента?", ["useEffect", "useContext", "useState", "useReducer"], "useState");
        AddQ("Что делает Babel в React-проекте?", ["Управляет зависимостями", "Транспилирует JSX в JavaScript", "Собирает финальный бандл", "Запускает тесты"], "Транспилирует JSX в JavaScript");
        AddQ("Какой пакет отвечает за рендеринг React в браузере?", ["react", "react-dom", "react-router", "react-scripts"], "react-dom");
        AddQ("Как React обновляет реальный DOM?", ["Полной перезагрузкой страницы", "Сравнением виртуального DOM с реальным (diffing)", "Ручным изменением элементов", "Через jQuery"], "Сравнением виртуального DOM с реальным (diffing)");

        // Test 2: Компоненты и пропсы
        AddQ("Как передать данные от родительского компонента дочернему?", ["Через props", "Через state", "Через ref", "Через контекст"], "Через props");
        AddQ("Что такое props в React?", ["Внутреннее состояние компонента", "Входные данные компонента", "Методы жизненного цикла", "Стили компонента"], "Входные данные компонента");
        AddQ("Как задать значение props по умолчанию?", ["defaultProps", "initialProps", "static defaultProps = {}", "defaultValue"], "static defaultProps = {}");
        AddQ("Что такое композиция компонентов?", ["Объединение нескольких компонентов в один файл", "Вложение одного компонента в другой через children", "Наследование классов", "Импорт компонентов"], "Вложение одного компонента в другой через children");
        AddQ("Как получить children в функциональном компоненте?", ["this.props.children", "props.children", "children()", "useChildren()"], "props.children");
        AddQ("Можно ли изменять props внутри компонента?", ["Да, напрямую", "Да, через setProps()", "Нет, props иммутабельны", "Только в классовых компонентах"], "Нет, props иммутабельны");
        AddQ("Какие типы компонентов существуют в React?", ["Функциональные и классовые", "Статические и динамические", "Простые и составные", "Встроенные и внешние"], "Функциональные и классовые", QuestionType.MultipleChoice, 10);
        AddQ("Что такое PropTypes?", ["Библиотека для типизации", "Механизм проверки типов props", "TypeScript для React", "Спецификация пропсов"], "Механизм проверки типов props");
        AddQ("Как пробросить props через несколько уровней вложенности?", ["Через props drilling", "Через useContext", "Через PropProvider", "Через prop forwarding"], "Через useContext");
        AddQ("Как рендерить компонент внутри JSX?", ["{Component()}", "<Component>", "<Component />", "{Component}"], "<Component />");

        // Test 3: useState
        AddQ("Какой хук даёт возможность использовать состояние в функциональном компоненте?", ["useEffect", "useContext", "useState", "useRef"], "useState");
        AddQ("Что возвращает useState?", ["Объект", "Массив из двух элементов", "Число", "Функцию"], "Массив из двух элементов");
        AddQ("Как правильно обновить состояние на основе предыдущего?", ["setCount(count + 1)", "setCount(prev => prev + 1)", "count = count + 1", "this.setState()"], "setCount(prev => prev + 1)");
        AddQ("Что произойдёт при вызове setState с тем же значением?", ["Компонент перерендерится", "React пропустит рендер", "Будет ошибка", "Сбросится состояние"], "React пропустит рендер");
        AddQ("Где можно вызывать хуки?", ["Внутри циклов", "На верхнем уровне компонента", "Внутри условий", "В колбэках"], "На верхнем уровне компонента");
        AddQ("Что такое подъём состояния (lifting state up)?", ["Удаление состояния", "Перенос общего состояния в родительский компонент", "Создание глобального хранилища", "Удаление компонента"], "Перенос общего состояния в родительский компонент");
        AddQ("Сколько раз выполнится setCount(count + 1) при трёх последовательных вызовах?", ["1", "3", "0", "Зависит от значения"], "3");
        AddQ("Как инициализировать состояние значением из localStorage?", ["useState(localStorage.getItem('key'))", "useState(() => localStorage.getItem('key'))", "useEffect(() => localStorage.getItem('key'))", "useRef(localStorage.getItem('key'))"], "useState(() => localStorage.getItem('key'))");
        AddQ("Как объявить несколько состояний в одном компоненте?", ["Вызвать useState несколько раз", "Использовать один объект", "Использовать useReducer", "Создать отдельный файл"], "Вызвать useState несколько раз");
        AddQ("Какое правило НЕ относится к правилам хуков?", ["Хуки вызываются на верхнем уровне", "Хуки вызываются только из React-функций", "Хуки можно вызывать внутри условий", "Хуки нельзя вызывать в циклах"], "Хуки можно вызывать внутри условий");

        // Test 4: События
        AddQ("Как React называет свою систему событий?", ["DOM Events", "Synthetic Events", "Virtual Events", "React Events"], "Synthetic Events");
        AddQ("Как передать аргумент в обработчик события?", ["onClick={handleClick(arg)}", "onClick={() => handleClick(arg)}", "onClick={handleClick.bind(arg)}", "onClick={handleClick} arg={arg}"], "onClick={() => handleClick(arg)}");
        AddQ("Как предотвратить действие по умолчанию в React?", ["event.preventDefault()", "return false", "event.stopPropagation()", "preventDefault()"], "event.preventDefault()");
        AddQ("Какой атрибут используется для события клика?", ["onclick", "onClick", "on-click", "OnClick"], "onClick");
        AddQ("Как получить значение поля ввода при onChange?", ["e.value", "e.target.value", "e.current.value", "e.data"], "e.target.value");
        AddQ("SyntheticEvent в React — это:", ["Нативный браузерный объект", "Кроссплатформенная обёртка над нативным событием", "Кастомный тип события", "Объект Redux"], "Кроссплатформенная обёртка над нативным событием");
        AddQ("Как передать id элемента в onClick?", ["data-id атрибут", "Замыкание с параметром", "dataset свойство", "id пропс"], "data-id атрибут", QuestionType.MultipleChoice);
        AddQ("Что произойдёт без вызова stopPropagation?", ["Событие всплывёт к родителям", "Событие отменится", "Форма отправится", "Ничего"], "Событие всплывёт к родителям");
        AddQ("Как подписаться на событие клавиатуры?", ["onKeyDown", "onKeyboard", "onPress", "onType"], "onKeyDown");
        AddQ("Что такое event pooling в React?", ["События хранятся в пуле и переиспользуются", "События удаляются после обработки", "События ставятся в очередь", "События кэшируются"], "События хранятся в пуле и переиспользуются");

        // Test 5: Условный рендеринг и списки
        AddQ("Какой оператор НЕ используется для условного рендеринга?", ["&&", "?", "||", "if/else"], "||");
        AddQ("Как отрендерить список элементов из массива?", ["list.forEach()", "list.map()", "list.filter()", "list.reduce()"], "list.map()");
        AddQ("Зачем нужен атрибут key в списках?", ["Для стилизации", "Для идентификации элементов при обновлении", "Для сортировки", "Для доступа к элементу"], "Для идентификации элементов при обновлении");
        AddQ("Что будет, если не указать key в списке?", ["Ошибка компиляции", "React выдаст предупреждение, производительность упадёт", "Ничего", "Список не отрендерится"], "React выдаст предупреждение, производительность упадёт");
        AddQ("Какое значение key считается лучшей практикой?", ["Индекс в массиве", "Уникальный id элемента", "Случайное число", "Имя элемента"], "Уникальный id элемента");
        AddQ("Что вернёт выражение {isLogged && <LogoutButton />} если isLogged = false?", ["false", "null", "LogoutButton", "Ошибку"], "false");
        AddQ("Как отфильтровать массив перед рендерингом?", ["filter().map()", "map().filter()", "forEach().filter()", "reduce().filter()"], "filter().map()");
        AddQ("Как отрендерить элементы по индексу?", ["Добавить key={index}", "Использовать i < items.length", "Применить Object.keys()", "Использовать for"], "Добавить key={index}");
        AddQ("Что такое условный return в компоненте?", ["Возврат null если условие не выполнено", "Двойной return", "Тернарный return", "Асинхронный return"], "Возврат null если условие не выполнено");
        AddQ("Как отрендерить несколько элементов без обёртки?", ["<></> или Fragment", "Вернуть массив", "Обернуть в div", "Использовать span"], "<></> или Fragment");

        // Test 6: Формы
        AddQ("Что такое управляемый компонент формы?", ["Компонент без состояния", "Компонент, чьё значение контролируется React через state", "Компонент с ref", "HTML-элемент form"], "Компонент, чьё значение контролируется React через state");
        AddQ("Как получить значение select в управляемом компоненте?", ["e.options[e.selectedIndex].value", "e.target.value", "selectRef.current.value", "select.value"], "e.target.value");
        AddQ("Что такое неуправляемый компонент?", ["Компонент без onChange", "Компонент, где значение хранится в DOM", "Компонент без state", "Пустой компонент"], "Компонент, где значение хранится в DOM");
        AddQ("Как создать ref для неуправляемого поля?", ["useRef() + ref атрибут", "createRef()", "useState()", "useEffect()"], "useRef() + ref атрибут");
        AddQ("Как обработать отправку формы?", ["onClick на кнопке", "onSubmit на форме", "onChange на форме", "onSubmit на кнопке"], "onSubmit на форме");
        AddQ("Как сбросить форму после отправки?", ["form.reset()", "setState с пустыми значениями", "reload()", "clearForm()"], "setState с пустыми значениями");
        AddQ("Как реализовать checkbox в управляемой форме?", ["checked={isChecked} onChange={handler}", "value={isChecked}", "defaultChecked", "ref={checkboxRef}"], "checked={isChecked} onChange={handler}");
        AddQ("Какой атрибут связывает label с input?", ["for", "htmlFor", "id", "name"], "htmlFor");
        AddQ("Как получить все значения формы сразу при submit?", ["FormData(e.target)", "e.target.value", "Object.fromEntries(new FormData(e.target))", "e.formData()"], "Object.fromEntries(new FormData(e.target))");
        AddQ("Можно ли использовать defaultValue в управляемых полях?", ["Да", "Нет, это бессмысленно", "Только в select", "Только в textarea"], "Нет, это бессмысленно");

        // Test 7: useEffect
        AddQ("Для чего используется useEffect?", ["Для рендеринга", "Для побочных эффектов", "Для управления состоянием", "Для стилей"], "Для побочных эффектов");
        AddQ("Что произойдёт при пустом массиве зависимостей []?", ["Эффект выполнится при каждом рендере", "Эффект выполнится только при монтировании", "Эффект не выполнится", "Будет ошибка"], "Эффект выполнится только при монтировании");
        AddQ("Как очистить подписку в useEffect?", ["Вернуть функцию очистки", "Вызвать cleanup()", "Использовать abortController", "Удалить эффект"], "Вернуть функцию очистки");
        AddQ("Когда выполняется эффект с зависимостями [a, b]?", ["При каждом рендере", "Только при изменении a или b", "При монтировании и изменении a или b", "Один раз"], "При монтировании и изменении a или b");
        AddQ("Как сделать HTTP-запрос при монтировании?", ["fetch внутри useEffect с []", "fetch на верхнем уровне", "fetch в useState", "fetch в useRef"], "fetch внутри useEffect с []");
        AddQ("Как избежать бесконечного цикла при fetch внутри useEffect?", ["Добавить пустой массив зависимостей", "Добавить зависимость []", "Использовать async/await", "Не использовать fetch"], "Добавить пустой массив зависимостей");
        AddQ("Что если не указать массив зависимостей вообще?", ["Эффект выполнится после каждого рендера", "Будет ошибка", "Эффект выполнится один раз", "React предупредит"], "Эффект выполнится после каждого рендера");
        AddQ("Как отменить fetch-запрос при размонтировании?", ["AbortController + signal", "CancelToken", "Отмена через useEffect cleanup", "Никак"], "AbortController + signal");
        AddQ("Можно ли использовать async функцию в useEffect напрямую?", ["Да", "Нет, нужно обернуть", "Только с useCallback", "Только с then"], "Нет, нужно обернуть");
        AddQ("Как симулировать componentDidMount с useEffect?", ["useEffect(() => {}, [])", "useEffect(() => {})", "useEffect(() => {}, null)", "useEffect(() => {}, [props])"], "useEffect(() => {}, [])");

        // Test 8: useRef
        AddQ("Что возвращает useRef?", ["{ current: initialValue }", "initialValue", "Объект", "Функцию"], "{ current: initialValue }");
        AddQ("Изменение .current у useRef вызывает ли перерендер?", ["Да", "Нет", "Зависит от значения", "Только при первом изменении"], "Нет");
        AddQ("Как получить DOM-элемент по ref?", ["ref.current", "ref.value", "current(ref)", "ref.dom"], "ref.current");
        AddQ("Для чего используется forwardRef?", ["Для рефакторинга", "Для передачи ref дочернему компоненту", "Для создания ref", "Для проброса props"], "Для передачи ref дочернему компоненту");
        AddQ("Как поставить фокус на поле ввода при монтировании?", ["useEffect + inputRef.current.focus()", "autoFocus атрибут", "focus() в useState", "useFocus хук"], "useEffect + inputRef.current.focus()");
        AddQ("Можно ли использовать useRef для хранения мутируемого значения?", ["Да, это его основное назначение кроме DOM", "Нет, только для DOM", "Только для чисел", "Только с useState"], "Да, это его основное назначение кроме DOM");
        AddQ("Чем useRef отличается от useState?", ["useRef не вызывает перерендер", "useRef иммутабелен", "useRef асинхронный", "useRef глобальный"], "useRef не вызывает перерендер");
        AddQ("Как измерить ширину элемента после рендера?", ["useRef + offsetWidth в useEffect", "getBoundingClientRect", "ResizeObserver", "width атрибут"], "useRef + offsetWidth в useEffect");
        AddQ("Что произойдёт при попытке доступа к ref.current до монтирования?", ["null", "undefined", "Ошибка", "Пустой объект"], "null");
        AddQ("Как передать ref через компонент-обёртку?", ["forwardRef + второй аргумент", "prop с именем ref", "useImperativeHandle", "cloneElement"], "forwardRef + второй аргумент");

        // Test 9: Контекст
        AddQ("Как создать контекст в React?", ["React.createContext()", "useContext()", "createContext()", "new Context()"], "React.createContext()");
        AddQ("Что возвращает createContext?", ["Объект с Provider и Consumer", "Компонент-обёртку", "Хук", "Строку"], "Объект с Provider и Consumer");
        AddQ("Какой компонент предоставляет значение контекста?", ["Context.Provider", "Context.Consumer", "Context.Provider value={}", "Context"], "Context.Provider value={}");
        AddQ("Как получить значение контекста в функциональном компоненте?", ["useContext(Context)", "Context.Consumer", "this.context", "useContext()"], "useContext(Context)");
        AddQ("Когда React перерендеривает потребителей контекста?", ["При изменении значения Provider", "При любом рендере", "Никогда", "По таймеру"], "При изменении значения Provider");
        AddQ("Что произойдёт при изменении state родителя, содержащего Provider?", ["Все потребители перерендерятся", "Provider не перерендерит потребителей", "Перерендерятся только дети Provider", "Ничего"], "Все потребители перерендерятся");
        AddQ("Как оптимизировать контекст во избежание лишних рендеров?", ["Мемоизировать value через useMemo", "Разделить на несколько контекстов", "Использовать zustand", "Удалить Provider"], "Мемоизировать value через useMemo", QuestionType.MultipleChoice);
        AddQ("Можно ли вкладывать Provider одного контекста друг в друга?", ["Да", "Нет", "Только два уровня", "С ограничениями"], "Да");
        AddQ("Что делает Context.Consumer?", ["Рендерит children как функцию с value", "Предоставляет значение", "Создаёт контекст", "Проверяет тип"], "Рендерит children как функцию с value");
        AddQ("Контекст следует использовать для:", ["Глобальных данных (тема, язык, auth)", "Каждого пропса", "Только для состояния", "Только для стилей"], "Глобальных данных (тема, язык, auth)");

        // Test 10: useReducer
        AddQ("Из каких частей состоит useReducer?", ["Редьюсер и начальное состояние", "Store и action", "Reducer и effect", "State и mutation"], "Редьюсер и начальное состояние");
        AddQ("Что возвращает useReducer?", ["[state, dispatch]", "[state, reducer]", "{state, dispatch}", "[reducer, dispatch]"], "[state, dispatch]");
        AddQ("Что такое action в useReducer?", ["Объект с полем type и опционально payload", "Функция", "Строка", "Число"], "Объект с полем type и опционально payload");
        AddQ("Как dispatch отправляет action?", ["dispatch({ type: 'INCREMENT' })", "dispatch('INCREMENT')", "action('INCREMENT')", "reducer('INCREMENT')"], "dispatch({ type: 'INCREMENT' })");
        AddQ("Где должна быть чистая логика редьюсера?", ["Внутри функции-редьюсера", "В dispatch", "В action", "В компоненте"], "Внутри функции-редьюсера");
        AddQ("Когда использовать useReducer вместо useState?", ["Когда состояние сложное или зависит от предыдущего", "Всегда", "Никогда", "Только с TypeScript"], "Когда состояние сложное или зависит от предыдущего");
        AddQ("Как редьюсер должен обрабатывать неизвестный action type?", ["Вернуть текущее состояние", "Выбросить ошибку", "Вернуть undefined", "Пропустить"], "Вернуть текущее состояние");
        AddQ("Можно ли делать асинхронные вызовы внутри редьюсера?", ["Нет, редьюсер должен быть чистым", "Да, напрямую", "Да, через dispatch", "Только через thunk"], "Нет, редьюсер должен быть чистым");
        AddQ("Что такое initializer для useReducer?", ["Третий аргумент — функция ленивой инициализации", "Начальное состояние", "Диспатчер", "Action creator"], "Третий аргумент — функция ленивой инициализации");
        AddQ("Как смоделировать enum action types в TypeScript?", ["Строковый union тип", "Числовые константы", "Enum", "Interface"], "Строковый union тип");

        // Test 11: Кастомные хуки
        AddQ("Какие правила нужно соблюдать при создании кастомных хуков?", ["Начинать с 'use', вызывать другие хуки", "Заканчивать на 'Hook'", "Использовать классы", "Импортировать из React"], "Начинать с 'use', вызывать другие хуки");
        AddQ("Кастомный хук может вызывать другие хуки?", ["Да", "Нет", "Только useState", "Только useEffect"], "Да");
        AddQ("Что типизирует возвращаемое значение кастомного хука?", ["Кортеж или объект", "Только число", "Только строку", "undefined"], "Кортеж или объект");
        AddQ("Как хук useLocalStorage должен синхронизировать данные?", ["useState + useEffect для записи в localStorage", "useRef", "useReducer", "useMemo"], "useState + useEffect для записи в localStorage");
        AddQ("Можно ли использовать кастомный хук внутри условия?", ["Нет, нарушает правила хуков", "Да", "Только если хук простой", "С dependsOn"], "Нет, нарушает правила хуков");
        AddQ("Как передать параметры в кастомный хук?", ["Как аргументы функции", "Через контекст", "Через props", "Через ref"], "Как аргументы функции");
        AddQ("Какой хук использовать для дебаунса ввода?", ["useDebounce (кастомный)", "useEffect с setTimeout", "onChange с setTimeout", "useMemo"], "useDebounce (кастомный)");
        AddQ("Для чего используют useToggle?", ["Для переключения boolean состояния", "Для счётчика", "Для запросов", "Для анимаций"], "Для переключения boolean состояния");
        AddQ("Что вернёт хук useFetch(url)?", ["{ data, loading, error }", "{ data }", "data", "Promise"], "{ data, loading, error }");
        AddQ("Как композировать хуки?", ["Вызвать один кастомный хук внутри другого", "Импортировать вложенный хук", "Наследовать хук", "Создать класс"], "Вызвать один кастомный хук внутри другого");

        // Test 12: Мемоизация
        AddQ("Что делает useMemo?", ["Мемоизирует результат вычисления", "Мемоизирует функцию", "Мемоизирует компонент", "Мемоизирует контекст"], "Мемоизирует результат вычисления");
        AddQ("Что делает useCallback?", ["Мемоизирует функцию", "Мемоизирует значение", "Мемоизирует компонент", "Мемоизирует пропс"], "Мемоизирует функцию");
        AddQ("Что делает React.memo?", ["Мемоизирует компонент", "Мемоизирует функцию", "Мемоизирует значение", "Оптимизирует рендер"], "Мемоизирует компонент");
        AddQ("Когда useMemo не даёт выигрыша?", ["При простых вычислениях", "При сложных вычислениях", "При частых изменениях зависимостей", "Никогда"], "При простых вычислениях");
        AddQ("Как React.memo сравнивает props?", ["Поверхностное сравнение (shallow equal)", "Глубокое сравнение", "Референсное сравнение", "Строгое сравнение"], "Поверхностное сравнение (shallow equal)");
        AddQ("Как передать функцию сравнения в React.memo?", ["React.memo(Component, areEqual)", "React.memo(Component, compareFn)", "React.memo(areEqual)(Component)", "React.memo(Component, propsAreEqual)"], "React.memo(Component, areEqual)");
        AddQ("Что произойдёт без useCallback при передаче inline функции?", ["Компонент будет создавать новую функцию на каждый рендер", "Ничего", "Будет ошибка", "Функция закешируется"], "Компонент будет создавать новую функцию на каждый рендер");
        AddQ("Какой инструмент помогает обнаружить лишние рендеры?", ["React DevTools Profiler", "Chrome DevTools", "ESLint", "TypeScript"], "React DevTools Profiler");
        AddQ("Мемоизация нужна:", ["Только при доказанной проблеме производительности", "Всегда", "Никогда", "Только для классов"], "Только при доказанной проблеме производительности");
        AddQ("Что передать вторым аргументом useCallback, чтобы функция не менялась?", ["[]", "null", "undefined", "Зависимости"], "[]");

        // Test 13: React Router
        AddQ("Какой компонент оборачивает всё приложение для роутинга?", ["BrowserRouter", "Router", "Routes", "Route"], "BrowserRouter");
        AddQ("Какой компонент определяет маршрут?", ["<Route path='/' element={<Home />} />", "Route", "Router", "Path"], "<Route path='/' element={<Home />} />");
        AddQ("Какой компонент рендерит подходящий маршрут?", ["<Routes>", "<Route>", "<Switch>", "Router"], "<Routes>");
        AddQ("Как перейти на другую страницу без перезагрузки?", ["<Link to='/about'>", "<a href='/about'>", "window.location.href", "document.navigate"], "<Link to='/about'>");
        AddQ("Чем NavLink отличается от Link?", ["NavLink добавляет active class", "NavLink быстрее", "NavLink не перезагружает страницу", "NavLink только для меню"], "NavLink добавляет active class");
        AddQ("Как получить текущий pathname?", ["useLocation()", "useParams()", "useNavigate()", "usePathname()"], "useLocation()");
        AddQ("Как сделать редирект после действия?", ["useNavigate()('path')", "redirect('path')", "history.push('path')", "Link to='path'"], "useNavigate()('path')");
        AddQ("Как сделать вложенный маршрут в React Router v6?", ["<Route path='parent'><Route path='child'/></Route>", "Вложить Route внутри другого Route", "Использовать Outlet", "Использовать children"], "Вложить Route внутри другого Route");
        AddQ("Какой элемент рендерит дочерний маршрут?", ["<Outlet />", "<Child />", "<RouteChildren />", "<NestedRoute />"], "<Outlet />");
        AddQ("Что такое index route?", ["Путь по умолчанию для родителя", "Маршрут с индексом", "Служебный маршрут", "Главная страница"], "Путь по умолчанию для родителя");

        // Test 14: Навигация и параметры
        AddQ("Как получить параметры из URL?: /users/42", ["useParams()", "useLocation()", "useQuery()", "useNavigate()"], "useParams()");
        AddQ("Как получить query-параметры из URL?", ["useSearchParams()", "useParams()", "useLocation().search", "new URLSearchParams(location.search)"], "useSearchParams()");
        AddQ("Как передать state при навигации?", ["navigate('/path', { state: { from: '/' } })", "navigate('/path', { data: {} })", "navigate('/path', { query: {} })", "navigate('/path', { params: {} })"], "navigate('/path', { state: { from: '/' } })");
        AddQ("Как защитить маршрут от неавторизованных пользователей?", ["Создать ProtectedRoute компонент", "Guard middleware", "Route guard", "Auth wrapper"], "Создать ProtectedRoute компонент");
        AddQ("Что делает useNavigate при вызове с -1?", ["Возврат на предыдущую страницу", "Переход на главную", "Обновление страницы", "Ошибка"], "Возврат на предыдущую страницу");
        AddQ("Как подписаться на изменение location?", ["useEffect с location как зависимостью", "useLocation()", "useNavigate()", "subscribe()"], "useEffect с location как зависимостью");
        AddQ("Как сделать 404 страницу в React Router?", ["Route path='*' element={<NotFound />}", "Route path='404'", "NotFound компонент", "ErrorBoundary"], "Route path='*' element={<NotFound />}");
        AddQ("Что такое Navigate компонент?", ["Компонент для редиректа", "Компонент для навигации", "Компонент для ссылок", "Компонент для кнопок"], "Компонент для редиректа");
        AddQ("Как передать параметр в Link?", ["<Link to={`/users/${id}`}>", "<Link to='/users/:id'>", "<Link params={{id}}>", "<Link path='/users/:id'>"], "<Link to={`/users/${id}`}>");
        AddQ("Можно ли использовать React Router вне браузера?", ["Да, есть NativeRouter и StaticRouter", "Нет", "Только ServerRouter", "Только MemoryRouter"], "Да, есть NativeRouter и StaticRouter");

        // Test 15: API
        AddQ("Какой метод HTTP используется для получения данных?", ["GET", "POST", "PUT", "DELETE"], "GET");
        AddQ("Как обработать ответ fetch?", ["res.json()", "res.text()", "res.data()", "res.parse()"], "res.json()");
        AddQ("Чем отличается axios от fetch?", ["axios автоматически парсит JSON", "fetch быстрее", "axios не поддерживает перехватчики", "fetch парсит JSON автоматически"], "axios автоматически парсит JSON");
        AddQ("Как отменить fetch-запрос?", ["AbortController + signal", "cancel()", "abort()", "stop()"], "AbortController + signal");
        AddQ("Как обработать ошибку сети в fetch?", ["try/catch", ".catch()", "error callback", "onError"], "try/catch");
        AddQ("Как отправить POST-запрос с JSON-телом?", ["fetch(url, { method: 'POST', body: JSON.stringify(data) })", "fetch.post(url, data)", "fetch(url, { body: data })", "fetch(url, { method: 'POST', data })"], "fetch(url, { method: 'POST', body: JSON.stringify(data) })");
        AddQ("Для чего нужен перехватчик (interceptor) в axios?", ["Для модификации запросов/ответов глобально", "Для логирования", "Для кэширования", "Для трансформации"], "Для модификации запросов/ответов глобально");
        AddQ("Как установить заголовок Authorization?", ["headers: { Authorization: `Bearer ${token}` }", "auth: token", "bearer: token", "token: token"], "headers: { Authorization: `Bearer ${token}` }");
        AddQ("Что такое CORS и когда возникает ошибка?", ["Политика браузера при запросе на другой домен", "Ошибка сервера", "Проблема с JSON", "Ошибка валидации"], "Политика браузера при запросе на другой домен");
        AddQ("Как выполнить несколько параллельных запросов?", ["Promise.all([fetch1, fetch2])", "fetch.all()", "parallel()", "join()"], "Promise.all([fetch1, fetch2])");

        // Test 16: Загрузка и ошибки
        AddQ("Какой компонент ловит ошибки рендеринга?", ["ErrorBoundary", "ErrorHandler", "CatchError", "SafeComponent"], "ErrorBoundary");
        AddQ("ErrorBoundary ловит ошибки в:", ["Дочерних компонентах", "Своих обработчиках событий", "Асинхронном коде", "Серверном рендеринге"], "Дочерних компонентах");
        AddQ("Как показать спиннер во время загрузки?", ["Условный рендеринг на основе loading state", "useLoading()", "Loading компонент", "Spin компонент"], "Условный рендеринг на основе loading state");
        AddQ("Что такое скелетон (skeleton)?", ["Заглушка загрузки, имитирующая структуру контента", "Тип анимации", "Пустой компонент", "Спиннер"], "Заглушка загрузки, имитирующая структуру контента");
        AddQ("Как показать сообщение об ошибке пользователю?", ["Error-компонент с текстом ошибки и кнопкой повтора", "Просто лог в консоль", "alert()", "игнорировать"], "Error-компонент с текстом ошибки и кнопкой повтора");
        AddQ("Какой жизненный цикл ошибки в ErrorBoundary?", ["componentDidCatch(error, info)", "onError(error)", "errorHandler(error)", "catchError(error)"], "componentDidCatch(error, info)");
        AddQ("Можно ли ErrorBoundary восстановить UI?", ["Да, через сброс состояния", "Нет, только показать fallback", "Автоматически", "Через reload"], "Да, через сброс состояния");
        AddQ("Как симулировать задержку при тестировании?", ["await new Promise(r => setTimeout(r, ms))", "sleep(ms)", "delay(ms)", "wait(ms)"], "await new Promise(r => setTimeout(r, ms))");
        AddQ("Что такое fallback UI в ErrorBoundary?", ["Резервный UI при ошибке", "UI по умолчанию", "Загрузочный UI", "Пустой UI"], "Резервный UI при ошибке");
        AddQ("Как показать тост с ошибкой?", ["Библиотека уведомлений (sonner, react-toastify)", "alert()", "Окно браузера", "console.error"], "Библиотека уведомлений (sonner, react-toastify)");

        // Test 17: React Hook Form
        AddQ("Какой хук предоставляет React Hook Form?", ["useForm()", "useFormHook()", "useFormState()", "form()"], "useForm()");
        AddQ("Как зарегистрировать поле в RHF?", ["{...register('fieldName')}", "register('fieldName')", "useRegister('fieldName')", "bind('fieldName')"], "{...register('fieldName')}");
        AddQ("Как получить ошибки формы в RHF?", ["formState.errors", "errors", "formErrors", "validationErrors"], "formState.errors");
        AddQ("Как интегрировать Yup/Zod с RHF?", ["resolver от @hookform/resolvers", "validationSchema", "schema", "validator"], "resolver от @hookform/resolvers");
        AddQ("Как сбросить форму в RHF?", ["reset()", "clear()", "resetForm()", "clean()"], "reset()");
        AddQ("Как показать сообщение об ошибке под полем?", ["{errors.fieldName && <p>{errors.fieldName.message}</p>}", "error('fieldName')", "showError('fieldName')", "ErrorField component"], "{errors.fieldName && <p>{errors.fieldName.message}</p>}");
        AddQ("Как отслеживать изменения поля в RHF?", ["watch('fieldName')", "onChange", "subscribe('fieldName')", "track('fieldName')"], "watch('fieldName')");
        AddQ("Как отправить форму в RHF?", ["handleSubmit(onSubmit)", "submit(onSubmit)", "onSubmit(handleSubmit)", "form.handleSubmit(onSubmit)"], "handleSubmit(onSubmit)");
        AddQ("Как добавить кастомную валидацию в Yup?", [".test('name', 'message', fn)", ".custom('name', fn)", ".validate('name', fn)", ".check('name', fn)"], ".test('name', 'message', fn)");
        AddQ("Какой тип Zod подходит для строки email?", ["z.string().email()", "z.email()", "z.string().email", "z.email().string()"], "z.string().email()");

        // Test 18: Redux Toolkit
        AddQ("Что такое slice в Redux Toolkit?", ["Фрагмент состояния с редьюсерами и экшенами", "Файл конфигурации", "Тип экшена", "MiddleWare"], "Фрагмент состояния с редьюсерами и экшенами");
        AddQ("Как создать slice?", ["createSlice({ name, initialState, reducers })", "createSlice({ ... })", "new Slice({ ... })", "slice({ ... })"], "createSlice({ name, initialState, reducers })");
        AddQ("Что делает configureStore?", ["Создаёт Redux store с devtools и middleware", "Настраивает роутер", "Конфигурирует API", "Устанавливает темы"], "Создаёт Redux store с devtools и middleware");
        AddQ("Как получить данные из store?", ["useSelector(state => state.some)", "useStore(state => state.some)", "getState().some", "useSelect(state => state.some)"], "useSelector(state => state.some)");
        AddQ("Как отправить экшен?", ["useDispatch()()", "dispatch()", "store.dispatch(action)", "send(action)"], "useDispatch()()");
        AddQ("Как создать экшен с payload в slice?", ["reducers: { myAction: (state, action) => {} }", "actions: { myAction }", "reducers.myAction = (state, action) => {}", "payload: myAction"], "reducers: { myAction: (state, action) => {} }");
        AddQ("Можно ли мутировать state в Redux Toolkit?", ["Да, Immer позволяет", "Нет, только иммутабельно", "Только через createAction", "Через spread"], "Да, Immer позволяет");
        AddQ("Как добавить middleware в configureStore?", ["getDefaultMiddleware().concat(middleware)", "middleware: [mw]", "middlewares: [mw]", "addMiddleware(mw)"], "getDefaultMiddleware().concat(middleware)");
        AddQ("Что такое extraReducers?", ["Редьюсеры для внешних экшенов (createAsyncThunk)", "Дополнительные редьюсеры", "Редьюсеры по умолчанию", "Асинхронные редьюсеры"], "Редьюсеры для внешних экшенов (createAsyncThunk)");
        AddQ("Как типизировать useSelector?", ["(state: RootState) => state.value", "useSelector<RootState>('value')", "useSelector<ReturnType<typeof store.getState>>(state => state.value)", "useSelector typed"], "(state: RootState) => state.value");

        // Test 19: Redux Async
        AddQ("Что создаёт createAsyncThunk?", ["Три экшена: pending/fulfilled/rejected", "Один экшен", "Slice", "Reducer"], "Три экшена: pending/fulfilled/rejected");
        AddQ("Где обрабатываются экшены createAsyncThunk?", ["В extraReducers", "В reducers", "В middleware", "В thunks"], "В extraReducers");
        AddQ("Как обработать успешный ответ thunk?", [".addCase(fetchUsers.fulfilled, (state, action) => {})", ".addCase(fetchUsers.done, ...)", ".addCase(fetchUsers.ok, ...)", ".addCase(fetchUsers.success, ...)"], ".addCase(fetchUsers.fulfilled, (state, action) => {})");
        AddQ("Что такое builder в extraReducers?", ["Объект для цепочки addCase", "Конструктор", "Функция", "Callback"], "Объект для цепочки addCase");
        AddQ("Как типизировать возвращаемое значение thunk?", ["Тип в дженерике createAsyncThunk<ReturnType, ArgType>", "Автоматически", "Через ReturnType", "Через Promise<Type>"], "Тип в дженерике createAsyncThunk<ReturnType, ArgType>");
        AddQ("Как передать аргумент в thunk?", ["dispatch(thunk(arg))", "thunk.dispatch(arg)", "thunk(arg)", "thunk.arg()"], "dispatch(thunk(arg))", QuestionType.MultipleChoice);
        AddQ("Как обработать ошибку в thunk?", ["return rejectWithValue(error)", "throw error", "return thunk.reject(error)", "return { error }"], "return rejectWithValue(error)");
        AddQ("Где лучше делать асинхронные запросы в Redux?", ["В createAsyncThunk", "В редьюсере", "В компоненте useEffect", "В middleware"], "В createAsyncThunk");
        AddQ("Как обновить состояние после rejected?", [".addCase(thunk.rejected, (state, action) => { state.error = action.payload })", "Ошибка обрабатывается автоматически", "В extraReducers.rejected", "В rejected"], ".addCase(thunk.rejected, (state, action) => { state.error = action.payload })");
        AddQ("Что такое matcher в extraReducers?", ["Функция для группировки нескольких экшенов", "Утилита", "Тип", "Middleware"], "Функция для группировки нескольких экшенов");

        // Test 20: Тестирование
        AddQ("Какая библиотека используется для тестирования React-компонентов?", ["@testing-library/react", "Jest", "Mocha", "Chai"], "@testing-library/react");
        AddQ("Что делает render из RTL?", ["Рендерит компонент в jsdom", "Рендерит на сервере", "Компилирует компонент", "Маунтит в браузере"], "Рендерит компонент в jsdom");
        AddQ("Как найти элемент по тексту?", ["screen.getByText('text')", "findByText('text')", "querySelector('text')", "getElementByText('text')"], "screen.getByText('text')");
        AddQ("Как симулировать клик?", ["fireEvent.click(element)", "element.click()", "simulateClick(element)", "click(element)"], "fireEvent.click(element)");
        AddQ("Как найти элемент по роли?", ["screen.getByRole('button')", "findByRole('button')", "getElementByRole('button')", "getByRole('button')"], "screen.getByRole('button')");
        AddQ("Что делает userEvent?", ["Симулирует более реалистичные события, чем fireEvent", "Событие мыши", "Событие клавиатуры", "Ивент"], "Симулирует более реалистичные события, чем fireEvent");
        AddQ("Как проверить, что элемент не отрендерился?", ["expect(screen.queryByText('text')).toBeNull()", "expect.not(screen.getByText('text'))", "screen.assertNoText('text')", "expect(screen.getByText('text')).not.toBeInTheDocument()"], "expect(screen.queryByText('text')).toBeNull()");
        AddQ("Как обернуть компонент в Provider для теста?", ["render(<Provider><Component /></Provider>)", "wrapInProvider(Component)", "Provider.render(Component)", "mockProvider(Component)"], "render(<Provider><Component /></Provider>)");
        AddQ("Как протестировать хук?", ["renderHook из @testing-library/react", "HookTest компонент", "callHook()", "testHook()"], "renderHook из @testing-library/react");
        AddQ("Как проверить, что функция была вызвана с определёнными аргументами?", ["expect(mock).toHaveBeenCalledWith(arg)", "mock.calls.includes(arg)", "assertCalled(mock, arg)", "verify(mock, arg)"], "expect(mock).toHaveBeenCalledWith(arg)");

        // Test 21: Оптимизация
        AddQ("Как лениво загрузить компонент?", ["React.lazy(() => import('./Component'))", "dynamic import()", "lazy(() => Component)", "Suspense(Component)"], "React.lazy(() => import('./Component'))");
        AddQ("Что рендерит Suspense пока грузится lazy-компонент?", ["fallback пропс", "spinner", "null", "loading"], "fallback пропс");
        AddQ("Что такое code splitting?", ["Разделение кода на чанки", "Удаление мёртвого кода", "Минификация", "Обфускация"], "Разделение кода на чанки");
        AddQ("Как использовать React.lazy с роутингом?", ["lazy(() => import('./Pages/Home')) внутри Route element", "Только на главной", "Вне BrowserRouter", "В index.js"], "lazy(() => import('./Pages/Home')) внутри Route element");
        AddQ("Что измеряет Profiler?", ["Производительность рендера компонента", "Размер бандла", "Скорость сети", "Потребление памяти"], "Производительность рендера компонента");
        AddQ("Как избежать re-render при изменении пропсов родителя?", ["React.memo + useCallback/useMemo", "shouldComponentUpdate", "PureComponent", "useEffect"], "React.memo + useCallback/useMemo");
        AddQ("Что такое виртуализация списков?", ["Рендеринг только видимых элементов", "Рендеринг в виртуальном DOM", "Кэширование списков", "Фрагментация"], "Рендеринг только видимых элементов");
        AddQ("Какая библиотека помогает с виртуализацией?", ["react-window или react-virtuoso", "react-list", "virtual-list", "react-virtualized"], "react-window или react-virtuoso");
        AddQ("Что такое bundle-анализ?", ["Проверка размера бандла (webpack-bundle-analyzer)", "Анализ кода", "Проверка ошибок", "Линтинг"], "Проверка размера бандла (webpack-bundle-analyzer)");
        AddQ("Как удалить неиспользуемый код?", ["Tree shaking (dead code elimination)", "Минификация", "Uglify", "Обфускация"], "Tree shaking (dead code elimination)");

        // Test 22: Сборка и деплой
        AddQ("Какая команда собирает production-бандл Vite?", ["vite build", "npm run build", "vite production", "npm run prod"], "vite build");
        AddQ("Что делает Docker в контексте React?", ["Упаковывает приложение с nginx", "Компилирует код", "Запускает тесты", "Дебажит"], "Упаковывает приложение с nginx");
        AddQ("Как настроить переменные окружения в Vite?", ["VITE_API_URL в .env файле", "REACT_APP_API_URL", "NEXT_PUBLIC_API_URL", "API_URL"], "VITE_API_URL в .env файле");
        AddQ("Что делает nginx в production-сборке React?", ["Раздаёт статику и проксирует API", "Компилирует React", "Сервер рендеринга", "База данных"], "Раздаёт статику и проксирует API");
        AddQ("Как деплоить на Vercel?", ["Подключить Git-репозиторий → авто-деплой", "Загрузить ZIP", "Через FTP", "SCP файлов"], "Подключить Git-репозиторий → авто-деплой");
        AddQ("Что такое CI/CD?", ["Непрерывная интеграция и доставка", "Компиляция", "Тестирование", "Документация"], "Непрерывная интеграция и доставка");
        AddQ("Как сделать preview-деплой для PR?", ["Vercel создаёт preview для каждого PR", "Вручную загрузить", "Невозможно", "Через GitHub Pages"], "Vercel создаёт preview для каждого PR");
        AddQ("Как кэшировать node_modules в Docker?", ["COPY package*.json && RUN npm ci", "COPY node_modules", "RUN npm cache", "ENV NODE_ENV=production"], "COPY package*.json && RUN npm ci");
        AddQ("Что выдает vite build при успешной сборке?", ["Папку dist со статическими файлами", "server.js", "bundle.js", "index.html"], "Папку dist со статическими файлами");
        AddQ("Как проверить размер бандла после сборки?", ["vite build --analyze", "ls -la dist", "npm run analyze", "webpack-bundle-analyzer"], "vite build --analyze");

        // Test 23: Next.js основы
        AddQ("Чем отличается SSR от CSR?", ["SSR рендерит на сервере, CSR в браузере", "Только названием", "SSR быстрее CSR", "CSR безопаснее"], "SSR рендерит на сервере, CSR в браузере");
        AddQ("Что такое App Router в Next.js 13+?", ["Новый маршрутизатор на основе файловой системы", "Клиентский роутер", "Серверный роутер", "Роутер для API"], "Новый маршрутизатор на основе файловой системы");
        AddQ("Как создать серверный компонент в Next.js?", ["Просто export default function в app/", "Добавить 'use server'", "Добавить 'use client'", "extends ServerComponent"], "Просто export default function в app/");
        AddQ("Что такое SSG?", ["Статическая генерация на этапе сборки", "Динамический рендеринг", "Серверная генерация", "Гибрид"], "Статическая генерация на этапе сборки");
        AddQ("Как сделать страницу динамической (SSR)?", ["export const dynamic = 'force-dynamic'", "use server", "getServerSideProps", "export async function generateStaticParams()"], "export const dynamic = 'force-dynamic'");
        AddQ("Как загружать данные в серверном компоненте?", ["async компонент с await fetch()", "useEffect", "getServerSideProps", "useSWR"], "async компонент с await fetch()");
        AddQ("Что такое layout в Next.js?", ["Обёртка для дочерних страниц", "Стиль страницы", "Шаблон", "Компонент"], "Обёртка для дочерних страниц");
        AddQ("Как создать клиентский компонент в Next.js?", ["'use client' в начале файла", "'use client' не нужен", "ClientComponent wrapper", "addClientDirective()"], "'use client' в начале файла");
        AddQ("Что такое ISR?", ["Инкрементальная статическая регенерация", "Серверный рендеринг", "Статическая генерация", "Динамический импорт"], "Инкрементальная статическая регенерация");
        AddQ("Как настроить ISR в Next.js?", ["export const revalidate = 60", "revalidate: 60", "isr: true", "cache: 'force-cache'"], "export const revalidate = 60");

        // Test 24: Next.js роутинг
        AddQ("Как создать API route в Next.js App Router?", ["app/api/route.ts с export async function GET()", "api/route.js", "pages/api/route.js", "server/api.ts"], "app/api/route.ts с export async function GET()");
        AddQ("Как создать динамический маршрут [id]?", ["app/users/[id]/page.tsx", "app/users/:id/page.tsx", "app/users/_id/page.tsx", "app/users/$id/page.tsx"], "app/users/[id]/page.tsx");
        AddQ("Что такое middleware в Next.js?", ["Функция, выполняемая перед запросом", "Компонент", "Плагин", "Конфиг"], "Функция, выполняемая перед запросом");
        AddQ("Как редиректить в Next.js middleware?", ["return NextResponse.redirect(new URL(url, request.url))", "redirect()", "res.redirect()", "navigate()"], "return NextResponse.redirect(new URL(url, request.url))");
        AddQ("Где лежит файл middleware в Next.js?", ["middleware.ts в корне проекта", "app/middleware.ts", "src/middleware.ts", "pages/middleware.ts"], "middleware.ts в корне проекта");
        AddQ("Как получить searchParams в серверном компоненте?", ["props.searchParams", "useSearchParams()", "getSearchParams()", "searchParams из контекста"], "props.searchParams");
        AddQ("Как сделать catch-all маршрут [...slug]?", ["app/shop/[...slug]/page.tsx", "app/shop/*/page.tsx", "app/shop/__slug__/page.tsx", "app/shop/:slug/page.tsx"], "app/shop/[...slug]/page.tsx");
        AddQ("Как генерировать статические параметры?", ["export async function generateStaticParams()", "getStaticPaths()", "generateParams()", "staticPaths()"], "export async function generateStaticParams()");
        AddQ("Что делает notFound() в Next.js?", ["Рендерит страницу 404", "Редиректит на 404", "Бросает ошибку", "Возвращает null"], "Рендерит страницу 404");
        AddQ("Как задать метаданные страницы в App Router?", ["export const metadata = { title: '...' }", "Head компонент", "next/head", "useMetadata()"], "export const metadata = { title: '...' }");

        // Test 25: DnD и анимации
        AddQ("Какая библиотека популярна для Drag and Drop в React?", ["@dnd-kit/core", "react-dnd", "dragula", "sortablejs"], "@dnd-kit/core");
        AddQ("Какой компонент делает элемент перетаскиваемым в dnd-kit?", ["useDraggable", "DndContext", "Draggable", "DragOverlay"], "useDraggable");
        AddQ("Какая библиотека популярна для анимаций в React?", ["framer-motion", "animate.css", "react-spring", "gsap"], "framer-motion");
        AddQ("Какой хук в framer-motion для анимации появления?", ["useAnimate", "useAnimation", "motion.div с initial/animate", "animate()"], "motion.div с initial/animate");
        AddQ("Как сделать анимацию при размонтировании в framer-motion?", ["AnimatePresence + exit", "onUnmount", "exitAnimation", "leave"], "AnimatePresence + exit");
        AddQ("Как сделать сортировку элементов в dnd-kit?", ["@dnd-kit/sortable + useSortable", "SortableContainer", "DragSort", "SortableList"], "@dnd-kit/sortable + useSortable");
        AddQ("Какой компонент отображает перетаскиваемый элемент поверх других?", ["DragOverlay", "DragPreview", "DragClone", "DragShadow"], "DragOverlay");
        AddQ("Какая CSS-анимация подходит для accordion?", ["height переход с overflow: hidden", "opacity", "transform scale", "display none/block"], "height переход с overflow: hidden");
        AddQ("Как анимировать переход между страницами в Next.js?", ["framer-motion + AnimatePresence в layout", "CSS transitions", "next/router events", "page transitions"], "framer-motion + AnimatePresence в layout");
        AddQ("Что такое layout animation в framer-motion?", ["Анимация при перестановке элементов с layout prop", "Анимация макета", "Анимация сетки", "CSS Grid анимация"], "Анимация при перестановке элементов с layout prop");

        db.TestQuestions.AddRange(allQuestions);
        await db.SaveChangesAsync();

        // ── Link lectures to tests ──
        var savedLectures = await db.Lectures.Where(l => l.CourseId == course.Id).OrderBy(l => l.Order).ToListAsync();
        var savedCourseTests = await db.Tests.Where(t => t.CourseId == course.Id).OrderBy(t => t.Title).ToListAsync();
        for (int i = 0; i < Math.Min(savedLectures.Count, savedCourseTests.Count); i++)
        {
            savedLectures[i].TestId = savedCourseTests[i].Id;
        }
        await db.SaveChangesAsync();

        // ── CourseGroup links ──
        db.CourseGroups.AddRange(
            new CourseGroup
            {
                Id = Guid.Parse("c5000000-0000-0000-0000-000000000001"),
                CourseId = course.Id,
                GroupId = group31.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new CourseGroup
            {
                Id = Guid.Parse("c5000000-0000-0000-0000-000000000002"),
                CourseId = course.Id,
                GroupId = group32.Id,
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 3,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 3,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 2,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                NumberPair = 1,
                Weeks = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
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
                ImageUrl = "https://picsum.photos/seed/college-year/1200/600",
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
                ImageUrl = "https://picsum.photos/seed/open-day/1200/600",
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
                ImageUrl = "https://picsum.photos/seed/hackathon/1200/600",
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
                ImageUrl = "https://picsum.photos/seed/schedule/1200/600",
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
                ImageUrl = "https://picsum.photos/seed/best-student/1200/600",
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

    private static async Task ImportWordPressDataAsync(AppDbContext db)
    {
        var newsCount = await db.News.CountAsync();
        if (newsCount >= 100)
            return;

        string[] jsonPaths =
        [
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "import", "wp_data_full.json"),
            "/import/wp_data_full.json",
            Path.Combine(AppContext.BaseDirectory, "import", "wp_data_full.json"),
        ];

        string? jsonPath = null;
        foreach (var p in jsonPaths)
        {
            if (File.Exists(p))
            {
                jsonPath = p;
                break;
            }
        }

        if (jsonPath == null)
            return;

        try
        {
            var jsonBytes = await File.ReadAllBytesAsync(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            var root = doc.RootElement;

            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@collegelms.ru");

            var wpCategoryMap = new Dictionary<int, Guid>();

            if (root.TryGetProperty("categories", out var categoriesEl))
            {
                foreach (var cat in categoriesEl.EnumerateArray())
                {
                    var wpId = cat.GetProperty("id").GetInt32();
                    var name = cat.GetProperty("name").GetString() ?? "";
                    var slug = cat.GetProperty("slug").GetString() ?? "";

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var existing = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == slug);
                    if (existing != null)
                    {
                        wpCategoryMap[wpId] = existing.Id;
                        continue;
                    }

                    var entity = new NewsCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = name.Trim(),
                        Slug = slug,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    db.NewsCategories.Add(entity);
                    wpCategoryMap[wpId] = entity.Id;
                }
                await db.SaveChangesAsync();
            }

            if (!root.TryGetProperty("posts", out var postsEl))
                return;

            var imported = 0;
            foreach (var post in postsEl.EnumerateArray())
            {
                var slug = post.GetProperty("slug").GetString() ?? "";
                if (await db.News.AnyAsync(n => n.Slug == slug))
                    continue;

                var title = post.GetProperty("title").GetProperty("rendered").GetString() ?? "";
                var contentHtml =
                    post.GetProperty("content").GetProperty("rendered").GetString() ?? "";
                var dateStr = post.GetProperty("date").GetString() ?? "";
                var status = post.GetProperty("status").GetString() ?? "";

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                DateTime publishedAt = DateTime.TryParse(dateStr, out var dt)
                    ? dt
                    : DateTime.UtcNow;

                string? imageUrl = null;
                if (
                    post.TryGetProperty("_embedded", out var embedded)
                    && embedded.TryGetProperty("wp:featuredmedia", out var media)
                    && media.GetArrayLength() > 0
                )
                {
                    var mediaObj = media[0];
                    if (
                        mediaObj.TryGetProperty("source_url", out var src)
                        && src.ValueKind == System.Text.Json.JsonValueKind.String
                    )
                    {
                        imageUrl = src.GetString();
                    }
                }

                Guid? categoryId = null;
                if (post.TryGetProperty("categories", out var catIds))
                {
                    foreach (var cid in catIds.EnumerateArray())
                    {
                        var wpId = cid.GetInt32();
                        if (wpCategoryMap.TryGetValue(wpId, out var mappedId))
                        {
                            categoryId = mappedId;
                            break;
                        }
                    }
                }

                db.News.Add(
                    new News
                    {
                        Id = Guid.NewGuid(),
                        Title = title
                            .Replace("&#8212;", "—")
                            .Replace("&#8211;", "–")
                            .Replace("&amp;", "&")
                            .Replace("&laquo;", "«")
                            .Replace("&raquo;", "»")
                            .Trim(),
                        Content = contentHtml,
                        Slug = slug,
                        ImageUrl = imageUrl,
                        CategoryId = categoryId,
                        IsPublished = status == "publish",
                        PublishedAt = publishedAt,
                        IsDeleted = false,
                        CreatedById = admin?.Id ?? Guid.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
                imported++;

                if (imported % 100 == 0)
                    await db.SaveChangesAsync();
            }

            await db.SaveChangesAsync();
        }
        catch
        {
            // silent — import is best-effort
        }
    }
}
