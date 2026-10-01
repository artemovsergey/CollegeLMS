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

function sameLesson(left: string | null | undefined, right: string | null | undefined) {
  const normalize = (value: string | null | undefined) =>
    (value ?? "").trim().toLowerCase().replace(/\s+/g, " ")
  return normalize(left) === normalize(right)
}

/**
 * Строка позиции для файла. Замена заполняет обе колонки, а перенос — только
 * примечанием «вм.X»: вводимое занятие само переезжает из старой пары, и в
 * колонке «снимается» для него места нет.
 */
export function toCorrectionRow(input: CorrectionRowInput): CorrectionRowPreview {
  const isRemove = input.changeType === "Remove"
  const isMove =
    !isRemove &&
    input.removedSubject != null &&
    sameLesson(input.removedSubject, input.subject)

  const note =
    input.note ??
    (input.removedNumberPair != null && input.removedNumberPair !== input.numberPair
      ? `вм.${input.removedNumberPair}`
      : "")

  // При переносе снимается само вводимое занятие, но диспетчеру в файле и в
  // предпросмотре нужно видеть его в колонке «снимается» — пара «откуда»
  // остаётся в примечании «вм.X».
  const movedFrom =
    isMove && input.removedNumberPair != null && input.removedNumberPair !== input.numberPair
      ? {
          pair: input.removedNumberPair,
          lesson: [input.removedSubject, shortTeacherName(input.removedTeacherName)]
            .filter(Boolean)
            .join(" "),
        }
      : null

  return {
    groupName: input.groupName,
    removedSubject: input.removedSubject ?? "",
    removedTeacher: shortTeacherName(input.removedTeacherName),
    addedSubject: isRemove ? "" : (input.subject ?? ""),
    addedTeacher: isRemove ? "" : shortTeacherName(input.teacherName),
    numberPair: input.numberPair,
    note,
    movedFrom,
  }
}
