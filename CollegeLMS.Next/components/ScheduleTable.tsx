"use client"

import type { ScheduleResponse } from "@/types/schedule"
import {
  Clock,
  MapPin,
  GraduationCap,
  Users,
  Calendar,
  Trash2,
  Radio,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface ScheduleCardsProps {
  entries: ScheduleResponse[]
  selectedDay: number | null
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
}

const SEMESTER_START = new Date(2026, 8, 1)

function getMondayOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const offset = day === 0 ? 6 : day - 1
  d.setDate(d.getDate() - offset)
  d.setHours(0, 0, 0, 0)
  return d
}

function getCurrentWeek(): number {
  const now = new Date()
  const currentMonday = getMondayOfWeek(now)
  const semesterMonday = getMondayOfWeek(SEMESTER_START)
  const diffMs = currentMonday.getTime() - semesterMonday.getTime()
  const diffWeeks = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000))
  return Math.max(1, diffWeeks + 1)
}

function formatTime(time: string) {
  return time.slice(0, 5)
}

function formatTimeSlot(start: string, end: string) {
  return `${formatTime(start)} – ${formatTime(end)}`
}

function formatWeeks(weeks: number[]): string {
  if (weeks.length === 0) return ""
  if (weeks.length === 16 && weeks[0] === 1 && weeks[weeks.length - 1] === 16)
    return "все"
  const ranges: string[] = []
  let start = weeks[0]
  let end = weeks[0]
  for (let i = 1; i < weeks.length; i++) {
    if (weeks[i] === end + 1) {
      end = weeks[i]
    } else {
      ranges.push(start === end ? `${start}` : `${start}-${end}`)
      start = weeks[i]
      end = weeks[i]
    }
  }
  ranges.push(start === end ? `${start}` : `${start}-${end}`)
  return ranges.join(", ")
}

function isCurrentlyHappening(entry: ScheduleResponse): boolean {
  const now = new Date()
  const dayOfWeek = now.getDay() === 0 ? 7 : now.getDay()
  if (dayOfWeek !== entry.dayOfWeek) return false

  const currentWeek = getCurrentWeek()
  if (!entry.weeks.includes(currentWeek)) return false

  const currentMinutes = now.getHours() * 60 + now.getMinutes()
  const [sh, sm] = entry.startTime.split(":").map(Number)
  const [eh, em] = entry.endTime.split(":").map(Number)
  const startMinutes = sh * 60 + sm
  const endMinutes = eh * 60 + em

  return currentMinutes >= startMinutes && currentMinutes < endMinutes
}

export default function ScheduleCards({
  entries,
  selectedDay,
  onEntryClick,
  onDeleteClick,
}: ScheduleCardsProps) {
  const filtered = selectedDay
    ? entries.filter((e) => e.dayOfWeek === selectedDay)
    : entries

  const sorted = [...filtered].sort((a, b) => {
    if (a.dayOfWeek !== b.dayOfWeek) return a.dayOfWeek - b.dayOfWeek
    return a.numberPair - b.numberPair
  })

  const currentId = sorted.find(isCurrentlyHappening)?.id ?? null

  if (sorted.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
        <Calendar className="size-12 opacity-40" />
        <p className="text-lg font-medium">Нет занятий</p>
        <p className="text-sm">На выбранный период расписание не найдено</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      {sorted.map((entry) => {
        const isCurrent = entry.id === currentId
        return (
          <div
            key={entry.id}
            className={cn(
              "group relative flex items-stretch gap-3 rounded-lg border-t-2 bg-card p-3 transition-colors",
              isCurrent
                ? "border-primary bg-primary/[0.06] dark:bg-primary/[0.12]"
                : "border-t-transparent",
            )}
            onClick={() => onEntryClick?.(entry)}
          >
            <div className="flex flex-col items-center justify-center min-w-[40px]">
              <span
                className={cn(
                  "text-lg font-bold leading-none",
                  isCurrent ? "text-primary" : "text-primary",
                )}
              >
                {entry.numberPair}
              </span>
              <span className="mt-1 text-[10px] text-muted-foreground whitespace-nowrap">
                {formatTime(entry.startTime)}
              </span>
            </div>

            <div className="flex-1 min-w-0">
              <p className="font-semibold text-sm leading-tight truncate">
                {entry.subject}
              </p>
              {isCurrent && (
                <span className="mt-0.5 inline-flex items-center gap-1 text-[11px] font-medium text-primary">
                  <Radio className="size-3 animate-pulse" aria-hidden />
                  Сейчас идёт
                </span>
              )}
              <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-muted-foreground">
                {entry.teacherName && (
                  <span className="flex items-center gap-1">
                    <GraduationCap className="size-3 shrink-0" />
                    {entry.teacherName}
                  </span>
                )}
                <span className="flex items-center gap-1">
                  <MapPin className="size-3 shrink-0" />
                  {entry.room}
                </span>
                <span className="flex items-center gap-1">
                  <Users className="size-3 shrink-0" />
                  {entry.groupName}
                </span>
              </div>
              {entry.weeks && entry.weeks.length > 0 && (
                <span className="mt-1 inline-flex items-center gap-1 text-[11px] text-muted-foreground">
                  <Calendar className="size-3" />
                  нед. {formatWeeks(entry.weeks)}
                </span>
              )}
            </div>

            <span className="text-[10px] text-muted-foreground whitespace-nowrap shrink-0 self-center">
              {formatTimeSlot(entry.startTime, entry.endTime)}
            </span>

            {onDeleteClick && (
              <Button
                variant="ghost"
                size="icon"
                aria-label="Удалить занятие"
                className="absolute right-1 top-1 size-5 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={(e) => {
                  e.stopPropagation()
                  onDeleteClick(entry.id)
                }}
              >
                <Trash2 className="size-3 text-destructive" />
              </Button>
            )}
          </div>
        )
      })}
    </div>
  )
}
