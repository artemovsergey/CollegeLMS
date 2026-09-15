import { parseIsoDate } from "@/api/schedule"

// Начало семестра — единый источник для mini-app.
// Синхронизировано с StudyWeek.SemesterStart в боте и API /api/schedule/meta.
export const SEMESTER_START = new Date(2026, 8, 1) // 1 сентября 2026

export function mondayOfWeekOne(semesterStart: Date = SEMESTER_START): Date {
  const result = new Date(semesterStart)
  result.setDate(result.getDate() - ((semesterStart.getDay() + 6) % 7))
  return result
}

// Дата занятия по номеру недели (с 1) и смещению дня от понедельника (Пн=0).
export function dateForLesson(
  semesterStartIso: string | undefined,
  week: number,
  dayOffset: number,
): Date {
  const base =
    semesterStartIso && semesterStartIso.length > 0
      ? parseIsoDate(semesterStartIso)
      : SEMESTER_START
  const monday = mondayOfWeekOne(base)
  monday.setDate(monday.getDate() + (week - 1) * 7 + dayOffset)
  return monday
}
