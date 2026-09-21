"use client"

import { Button } from "@/components/ui/button"
import { ChevronLeft, ChevronRight, CalendarDays } from "lucide-react"

interface WeekNavigationProps {
  currentWeek: number
  onChange: (week: number) => void
  totalWeeks: number
  /** Начало семестра (из GET /api/schedule/meta). */
  semesterStart: Date
  /** Текущая учебная неделя (из GET /api/schedule/meta). */
  todayWeek?: number
}

function mondayOfWeekOne(semesterStart: Date): Date {
  const d = new Date(semesterStart)
  const offset = (d.getDay() + 6) % 7
  d.setDate(d.getDate() - offset)
  d.setHours(0, 0, 0, 0)
  return d
}

function getWeekDates(
  week: number,
  semesterStart: Date,
): { start: Date; end: Date } {
  const monday = mondayOfWeekOne(semesterStart)
  const start = new Date(monday)
  start.setDate(start.getDate() + (week - 1) * 7)
  const end = new Date(start)
  end.setDate(end.getDate() + 4) // Пн–Пт
  return { start, end }
}

function formatDate(d: Date): string {
  return `${d.getDate().toString().padStart(2, "0")}.${(d.getMonth() + 1)
    .toString()
    .padStart(2, "0")}`
}

export default function WeekNavigation({
  currentWeek,
  onChange,
  totalWeeks,
  semesterStart,
  todayWeek,
}: WeekNavigationProps) {
  const { start, end } = getWeekDates(currentWeek, semesterStart)
  const isCurrentWeek = todayWeek !== undefined && todayWeek === currentWeek

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        variant="outline"
        size="icon"
        className="size-11"
        aria-label="Предыдущая неделя"
        onClick={() => onChange(Math.max(1, currentWeek - 1))}
        disabled={currentWeek <= 1}
      >
        <ChevronLeft className="size-4" aria-hidden />
      </Button>

      <div className="flex min-w-0 items-center gap-2">
        <span className="whitespace-nowrap text-sm font-medium">
          Неделя {currentWeek}
        </span>
        <span className="hidden whitespace-nowrap text-xs text-muted-foreground sm:inline">
          {formatDate(start)} – {formatDate(end)}
        </span>
      </div>

      <Button
        type="button"
        variant="outline"
        size="icon"
        className="size-11"
        aria-label="Следующая неделя"
        onClick={() => onChange(Math.min(totalWeeks, currentWeek + 1))}
        disabled={currentWeek >= totalWeeks}
      >
        <ChevronRight className="size-4" aria-hidden />
      </Button>

      {todayWeek !== undefined && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange(todayWeek)}
          disabled={isCurrentWeek}
          className="ml-1 h-11"
        >
          <CalendarDays className="size-3.5" aria-hidden />
          Сегодня
        </Button>
      )}
    </div>
  )
}
