"use client"

import { Clock3 } from "lucide-react"
import { CellList, CellSimple, Typography } from "@maxhub/max-ui"
import type { ScheduleResponse } from "@/types/schedule"
import {
  formatTime,
  lessonTypeColor,
  lessonTypeLabel,
} from "@/lib/max-lesson"

export default function DayFeed({
  entries,
  header,
}: {
  entries: ScheduleResponse[]
  header?: React.ReactNode
}) {
  if (entries.length === 0) return null
  return (
    <CellList mode="island" header={header}>
      {entries
        .slice()
        .sort((a, b) => a.numberPair - b.numberPair)
        .map((entry) => (
          <CellSimple
            key={entry.id}
            separator
            title={entry.subject}
            before={
              <div
                className="max-schedule__pair"
                style={{ borderColor: lessonTypeColor(entry.lessonType) }}
              >
                <Typography.Label>{entry.numberPair}</Typography.Label>
                <span>{formatTime(entry.startTime)}</span>
              </div>
            }
            subtitle={
              <span>
                <Clock3 size={14} aria-hidden /> {formatTime(entry.startTime)} –{" "}
                {formatTime(entry.endTime)}
              </span>
            }
            after={
              <span
                className="max-schedule__type"
                style={{ color: lessonTypeColor(entry.lessonType) }}
              >
                {lessonTypeLabel(entry.lessonType)}
              </span>
            }
          />
        ))}
    </CellList>
  )
}