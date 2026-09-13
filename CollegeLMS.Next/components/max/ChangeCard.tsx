"use client"

import Link from "next/link"
import { Plus, Minus, Repeat, ArrowRightLeft, CalendarDays } from "lucide-react"
import { CellSimple, Typography } from "@maxhub/max-ui"
import type { ScheduleHistoryItem } from "@/types/correction"
import {
  dayLabelFromString,
  formatTime,
} from "@/lib/max-lesson"
import { toIsoDate } from "@/api/schedule"

const CHANGE_TYPE_META: Record<
  ScheduleHistoryItem["changeType"],
  { label: string; className: string; short: string }
> = {
  Add: { label: "Добавлено", short: "Добавлено", className: "max-app__badge--add" },
  Remove: { label: "Снято", short: "Снято", className: "max-app__badge--remove" },
  Replace: { label: "Замена", short: "Замена", className: "max-app__badge--replace" },
  Move: { label: "Перенос", short: "Перенос", className: "max-app__badge--move" },
}

const TYPE_ICONS = {
  Add: Plus,
  Remove: Minus,
  Replace: Repeat,
  Move: ArrowRightLeft,
} as const

const MONDAY_OF_WEEK_1 = new Date(2026, 7, 31)

const DAY_OFFSET: Record<string, number> = {
  Monday: 0,
  Tuesday: 1,
  Wednesday: 2,
  Thursday: 3,
  Friday: 4,
  Saturday: 5,
  Sunday: 6,
}

function changeDate(item: ScheduleHistoryItem): Date {
  const base = new Date(MONDAY_OF_WEEK_1)
  base.setDate(base.getDate() + (item.week - 1) * 7 + (DAY_OFFSET[item.dayOfWeek] ?? 0))
  return base
}

export default function ChangeCard({ item }: { item: ScheduleHistoryItem }) {
  const meta = CHANGE_TYPE_META[item.changeType]
  const Icon = TYPE_ICONS[item.changeType]

  const dayLabel = dayLabelFromString(item.dayOfWeek)

  const appliedAt = new Date(item.appliedAt ?? Date.now())
  const timeLabel = `${appliedAt.toLocaleDateString("ru-RU")} ${formatTime(
    appliedAt.toTimeString().slice(0, 5),
  )}`

  const subjectParts: string[] = []
  if (item.removedSubject) subjectParts.push(`«${item.removedSubject}» сторона снята`)
  if (item.subject) subjectParts.push(`«${item.subject}» вводится`)
  const subjectText =
    subjectParts.length > 0 ? subjectParts.join(" → ") : item.subject

  const date = changeDate(item)

  return (
    <CellSimple
      separator
      before={
        <span className={`max-app__badge ${meta.className}`}>
          <Icon size={12} aria-hidden /> {meta.short}
        </span>
      }
      title={subjectText}
      overline={`${item.groupName} · ${dayLabel}, пар ${item.numberPair}`}
      subtitle={
        <span className="max-app__note">
          {item.teacherName ?? "—"} · {timeLabel}
          {item.note ? ` · ${item.note}` : ""}
        </span>
      }
      after={
        <Link
          href={`/max/schedule?route=day&date=${toIsoDate(date)}`}
          aria-label={`Открыть расписание на ${toIsoDate(date)}`}
        >
          <CalendarDays size={18} aria-hidden />
        </Link>
      }
    />
  )
}