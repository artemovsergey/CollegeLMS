using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Dtos;

/// <summary>Преподаватель практики в ответе API.</summary>
public class PracticeTeacherResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Учебный день УП с номерами пар.</summary>
public class PracticeDayDto
{
    public DateTime Date { get; set; }

    /// <summary>Номера пар в этот день (1–8).</summary>
    public List<int> PairNumbers { get; set; } = [];
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

    /// <summary>Учебные дни УП с номерами пар (для ПП всегда пусто).</summary>
    public List<PracticeDayDto> Days { get; set; } = [];

    public string? Note { get; set; }

    /// <summary>Номер кабинета (для УП).</summary>
    public string? Room { get; set; }

    /// <summary>Номер подгруппы (для УП).</summary>
    public int? Subgroup { get; set; }
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

    /// <summary>Номер кабинета (для УП).</summary>
    public string? Room { get; set; }

    /// <summary>Номер подгруппы (для УП).</summary>
    public int? Subgroup { get; set; }

    /// <summary>Учебные дни УП с номерами пар (обязательно для УП, для ПП игнорируется).</summary>
    public List<PracticeDayRequest>? Days { get; set; }
}

/// <summary>Учебный день УП в запросе.</summary>
public class PracticeDayRequest
{
    public DateTime Date { get; set; }

    /// <summary>Номера пар в этот день (1–8, без дублей).</summary>
    public List<int> PairNumbers { get; set; } = [];
}

/// <summary>День графика УП (дата и номера пар).</summary>
public class PracticeGraphDay
{
    public DateTime Date { get; set; }
    public List<int> PairNumbers { get; set; } = [];
}

/// <summary>Строка графика УП — подгруппа с темой, кабинетом, днями и преподавателем.</summary>
public class PracticeGraphRow
{
    public int Row { get; set; }

    /// <summary>Номер подгруппы.</summary>
    public int? Subgroup { get; set; }

    /// <summary>Тема УП (наименование).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Номер кабинета.</summary>
    public string? Room { get; set; }

    /// <summary>ФИО преподавателя.</summary>
    public string TeacherName { get; set; } = string.Empty;

    /// <summary>Дни с номерами пар.</summary>
    public List<PracticeGraphDay> Days { get; set; } = [];

    public string? Note { get; set; }
}

/// <summary>Превью импорта графика УП из DOCX.</summary>
public class PracticeGraphPreviewResponse
{
    /// <summary>Группа из шапки документа.</summary>
    public string? GroupName { get; set; }

    /// <summary>Название практики из шапки документа («по УП.01 …»).</summary>
    public string? PracticeName { get; set; }

    /// <summary>Период из шапки документа.</summary>
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public int TotalRows { get; set; }
    public List<PracticeGraphRow> Rows { get; set; } = [];
    public List<ScheduleValidationError> Errors { get; set; } = [];
}

/// <summary>Подтверждение импорта графика УП (в транзакции).</summary>
public class PracticeGraphConfirmRequest
{
    /// <summary>Группа из шапки документа (сопоставляется по названию).</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Название практики (общее для подгрупп).</summary>
    public string Name { get; set; } = string.Empty;

    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<PracticeGraphRow> Rows { get; set; } = [];
}

public class PracticeGraphConfirmResponse
{
    public int Imported { get; set; }
    public List<PracticeResponse> Practices { get; set; } = [];
}

/// <summary>Запрос экспорта графика УП по группе.</summary>
public class PracticeGraphExportRequest
{
    public Guid GroupId { get; set; }
}
