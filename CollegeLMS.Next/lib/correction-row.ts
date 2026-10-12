import type { CorrectionChangeType } from "@/types/correction"

/**
 * Строка позиции корректировки в том виде, в каком она уходит в файл XLSX и в
 * уведомление бота. Форма показывает её в модальном окне перед сохранением, а
 * предпросмотр файла — в таблице пакета: правила должны быть одни и те же.
 */
export interface CorrectionRowPreview {
  groupName: string
  removedSubject: string
  removedTeacher: string
  addedSubject: string
  addedTeacher: string
  numberPair: number
  note: string
  /**
   * Пара «откуда» при переносе. В файле для переноса заполняется только
   * примечание «вм.X», поэтому колонка «снимается» пустая — показываем
   * освобождаемую пару отдельно, чтобы потерянной она не выглядела.
   */
  movedFrom: { pair: number; lesson: string } | null
}

export interface CorrectionRowInput {
  changeType: CorrectionChangeType
  groupName: string
  numberPair: number
  subject: string | null
  teacherName: string | null
  removedSubject: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string | null
}

/** Преподаватель так же, как в файле: «Иванов И.И.». */
export function shortTeacherName(value: string | null | undefined): string {
  if (!value) return ""
  return value
    .split("/")
    .map((part) => {
      const pieces = part.trim().split(/\s+/).filter(Boolean)
      if (pieces.length < 2) return part.trim()
      const initials = pieces
        .slice(1)
        .map((piece) =>
          piece.length === 1 || piece.endsWith(".") ? piece : `${piece[0].toUpperCase()}.`,
        )
        .join("")
      return `${pieces[0]} ${initials}`
    })
    .join("/")
}

/**
 * Одинаковый текст: регистр и лишние пробелы не учитываются. Так «Рахимова
 * А.Л.» и «рахимова а.л.» — один преподаватель.
 */
function sameText(left: string | null | undefined, right: string | null | undefined) {
  const normalize = (value: string | null | undefined) =>
    (value ?? "").trim().toLowerCase().replace(/\s+/g, " ")
  return normalize(left) === normalize(right)
}

/** Пара «откуда» при переносе: `RemovedNumberPair`, если он отличается от пары ввода. */
function movePairOf(input: CorrectionRowInput): number | null {
  return input.removedNumberPair != null && input.removedNumberPair !== input.numberPair
    ? input.removedNumberPair
    : null
}

/**
 * Чистый перенос: в `removed*` лежит само вводимое занятие, и пара «откуда» от
 * него отличается. Снимать нечего — занятие просто меняет пару, поэтому графа
 * «снимается» остаётся пустой, а пара «откуда» живёт в примечании «вм.X».
 *
 * Отличить перенос от замены с переносом (где снимается ещё и прежнее занятие
 * выбранной пары) можно только сравнением снимаемого с вводимым.
 */
function isPureMove(input: CorrectionRowInput): boolean {
  return (
    input.changeType !== "Remove" &&
    movePairOf(input) != null &&
    input.removedSubject != null &&
    sameText(input.removedSubject, input.subject) &&
    sameText(input.removedTeacherName, input.teacherName)
  )
}

/**
 * Строка позиции для файла. Замена заполняет обе колонки, а перенос — только
 * примечанием «вм.X»: вводимое занятие само переезжает из старой пары, и в
 * колонке «снимается» для него места нет.
 */
export function toCorrectionRow(input: CorrectionRowInput): CorrectionRowPreview {
  const isRemove = input.changeType === "Remove"
  const isMove = isPureMove(input)
  const movePair = movePairOf(input)

  const note = input.note ?? (movePair != null ? `вм.${movePair}` : "")

  // Освобождаемая пара «откуда»: из неё уходит вводимое занятие, поэтому в
  // подсказке оно и называется — даже когда снимается ещё и прежнее занятие
  // выбранной пары (замена с переносом). Это подсказка рядом с таблицей, а не
  // содержимое ячейки: в графе «снимается» при переносе по-прежнему пусто.
  const movedFrom =
    movePair != null
      ? {
          pair: movePair,
          lesson: [input.subject, shortTeacherName(input.teacherName)]
            .filter(Boolean)
            .join(" "),
        }
      : null

  return {
    groupName: input.groupName,
    removedSubject: isMove ? "" : (input.removedSubject ?? ""),
    removedTeacher: isMove ? "" : shortTeacherName(input.removedTeacherName),
    addedSubject: isRemove ? "" : (input.subject ?? ""),
    addedTeacher: isRemove ? "" : shortTeacherName(input.teacherName),
    numberPair: input.numberPair,
    note,
    movedFrom,
  }
}
