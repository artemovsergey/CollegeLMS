"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import { Calendar } from "lucide-react"

interface SemesterViewProps {
  entries: ScheduleResponse[]
  selectedWeek: number
  onCellClick: (week: number, day: number) => void
}

interface CompactEntry {
  subject: string
  room: string
  teacherName: string | null
  numberPair: number
  startTime: string
  weeks: number[]
}

function formatWeeks(weeks: number[]): string {
  if (weeks.length === 0) return ""
  if (weeks.length === 16 && weeks[0] === 1 && weeks[weeks.length - 1] === 16)
    return "все"
  const ranges: string[] = []
  let start = weeks[0]
  let end = weeks[0]
  for (let i = 1; i < weeks.length; i++) {
    if (weeks[i] === end + 1) {
      end = weeks[i]
    } else {
      ranges.push(start === end ? `${start}` : `${start}-${end}`)
      start = weeks[i]
      end = weeks[i]
    }
  }
  ranges.push(start === end ? `${start}` : `${start}-${end}`)
  return ranges.join(", ")
}

function formatTime(time: string) {
  return time.slice(0, 5)
}

export default function SemesterView({
  entries,
  selectedWeek,
  onCellClick,
}: SemesterViewProps) {
  const workdays = DAYS.filter((d) => d.value >= 1 && d.value <= 5)

  const byDay = new Map<number, CompactEntry[]>()
  for (const entry of entries) {
    const list = byDay.get(entry.dayOfWeek) ?? []
    const existing = list.find(
      (e) =>
        e.subject === entry.subject &&
        e.room === entry.room &&
        e.teacherName === entry.teacherName &&
        e.numberPair === entry.numberPair,
    )
    if (existing) {
      existing.weeks = [...new Set([...existing.weeks, ...entry.weeks])].sort(
        (a, b) => a - b,
      )
    } else {
      list.push({
        subject: entry.subject,
        room: entry.room,
        teacherName: entry.teacherName,
        numberPair: entry.numberPair,
        startTime: entry.startTime,
        weeks: [...entry.weeks].sort((a, b) => a - b),
      })
    }
  }

  for (const list of byDay.values()) {
    list.sort((a, b) => a.numberPair - b.numberPair)
  }

  if (entries.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
        <Calendar className="size-12 opacity-40" />
        <p className="text-lg font-medium">Нет занятий</p>
        <p className="text-sm">Импортируйте расписание для просмотра семестра</p>
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card divide-y">
      {workdays.map((day) => {
        const dayEntries = byDay.get(day.value) ?? []
        if (dayEntries.length === 0) return null

        return (
          <div key={day.value} className="p-3">
            <h4 className="text-sm font-semibold mb-2">{day.full}</h4>
            <div className="space-y-1">
              {dayEntries.map((entry, idx) => (
                <div
                  key={idx}
                  className="flex items-center gap-3 text-xs cursor-pointer hover:bg-muted/30 rounded px-2 py-1.5 -mx-2 transition-colors"
                  onClick={() => onCellClick(entry.weeks[0], day.value)}
                >
                  <span className="font-mono text-muted-foreground w-5 text-right shrink-0">
                    {entry.numberPair}
                  </span>
                  <span className="text-muted-foreground w-16 shrink-0">
                    {formatTime(entry.startTime)}
                  </span>
                  <span className="font-medium truncate">{entry.subject}</span>
                  <span className="text-muted-foreground shrink-0">
                    {entry.room}
                  </span>
                  {entry.teacherName && (
                    <span className="text-muted-foreground truncate">
                      {entry.teacherName}
                    </span>
                  )}
                  <span className="ml-auto text-muted-foreground shrink-0">
                    {formatWeeks(entry.weeks)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )
      })}
    </div>
  )
}
