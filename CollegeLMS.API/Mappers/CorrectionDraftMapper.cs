using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities;

namespace CollegeLMS.API.Mappers;

public static class CorrectionDraftMapper
{
    public static CorrectionPositionResponse ToDto(this CorrectionPosition position) =>
        new()
        {
            Id = position.Id,
            Row = position.Row,
            ChangeType = position.ChangeType,
            GroupId = position.GroupId,
            GroupName = position.GroupName,
            DayOfWeek = position.DayOfWeek,
            Week = position.Week,
            NumberPair = position.NumberPair,
            Subject = position.Subject,
            TeacherId = position.TeacherId,
            TeacherName = position.TeacherName,
            RemovedSubject = position.RemovedSubject,
            RemovedTeacherId = position.RemovedTeacherId,
            RemovedTeacherName = position.RemovedTeacherName,
            RemovedNumberPair = position.RemovedNumberPair,
            Note = position.Note,
            Status = position.Status,
            HistoryId = position.HistoryId,
        };

    public static CorrectionBatchResponse ToDto(this CorrectionBatch batch) =>
        new()
        {
            Id = batch.Id,
            CorrectionDate = batch.CorrectionDate,
            Week = batch.Week,
            DayOfWeek = batch.DayOfWeek,
            Status = batch.Status,
            CreatedAt = batch.CreatedAt,
            PositionCount = batch.Positions.Count,
            Positions = batch.Positions.Select(p => p.ToDto()).ToList(),
        };

    public static CorrectionPreviewEntry ToPreviewEntry(this CorrectionPosition position) =>
        new()
        {
            Row = position.Row,
            GroupId = position.GroupId,
            GroupName = position.GroupName,
            ChangeType = position.ChangeType,
            DayOfWeek = position.DayOfWeek,
            Week = position.Week,
            NumberPair = position.NumberPair,
            Subject = position.Subject,
            TeacherId = position.TeacherId,
            TeacherName = position.TeacherName,
            RemovedSubject = position.RemovedSubject,
            RemovedTeacherId = position.RemovedTeacherId,
            RemovedTeacherName = position.RemovedTeacherName,
            RemovedNumberPair = position.RemovedNumberPair,
            Note = position.Note,
        };
}
