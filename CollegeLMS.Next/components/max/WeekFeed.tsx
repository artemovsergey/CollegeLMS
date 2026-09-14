"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { WEEKDAYS } from "@/lib/max-lesson"
import DayFeed from "@/components/max/DayFeed"

function isoDayOfWeek(date: Date): number {
  const day = date.getDay()
  return day === 0 ? 7 : day
}

export default function WeekFeed({
  entries,
  rangeLabel,
  highlightToday = false,
}: {
  entries: ScheduleResponse[]
  rangeLabel: string
  highlightToday?: boolean
}) {
  const byDay = new Map<number, ScheduleResponse[]>()
  for (const entry of entries) {
    const list = byDay.get(entry.dayOfWeek)
    if (list) list.push(entry)
    else byDay.set(entry.dayOfWeek, [entry])
  }

  const today = isoDayOfWeek(new Date())

  return (
    <div className="max-week">
      <div className="max-week__range">{rangeLabel}</div>
      {WEEKDAYS.map((day) => {
        const list = byDay.get(day.value)
        if (!list || list.length === 0) return null
        return (
          <DayFeed
            key={day.value}
            entries={list}
            today={highlightToday && day.value === today}
            header={
              <div className="max-week__day">
                <span className="max-week__day--strong">{day.full}</span>
                <span className="max-app__note">{list.length} пар</span>
              </div>
            }
          />
        )
      })}
    </div>
  )
}