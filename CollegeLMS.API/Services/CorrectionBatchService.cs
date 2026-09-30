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
    CorrectionApplyEngine engine,
    CorrectionImageService imageService,
    MaxBotHttpClient maxBot,
    ILogger<CorrectionBatchService> logger
) : ICorrectionBatchService
{
    private static bool IsSelfStudyNote(string? note) =>
        ScheduleImportService.IsSelfStudyNote(note);

    public async Task<Result<CorrectionBatchResponse>> CreateBatchAsync(
        CreateCorrectionBatchRequest request,
        Guid createdByUserId,
        CancellationToken ct
    )
    {
        if (request.CorrectionDate == default)
            return Result<CorrectionBatchResponse>.Fail("Укажите дату корректировки.", 400);

        if (
            request.CorrectionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            && !await HasWorkingOverrideAsync(request.CorrectionDate, ct)
        )
            return Result<CorrectionBatchResponse>.Fail(
                "Корректировка не может быть на выходной день.",
                400
            );

        if (!StudyWeek.IsInSemester(request.CorrectionDate))
            return Result<CorrectionBatchResponse>.Fail("Дата вне учебного семестра.", 400);

        var nonWorking = await FindNonWorkingAsync(request.CorrectionDate, ct);
        if (nonWorking is not null)
            return Result<CorrectionBatchResponse>.Fail(
                $"Дата нерабочая: {nonWorking.Title}.",
                400
            );

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

    public async Task<Result<CorrectionBatchResponse>> UpdateBatchAsync(
        Guid id,
        CreateCorrectionBatchRequest request,
        CancellationToken ct
    )
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null)
            return Result<CorrectionBatchResponse>.Fail("Пакет корректировки не найден.", 404);

        if (batch.Status != CorrectionBatchStatus.Draft)
            return Result<CorrectionBatchResponse>.Fail(
                "Дату применённого пакета менять нельзя.",
                409
            );

        if (request.CorrectionDate == default)
            return Result<CorrectionBatchResponse>.Fail("Укажите дату корректировки.", 400);

        if (
            request.CorrectionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            && !await HasWorkingOverrideAsync(request.CorrectionDate, ct)
        )
            return Result<CorrectionBatchResponse>.Fail(
                "Корректировка не может быть на выходной день.",
                400
            );

        if (!StudyWeek.IsInSemester(request.CorrectionDate))
            return Result<CorrectionBatchResponse>.Fail("Дата вне учебного семестра.", 400);

        var nonWorking = await FindNonWorkingAsync(request.CorrectionDate, ct);
        if (nonWorking is not null)
            return Result<CorrectionBatchResponse>.Fail(
                $"Дата нерабочая: {nonWorking.Title}.",
                400
            );

        batch.CorrectionDate = request.CorrectionDate.Date;
        batch.Week = StudyWeek.ForDate(request.CorrectionDate);
        batch.DayOfWeek = (int)request.CorrectionDate.DayOfWeek;
        batch.UpdatedAt = DateTime.UtcNow;

        // Позиции наследуют неделю и день пакета — синхронизируем, иначе они
        // останутся на старой дате и применятся не туда.
        var positions = await db.CorrectionPositions.Where(p => p.BatchId == id).ToListAsync(ct);
        foreach (var position in positions)
        {
            position.Week = batch.Week;
            position.DayOfWeek = batch.DayOfWeek;
            position.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        batch.Positions = positions;
        return Result<CorrectionBatchResponse>.Ok(batch.ToDto());
    }

    public async Task<Result<PagedResponse<CorrectionBatchResponse>>> GetBatchesAsync(
        CorrectionBatchStatus? status,
        DateTime? from,
        DateTime? to,
        int? page,
        int? pageSize,
        CancellationToken ct
    )
    {
        var query = db.CorrectionBatches.AsNoTracking().Include(b => b.Positions).AsQueryable();
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        if (from.HasValue)
            query = query.Where(b => b.CorrectionDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(b => b.CorrectionDate <= to.Value.Date);

        var totalCount = await query.CountAsync(ct);
        var p = Math.Max(page ?? 1, 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);

        var batches = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        var appliedByIds = batches
            .Where(b => b.AppliedByUserId.HasValue)
            .Select(b => b.AppliedByUserId!.Value)
            .Distinct()
            .ToList();
        var appliedByNames =
            appliedByIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await db
                    .Users.AsNoTracking()
                    .Where(u => appliedByIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = batches
            .Select(b =>
            {
                var dto = b.ToDto(includePositions: false);
                if (b.AppliedByUserId.HasValue)
                    dto.AppliedByName = appliedByNames.GetValueOrDefault(b.AppliedByUserId.Value);
                return dto;
            })
            .ToList();

        return Result<PagedResponse<CorrectionBatchResponse>>.Ok(
            new PagedResponse<CorrectionBatchResponse>(items, totalCount, p, ps)
        );
    }

    public async Task<Result<CorrectionBatchResponse>> GetBatchAsync(Guid id, CancellationToken ct)
    {
        var batch = await db
            .CorrectionBatches.AsNoTracking()
            .Include(b => b.Positions)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null)
            return Result<CorrectionBatchResponse>.Fail("Пакет корректировки не найден.", 404);

        var dto = batch.ToDto();

        if (batch.AppliedByUserId.HasValue)
        {
            dto.AppliedByName = await db
                .Users.AsNoTracking()
                .Where(u => u.Id == batch.AppliedByUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        if (batch.Status == CorrectionBatchStatus.Draft)
        {
            var errors = await engine.ValidateBatchAsync(batch, ct);
            dto.Errors = errors;
            foreach (var position in dto.Positions)
                position.Errors = errors.Where(e => e.Row == position.Row).ToList();
        }

        return Result<CorrectionBatchResponse>.Ok(dto);
    }

    public async Task<Result<CorrectionBatchDeleteResult>> DeleteBatchAsync(
        Guid id,
        CancellationToken ct
    )
    {
        var batch = await db.CorrectionBatches.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null)
            return Result<CorrectionBatchDeleteResult>.Fail("Пакет корректировки не найден.", 404);

        var reverted = await RemoveBatchAsync([batch], ct);
        return Result<CorrectionBatchDeleteResult>.Ok(
            new CorrectionBatchDeleteResult
            {
                Batches = 1,
                Reverted = reverted,
                Message =
                    reverted > 0
                        ? $"Пакет удалён, расписание возвращено по {reverted} изменениям."
                        : "Пакет удалён.",
            }
        );
    }

    public async Task<Result<CorrectionBatchDeleteResult>> ClearAppliedBatchesAsync(
        CancellationToken ct
    )
    {
        var batches = await db
            .CorrectionBatches.Where(b => b.Status == CorrectionBatchStatus.Applied)
            .ToListAsync(ct);

        if (batches.Count == 0)
            return Result<CorrectionBatchDeleteResult>.Ok(
                new CorrectionBatchDeleteResult
                {
                    Message = "Применённых пакетов нет — очищать нечего.",
                }
            );

        var reverted = await RemoveBatchAsync(batches, ct);
        return Result<CorrectionBatchDeleteResult>.Ok(
            new CorrectionBatchDeleteResult
            {
                Batches = batches.Count,
                Reverted = reverted,
                Message = $"Удалено пакетов: {batches.Count}, изменений возвращено: {reverted}.",
            }
        );
    }

    /// <summary>
    /// Откатывает записи журнала пакетов и удаляет сами пакеты. Черновые пакеты
    /// удаляются без отката — они ещё не меняли расписание.
    /// </summary>
    private async Task<int> RemoveBatchAsync(
        IReadOnlyList<CorrectionBatch> batches,
        CancellationToken ct
    )
    {
        var batchIds = batches.Select(b => b.Id).ToList();
        var history = await db
            .ScheduleHistory.Where(h => h.BatchId != null && batchIds.Contains(h.BatchId.Value))
            .ToListAsync(ct);

        history.AddRange(await FindUnlinkedHistoryAsync(batches, ct));

        var reverted = 0;
        if (history.Count > 0)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            reverted = await correctionService.RevertBatchAsync(history, ct);
            foreach (var batch in batches)
                db.CorrectionBatches.Remove(batch);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        else
        {
            db.CorrectionBatches.RemoveRange(batches);
            await db.SaveChangesAsync(ct);
        }

        return reverted;
    }

    /// <summary>
    /// Записи журнала без связи с пакетом. Их создавал ручной путь применения и
    /// они остались в базе до появления <c>BatchId</c> у <see cref="ScheduleHistory"/>,
    /// поэтому по пакету не находятся. Такие записи не откатывались и не удалялись:
    /// пакет исчезал, а его изменение навсегда оставалось в меню «Изменения», и
    /// расписание не возвращалось в исходное состояние — ровно то, что видно и в
    /// LMS, и в боте, пока файл расписания не импортируют заново.
    ///
    /// Узнаём их по содержимому: запись описывает ровно то же изменение, что и
    /// позиция удаляемого пакета, и сделана в его пределах по времени. Совпадение
    /// строгое — по группе, дню, неделе, типу, паре, предмету, преподавателю и
    /// примечанию, — чтобы не задеть чужую запись журнала.
    /// </summary>
    private async Task<List<ScheduleHistory>> FindUnlinkedHistoryAsync(
        IReadOnlyList<CorrectionBatch> batches,
        CancellationToken ct
    )
    {
        var appliedBatches = batches.Where(b => b.Status == CorrectionBatchStatus.Applied).ToList();
        if (appliedBatches.Count == 0)
            return [];

        var batchIds = appliedBatches.Select(b => b.Id).ToList();
        var positions = await db
            .CorrectionPositions.AsNoTracking()
            .Where(p => batchIds.Contains(p.BatchId))
            .ToListAsync(ct);
        if (positions.Count == 0)
            return [];

        // Без AsNoTracking: запись может быть уже в контексте, и тогда нужна
        // именно она — иначе откат получит второй экземпляр с тем же ключом.
        var unlinked = await db.ScheduleHistory.Where(h => h.BatchId == null).ToListAsync(ct);

        return unlinked
            .Where(h =>
                appliedBatches.Any(b =>
                    h.AppliedAt >= b.CreatedAt
                    && (b.AppliedAt == null || h.AppliedAt <= b.AppliedAt)
                )
            )
            .Where(h => positions.Any(p => SameChange(p, h)))
            .ToList();
    }

    /// <summary>Описывает ли позиция пакета ровно это изменение журнала.</summary>
    private static bool SameChange(CorrectionPosition position, ScheduleHistory history)
    {
        if (
            position.ChangeType != history.ChangeType
            || position.GroupId != history.GroupId
            || position.DayOfWeek != (int)history.DayOfWeek
            || position.Week != history.Week
            || position.NumberPair != history.NumberPair
            || !SameText(position.Note, history.Note)
        )
            return false;

        // При снятии журнал пишет снимаемое занятие в Subject/TeacherId, а позиция
        // хранит его в Removed*: сравниваем по смыслу, а не по имени поля.
        if (position.ChangeType == ScheduleChangeType.Remove)
            return SameText(position.RemovedSubject, history.Subject)
                && position.RemovedTeacherId == history.TeacherId
                && history.RemovedSubject is null
                && history.RemovedNumberPair is null;

        return SameText(position.Subject, history.Subject)
            && position.TeacherId == history.TeacherId
            && SameText(position.RemovedSubject, history.RemovedSubject)
            && position.RemovedTeacherId == history.RemovedTeacherId
            && position.RemovedNumberPair == history.RemovedNumberPair;
    }

    private static bool SameText(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.Ordinal);

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
            return Result<CorrectionPositionResponse>.Fail("Пакет уже применён.", 409);

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
            Subject = request.Subject?.Trim(),
            TeacherId = request.TeacherId,
            TeacherName = request.TeacherName,
            RemovedSubject = string.IsNullOrWhiteSpace(request.RemovedSubject)
                ? request.RemovedSubject
                : request.RemovedSubject.Trim(),
            RemovedTeacherId = request.RemovedTeacherId,
            RemovedTeacherName = request.RemovedTeacherName,
            RemovedNumberPair = request.RemovedNumberPair,
            Note = ResolveNote(request),
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
            return Result<CorrectionPositionResponse>.Fail("Пакет уже применён.", 409);

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
        position.Subject = request.Subject?.Trim();
        position.TeacherId = request.TeacherId;
        position.TeacherName = request.TeacherName;
        position.RemovedSubject = string.IsNullOrWhiteSpace(request.RemovedSubject)
            ? request.RemovedSubject
            : request.RemovedSubject.Trim();
        position.RemovedTeacherId = request.RemovedTeacherId;
        position.RemovedTeacherName = request.RemovedTeacherName;
        position.RemovedNumberPair = request.RemovedNumberPair;
        position.Note = ResolveNote(request);
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
            return Result.Fail("Пакет уже применён.", 409);

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

        // Структурные ошибки (нет даты/шапки) — пакет создать нельзя.
        if (data.CorrectionDate == default || data.Errors.Any(e => e.Level == "structure"))
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
        foreach (var entry in data.AllEntries)
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

        // Ошибки пересчитываются при чтении (в БД не хранятся).
        batch.Positions = positions;
        var errors = await engine.ValidateBatchAsync(batch, ct);

        return Result<CorrectionImportResponse>.Ok(
            new CorrectionImportResponse
            {
                BatchId = batch.Id,
                CorrectionDate = data.CorrectionDate,
                Week = data.Week,
                DayOfWeek = data.DayOfWeek,
                TotalEntries = positions.Count,
                Positions = positions
                    .Select(p =>
                    {
                        var dto = p.ToDto();
                        dto.Errors = errors.Where(e => e.Row == p.Row).ToList();
                        return dto;
                    })
                    .ToList(),
                Errors = errors,
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
            return Result<CorrectionApplyResult>.Fail("Пакет уже применён.", 409);

        var positions = batch
            .Positions.Where(p => p.Status == CorrectionPositionStatus.Draft)
            .OrderBy(p => p.Row)
            .ToList();

        if (positions.Count == 0)
            return Result<CorrectionApplyResult>.Fail("В пакете нет позиций для применения.", 400);

        var nonWorking = await FindNonWorkingAsync(batch.CorrectionDate, ct);
        if (nonWorking is not null)
            return Result<CorrectionApplyResult>.Fail($"Дата нерабочая: {nonWorking.Title}.", 400);

        // Валидация до транзакции: 400 со списком «Строка N: сообщение».
        var errors = await engine.ValidateBatchAsync(batch, ct);
        if (errors.Count > 0)
            return Result<CorrectionApplyResult>.Fail(
                string.Join("\n", errors.Select(e => e.Message)),
                400
            );

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var outcome = await engine.ExecuteBatchAsync(batch, appliedByUserId, ct);

            batch.Status = CorrectionBatchStatus.Applied;
            batch.AppliedByUserId = appliedByUserId;
            batch.AppliedAt = DateTime.UtcNow;
            batch.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            if (outcome.Changes.Count > 0)
                await maxBot.SendChangesAsync(outcome.Changes, ct);

            await TrySendCorrectionImageAsync(batch, ct);

            var ids = outcome.Changes.Select(c => c.Id).ToList();
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
                    Applied = outcome.Changes.Count,
                    BatchId = batchId,
                    History = saved.Select(h => h.ToDto()).ToList(),
                }
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<CorrectionApplyResult>.Fail(
                "Пакет уже применён другим пользователем.",
                409
            );
        }
    }

    private async Task<NonWorkingDay?> FindNonWorkingAsync(DateTime date, CancellationToken ct)
    {
        if (await HasWorkingOverrideAsync(date, ct))
            return null;

        return await db
            .NonWorkingDays.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DateFrom <= date.Date && d.DateTo >= date.Date, ct);
    }

    /// <summary>Есть ли на дату рабочий день (override) — делает выходной рабочим.</summary>
    private async Task<bool> HasWorkingOverrideAsync(DateTime date, CancellationToken ct) =>
        await db
            .WorkingDayOverrides.AsNoTracking()
            .AnyAsync(d => d.DateFrom <= date.Date && d.DateTo >= date.Date, ct);

    private async Task TrySendCorrectionImageAsync(CorrectionBatch batch, CancellationToken ct)
    {
        try
        {
            var png = imageService.Render(batch);
            if (png.Length == 0)
                return;

            var caption = $"🔔 Корректировка расписания на {batch.CorrectionDate:dd.MM.yyyy}";
            await maxBot.SendCorrectionImageAsync(png, caption, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сбой подготовки картинки корректировки для канала Max");
        }
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

        return null;
    }

    private static string? ResolveNote(CreateCorrectionPositionRequest request)
    {
        var note = request.Note;
        if (
            request.ChangeType == ScheduleChangeType.Move
            && request.RemovedNumberPair.HasValue
            && string.IsNullOrWhiteSpace(note)
        )
        {
            note = $"вм.{request.RemovedNumberPair.Value}";
        }

        return note;
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
