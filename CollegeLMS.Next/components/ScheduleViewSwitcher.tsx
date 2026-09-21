"use client"

import type { LucideIcon } from "lucide-react"
import { CalendarRange, CalendarDays, Calendar, LayoutGrid } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { ScheduleViewMode } from "@/types/schedule"

interface ScheduleViewSwitcherProps {
  value: ScheduleViewMode
  onChange: (mode: ScheduleViewMode) => void
}

const MODES: { value: ScheduleViewMode; label: string; icon: LucideIcon }[] = [
  { value: "day", label: "День", icon: CalendarDays },
  { value: "week", label: "Неделя", icon: CalendarRange },
  { value: "calendar", label: "Календарь", icon: Calendar },
  { value: "semester", label: "Семестр", icon: LayoutGrid },
]

/** Переключатель режимов расписания: День / Неделя / Календарь / Семестр. */
export default function ScheduleViewSwitcher({
  value,
  onChange,
}: ScheduleViewSwitcherProps) {
  return (
    <div
      role="group"
      aria-label="Режим отображения расписания"
      className="flex flex-wrap overflow-hidden rounded-md border"
    >
      {MODES.map((mode) => {
        const Icon = mode.icon
        const isActive = mode.value === value
        return (
          <Button
            key={mode.value}
            type="button"
            variant={isActive ? "default" : "ghost"}
            size="sm"
            aria-pressed={isActive}
            onClick={() => onChange(mode.value)}
            className="min-h-11 rounded-none border-0 px-3"
          >
            <Icon className="size-3.5" aria-hidden />
            {mode.label}
          </Button>
        )
      })}
    </div>
  )
}
