using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

public class CorrectionBatchResponse
{
    public Guid Id { get; set; }
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public CorrectionBatchStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? AppliedByUserId { get; set; }
    public string? AppliedByName { get; set; }
    public DateTime? AppliedAt { get; set; }
    public int PositionCount { get; set; }

    /// <summary>
    /// Позиций, ещё не применённых. У применённого пакета их появляется после
    /// правки: при следующем применении выполняются только они.
    /// </summary>
    public int PendingCount { get; set; }

    public List<CorrectionPositionResponse> Positions { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class CorrectionPositionResponse
{
    public Guid Id { get; set; }
    public int Row { get; set; }
    public ScheduleChangeType ChangeType { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public int Week { get; set; }
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
    public CorrectionPositionStatus Status { get; set; }
    public Guid? HistoryId { get; set; }
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

/// <summary>Эффективное расписание группы на дату для флоу корректировки.</summary>
public class CorrectionDayResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<CorrectionDayEntry> Entries { get; set; } = [];
}

/// <summary>Строка расписания дня с учётом применённых и неприменённых корректировок.</summary>
public class CorrectionDayEntry
{
    public int NumberPair { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Note { get; set; }
    public bool IsSelfStudy { get; set; }

    /// <summary>
    /// Пара только для информирования («сам.р.» без «+»): в расписании её нет,
    /// слот она не занимает и снять её нельзя — это просто пометка.
    /// </summary>
    public bool Informational { get; set; }

    public ScheduleChangeType? PendingChangeType { get; set; }
    public List<ChangeTag> ChangeTags { get; set; } = [];
}

public class CreateCorrectionBatchRequest
{
    public DateTime CorrectionDate { get; set; }
}

/// <summary>Преподаватель, ведущий занятия у группы, с его предметами в этой группе.</summary>
public class CorrectionGroupTeacherDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public List<string> Subjects { get; set; } = [];
}

/// <summary>
/// Справочники для пошагового создания позиции корректировки: расписание группы
/// на дату и преподаватели этой группы с их предметами — чтобы нельзя было выбрать
/// преподавателя или предмет, которых у группы нет.
/// </summary>
public class CorrectionReferencesResponse
{
    public DateTime Date { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<CorrectionDayEntry> Entries { get; set; } = [];
    public List<CorrectionGroupTeacherDto> Teachers { get; set; } = [];
}

public class CreateCorrectionPositionRequest
{
    public ScheduleChangeType ChangeType { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int NumberPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? RemovedNumberPair { get; set; }
    public string? Note { get; set; }
}

public class CorrectionImportResponse
{
    public Guid? BatchId { get; set; }
    public DateTime CorrectionDate { get; set; }
    public int Week { get; set; }
    public int DayOfWeek { get; set; }
    public int TotalEntries { get; set; }
    public List<CorrectionPositionResponse> Positions { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class CorrectionApplyResult
{
    public int Applied { get; set; }
    public Guid BatchId { get; set; }
    public List<ScheduleHistoryResponse> History { get; set; } = [];
}

/// <summary>Итог удаления пакета: сколько пакетов и записей журнала убрано.</summary>
public class CorrectionBatchDeleteResult
{
    /// <summary>Сколько пакетов удалено.</summary>
    public int Batches { get; set; }

    /// <summary>Сколько записей журнала откачено и удалено.</summary>
    public int Reverted { get; set; }

    /// <summary>Человекочитаемое сообщение для тоста.</summary>
    public string Message { get; set; } = string.Empty;
}
