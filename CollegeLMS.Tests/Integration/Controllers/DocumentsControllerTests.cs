using System.Net;
using System.Net.Http.Headers;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

/// <summary>Панель документов диспетчера: шаблоны и выгрузка итогового расписания.</summary>
public class DocumentsControllerTests : BaseIntegrationTest
{
    private string GetToken(UserRole role)
    {
        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{role}@test.ru",
            FullName = role.ToString(),
            PasswordHash = "hash",
            Role = role,
        };
        return tokenService.GenerateAccessToken(user);
    }

    private void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<(Group Group, Teacher Teacher)> SeedGroupWithLessonAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "РЭУ-252",
            Course = 2,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Groups.Add(group);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Guid.NewGuid() + "@test.ru",
            FullName = "Сапрыкина А.А.",
            PasswordHash = "hash",
            Role = UserRole.Teacher,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CyclicalCommission = "Не указана",
            Position = "Преподаватель",
            CreatedAt = now,
            UpdatedAt = now,
            User = user,
        };
        db.Users.Add(user);
        db.Teachers.Add(teacher);

        db.ScheduleEntries.Add(
            new ScheduleEntry
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                TeacherId = teacher.Id,
                Subject = "Математика",
                Room = "233",
                DayOfWeek = DayOfWeek.Tuesday,
                NumberPair = 1,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                Weeks = [1, 2],
                CreatedAt = now,
                UpdatedAt = now,
            }
        );
        await db.SaveChangesAsync();
        return (group, teacher);
    }

    [Fact]
    public async Task DownloadSchedule_ReturnsImportFormatXlsx()
    {
        await SeedGroupWithLessonAsync();
        SetAuthHeader(GetToken(UserRole.Dispatcher));

        var response = await Client.GetAsync("/api/dispatcher/documents/schedule.xlsx");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);

        // Имя с кириллицей приходит в RFC 5987 (`filename*`), а в ASCII-дублере
        // Framework заменяет не-ASCII символы подчёркиваниями — как и при экспорте корректировок.
        var disposition = response.Content.Headers.GetValues("Content-Disposition").Single();
        Assert.Contains("filename*=UTF-8''", disposition);
        Assert.Contains("%D0%A0%D0%B0%D1%81", disposition);
        Assert.Matches(@"_\d{6}_\d{4}\.xlsx", disposition);

        // Файл должен разбираться тем же парсером, что и исходный шаблон.
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parser = new ScheduleImportService(db, new BellScheduleServiceStub());

        var (entries, errors) = parser.ParseScheduleMatrix(workbook);
        Assert.Empty(errors);
        var entry = Assert.Single(entries);
        Assert.Equal("РЭУ-252", entry.GroupName);
        Assert.Equal("Математика", entry.Subject);
        Assert.Equal("233", entry.Room);
        Assert.Equal("Сапрыкина А.А.", entry.TeacherName);
        Assert.Equal(new[] { 1, 2 }, entry.Weeks);
    }

    [Fact]
    public async Task DownloadSchedule_ReturnsBadRequestWhenScheduleIsEmpty()
    {
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync("/api/dispatcher/documents/schedule.xlsx");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadSchedule_RequiresDispatcherRole()
    {
        await SeedGroupWithLessonAsync();
        SetAuthHeader(GetToken(UserRole.Student));

        var response = await Client.GetAsync("/api/dispatcher/documents/schedule.xlsx");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTemplates_ListsScheduleDocuments()
    {
        SetAuthHeader(GetToken(UserRole.Dispatcher));

        var response = await Client.GetAsync("/api/dispatcher/documents/templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Расписание.xlsx", body);
    }
}
