"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import DayFeed from "@/components/max/DayFeed"

// Порядок дней недели: Пн → Сб, затем Воскресенье (резерв).
const DAY_ORDER = [1, 2, 3, 4, 5, 6, 0]

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
      {DAY_ORDER.map((dayValue) => {
        const list = byDay.get(dayValue)
        if (!list || list.length === 0) return null
        const day = DAYS.find((d) => d.value === dayValue)
        return (
          <DayFeed
            key={dayValue}
            entries={list}
            today={highlightToday && dayValue === today}
            header={
              <div className="max-week__day">
                <span className="max-week__day--strong">
                  {day?.full ?? String(dayValue)}
                </span>
                <span className="max-app__note">{list.length} пар</span>
              </div>
            }
          />
        )
      })}
    </div>
  )
}