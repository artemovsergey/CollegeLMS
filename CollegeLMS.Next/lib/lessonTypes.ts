import type { LessonKind } from "@/types"

export const LESSON_KIND_LABELS: Record<LessonKind, string> = {
  Lecture: "Лекция",
  Practice: "Практика",
  SelfStudy: "Самостоятельная работа",
}

export const LESSON_KIND_VARIANTS: Record<
  LessonKind,
  "default" | "secondary" | "outline" | "destructive"
> = {
  Lecture: "default",
  Practice: "secondary",
  SelfStudy: "outline",
}
