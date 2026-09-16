using CollegeLMS.API.Data;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Mappers;
using CollegeLMS.API.Response;
using Microsoft.EntityFrameworkCore;

namespace CollegeLMS.API.Services;

public class CorrectionBatchService(
    AppDbContext db,
    IScheduleCorrectionService correctionService,
    MaxBotHttpClient maxBot
) : ICorrectionBatchService
{
    private static bool IsSelfStudyNote(string? note) =>
        string.Equals(note?.Trim(), "сам.р.", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<CorrectionBatchResponse>> CreateBatchAsync(
        CreateCorrectionBatchRequest request,
        Guid createdByUserId,
        CancellationToken ct
    )
    {
        if (request.CorrectionDate == default)
            return Result<CorrectionBatchResponse>.Fail("Укажите дату корректировки.", 400);

        if (request.CorrectionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return Result<CorrectionBatchResponse>.Fail(
                "Корректировка не может быть на выходной день.",
                400
            );

        if (!StudyWeek.IsInSemester(request.CorrectionDate))
            return Result<CorrectionBatchResponse>.Fail("Дата вне учебного семестра.", 400);

        var week = StudyWeek.ForDate(request.CorrectionDate);
        var batch = new CorrectionBatch
        {
            Id = Guid.NewGuid(),
            CorrectionDate = request.CorrectionDate.Date,
            Week = week,
            DayOfWeek = (int)request.CorrectionDate.DayOfWeek,
            Status = CorrectionBatchStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.CorrectionBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return Result<CorrectionBatchResponse>.Ok(batch.ToDto());
    }

    public async Task<Result<List<CorrectionBatchResponse>>> GetBatchesAsync(
        CorrectionBatchStatus? status,
        CancellationToken ct
    )
    {
        var query = db.CorrectionBatches.AsNoTracking().Include(b => b.Positions).AsQueryable();
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var batches = await query.OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

        return Result<List<CorrectionBatchResponse>>.Ok(batches.Select(b => b.ToDto()).ToList());
    }

    public async Task<Result<CorrectionBatchResponse>> GetBatchAsync(Guid id, CancellationToken ct)
    {
        var batch = await db
            .CorrectionBatches.AsNoTracking()
            .Include(b => b.Positions)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null)
            return Result<CorrectionBatchResponse>.Fail("Пакет корректировки не найден.", 404);

        return Result<CorrectionBatchResponse>.Ok(batch.ToDto());
    }

    public async Task<Result> DeleteBatchAsync(Guid id, CancellationToken ct)
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null)
            return Result.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result.Fail("Можно удалить только черновой пакет.", 409);

        db.CorrectionBatches.Remove(batch);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<CorrectionPositionResponse>> AddPositionAsync(
        Guid batchId,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    )
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<CorrectionPositionResponse>.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result<CorrectionPositionResponse>.Fail("Пакет уже применён или отменён.", 409);

        var error = await ValidatePositionAsync(request, ct);
        if (error is not null)
            return Result<CorrectionPositionResponse>.Fail(error, 400);

        var maxRow =
            await db
                .CorrectionPositions.Where(p => p.BatchId == batchId)
                .MaxAsync(p => (int?)p.Row, ct)
            ?? 0;

        var position = new CorrectionPosition
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            Row = maxRow + 1,
            ChangeType = request.ChangeType,
            GroupId = request.GroupId,
            GroupName = request.GroupName,
            DayOfWeek = batch.DayOfWeek,
            Week = batch.Week,
            NumberPair = request.NumberPair,
            Subject = request.Subject,
            TeacherId = request.TeacherId,
            TeacherName = request.TeacherName,
            RemovedSubject = request.RemovedSubject,
            RemovedTeacherId = request.RemovedTeacherId,
            RemovedTeacherName = request.RemovedTeacherName,
            RemovedNumberPair = request.RemovedNumberPair,
            Note = request.Note,
            Status = CorrectionPositionStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.CorrectionPositions.Add(position);
        await db.SaveChangesAsync(ct);

        return Result<CorrectionPositionResponse>.Ok(position.ToDto());
    }

    public async Task<Result<CorrectionPositionResponse>> UpdatePositionAsync(
        Guid batchId,
        Guid positionId,
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    )
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<CorrectionPositionResponse>.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result<CorrectionPositionResponse>.Fail("Пакет уже применён или отменён.", 409);

        var position = await db.CorrectionPositions.FirstOrDefaultAsync(
            p => p.Id == positionId && p.BatchId == batchId,
            ct
        );
        if (position is null)
            return Result<CorrectionPositionResponse>.Fail("Позиция не найдена.", 404);

        var error = await ValidatePositionAsync(request, ct);
        if (error is not null)
            return Result<CorrectionPositionResponse>.Fail(error, 400);

        position.ChangeType = request.ChangeType;
        position.GroupId = request.GroupId;
        position.GroupName = request.GroupName;
        position.NumberPair = request.NumberPair;
        position.Subject = request.Subject;
        position.TeacherId = request.TeacherId;
        position.TeacherName = request.TeacherName;
        position.RemovedSubject = request.RemovedSubject;
        position.RemovedTeacherId = request.RemovedTeacherId;
        position.RemovedTeacherName = request.RemovedTeacherName;
        position.RemovedNumberPair = request.RemovedNumberPair;
        position.Note = request.Note;
        position.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<CorrectionPositionResponse>.Ok(position.ToDto());
    }

    public async Task<Result> DeletePositionAsync(
        Guid batchId,
        Guid positionId,
        CancellationToken ct
    )
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result.Fail("Пакет уже применён или отменён.", 409);

        var position = await db.CorrectionPositions.FirstOrDefaultAsync(
            p => p.Id == positionId && p.BatchId == batchId,
            ct
        );
        if (position is null)
            return Result.Fail("Позиция не найдена.", 404);

        db.CorrectionPositions.Remove(position);
        await db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<CorrectionImportResponse>> ImportAsync(
        Stream fileStream,
        Guid createdByUserId,
        CancellationToken ct
    )
    {
        var preview = await correctionService.PreviewAsync(fileStream, ct);
        if (!preview.IsSuccess)
            return Result<CorrectionImportResponse>.Fail(preview.ErrorMessage!, preview.StatusCode);

        var data = preview.Data!;

        if (data.Errors.Count > 0)
        {
            return Result<CorrectionImportResponse>.Ok(
                new CorrectionImportResponse
                {
                    CorrectionDate = data.CorrectionDate,
                    Week = data.Week,
                    DayOfWeek = data.DayOfWeek,
                    TotalEntries = 0,
                    Positions = [],
                    Errors = data.Errors,
                }
            );
        }

        var batch = new CorrectionBatch
        {
            Id = Guid.NewGuid(),
            CorrectionDate = data.CorrectionDate.Date,
            Week = data.Week,
            DayOfWeek = data.DayOfWeek,
            Status = CorrectionBatchStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.CorrectionBatches.Add(batch);

        var row = 0;
        var positions = new List<CorrectionPosition>();
        foreach (var entry in data.Entries)
        {
            row++;
            positions.Add(
                new CorrectionPosition
                {
                    Id = Guid.NewGuid(),
                    BatchId = batch.Id,
                    Row = row,
                    ChangeType = entry.ChangeType,
                    GroupId = entry.GroupId,
                    GroupName = entry.GroupName,
                    DayOfWeek = entry.DayOfWeek,
                    Week = entry.Week,
                    NumberPair = entry.NumberPair,
                    Subject = entry.Subject,
                    TeacherId = entry.TeacherId,
                    TeacherName = entry.TeacherName,
                    RemovedSubject = entry.RemovedSubject,
                    RemovedTeacherId = entry.RemovedTeacherId,
                    RemovedTeacherName = entry.RemovedTeacherName,
                    RemovedNumberPair = entry.RemovedNumberPair,
                    Note = entry.Note,
                    Status = CorrectionPositionStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }

        db.CorrectionPositions.AddRange(positions);
        await db.SaveChangesAsync(ct);

        return Result<CorrectionImportResponse>.Ok(
            new CorrectionImportResponse
            {
                BatchId = batch.Id,
                CorrectionDate = data.CorrectionDate,
                Week = data.Week,
                DayOfWeek = data.DayOfWeek,
                TotalEntries = positions.Count,
                Positions = positions.Select(p => p.ToDto()).ToList(),
                Errors = [],
            }
        );
    }

    public async Task<Result<DocumentDownloadResult>> ExportAsync(
        Guid batchId,
        CancellationToken ct
    )
    {
        var batch = await db
            .CorrectionBatches.AsNoTracking()
            .Include(b => b.Positions)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<DocumentDownloadResult>.Fail("Пакет корректировки не найден.", 404);

        var rows = batch.Positions.OrderBy(p => p.Row).Select(ToManualRow).ToList();

        var request = new ManualCorrectionExportRequest
        {
            CorrectionDate = batch.CorrectionDate,
            Rows = rows,
        };

        return await correctionService.ExportManualAsync(request, ct);
    }

    public async Task<Result<CorrectionApplyResult>> ApplyAsync(
        Guid batchId,
        string idempotencyKey,
        Guid appliedByUserId,
        CancellationToken ct
    )
    {
        var batch = await db
            .CorrectionBatches.Include(b => b.Positions)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return Result<CorrectionApplyResult>.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result<CorrectionApplyResult>.Fail("Пакет уже применён или отменён.", 409);

        var positions = batch
            .Positions.Where(p => p.Status == CorrectionPositionStatus.Draft)
            .OrderBy(p => p.Row)
            .ToList();

        if (positions.Count == 0)
            return Result<CorrectionApplyResult>.Fail("В пакете нет позиций для применения.", 400);

        var entries = positions.Select(p => p.ToPreviewEntry()).ToList();

        var applyResult = await correctionService.ApplyEntriesAsync(
            entries,
            appliedByUserId,
            batch.CorrectionDate,
            ct
        );
        if (!applyResult.IsSuccess)
            return Result<CorrectionApplyResult>.Fail(
                applyResult.ErrorMessage!,
                applyResult.StatusCode
            );

        var changes = applyResult.Data!;

        // Связываем позиции с историей (порядок 1:1) и помечаем применёнными.
        for (var i = 0; i < positions.Count && i < changes.Count; i++)
        {
            positions[i].Status = CorrectionPositionStatus.Applied;
            positions[i].HistoryId = changes[i].Id;
            positions[i].UpdatedAt = DateTime.UtcNow;
        }

        batch.Status = CorrectionBatchStatus.Applied;
        batch.AppliedByUserId = appliedByUserId;
        batch.AppliedAt = DateTime.UtcNow;
        batch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Оповещение MaxBot — fail-safe.
        if (changes.Count > 0)
            await maxBot.SendChangesAsync(changes, ct);

        var ids = changes.Select(c => c.Id).ToList();
        var saved = await db
            .ScheduleHistory.AsNoTracking()
            .Include(h => h.Group)
            .Include(h => h.Teacher!)
                .ThenInclude(t => t.User)
            .Where(h => ids.Contains(h.Id))
            .OrderBy(h => h.AppliedAt)
            .ToListAsync(ct);

        return Result<CorrectionApplyResult>.Ok(
            new CorrectionApplyResult
            {
                Applied = changes.Count,
                BatchId = batchId,
                History = saved.Select(h => h.ToDto()).ToList(),
            }
        );
    }

    private async Task<string?> ValidatePositionAsync(
        CreateCorrectionPositionRequest request,
        CancellationToken ct
    )
    {
        if (request.NumberPair < 1 || request.NumberPair > 8)
            return "Номер пары должен быть от 1 до 8.";

        if (string.IsNullOrWhiteSpace(request.GroupName))
            return "Укажите группу.";

        var groupExists = await db.Groups.AsNoTracking().AnyAsync(g => g.Id == request.GroupId, ct);
        if (!groupExists)
            return $"Группа «{request.GroupName}» не найдена в системе.";

        if (IsSelfStudyNote(request.Note) && request.ChangeType != ScheduleChangeType.Remove)
            return "Примечание «сам.р.» допустимо только для снятия.";

        return null;
    }

    private static ManualCorrectionRow ToManualRow(CorrectionPosition p)
    {
        var note = p.Note;
        if (p.ChangeType == ScheduleChangeType.Move && p.RemovedNumberPair.HasValue && note is null)
        {
            note = $"вм.{p.RemovedNumberPair}";
        }

        return new ManualCorrectionRow
        {
            GroupName = p.GroupName,
            RemovedSubject = p.ChangeType is ScheduleChangeType.Remove or ScheduleChangeType.Replace
                ? p.RemovedSubject
                : null,
            RemovedTeacherName = p.ChangeType
                is ScheduleChangeType.Remove
                    or ScheduleChangeType.Replace
                ? p.RemovedTeacherName
                : null,
            AddedSubject = p.ChangeType != ScheduleChangeType.Remove ? p.Subject : null,
            AddedTeacherName = p.ChangeType != ScheduleChangeType.Remove ? p.TeacherName : null,
            NumberPair = p.NumberPair,
            Note = note,
        };
    }
}
