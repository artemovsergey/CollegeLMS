/** Дата и время справочников расписания. API отдаёт время "HH:mm:ss", даты — ISO. */

/** "08:30:00" → "08:30" (для <input type="time">). */
export function toTimeInput(value: string | null | undefined): string {
  return value ? value.slice(0, 5) : ""
}

/** "08:30" → "08:30:00" (обратно в API). */
export function toApiTime(value: string): string {
  if (!value) return value
  return value.length === 5 ? `${value}:00` : value
}

/** ISO-дата → "YYYY-MM-DD" (для <input type="date">). */
export function toDateInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : ""
}

/** ISO-дата → "ДД.ММ.ГГГГ". */
export function formatDate(value: string | null | undefined): string {
  if (!value) return "—"
  const [year, month, day] = value.slice(0, 10).split("-")
  if (!year || !month || !day) return "—"
  return `${day}.${month}.${year}`
}

/** ISO-дата → "ДД.ММ.ГГГГ" или "ДД.ММ — ДД.ММ.ГГГГ" для периода. */
export function formatDateRange(from: string, to: string): string {
  const shortFrom = forceTrimYear(from)
  const fullTo = formatDate(to)
  if (from.slice(0, 10) === to.slice(0, 10)) return fullTo
  return `${shortFrom} — ${fullTo}`
}

function forceTrimYear(value: string): string {
  const [year, month, day] = value.slice(0, 10).split("-")
  if (!year || !month || !day) return "—"
  return `${day}.${month}`
}

export const DAY_OF_WEEK_LABELS: Record<number, string> = {
  0: "Воскресенье",
  1: "Понедельник",
  2: "Вторник",
  3: "Среда",
  4: "Четверг",
  5: "Пятница",
  6: "Суббота",
}

/** Винительный падеж дней (1–5) для подписи «за …» у рабочего дня. */
export const SUBSTITUTE_DAY_ACCUSATIVE: Record<number, string> = {
  1: "понедельник",
  2: "вторник",
  3: "среду",
  4: "четверг",
  5: "пятницу",
}

/** Подпись рабочего дня: «Работа в субботу: за понедельник». */
export function workingDayLabel(
  title: string | null | undefined,
  substituteDayOfWeek: number | null | undefined,
): string {
  const base = title?.trim()
  const substitute = substituteDayOfWeek
    ? SUBSTITUTE_DAY_ACCUSATIVE[substituteDayOfWeek]
    : undefined
  if (!base) {
    return substitute ? `Работа в выходной: за ${substitute}` : "Рабочий день"
  }
  return substitute ? `${base}: за ${substitute}` : base
}

/** Подпись дня-заменителя для таблиц: «За понедельник» / «—». */
export function substituteDayLabel(value: number | null | undefined): string {
  if (!value) return "—"
  const accusative = SUBSTITUTE_DAY_ACCUSATIVE[value]
  return accusative ? `За ${accusative}` : "—"
}

/** Дни недели профилей звонков: 1 = Пн … 7 = Вс. */
export const WEEK_DAYS = [
  { value: 1, label: "Пн", full: "Понедельник" },
  { value: 2, label: "Вт", full: "Вторник" },
  { value: 3, label: "Ср", full: "Среда" },
  { value: 4, label: "Чт", full: "Четверг" },
  { value: 5, label: "Пт", full: "Пятница" },
  { value: 6, label: "Сб", full: "Суббота" },
  { value: 7, label: "Вс", full: "Воскресенье" },
] as const

/** Дни, допустимые для вставок (без воскресенья): значение API-строки + число + подпись. */
export const INSERT_DAYS = [
  { value: "Monday", num: 1, label: "Понедельник" },
  { value: "Tuesday", num: 2, label: "Вторник" },
  { value: "Wednesday", num: 3, label: "Среда" },
  { value: "Thursday", num: 4, label: "Четверг" },
  { value: "Friday", num: 5, label: "Пятница" },
  { value: "Saturday", num: 6, label: "Суббота" },
] as const

export type InsertDayValue = (typeof INSERT_DAYS)[number]["value"]

export const PAIR_NUMBERS = [1, 2, 3, 4, 5, 6, 7, 8] as const

export const COURSE_NUMBERS = [1, 2, 3, 4] as const
