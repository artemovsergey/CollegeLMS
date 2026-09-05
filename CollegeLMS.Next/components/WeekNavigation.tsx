"use client"

import { Button } from "@/components/ui/button"
import { ChevronLeft, ChevronRight, CalendarDays } from "lucide-react"

interface WeekNavigationProps {
  currentWeek: number
  onChange: (week: number) => void
  totalWeeks?: number
}

const SEMESTER_START = new Date(2026, 8, 1) // 1 сентября 2026

function getMondayOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const offset = day === 0 ? 6 : day - 1
  d.setDate(d.getDate() - offset)
  d.setHours(0, 0, 0, 0)
  return d
}

function getWeekDates(week: number): { start: Date; end: Date } {
  const start = new Date(SEMESTER_START)
  if (week <= 1) {
    const end = new Date(start)
    end.setDate(end.getDate() + 3) // 01.09 → 04.09
    return { start, end }
  }
  start.setDate(start.getDate() + (week - 1) * 7 - 1)
  const end = new Date(start)
  end.setDate(end.getDate() + 4)
  return { start, end }
}

function formatDate(d: Date): string {
  return `${d.getDate().toString().padStart(2, "0")}.${(d.getMonth() + 1).toString().padStart(2, "0")}`
}

export default function WeekNavigation({
  currentWeek,
  onChange,
  totalWeeks = 52,
}: WeekNavigationProps) {
  const { start, end } = getWeekDates(currentWeek)
  const isCurrentWeek = (() => {
    const now = new Date()
    const currentMonday = getMondayOfWeek(now)
    const semesterMonday = getMondayOfWeek(SEMESTER_START)
    const diffMs = currentMonday.getTime() - semesterMonday.getTime()
    const diffWeeks = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000))
    return diffWeeks + 1 === currentWeek
  })()

  const goToToday = () => {
    const now = new Date()
    const currentMonday = getMondayOfWeek(now)
    const semesterMonday = getMondayOfWeek(SEMESTER_START)
    const diffMs = currentMonday.getTime() - semesterMonday.getTime()
    const diffWeeks = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000))
    onChange(Math.max(1, Math.min(totalWeeks, diffWeeks + 1)))
  }

  return (
    <div className="flex items-center gap-2">
      <Button
        variant="outline"
        size="icon"
        className="size-8"
        onClick={() => onChange(Math.max(1, currentWeek - 1))}
        disabled={currentWeek <= 1}
      >
        <ChevronLeft className="size-4" />
      </Button>

      <div className="flex items-center gap-2 min-w-0">
        <span className="text-sm font-medium whitespace-nowrap">
          Неделя {currentWeek}
        </span>
        <span className="text-xs text-muted-foreground whitespace-nowrap hidden sm:inline">
          {formatDate(start)} – {formatDate(end)}
        </span>
      </div>

      <Button
        variant="outline"
        size="icon"
        className="size-8"
        onClick={() => onChange(Math.min(totalWeeks, currentWeek + 1))}
        disabled={currentWeek >= totalWeeks}
      >
        <ChevronRight className="size-4" />
      </Button>

      {!isCurrentWeek && (
        <Button variant="outline" size="sm" onClick={goToToday} className="ml-1">
          <CalendarDays className="size-3.5 mr-1" />
          Сегодня
        </Button>
      )}
    </div>
  )
}
