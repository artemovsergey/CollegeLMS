"use client"

import Link from "next/link"
import type { LucideIcon } from "lucide-react"
import {
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
  ArrowLeft,
  BookOpen,
  CalendarDays,
  Clock,
  DoorOpen,
  User,
} from "lucide-react"
import type { ScheduleHistoryItem, CorrectionChangeType } from "@/types/correction"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { cn } from "@/lib/utils"
import { dateForLesson } from "@/lib/semester"
import { dayLabelFromString } from "@/lib/max-lesson"

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: {
    label: "Добавлено",
    icon: Plus,
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    icon: Minus,
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    icon: Repeat,
    className:
      "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    icon: ArrowRightLeft,
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

const DAY_OFFSET: Record<string, number> = {
  Monday: 0,
  Tuesday: 1,
  Wednesday: 2,
  Thursday: 3,
  Friday: 4,
  Saturday: 5,
  Sunday: 6,
}

function isSelfStudy(item: ScheduleHistoryItem): boolean {
  return item.note?.trim().toLowerCase() === "сам.р."
}

function formatDate(date: Date): string {
  return date.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  })
}

function formatAppliedAt(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ""
  return date.toLocaleString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

interface ChangeCardProps {
  item: ScheduleHistoryItem
  semesterStartIso?: string
  className?: string
}

export default function ChangeCard({
  item,
  semesterStartIso,
  className,
}: ChangeCardProps) {
  const meta = CHANGE_TYPE_META[item.changeType]
  const Icon = meta.icon
  const selfStudy = isSelfStudy(item)
  const dayIndex = DAY_OFFSET[item.dayOfWeek] ?? 0
  const date = dateForLesson(semesterStartIso, item.week, dayIndex)
  const appliedAt = formatAppliedAt(item.appliedAt)

  const dayLabel = dayLabelFromString(item.dayOfWeek)
  const pairLabel =
    item.removedNumberPair != null &&
    item.removedNumberPair !== item.numberPair &&
    (item.changeType === "Replace" || item.changeType === "Move")
      ? `пара ${item.removedNumberPair} → ${item.numberPair}`
      : `пара ${item.numberPair}`

  const isMoveOrReplace =
    item.changeType === "Replace" || item.changeType === "Move"
  const primarySubject =
    item.changeType === "Remove"
      ? item.removedSubject ?? item.subject
      : item.subject

  return (
    <Card
      className={cn("gap-0 py-4", className)}
      role="article"
      aria-label={`${meta.label}: ${item.subject}, ${item.groupName}`}
    >
      <CardContent className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline" className={meta.className}>
            <Icon aria-hidden /> {meta.label}
          </Badge>
          {selfStudy && (
            <Badge
              variant="outline"
              className="bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300"
            >
              <BookOpen aria-hidden /> Сам.р.
            </Badge>
          )}
          <span className="inline-flex items-center gap-1 text-sm font-semibold text-foreground">
            <CalendarDays className="size-3.5 text-muted-foreground" aria-hidden />
            {formatDate(date)}
          </span>
          <span className="text-sm text-muted-foreground">
            {dayLabel}, {item.week}-я неделя
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{item.groupName}</span>
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5" aria-hidden />
            {pairLabel}
          </span>
          {item.room && (
            <span className="inline-flex items-center gap-1">
              <DoorOpen className="size-3.5" aria-hidden />
              ауд. {item.room}
            </span>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2 text-base">
          {isMoveOrReplace && item.removedSubject && (
            <>
              <span className="text-muted-foreground line-through">
                {item.removedSubject}
              </span>
              <ArrowLeft className="size-4 rotate-180 text-muted-foreground" aria-hidden />
            </>
          )}
          <span className="font-medium">{primarySubject}</span>
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <User className="size-3.5" aria-hidden />
            {item.teacherName ?? "Преподаватель не указан"}
          </span>
          {item.note && <span>Примечание: {item.note}</span>}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-xs text-muted-foreground">
            Применено: {appliedAt || "—"}
          </span>
          <Link
            href={`/schedule?week=${item.week}&day=${dayIndex}`}
            className="inline-flex items-center gap-1 text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded"
          >
            Открыть день расписания
          </Link>
        </div>
      </CardContent>
    </Card>
  )
}
