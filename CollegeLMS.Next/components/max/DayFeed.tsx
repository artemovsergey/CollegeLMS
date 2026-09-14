"use client"

import { Clock3 } from "lucide-react"
import { CellList, CellSimple, Typography } from "@maxhub/max-ui"
import type { ScheduleResponse } from "@/types/schedule"
import {
  formatTime,
  lessonTypeColor,
  lessonTypeLabel,
} from "@/lib/max-lesson"
import { toDateFromTime } from "@/api/schedule"
import { useMaxContext } from "@/lib/max-context"

export default function DayFeed({
  entries,
  header,
  today = false,
}: {
  entries: ScheduleResponse[]
  header?: React.ReactNode
  today?: boolean
}) {
  const { viewContext } = useMaxContext()

  const sorted = [...entries].sort((a, b) => a.numberPair - b.numberPair)
  const now = new Date()
  const currentId = today
    ? sorted.find((e) => {
        const start = toDateFromTime(e.startTime, now)
        const end = toDateFromTime(e.endTime, now)
        return now >= start && now <= end
      })?.id
    : undefined

  const isTeacherContext = Boolean(viewContext.teacherId)

  return (
    <CellList mode="island" header={header}>
      {sorted.map((entry) => {
        const typeLabel = lessonTypeLabel(entry.lessonType)
        const isCurrent = entry.id === currentId
        const counterpart = isTeacherContext
          ? entry.groupName
          : entry.teacherName
        return (
          <CellSimple
            key={entry.id}
            separator
            className={isCurrent ? "max-schedule__cell--current" : undefined}
            title={entry.subject}
            before={
              <div
                className={`max-schedule__pair${isCurrent ? " max-schedule__pair--current" : ""}`}
                style={{ borderColor: lessonTypeColor(entry.lessonType) }}
              >
                <Typography.Label>{entry.numberPair}</Typography.Label>
              </div>
            }
            subtitle={
              <span className="max-schedule__subtitle">
                <Clock3 size={14} aria-hidden />{" "}
                {formatTime(entry.startTime)} – {formatTime(entry.endTime)}
                {counterpart ? ` · ${counterpart}` : ""}
                {entry.room ? ` · ${entry.room}` : ""}
              </span>
            }
            after={
              typeLabel ? (
                <span
                  className="max-schedule__type"
                  style={{ color: lessonTypeColor(entry.lessonType) }}
                >
                  {typeLabel}
                </span>
              ) : undefined
            }
          />
        )
      })}
    </CellList>
  )
}