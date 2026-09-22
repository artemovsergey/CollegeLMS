export type LessonType = "Lecture" | "Practice" | "Lab" | "Exam" | "None"

import type { ChangeTag } from "@/types/correction"
import type { Practice, PracticeKind } from "@/api/practices"
import type { ScheduleInsert } from "@/api/inserts"

export interface ScheduleResponse {
  id: string
  groupId: string
  groupName: string
  teacherId: string | null
  teacherName: string | null
  subject: string
  room: string
  dayOfWeek: number
  numberPair: number
  startTime: string
  endTime: string
  weeks: number[]
  lessonType: LessonType
  /** Пара синтезирована из дня УП. */
  isPractice?: boolean
  /** Название практики для пар, синтезированных из дней УП. */
  practiceName?: string | null
  changeTags: ChangeTag[]
}

export const DAYS = [
  { value: 1, label: "Пн", full: "Понедельник" },
  { value: 2, label: "Вт", full: "Вторник" },
  { value: 3, label: "Ср", full: "Среда" },
  { value: 4, label: "Чт", full: "Четверг" },
  { value: 5, label: "Пт", full: "Пятница" },
  { value: 6, label: "Сб", full: "Суббота" },
  { value: 0, label: "Вс", full: "Воскресенье" },
] as const

export const LESSON_TYPE_LABELS: Record<LessonType, string> = {
  Lecture: "Лекция",
  Practice: "Практика",
  Lab: "Лабораторная",
  Exam: "Экзамен",
  None: "Занятие",
}

export const LESSON_TYPE_STYLES: Record<LessonType, string> = {
  Lecture: "border-l-blue-500 bg-blue-50/60 dark:bg-blue-950/20",
  Practice: "border-l-emerald-500 bg-emerald-50/60 dark:bg-emerald-950/20",
  Lab: "border-l-amber-500 bg-amber-50/60 dark:bg-amber-950/20",
  Exam: "border-l-red-500 bg-red-50/60 dark:bg-red-950/20",
  None: "border-l-slate-400 bg-slate-50/60 dark:bg-slate-950/20",
}

/** Режим отображения расписания на странице /schedule. */
export type ScheduleViewMode = "day" | "week" | "calendar" | "semester"

/** Серверный вид дня со слоями: вставки, практики, пары с бейджами. */
export interface ScheduleDayView {
  date: string
  week: number
  dayOfWeek: number
  isSunday: boolean
  isNonWorking: boolean
  nonWorkingTitle: string | null
  practices: Practice[]
  inserts: ScheduleInsert[]
  entries: ScheduleResponse[]
  /** День сделан рабочим (перенос с другого дня недели). */
  isWorkingDay?: boolean
  /** День недели, за который идёт работа (1–5). */
  substituteDayOfWeek?: number | null
  /** Название рабочего дня из справочника («Работа в субботу»). */
  workingDayTitle?: string | null
}

/** Серверный вид недели: Пн–Сб с датами. */
export interface ScheduleWeekView {
  week: number
  weekStart: string
  days: ScheduleDayView[]
}

export interface ScheduleSemesterWeek {
  week: number
  weekStart: string
  days: ScheduleDayView[]
}

/** Серверный вид семестра: матрица недель. */
export interface ScheduleSemesterView {
  totalWeeks: number
  weeks: ScheduleSemesterWeek[]
}

/** День месячного календаря с количеством пар и маркерами. */
export interface ScheduleMonthDay {
  date: string
  dayOfWeek: number
  isSunday: boolean
  isNonWorking: boolean
  nonWorkingTitle: string | null
  practiceKinds: PracticeKind[]
  /** Название практики в дне (первая), если есть. */
  practiceName?: string | null
  isOutOfSemester: boolean
  pairCount: number
  /** День сделан рабочим (перенос с другого дня недели). */
  isWorkingDay?: boolean
  /** День недели, за который идёт работа (1–5). */
  substituteDayOfWeek?: number | null
  /** Название рабочего дня из справочника. */
  workingDayTitle?: string | null
}

export interface ScheduleMonthView {
  year: number
  month: number
  days: ScheduleMonthDay[]
}
