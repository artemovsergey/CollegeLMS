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
