using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Response;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Integration.Controllers;

public class ScheduleCorrectionControllerTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

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

    private async Task<(Group Group, Teacher Teacher)> SeedGroupAndTeacherAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО-262",
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        db.Groups.Add(group);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.ru",
            FullName = "Марченко И.А.",
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
        db.Teachers.Add(teacher);

        await db.SaveChangesAsync();
        return (group, teacher);
    }

    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> fill)
    {
        var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Корректировка");
        ws.Cell(3, 1).Value = "Корректировка на 08.09.2026 г.";
        ws.Cell(5, 1).Value = "Группа";
        ws.Cell(5, 2).Value = "Снимается по расписанию";
        ws.Cell(5, 3).Value = "Преподаватель";
        ws.Cell(5, 4).Value = "Вводится в расписание";
        ws.Cell(5, 5).Value = "Преподаватель";
        ws.Cell(5, 6).Value = "№ пары";
        ws.Cell(5, 7).Value = "Примечание";
        fill(ws);

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        workbook.Dispose();
        return ms;
    }

    private async Task<T?> DeserializeWithEnumsAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    // --- Задача 9.1: превью ---

    [Fact]
    public async Task PreviewCorrection_ValidFile_ReturnsEntries()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        SetAuthHeader(GetToken(UserRole.Admin));

        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(7, 1).Value = group.Name;
            ws.Cell(7, 4).Value = "Математика";
            ws.Cell(7, 5).Value = teacher.User.FullName;
            ws.Cell(7, 6).Value = 4;
        });
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        );
        form.Add(fileContent, "file", "correction.xlsx");

        var response = await Client.PostAsync("/api/schedule/correction/preview", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<CorrectionPreviewResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Empty(body.Data!.Errors);
        Assert.True(body.Data!.TotalEntries > 0);
        Assert.Equal(2, body.Data!.Week);
        Assert.Equal(ScheduleChangeType.Add, body.Data!.Entries[0].ChangeType);
    }

    [Fact]
    public async Task PreviewCorrection_BrokenFile_ReturnsBadRequest()
    {
        SetAuthHeader(GetToken(UserRole.Admin));

        using var stream = new MemoryStream(new byte[] { 9, 8, 7, 6, 5 });
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        );
        form.Add(fileContent, "file", "broken.xlsx");

        var response = await Client.PostAsync("/api/schedule/correction/preview", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Задача 9.2: подтверждение ---

    [Fact]
    public async Task ConfirmCorrection_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/schedule/correction/confirm",
            new CorrectionConfirmRequest()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmCorrection_StudentToken_ReturnsForbidden()
    {
        SetAuthHeader(GetToken(UserRole.Student));

        var response = await Client.PostAsJsonAsync(
            "/api/schedule/correction/confirm",
            new CorrectionConfirmRequest()
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmCorrection_PersistsEntriesAndHistory()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.PostAsJsonAsync(
            "/api/schedule/correction/confirm",
            new CorrectionConfirmRequest
            {
                Entries =
                [
                    new CorrectionPreviewEntry
                    {
                        GroupId = group.Id,
                        ChangeType = ScheduleChangeType.Add,
                        DayOfWeek = 2,
                        Week = 2,
                        NumberPair = 4,
                        Subject = "Математика",
                        TeacherId = teacher.Id,
                        TeacherName = teacher.User.FullName,
                        Note = "вм. 4 п",
                    },
                ],
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<CorrectionConfirmResult>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(1, body.Data!.Applied);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = Assert.Single(db.ScheduleEntries);
        Assert.Equal(4, entry.NumberPair);
        Assert.Equal([2], entry.Weeks);

        var history = Assert.Single(db.ScheduleHistory);
        Assert.Equal(ScheduleChangeType.Add, history.ChangeType);
        Assert.Equal(group.Id, history.GroupId);
    }

    // --- Задача 9.3: история ---

    [Fact]
    public async Task GetHistory_FilterByGroupId_ReturnsItems()
    {
        var groupId = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var utcNow = DateTime.UtcNow;
            var group = new Group
            {
                Id = groupId,
                Name = "ПО-262",
                Course = 2,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            db.Groups.Add(group);
            db.ScheduleHistory.AddRange(
                new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Add,
                    AppliedAt = utcNow,
                    AppliedByUserId = Guid.NewGuid(),
                    GroupId = groupId,
                    Group = group,
                    Subject = "Математика",
                    DayOfWeek = DayOfWeek.Tuesday,
                    NumberPair = 4,
                    Week = 2,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                },
                new ScheduleHistory
                {
                    Id = Guid.NewGuid(),
                    ChangeType = ScheduleChangeType.Remove,
                    AppliedAt = utcNow,
                    AppliedByUserId = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    Subject = "История",
                    DayOfWeek = DayOfWeek.Tuesday,
                    NumberPair = 5,
                    Week = 2,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            await db.SaveChangesAsync();
        }
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync($"/api/schedule/history?groupId={groupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleHistoryResponse>>>(
            response
        );
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(1, body.Data!.Items.Count);
        Assert.Equal("ПО-262", body.Data!.Items[0].GroupName);
    }

    [Fact]
    public async Task GetHistory_Pagination_ReturnsPage()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var utcNow = DateTime.UtcNow;
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = "ПО-262",
                Course = 2,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };
            db.Groups.Add(group);
            for (var i = 0; i < 3; i++)
            {
                db.ScheduleHistory.Add(
                    new ScheduleHistory
                    {
                        Id = Guid.NewGuid(),
                        ChangeType = ScheduleChangeType.Add,
                        AppliedAt = utcNow.AddMinutes(i),
                        AppliedByUserId = Guid.NewGuid(),
                        GroupId = group.Id,
                        Group = group,
                        Subject = $"Предмет {i}",
                        DayOfWeek = DayOfWeek.Tuesday,
                        NumberPair = 1 + i,
                        Week = 2,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow,
                    }
                );
            }
            await db.SaveChangesAsync();
        }
        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync("/api/schedule/history?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeWithEnumsAsync<Result<PagedResponse<ScheduleHistoryResponse>>>(
            response
        );
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(2, body.Data!.Items.Count);
        Assert.Equal(3, body.Data!.TotalCount);
    }
}
