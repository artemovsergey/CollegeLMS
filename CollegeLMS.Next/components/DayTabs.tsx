"use client"

import { cn } from "@/lib/utils"
import { DAYS } from "@/types/schedule"

interface DayTabsProps {
  selectedDay: number | null
  onChange: (day: number | null) => void
}

export default function DayTabs({ selectedDay, onChange }: DayTabsProps) {
  const workdays = DAYS.filter((d) => d.value >= 1 && d.value <= 5)

  return (
    <div className="flex gap-1 overflow-x-auto">
      {workdays.map((day) => (
        <button
          key={day.value}
          onClick={() => onChange(day.value === selectedDay ? null : day.value)}
          className={cn(
            "flex flex-col items-center rounded-lg px-3 py-2 text-sm font-medium transition-colors min-w-[48px]",
            day.value === selectedDay
              ? "bg-primary text-primary-foreground"
              : "bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground",
          )}
        >
          <span>{day.label}</span>
        </button>
      ))}
    </div>
  )
}
