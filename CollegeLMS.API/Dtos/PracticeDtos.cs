using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

/// <summary>Преподаватель практики в ответе API.</summary>
public class PracticeTeacherResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Учебный день УП с числом пар.</summary>
public class PracticeDayDto
{
    public DateTime Date { get; set; }
    public int PairCount { get; set; }
}

public class PracticeResponse
{
    public Guid Id { get; set; }
    public PracticeKind Kind { get; set; }

    /// <summary>Название практики, например «УП 01».</summary>
    public string Name { get; set; } = string.Empty;

    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Идентификаторы преподавателей практики.</summary>
    public List<Guid> TeacherIds { get; set; } = [];

    /// <summary>Преподаватели практики (идентификатор и ФИО).</summary>
    public List<PracticeTeacherResponse> Teachers { get; set; } = [];

    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }

    /// <summary>Учебные дни УП (для ПП всегда пусто).</summary>
    public List<PracticeDayDto> Days { get; set; } = [];

    public string? Note { get; set; }
}

public class PracticeRequest
{
    public PracticeKind Kind { get; set; }

    /// <summary>Название практики (обязательно, до 100 символов).</summary>
    public string Name { get; set; } = string.Empty;

    public Guid GroupId { get; set; }

    /// <summary>Преподаватели практики (минимум один).</summary>
    public List<Guid> TeacherIds { get; set; } = [];

    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Note { get; set; }

    /// <summary>Учебные дни УП с числом пар (обязательно для УП, для ПП игнорируется).</summary>
    public List<PracticeDayRequest>? Days { get; set; }
}

/// <summary>Учебный день УП в запросе.</summary>
public class PracticeDayRequest
{
    public DateTime Date { get; set; }
    public int PairCount { get; set; }
}

/// <summary>Строка импорта практик из XLSX (все поля — сырые значения файла).</summary>
public class PracticeImportRow
{
    public int Row { get; set; }
    public string Kind { get; set; } = string.Empty;

    /// <summary>Название практики.</summary>
    public string Name { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;

    /// <summary>ФИО преподавателей; несколько значений разделяются «;».</summary>
    public string TeacherName { get; set; } = string.Empty;

    public string? Note { get; set; }
}

public class PracticeImportPreviewResponse
{
    public int TotalRows { get; set; }
    public List<PracticeImportRow> Rows { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

public class PracticeImportConfirmRequest
{
    public List<PracticeImportRow> Rows { get; set; } = [];
}

public class PracticeImportConfirmResponse
{
    public int Imported { get; set; }
    public List<PracticeResponse> Practices { get; set; } = [];
}
