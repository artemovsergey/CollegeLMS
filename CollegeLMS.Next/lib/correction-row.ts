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
  /**
   * Перенос ли позиция — признак с сервера (`isPureMove` в DTO позиции).
   *
   * Считать его здесь нельзя: сравнение предмета у клиента сводилось к
   * регистру и пробелам, а у сервера — к ключу предмета, где «ОБП и ЗР» и
   * «ОБПиЗР» это одно. Из-за этого предпросмотр показывал занятие в графе
   * «снимается» там, где файл её оставлял пустой.
   */
  isPureMove: boolean
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

/** Пара «откуда» при переносе: `RemovedNumberPair`, если он отличается от пары ввода. */
function movePairOf(input: CorrectionRowInput): number | null {
  return input.removedNumberPair != null && input.removedNumberPair !== input.numberPair
    ? input.removedNumberPair
    : null
}

/**
 * Строка позиции для файла. Замена заполняет обе колонки, а перенос — только
 * примечанием «вм.X»: вводимое занятие само переезжает из старой пары, и в
 * колонке «снимается» для него места нет.
 */
export function toCorrectionRow(input: CorrectionRowInput): CorrectionRowPreview {
  const isRemove = input.changeType === "Remove"
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
    // Перенос ничего не снимает — графы «снимается» пустые, как в файле.
    removedSubject: input.isPureMove ? "" : (input.removedSubject ?? ""),
    removedTeacher: input.isPureMove ? "" : shortTeacherName(input.removedTeacherName),
    addedSubject: isRemove ? "" : (input.subject ?? ""),
    addedTeacher: isRemove ? "" : shortTeacherName(input.teacherName),
    numberPair: input.numberPair,
    note,
    movedFrom,
  }
}
