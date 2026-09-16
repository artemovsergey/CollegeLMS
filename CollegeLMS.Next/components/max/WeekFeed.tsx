"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import { parseIsoDate, toIsoDate } from "@/api/schedule"
import { formatDay, pluralPairs } from "@/lib/max-lesson"
import DayFeed from "@/components/max/DayFeed"

// Порядок дней недели: Пн → Сб, затем Воскресенье (резерв).
const DAY_ORDER = [1, 2, 3, 4, 5, 6, 0]

function addDays(date: Date, days: number): Date {
  const result = new Date(date)
  result.setDate(result.getDate() + days)
  return result
}

function isoDayOfWeek(date: Date): number {
  const day = date.getDay()
  return day === 0 ? 7 : day
}

export default function WeekFeed({
  entries,
  weekStart,
  highlightToday = false,
}: {
  entries: ScheduleResponse[]
  weekStart: string
  highlightToday?: boolean
}) {
  const byDay = new Map<number, ScheduleResponse[]>()
  for (const entry of entries) {
    const list = byDay.get(entry.dayOfWeek)
    if (list) list.push(entry)
    else byDay.set(entry.dayOfWeek, [entry])
  }

  const today = isoDayOfWeek(new Date())
  const weekStartDate = parseIsoDate(weekStart)

  return (
    <div className="max-week">
      {DAY_ORDER.map((dayValue, index) => {
        const list = byDay.get(dayValue)
        const day = DAYS.find((d) => d.value === dayValue)
        const dayDate = formatDay(toIsoDate(addDays(weekStartDate, index)))
        if (!list || list.length === 0) {
          return (
            <div key={dayValue} className="max-week__day max-week__day--empty">
              <span className="max-week__day--strong">
                {day?.full ?? String(dayValue)}
              </span>
              <span className="max-app__note">
                {dayDate} · нет пар
              </span>
            </div>
          )
        }
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
                <span className="max-app__note">
                  {dayDate} · {list.length} {pluralPairs(list.length)}
                </span>
              </div>
            }
          />
        )
      })}
    </div>
  )
}