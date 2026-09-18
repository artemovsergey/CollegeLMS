using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Models.Max;

namespace CollegeLMS.MaxBot.Services;

/// <summary>Шаги визарда корректировки.</summary>
public enum DispatcherWizardStep
{
    None,
    Type,
    Day,
    Week,
    Group,
    RemovedPair,
    NewPair,
    Subject,
    Teacher,
    Note,
}

/// <summary>Состояние визарда одного диспетчера.</summary>
public class DispatcherWizardState
{
    public DispatcherWizardStep Step { get; set; } = DispatcherWizardStep.None;
    public string? ChangeType { get; set; }
    public int Day { get; set; }
    public int Week { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public int? RemovedNumberPair { get; set; }
    public string? RemovedSubject { get; set; }
    public Guid? RemovedTeacherId { get; set; }
    public string? RemovedTeacherName { get; set; }
    public int? NewPair { get; set; }
    public string? Subject { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Чистая машина состояний визарда корректировок: сборка entry, клавиатуры шагов.
/// Не зависит от Max API и БД — покрывается юнит-тестами.
/// </summary>
public static class DispatcherCorrectionWizard
{
    private static readonly string[] DayAbbr = ["", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"];

    /// <summary>Переводит номер недели и дня в дату по календарю API.</summary>
    public static DateTime CalculateCorrectionDate(DateTime semesterStart, int week, int dayOfWeek)
    {
        var day = (int)semesterStart.DayOfWeek;
        var offset = day == 0 ? 6 : day - 1;
        var monday = semesterStart.Date.AddDays(-offset);
        return monday.AddDays((week - 1) * 7 + dayOfWeek - 1);
    }

    /// <summary>Собирает запись корректировки из состояния визарда.</summary>
    public static CorrectionEntryDto BuildEntry(DispatcherWizardState state)
    {
        var isRemove = state.ChangeType == "Remove";
        var hasRemoved = isRemove || state.ChangeType == "Replace" || state.ChangeType == "Move";

        var numberPair = state.ChangeType switch
        {
            "Add" => state.NewPair ?? 0,
            "Move" => state.NewPair ?? 0,
            _ => state.RemovedNumberPair ?? 0,
        };

        return new CorrectionEntryDto
        {
            Row = 1,
            GroupId = state.GroupId,
            GroupName = state.GroupName,
            ChangeType = state.ChangeType ?? "Add",
            DayOfWeek = state.Day,
            Week = state.Week,
            NumberPair = numberPair,
            Subject = isRemove ? null : state.Subject,
            TeacherId = isRemove ? null : state.TeacherId,
            TeacherName = isRemove ? null : state.TeacherName,
            RemovedNumberPair = hasRemoved ? state.RemovedNumberPair : null,
            RemovedSubject = hasRemoved ? state.RemovedSubject : null,
            RemovedTeacherId = hasRemoved ? state.RemovedTeacherId : null,
            RemovedTeacherName = hasRemoved ? state.RemovedTeacherName : null,
            Note = string.IsNullOrWhiteSpace(state.Note) || state.Note == "—" ? null : state.Note,
        };
    }

    /// <summary>Сетка недель по 4 кнопки в строке.</summary>
    public static List<List<MaxButton>> BuildWeekGrid(int currentWeek, int totalWeeks)
    {
        var buttons = Enumerable
            .Range(1, totalWeeks)
            .Select(w => new MaxButton
            {
                Type = "callback",
                Text = w == currentWeek ? $"• {w}" : w.ToString(),
                Payload = $"wiz:week:{w}",
            })
            .ToList();

        var rows = new List<List<MaxButton>>();
        for (var i = 0; i < buttons.Count; i += 4)
            rows.Add(buttons.Skip(i).Take(4).ToList());
        return rows;
    }

    /// <summary>Клавиатура выбора дня недели (Пн–Сб).</summary>
    public static List<List<MaxButton>> BuildDayKeyboard() =>
        [
            DayAbbr
                .Skip(1)
                .Select(
                    (d, i) =>
                        new MaxButton
                        {
                            Type = "callback",
                            Text = d,
                            Payload = $"wiz:day:{i + 1}",
                        }
                )
                .ToList(),
        ];

    /// <summary>Клавиатура типов изменения.</summary>
    public static List<List<MaxButton>> BuildTypeKeyboard() =>
        [
            [
                new MaxButton
                {
                    Type = "callback",
                    Text = "🟢 Добавить",
                    Payload = "wiz:type:Add",
                },
            ],
            [
                new MaxButton
                {
                    Type = "callback",
                    Text = "🔴 Снять",
                    Payload = "wiz:type:Remove",
                },
            ],
            [
                new MaxButton
                {
                    Type = "callback",
                    Text = "🔵 Замена",
                    Payload = "wiz:type:Replace",
                },
            ],
            [
                new MaxButton
                {
                    Type = "callback",
                    Text = "🔄 Перенос",
                    Payload = "wiz:type:Move",
                },
            ],
        ];

    /// <summary>Клавиатура выбора новой пары (1–7).</summary>
    public static List<List<MaxButton>> BuildPairKeyboard(string action) =>
        [
            Enumerable
                .Range(1, 7)
                .Select(n => new MaxButton
                {
                    Type = "callback",
                    Text = n.ToString(),
                    Payload = $"wiz:{action}:{n}",
                })
                .ToList(),
        ];

    /// <summary>Кнопка отмены визарда.</summary>
    public static List<MaxButton> CancelRow() =>
        [
            new MaxButton
            {
                Type = "callback",
                Text = "❌ Отмена",
                Payload = "wiz:cancel",
            },
        ];

    /// <summary>Клавиатура предпросмотра: применить / отмена.</summary>
    public static List<List<MaxButton>> BuildConfirmKeyboard() =>
        [
            [
                new MaxButton
                {
                    Type = "callback",
                    Text = "✅ Применить",
                    Payload = "wiz:apply",
                },
                new MaxButton
                {
                    Type = "callback",
                    Text = "❌ Отмена",
                    Payload = "wiz:cancel",
                },
            ],
        ];

    /// <summary>Название типа изменения по-русски.</summary>
    public static string TypeLabel(string changeType) =>
        changeType switch
        {
            "Add" => "добавление",
            "Remove" => "снятие",
            "Replace" => "замена",
            "Move" => "перенос",
            _ => changeType,
        };

    /// <summary>Текст предпросмотра корректировки перед применением.</summary>
    public static string BuildPreview(DispatcherWizardState state)
    {
        var entry = BuildEntry(state);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📋 *Предпросмотр корректировки*");
        sb.AppendLine();
        sb.AppendLine($"Тип: {TypeLabel(entry.ChangeType)}");
        sb.AppendLine(
            $"Дата: {DayAbbr[Math.Min(entry.DayOfWeek, DayAbbr.Length - 1)]} · нед. {entry.Week}"
        );
        sb.AppendLine($"Группа: {entry.GroupName}");
        sb.AppendLine($"Пара: {entry.NumberPair}");

        if (entry.ChangeType != "Remove")
        {
            sb.AppendLine($"Новый предмет: {entry.Subject ?? "—"}");
            sb.AppendLine($"Преподаватель: {entry.TeacherName ?? "—"}");
        }

        if (entry.ChangeType is "Remove" or "Replace" or "Move")
        {
            sb.AppendLine($"Снимается: {entry.RemovedSubject ?? "—"}");
            if (entry.RemovedTeacherName is not null)
                sb.AppendLine($"Снимаемый преподаватель: {entry.RemovedTeacherName}");
        }

        if (entry.Note is not null)
            sb.AppendLine($"Примечание: {entry.Note}");

        return sb.ToString().TrimEnd();
    }
}
