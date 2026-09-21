"use client"

import { ChevronLeft, ChevronRight, CalendarDays } from "lucide-react"
import { Button } from "@/components/ui/button"
import { isValidDate, parseIsoDate, toIsoDate } from "@/api/schedule"

interface DayNavigationProps {
  /** Текущая дата в формате YYYY-MM-DD. */
  date: string
  onChange: (date: string) => void
}

/** Навигация по дню: ‹ / выбор даты / › / «Сегодня». */
export default function DayNavigation({ date, onChange }: DayNavigationProps) {
  const parsed = parseIsoDate(date)
  const base = isValidDate(parsed) ? parsed : new Date()
  const isToday = date === toIsoDate(new Date())

  const shift = (delta: number) => {
    const target = new Date(base)
    target.setDate(target.getDate() + delta)
    onChange(toIsoDate(target))
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        variant="outline"
        size="icon"
        className="size-11"
        aria-label="Предыдущий день"
        onClick={() => shift(-1)}
      >
        <ChevronLeft className="size-4" aria-hidden />
      </Button>

      <input
        type="date"
        value={date}
        onChange={(e) => {
          if (e.target.value) onChange(e.target.value)
        }}
        aria-label="Выбрать дату"
        className="h-11 rounded-md border bg-background px-3 text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
      />

      <Button
        type="button"
        variant="outline"
        size="icon"
        className="size-11"
        aria-label="Следующий день"
        onClick={() => shift(1)}
      >
        <ChevronRight className="size-4" aria-hidden />
      </Button>

      <Button
        type="button"
        variant="outline"
        size="sm"
        className="h-11"
        onClick={() => onChange(toIsoDate(new Date()))}
        disabled={isToday}
      >
        <CalendarDays className="size-3.5" aria-hidden />
        Сегодня
      </Button>
    </div>
  )
}
