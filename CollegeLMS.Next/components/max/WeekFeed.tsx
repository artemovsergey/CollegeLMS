"use client"

import type { ScheduleWeekView } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import { normalizeDateOnly } from "@/api/schedule"
import { formatDay, pluralPairs } from "@/lib/max-lesson"
import DayFeed from "@/components/max/DayFeed"

function isoDayOfWeek(date: Date): number {
  const day = date.getDay()
  return day === 0 ? 7 : day
}

export default function WeekFeed({
  data,
  highlightToday = false,
}: {
  data: ScheduleWeekView
  highlightToday?: boolean
}) {
  const today = isoDayOfWeek(new Date())
  // Пн–Пт показываем всегда; Сб/Вс — только при контенте или рабочем дне.
  const visibleDays = data.days.filter(
    (day) =>
      (day.dayOfWeek >= 1 && day.dayOfWeek <= 5) ||
      day.entries.length > 0 ||
      day.inserts.length > 0 ||
      day.practices.length > 0 ||
      day.isNonWorking ||
      day.isWorkingDay === true,
  )

  return (
    <div className="max-week">
      {visibleDays.map((day) => {
        const iso = normalizeDateOnly(day.date)
        const info = DAYS.find((d) => d.value === day.dayOfWeek)
        const dayLabel = info?.full ?? String(day.dayOfWeek)
        const dayDate = formatDay(iso)
        const hasContent =
          day.entries.length > 0 ||
          day.inserts.length > 0 ||
          day.practices.length > 0 ||
          day.isNonWorking ||
          day.isSunday ||
          day.isWorkingDay === true

        if (!hasContent) {
          return (
            <div
              key={iso || String(day.dayOfWeek)}
              className="max-week__day max-week__day--empty"
            >
              <span className="max-week__day--strong">{dayLabel}</span>
              <span className="max-app__note">{dayDate} · нет пар</span>
            </div>
          )
        }

        return (
          <DayFeed
            key={iso || String(day.dayOfWeek)}
            entries={day.entries}
            inserts={day.inserts}
            practices={day.practices}
            isSunday={day.isSunday}
            isNonWorking={day.isNonWorking}
            nonWorkingTitle={day.nonWorkingTitle}
            nonWorkingLabel="Не работает"
            isWorkingDay={day.isWorkingDay ?? false}
            workingDayTitle={day.workingDayTitle ?? null}
            substituteDayOfWeek={day.substituteDayOfWeek ?? null}
            today={highlightToday && day.dayOfWeek === today}
            header={
              <div className="max-week__day">
                <span className="max-week__day--strong">{dayLabel}</span>
                <span className="max-app__note">
                  {dayDate}
                  {day.entries.length > 0
                    ? ` · ${day.entries.length} ${pluralPairs(day.entries.length)}`
                    : ""}
                </span>
              </div>
            }
          />
        )
      })}
    </div>
  )
}
