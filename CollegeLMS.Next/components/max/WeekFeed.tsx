"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { WEEKDAYS } from "@/lib/max-lesson"
import DayFeed from "@/components/max/DayFeed"

export default function WeekFeed({
  entries,
  rangeLabel,
}: {
  entries: ScheduleResponse[]
  rangeLabel: string
}) {
  const byDay = new Map<number, ScheduleResponse[]>()
  for (const entry of entries) {
    const list = byDay.get(entry.dayOfWeek)
    if (list) list.push(entry)
    else byDay.set(entry.dayOfWeek, [entry])
  }

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