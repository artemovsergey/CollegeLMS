"use client"

import type { ScheduleResponse } from "@/types/schedule"
import { LESSON_TYPE_LABELS } from "@/types/schedule"
import { cn } from "@/lib/utils"
import { Calendar } from "lucide-react"

interface SemesterViewProps {
  entries: ScheduleResponse[]
  selectedWeek: number
  onCellClick: (week: number, day: number) => void
}

const WORKDAYS = [1, 2, 3, 4, 5]
const WORKDAY_NAMES = ["Пн", "Вт", "Ср", "Чт", "Пт"]

const LESSON_DOT: Record<string, string> = {
  Lecture: "bg-blue-500",
  Practice: "bg-emerald-500",
  Lab: "bg-amber-500",
  Exam: "bg-red-500",
}

interface CellEntry {
  subject: string
  room: string
  teacherName: string | null
  lessonType: string
  numberPair: number
}

function formatWeeksShort(weeks: number[]): string {
  if (weeks.length === 0) return ""
  if (weeks.length === 1) return `${weeks[0]}`
  return `${weeks[0]}–${weeks[weeks.length - 1]}`
}

export default function SemesterView({
  entries,
  selectedWeek,
  onCellClick,
}: SemesterViewProps) {
  const maxWeek = entries.reduce(
    (max, e) => Math.max(max, ...e.weeks),
    1,
  )

  const cellMap = new Map<string, CellEntry[]>()
  for (const entry of entries) {
    for (const week of entry.weeks) {
      const key = `${week}:${entry.dayOfWeek}`
      if (!cellMap.has(key)) cellMap.set(key, [])
      cellMap.get(key)!.push({
        subject: entry.subject,
        room: entry.room,
        teacherName: entry.teacherName,
        lessonType: entry.lessonType,
        numberPair: entry.numberPair,
      })
    }
  }

  const totalSlots = entries.reduce(
    (sum, e) => sum + e.weeks.length,
    0,
  )

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
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-3 text-xs text-muted-foreground mb-1">
        <span className="font-medium">{totalSlots} слотов</span>
        <span>·</span>
        <span>{maxWeek} недель</span>
        <span>·</span>
        <div className="flex items-center gap-1.5">
          <span className="inline-block size-2 rounded-full bg-blue-500" />
          <span>Лекция</span>
          <span className="inline-block size-2 rounded-full bg-emerald-500" />
          <span>Практика</span>
          <span className="inline-block size-2 rounded-full bg-amber-500" />
          <span>Лабораторная</span>
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border bg-card">
        <table className="w-full text-xs border-collapse">
          <thead>
            <tr>
              <th className="sticky left-0 z-10 bg-muted/50 border-b border-r px-2 py-1.5 text-left font-medium text-muted-foreground w-12">
                №
              </th>
              {WORKDAY_NAMES.map((name, i) => (
                <th
                  key={WORKDAYS[i]}
                  className="bg-muted/50 border-b border-r last:border-r-0 px-2 py-1.5 text-center font-semibold text-xs"
                >
                  {name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: maxWeek }, (_, i) => i + 1).map((week) => (
              <tr key={week}>
                <td className="sticky left-0 z-10 bg-card border-b border-r px-2 py-1 text-left font-medium text-muted-foreground">
                  {week}
                </td>
                {WORKDAYS.map((day) => {
                  const entries = cellMap.get(`${week}:${day}`) ?? []
                  const isActive = week === selectedWeek

                  return (
                    <td
                      key={day}
                      className={cn(
                        "border-b border-r last:border-r-0 px-1 py-1 align-top min-w-[80px] cursor-pointer transition-colors",
                        entries.length > 0 && "hover:bg-muted/30",
                        isActive && "bg-primary/5",
                      )}
                      onClick={() => onCellClick(week, day)}
                    >
                      {entries.length === 0 ? (
                        <span className="text-muted-foreground/40">—</span>
                      ) : (
                        <div className="flex flex-col gap-0.5">
                          {entries.map((entry, idx) => (
                            <div
                              key={idx}
                              className="flex items-start gap-1 leading-tight"
                            >
                              <span
                                className={cn(
                                  "mt-0.5 size-1.5 shrink-0 rounded-full",
                                  LESSON_DOT[entry.lessonType] ?? "bg-gray-400",
                                )}
                              />
                              <span className="truncate" title={`${entry.subject} · ${entry.room} · ${entry.teacherName ?? ""}`}>
                                {entry.subject}
                                <span className="text-muted-foreground ml-0.5">
                                  {entry.room}
                                </span>
                              </span>
                            </div>
                          ))}
                        </div>
                      )}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
