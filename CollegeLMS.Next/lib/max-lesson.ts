import type { LessonType } from "@/types/schedule"
import type { ScheduleResponse } from "@/types/schedule"
import { isValidDate, parseIsoDate, toDateFromTime } from "@/api/schedule"

export const WEEKDAYS = [
  { value: 1, label: "Пн", full: "Понедельник" },
  { value: 2, label: "Вт", full: "Вторник" },
  { value: 3, label: "Ср", full: "Среда" },
  { value: 4, label: "Чт", full: "Четверг" },
  { value: 5, label: "Пт", full: "Пятница" },
  { value: 6, label: "Сб", full: "Суббота" },
]

export const DAY_RU: Record<string, string> = {
  Monday: "Понедельник",
  Tuesday: "Вторник",
  Wednesday: "Среда",
  Thursday: "Четверг",
  Friday: "Пятница",
  Saturday: "Суббота",
  Sunday: "Воскресенье",
}

export function dayLabelFromInt(dayOfWeek: number): string {
  return WEEKDAYS.find((d) => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
}

export function dayLabelFromString(dayOfWeek: string): string {
  return DAY_RU[dayOfWeek] ?? dayOfWeek
}

export function formatDay(value: string): string {
  const date = parseIsoDate(value)
  if (!isValidDate(date)) return ""
  return date.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
  })
}

// Склонение «пар»: 1 пара, 2 пары, 5 пар (с учётом 11–14).
export function pluralPairs(n: number): string {
  const mod10 = n % 10
  const mod100 = n % 100
  if (mod10 === 1 && mod100 !== 11) return "пара"
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return "пары"
  return "пар"
}

export function formatTime(value: string): string {
  return value.slice(0, 5)
}

export function lessonTypeLabel(type: LessonType): string {
  return (
    {
      Lecture: "Лекция",
      Practice: "Практика",
      Lab: "Лабораторная",
      Exam: "Экзамен",
      None: "",
    }[type] ?? ""
  )
}

export function lessonTypeColor(type: LessonType): string {
  return (
    {
      Lecture: "#3478f6",
      Practice: "#28a745",
      Lab: "#e6a700",
      Exam: "#e04f5f",
      None: "#8b929a",
    }[type] ?? "#8b929a"
  )
}

export interface CurrentAndNext {
  current?: ScheduleResponse
  next?: ScheduleResponse
}

export function currentPair(
  entries: ScheduleResponse[],
  now: Date = new Date(),
): CurrentAndNext {
  const sorted = [...entries].sort((a, b) => a.numberPair - b.numberPair)
  const current = sorted.find((e) => {
    const start = toDateFromTime(e.startTime, now)
    const end = toDateFromTime(e.endTime, now)
    return now >= start && now <= end
  })
  const next = sorted.find((e) => now < toDateFromTime(e.startTime, now))
  return { current, next }
}