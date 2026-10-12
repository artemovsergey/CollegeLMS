using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;

namespace CollegeLMS.API.Services;

/// <summary>
/// Что позиция корректировки делает с занятием в паре «откуда».
///
/// Перенос в единой модели — это добавление, у которого в <c>Removed*</c>
/// лежит само вводимое занятие: оно уезжает из пары «откуда» в выбранную.
/// Снимать при этом нечего — вводимое занятие не создаётся заново, а меняет
/// пару, поэтому в колонке «снимается» для него места нет, а пара «откуда»
/// остаётся в примечании «вм.X». Формат файла диспетчера именно такой.
///
/// Отличить перенос от замены с переносом (в выбранной паре стоит чужое
/// занятие, и его всё-таки снимают) можно только сравнением <c>Removed*</c>
/// с вводимым занятием, поэтому правило одно на файл, PNG и веб-предпросмотр.
/// </summary>
internal static class CorrectionMoves
{
    /// <summary>
    /// Пара «откуда» при переносе: <c>RemovedNumberPair</c>, если он отличается
    /// от пары ввода. У простой замены это поле совпадает с парой ввода, и пары
    /// «откуда» нет.
    /// </summary>
    public static int? FromPair(CorrectionPosition position) =>
        position.RemovedNumberPair is { } from && from != position.NumberPair ? from : null;

    /// <summary>
    /// Описано ли в <c>Removed*</c> именно вводимое занятие — то есть занятие
    /// не снимается, а переносится.
    /// </summary>
    public static bool IsIncomingLesson(CorrectionPosition position) =>
        position.RemovedSubject is not null
        && SameLesson(position.RemovedSubject, position.Subject)
        && SameTeacher(position);

    /// <summary>
    /// Чистый перенос: вводимое занятие само освобождает пару «откуда», слот
    /// ввода не трогается. Именно он оставляет колонку «снимается» пустой —
    /// и в файле, и в предпросмотре.
    /// </summary>
    public static bool IsPureMove(CorrectionPosition position) =>
        position.ChangeType != ScheduleChangeType.Remove
        && FromPair(position) is not null
        && IsIncomingLesson(position);

    /// <summary>Один и тот же предмет: «ОБП и ЗР» и «ОБПиЗР» — один предмет.</summary>
    private static bool SameLesson(string? left, string? right) =>
        string.Equals(
            ScheduleImportService.SubjectLookupKey(left ?? string.Empty),
            ScheduleImportService.SubjectLookupKey(right ?? string.Empty),
            StringComparison.OrdinalIgnoreCase
        );

    /// <summary>
    /// Один и тот же преподаватель: по id, когда он есть у обеих сторон,
    /// иначе по ФИО.
    /// </summary>
    private static bool SameTeacher(CorrectionPosition position) =>
        position.RemovedTeacherId.HasValue && position.TeacherId.HasValue
            ? position.RemovedTeacherId == position.TeacherId
            : string.Equals(
                ScheduleImportService.NormalizeTeacherName(position.RemovedTeacherName ?? ""),
                ScheduleImportService.NormalizeTeacherName(position.TeacherName ?? ""),
                StringComparison.OrdinalIgnoreCase
            );
}
