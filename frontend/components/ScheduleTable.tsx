"use client"

import type { ScheduleResponse } from "@/types/schedule"
import {
  DAYS,
  LESSON_TYPE_LABELS,
  LESSON_TYPE_STYLES,
} from "@/types/schedule"
import { Clock, MapPin, GraduationCap, Users, Calendar, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"

interface ScheduleTableProps {
  entries: ScheduleResponse[]
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
}

function formatTime(time: string) {
  return time.slice(0, 5)
}

function formatTimeSlot(start: string, end: string) {
  return `${formatTime(start)} – ${formatTime(end)}`
}

export default function ScheduleTable({ entries, onEntryClick, onDeleteClick }: ScheduleTableProps) {
  const weekDays = DAYS.filter(d => d.value >= 1 && d.value <= 5)

  const timeSlots = [
    ...new Set(
      entries.map(e => formatTimeSlot(e.startTime, e.endTime)),
    ),
  ].sort((a, b) => a.localeCompare(b))

  const cellMap = new Map<string, ScheduleResponse[]>()
  for (const entry of entries) {
    const slot = formatTimeSlot(entry.startTime, entry.endTime)
    const key = `${entry.dayOfWeek}:${slot}`
    if (!cellMap.has(key)) cellMap.set(key, [])
    cellMap.get(key)!.push(entry)
  }

  if (entries.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
        <Calendar className="size-12 opacity-40" />
        <p className="text-lg font-medium">Нет занятий</p>
        <p className="text-sm">На выбранный период расписание не найдено</p>
      </div>
    )
  }

  return (
    <>
      <div className="hidden md:block overflow-x-auto rounded-lg border bg-card">
        <div
          className="grid min-w-[700px]"
          style={{
            gridTemplateColumns: `140px repeat(${weekDays.length}, 1fr)`,
          }}
        >
          <div className="sticky top-0 z-10 border-b bg-muted/50 p-3 text-xs font-medium text-muted-foreground">
            Время
          </div>
          {weekDays.map(d => (
            <div
              key={d.value}
              className="sticky top-0 z-10 border-b bg-muted/50 p-3 text-center text-xs font-semibold"
            >
              {d.label}
            </div>
          ))}

          {timeSlots.map(slot => (
            <>
              <div
                key={`time-${slot}`}
                className="flex items-start gap-2 border-b border-r p-3 text-xs text-muted-foreground"
              >
                <Clock className="mt-0.5 size-3 shrink-0" />
                <span>{slot}</span>
              </div>
              {weekDays.map(day => {
                const dayEntries =
                  cellMap.get(`${day.value}:${slot}`) ?? []
                return (
                  <div
                    key={`${day.value}-${slot}`}
                    className="min-h-24 border-b p-1.5"
                  >
                    {dayEntries.map(entry => (
                      <div
                        key={entry.id}
                        className={`group relative mb-1 rounded-md border-l-[3px] p-2 text-xs shadow-sm last:mb-0 ${LESSON_TYPE_STYLES[entry.lessonType] ?? "border-l-gray-400 bg-gray-50 dark:bg-gray-900/20"} ${onEntryClick ? "cursor-pointer transition-colors hover:bg-accent/50" : ""}`}
                        onClick={() => onEntryClick?.(entry)}
                      >
                        <p className="mb-1 font-semibold text-foreground">
                          {entry.subject}
                        </p>
                        <div className="space-y-0.5 text-muted-foreground">
                          {entry.teacherName && (
                            <span className="flex items-center gap-1">
                              <GraduationCap className="size-3 shrink-0" />
                              {entry.teacherName}
                            </span>
                          )}
                          <span className="flex items-center gap-1">
                            <MapPin className="size-3 shrink-0" />
                            {entry.room}
                          </span>
                          <span className="flex items-center gap-1">
                            <Users className="size-3 shrink-0" />
                            {entry.groupName}
                          </span>
                        </div>
                        <span className="mt-1 inline-block rounded-full bg-background/80 px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                          {LESSON_TYPE_LABELS[entry.lessonType]}
                        </span>
                        {onDeleteClick && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="absolute right-1 top-1 size-5 opacity-0 transition-opacity group-hover:opacity-100"
                            onClick={(e) => {
                              e.stopPropagation()
                              onDeleteClick(entry.id)
                            }}
                          >
                            <Trash2 className="size-3 text-destructive" />
                          </Button>
                        )}
                      </div>
                    ))}
                  </div>
                )
              })}
            </>
          ))}
        </div>
      </div>

      <div className="md:hidden flex flex-col gap-4">
        {weekDays.map(day => {
          const dayEntries = entries
            .filter(e => e.dayOfWeek === day.value)
            .sort((a, b) => a.startTime.localeCompare(b.startTime))

          if (dayEntries.length === 0) return null

          return (
            <div key={day.value} className="rounded-lg border bg-card">
              <div className="border-b bg-muted/30 px-4 py-2.5 text-sm font-semibold">
                {day.full}
              </div>
              <div className="divide-y">
                {dayEntries.map(entry => (
                  <div
                    key={entry.id}
                    className={`group relative border-l-[3px] p-3 ${LESSON_TYPE_STYLES[entry.lessonType] ?? "border-l-gray-400"} ${onEntryClick ? "cursor-pointer transition-colors hover:bg-accent/50" : ""}`}
                    onClick={() => onEntryClick?.(entry)}
                  >
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <Clock className="size-3" />
                      {formatTimeSlot(entry.startTime, entry.endTime)}
                      <span className="ml-auto rounded-full bg-background px-1.5 py-0.5 text-[10px] font-medium">
                        {LESSON_TYPE_LABELS[entry.lessonType]}
                      </span>
                    </div>
                    <p className="mt-1 text-sm font-semibold">
                      {entry.subject}
                    </p>
                    <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-muted-foreground">
                      {entry.teacherName && (
                        <span className="flex items-center gap-1">
                          <GraduationCap className="size-3" />
                          {entry.teacherName}
                        </span>
                      )}
                      <span className="flex items-center gap-1">
                        <MapPin className="size-3" />
                        {entry.room}
                      </span>
                      <span className="flex items-center gap-1">
                        <Users className="size-3" />
                        {entry.groupName}
                      </span>
                    </div>
                    {onDeleteClick && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="absolute right-2 top-2 size-6 opacity-0 transition-opacity group-hover:opacity-100"
                        onClick={(e) => {
                          e.stopPropagation()
                          onDeleteClick(entry.id)
                        }}
                      >
                        <Trash2 className="size-3.5 text-destructive" />
                      </Button>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </>
  )
}
