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

/// <summary>
/// Пакетные корректировки: список с пагинацией, импорт XLSX, применение и расписание дня
/// (UC-SCH-18–27).
/// </summary>
public class CorrectionBatchControllerTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static readonly DateTime TestDate = new(2026, 9, 8);

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

    private async Task<Guid> SeedEntryAsync(Guid groupId, Guid teacherId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var utcNow = DateTime.UtcNow;
        var entry = new ScheduleEntry
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            TeacherId = teacherId,
            Subject = "Физика",
            Room = "301",
            DayOfWeek = DayOfWeek.Tuesday,
            NumberPair = 2,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Weeks = [2],
            LessonType = LessonType.None,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        db.ScheduleEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry.Id;
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

    private async Task<T?> DeserializeBodyAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private async Task<Guid> CreateBatchAsync()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/schedule/correction/batches",
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate }
        );
        response.EnsureSuccessStatusCode();
        var body = await DeserializeBodyAsync<Result<CorrectionBatchResponse>>(response);
        return body!.Data!.Id;
    }

    private async Task AddPositionAsync(Guid batchId, Guid groupId, Guid teacherId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/schedule/correction/batches/{batchId}/positions",
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = groupId,
                GroupName = "ПО-262",
                NumberPair = 3,
                Subject = "Математика",
                TeacherId = teacherId,
                TeacherName = "Марченко И.А.",
            }
        );
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetBatches_ReturnsPagedResponse()
    {
        SetAuthHeader(GetToken(UserRole.Admin));
        await CreateBatchAsync();

        var response = await Client.GetAsync("/api/schedule/correction/batches?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeBodyAsync<Result<PagedResponse<CorrectionBatchResponse>>>(
            response
        );
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Single(body.Data!.Items);
        Assert.Equal(1, body.Data.TotalCount);
        Assert.Equal(1, body.Data.Page);
        Assert.Equal(1, body.Data.PageSize);
        Assert.Equal(1, body.Data.TotalPages);
    }

    [Fact]
    public async Task Import_MultipartXlsx_CreatesBatch()
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

        var response = await Client.PostAsync("/api/schedule/correction/batches/import", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeBodyAsync<Result<CorrectionImportResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.NotNull(body.Data!.BatchId);
        Assert.NotEmpty(body.Data.Positions);
    }

    [Fact]
    public async Task Apply_ValidBatch_SucceedsThenReturnsConflict()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        SetAuthHeader(GetToken(UserRole.Admin));
        var batchId = await CreateBatchAsync();
        await AddPositionAsync(batchId, group.Id, teacher.Id);

        var first = await Client.PostAsync(
            $"/api/schedule/correction/batches/{batchId}/apply",
            content: null
        );
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await DeserializeBodyAsync<Result<CorrectionApplyResult>>(first);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal(1, body.Data!.Applied);

        var second = await Client.PostAsync(
            $"/api/schedule/correction/batches/{batchId}/apply",
            content: null
        );
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetCorrectionDay_ReturnsBaseAndPendingEntries()
    {
        var (group, teacher) = await SeedGroupAndTeacherAsync();
        await SeedEntryAsync(group.Id, teacher.Id);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var batchId = Guid.NewGuid();
            var utcNow = DateTime.UtcNow;
            db.CorrectionBatches.Add(
                new CorrectionBatch
                {
                    Id = batchId,
                    CorrectionDate = TestDate,
                    Week = 2,
                    DayOfWeek = (int)DayOfWeek.Tuesday,
                    Status = CorrectionBatchStatus.Draft,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            db.CorrectionPositions.Add(
                new CorrectionPosition
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    Row = 1,
                    ChangeType = ScheduleChangeType.Add,
                    GroupId = group.Id,
                    GroupName = group.Name,
                    DayOfWeek = (int)DayOfWeek.Tuesday,
                    Week = 2,
                    NumberPair = 4,
                    Subject = "Математика",
                    TeacherId = teacher.Id,
                    TeacherName = teacher.User.FullName,
                    Status = CorrectionPositionStatus.Draft,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        SetAuthHeader(GetToken(UserRole.Admin));

        var response = await Client.GetAsync(
            $"/api/schedule/correction/day?groupId={group.Id}&date=2026-09-08"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeBodyAsync<Result<CorrectionDayResponse>>(response);
        Assert.NotNull(body);
        Assert.True(body!.IsSuccess);
        Assert.Equal("ПО-262", body.Data!.GroupName);
        Assert.Contains(body.Data.Entries, e => e.NumberPair == 2 && e.Subject == "Физика");
    }
}
