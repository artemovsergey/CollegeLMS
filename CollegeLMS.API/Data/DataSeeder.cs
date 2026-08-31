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
        await SeedMdk0901CourseAsync(db);
        await SeedNewsCategoriesAsync(db);
        await SeedNewsAsync(db);
        await ImportWordPressDataAsync(db);
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                Email = "dispatcher@collegelms.ru",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("dispatcher"),
                FullName = "Диспетчер Системы",
                Role = UserRole.Dispatcher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

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
        var group = new Group
        {
            Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"),
            Name = "ИСП-31",
            Course = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        if (!await db.Groups.AnyAsync(g => g.Name == group.Name))
            db.Groups.Add(group);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTeachersAsync(AppDbContext db)
    {
        if (await db.Teachers.AnyAsync())
            return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "teacher@collegelms.ru");
        if (user == null)
            return;

        db.Teachers.Add(
            new Teacher
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000001"),
                UserId = user.Id,
                CyclicalCommission = "Информационных технологий",
                Position = "Преподаватель высшей категории",
                Category = Entities.Enums.TeacherCategory.Higher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentsAsync(AppDbContext db)
    {
        if (await db.Students.AnyAsync())
            return;

        var group = await db.Groups.FirstAsync(g => g.Name == "ИСП-31");
        var petrov = await db.Users.FirstAsync(u => u.Email == "student@collegelms.ru");

        db.Students.Add(
            new Student
            {
                Id = Guid.Parse("b3000000-0000-0000-0000-000000000001"),
                UserId = petrov.Id,
                GroupId = group.Id,
                RecordBookNumber = "ЗК-2025-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedNewsCategoriesAsync(AppDbContext db)
    {
        if (await db.NewsCategories.AnyAsync())
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

        db.NewsCategories.AddRange(categories);
        await db.SaveChangesAsync();
    }

    private static async Task SeedNewsAsync(AppDbContext db)
    {
        if (await db.News.AnyAsync(n => n.Slug == "nachalo-uchebnogo-goda-2026-2027"))
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
                    "Студенты группы ИСП-31 заняли I место в региональном хакатоне «Цифровой прорыв»! "
                    + "Команда разработала сервис для мониторинга экологической обстановки. Поздравляем!",
                ImageUrl = "https://picsum.photos/seed/hackathon/1200/600",
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
                    + "Актуальное расписание доступно в разделе «Расписание».",
                ImageUrl = "https://picsum.photos/seed/schedule/1200/600",
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

    private static async Task SeedMdk0901CourseAsync(AppDbContext db)
    {
        if (
            await db.Courses.AnyAsync(c =>
                c.Title == "МДК 09.01 Проектирование и разработка веб-приложений"
            )
        )
            return;

        string[] jsonPaths =
        [
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "import",
                "mdk0901_course.json"
            ),
            "/import/mdk0901_course.json",
            Path.Combine(AppContext.BaseDirectory, "import", "mdk0901_course.json"),
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

            var courseEl = root.GetProperty("course");
            var teacherEmail = root.GetProperty("teacherEmail").GetString() ?? "";
            var groupName = root.GetProperty("groupName").GetString() ?? "";

            var teacherUser = await db.Users.FirstOrDefaultAsync(u => u.Email == teacherEmail);
            if (teacherUser == null)
                return;

            var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUser.Id);
            if (teacher == null)
                return;

            var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group == null)
                return;

            var course = new Course
            {
                Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"),
                Title = courseEl.GetProperty("title").GetString() ?? "МДК 09.01",
                Description = courseEl.GetProperty("description").GetString() ?? string.Empty,
                TeacherId = teacher.Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            if (!root.TryGetProperty("lessons", out var lessonsEl))
                return;

            foreach (var item in lessonsEl.EnumerateArray())
            {
                var title = item.GetProperty("title").GetString() ?? "";
                var content = item.GetProperty("content").GetString() ?? "";
                var order = item.GetProperty("order").GetInt32();
                var kindStr = item.GetProperty("kind").GetString() ?? "Lecture";

                var kind = Enum.TryParse<LessonKind>(kindStr, out var parsed)
                    ? parsed
                    : LessonKind.Lecture;

                db.Lessons.Add(
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        Title = title,
                        Content = content,
                        Order = order,
                        Kind = kind,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
            }

            db.CourseGroups.Add(
                new CourseGroup
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    GroupId = group.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );

            await db.SaveChangesAsync();
        }
        catch
        {
            // silent — seed is best-effort
        }
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
