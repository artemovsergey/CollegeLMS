"use client"

import type { ScheduleResponse } from "@/types/schedule"
import {
  LESSON_TYPE_LABELS,
  LESSON_TYPE_STYLES,
} from "@/types/schedule"
import {
  Clock,
  MapPin,
  GraduationCap,
  Users,
  Calendar,
  Trash2,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface ScheduleCardsProps {
  entries: ScheduleResponse[]
  selectedDay: number | null
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

const DAY_COLORS: Record<number, string> = {
  1: "from-blue-500/10 to-blue-500/5 border-l-blue-500",
  2: "from-emerald-500/10 to-emerald-500/5 border-l-emerald-500",
  3: "from-amber-500/10 to-amber-500/5 border-l-amber-500",
  4: "from-purple-500/10 to-purple-500/5 border-l-purple-500",
  5: "from-rose-500/10 to-rose-500/5 border-l-rose-500",
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
    <div className="flex flex-col gap-2">
      {sorted.map((entry) => (
        <div
          key={entry.id}
          className={cn(
            "group relative flex items-stretch gap-3 rounded-lg border-l-[3px] bg-gradient-to-r p-3 transition-colors",
            DAY_COLORS[entry.dayOfWeek] ?? "from-gray-500/10 to-gray-500/5 border-l-gray-400",
            onEntryClick
              ? "cursor-pointer hover:shadow-sm hover:border-l-4"
              : "",
          )}
          onClick={() => onEntryClick?.(entry)}
        >
          <div className="flex flex-col items-center justify-center min-w-[40px]">
            <span className="text-lg font-bold text-primary leading-none">
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

          <div className="flex flex-col items-end justify-between shrink-0">
            <span className="rounded-full bg-background/80 px-2 py-0.5 text-[10px] font-medium text-muted-foreground border">
              {LESSON_TYPE_LABELS[entry.lessonType]}
            </span>
            <span className="text-[10px] text-muted-foreground whitespace-nowrap">
              {formatTimeSlot(entry.startTime, entry.endTime)}
            </span>
          </div>

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
      ))}
    </div>
  )
}
