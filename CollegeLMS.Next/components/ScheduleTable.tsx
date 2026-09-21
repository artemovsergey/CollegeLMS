"use client"

import type { ScheduleResponse } from "@/types/schedule"
import type { ScheduleInsert } from "@/api/inserts"
import {
  Clock,
  MapPin,
  GraduationCap,
  Users,
  Calendar,
  Pencil,
  Trash2,
  Radio,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import ChangeTagBadge from "@/components/ChangeTagBadge"
import { InsertRow } from "@/components/ScheduleLayers"
import { isEntryNow, mergeDayRows, type DayRow } from "@/lib/schedule-merge"
import { cn } from "@/lib/utils"

interface ScheduleCardsProps {
  entries: ScheduleResponse[]
  inserts: ScheduleInsert[]
  selectedDay: number | null
  /** Текущая учебная неделя — нужна для подсветки «Сейчас идёт». */
  currentWeek?: number
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
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

function isCurrentlyHappening(
  entry: ScheduleResponse,
  currentWeek?: number,
): boolean {
  if (currentWeek === undefined) return false
  return isEntryNow(entry, new Date().getDay(), currentWeek)
}

export default function ScheduleCards({
  entries,
  inserts,
  selectedDay,
  currentWeek,
  onEntryClick,
  onDeleteClick,
}: ScheduleCardsProps) {
  const filteredEntries = selectedDay
    ? entries.filter((e) => e.dayOfWeek === selectedDay)
    : entries

  const filteredInserts = selectedDay
    ? inserts.filter((i) => i.dayOfWeek === selectedDay)
    : inserts

  const rows: DayRow[] = mergeDayRows(filteredEntries, filteredInserts)

  const currentId =
    filteredEntries.find((entry) => isCurrentlyHappening(entry, currentWeek))
      ?.id ?? null

  if (rows.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
        <Calendar className="size-12 opacity-40" />
        <p className="text-lg font-medium">Нет занятий</p>
        <p className="text-sm">На выбранный период расписание не найдено</p>
      </div>
    )
  }

  const hasActions = Boolean(onEntryClick || onDeleteClick)

  return (
    <div className="flex flex-col gap-3">
      {rows.map((row) => {
        if (row.kind === "insert") {
          return <InsertRow key={`insert-${row.insert.id}`} insert={row.insert} />
        }

        const entry = row.entry
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
          >
            <div className="flex min-w-[40px] flex-col items-center justify-center">
              <span className="text-lg font-bold leading-none text-primary">
                {entry.numberPair}
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
                <span className="flex items-center gap-1">
                  <Clock className="size-3 shrink-0" />
                  {formatTimeSlot(entry.startTime, entry.endTime)}
                </span>
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
              {entry.changeTags && entry.changeTags.length > 0 && (
                <div className="mt-1.5 flex flex-wrap gap-1">
                  {entry.changeTags.map((tag, i) => (
                    <ChangeTagBadge key={i} tag={tag} />
                  ))}
                </div>
              )}
            </div>

            {hasActions && (
              <div className="flex shrink-0 items-start gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 max-sm:opacity-100">
                {onEntryClick && (
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Редактировать пару"
                    className="relative size-8 text-muted-foreground after:absolute after:-inset-1.5 hover:bg-primary/[0.08] hover:text-primary dark:hover:bg-primary/[0.12]"
                    onClick={() => onEntryClick(entry)}
                  >
                    <Pencil className="size-3.5" aria-hidden />
                  </Button>
                )}
                {onDeleteClick && (
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Удалить пару"
                    className="relative size-8 text-destructive after:absolute after:-inset-1.5 hover:bg-destructive/10 hover:text-destructive dark:hover:bg-destructive/20"
                    onClick={() => onDeleteClick(entry.id)}
                  >
                    <Trash2 className="size-3.5" aria-hidden />
                  </Button>
                )}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
