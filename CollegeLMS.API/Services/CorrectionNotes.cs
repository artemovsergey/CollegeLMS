using System.Text.RegularExpressions;

namespace CollegeLMS.API.Services;

/// <summary>
/// Служебные слова примечания позиции корректировки. Логику задают только
/// три из них, остальной текст — информация для преподавателей и студентов.
///
/// <list type="bullet">
/// <item><c>сам.р.</c> — самостоятельная работа: <b>новая</b> пара в расписание
/// <b>не</b> встаёт, нужна только пометка. Существующая пара под этим словом
/// просто помечается для студентов, а перенесённая (<c>вм.X</c>) остаётся в
/// расписании с бейджем — она не новая;</item>
/// <item><c>сам.р+</c> — та же самостоятельная работа, но пара <b>всегда</b>
/// добавляется в расписание и помечается;</item>
/// <item><c>вм.X</c> — пара «откуда»: занятие преподавателя с предметом
/// замены переносится из пары X в выбранную. Действует, только если такой
/// преподаватель с этим предметом есть в группе в этот день.</item>
/// </list>
/// </summary>
internal static class CorrectionNotes
{
    /// <summary>Самостоятельная работа в любом виде: «сам.р.», «сам/р», «сам-р».</summary>
    private static readonly Regex SelfStudyRegex = new(
        @"сам[\s./\-]*р",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Самостоятельная работа с добавлением в расписание: «сам.р+», «сам.р.+».
    /// Плюс стоит сразу после слова, поэтому «сам.р. пара» — это ещё не оно.
    /// </summary>
    private static readonly Regex PhysicalSelfStudyRegex = new(
        @"сам[\s./\-]*р[\s.]*\+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>Пара «откуда»: «вм.4», «вм. 4 п.».</summary>
    private static readonly Regex MovePairRegex = new(
        @"вм\.?\s*(\d{1,2})\s*п?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>Помечена ли пара как самостоятельная работа.</summary>
    public static bool IsSelfStudy(string? note) =>
        !string.IsNullOrWhiteSpace(note) && SelfStudyRegex.IsMatch(note);

    /// <summary>Самостоятельная работа, которая всё же добавляется в расписание.</summary>
    public static bool IsPhysicalSelfStudy(string? note) =>
        !string.IsNullOrWhiteSpace(note) && PhysicalSelfStudyRegex.IsMatch(note);

    /// <summary>
    /// Пара только для информирования: в расписание не встаёт, в журнале и
    /// уведомлениях остаётся. Всё, что не помечено <c>сам.р+</c>.
    ///
    /// Про перенос с пометкой («вм.X п.сам/р.») это слово отвечает только за
    /// бейдж: пара не новая, она переезжает и остаётся в расписании. Вставать
    /// или нет решает движок применения, у которого есть <c>RemovedNumberPair</c>.
    /// </summary>
    public static bool IsInformational(string? note) =>
        IsSelfStudy(note) && !IsPhysicalSelfStudy(note);

    /// <summary>Пара «откуда» из примечания или <c>null</c>, если переноса нет.</summary>
    public static int? MovePair(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var match = MovePairRegex.Match(note);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }
}
