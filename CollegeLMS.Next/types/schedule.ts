export type LessonType = "Lecture" | "Practice" | "Lab" | "Exam"

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
}

export const LESSON_TYPE_STYLES: Record<LessonType, string> = {
  Lecture: "border-l-blue-500 bg-blue-50/60 dark:bg-blue-950/20",
  Practice: "border-l-emerald-500 bg-emerald-50/60 dark:bg-emerald-950/20",
  Lab: "border-l-amber-500 bg-amber-50/60 dark:bg-amber-950/20",
  Exam: "border-l-red-500 bg-red-50/60 dark:bg-red-950/20",
}
