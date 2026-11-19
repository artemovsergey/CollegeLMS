using System.Net.Http;
using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Services;
using CollegeLMS.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.Tests.Unit.Services;

/// <summary>
/// Валидация позиций корректировки для операции «Экзамен»: пара 0 допустима
/// только для экзамена, предмет должен быть «ЭКЗАМЕН», остальные типы — пары 1–7.
/// </summary>
public class ExamCorrectionValidationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CorrectionBatchService _service;

    private static readonly DateTime TestDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

    public ExamCorrectionValidationTests()
    {
        _db = TestDbContextFactory.Create();
        var engine = new CorrectionApplyEngine(_db, new BellScheduleServiceStub());
        var maxBot = new MaxBotHttpClient(
            new HttpClient(),
            new ConfigurationBuilder().Build(),
            NullLogger<MaxBotHttpClient>.Instance
        );
        var correctionService = new ScheduleCorrectionService(_db, maxBot, engine);
        _service = new CorrectionBatchService(
            _db,
            correctionService,
            engine,
            new CorrectionImageService(),
            maxBot,
            NullLogger<CorrectionBatchService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    private async Task<Group> SeedGroupAsync()
    {
        var utcNow = DateTime.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "ПО-262",
            Course = 2,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    private async Task<Guid> CreateBatchAsync()
    {
        var result = await _service.CreateBatchAsync(
            new CreateCorrectionBatchRequest { CorrectionDate = TestDate },
            Guid.NewGuid(),
            CancellationToken.None
        );
        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        return result.Data!.Id;
    }

    [Fact]
    public async Task ExamPositionWithZeroPairIsValid()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();

        var result = await _service.AddPositionAsync(
            batchId,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Exam,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 0,
                Subject = "ЭКЗАМЕН",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
    }

    [Fact]
    public async Task ExamPositionWithPairOneIsRejected()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();

        var result = await _service.AddPositionAsync(
            batchId,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Exam,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 1,
                Subject = "ЭКЗАМЕН",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Пара для экзамена должна быть 0");
    }

    [Fact]
    public async Task AddPositionWithZeroPairIsStillRejected()
    {
        var group = await SeedGroupAsync();
        var batchId = await CreateBatchAsync();

        var result = await _service.AddPositionAsync(
            batchId,
            new CreateCorrectionPositionRequest
            {
                ChangeType = ScheduleChangeType.Add,
                GroupId = group.Id,
                GroupName = group.Name,
                NumberPair = 0,
                Subject = "Математика",
            },
            CancellationToken.None
        );

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Номер пары должен быть от 1 до 7.");
    }
}
