"use client"

import { Clock3 } from "lucide-react"
import { CellSimple, CellList, Typography } from "@maxhub/max-ui"
import type { ScheduleResponse } from "@/types/schedule"
import { formatTime, lessonTypeColor, lessonTypeLabel } from "@/lib/max-lesson"

export default function CurrentPairCard({
  current,
  next,
}: {
  current?: ScheduleResponse
  next?: ScheduleResponse
}) {
  const card = current ?? next
  if (!card) {
    const hour = new Date().getHours()
    const label =
      hour >= 21 ? "Расписание на сегодня закончилось" : "Сегодня пар больше нет"
    return (
      <CellList mode="island">
        <CellSimple title={label} subtitle="Завтра всё начнётся заново" />
      </CellList>
    )
  }

  return (
    <CellList mode="island">
      <CellSimple
        separator
        overline={current ? "Идёт сейчас" : "Следующая пара"}
        title={card.subject}
        before={
          <div
            className="max-schedule__pair"
            style={{ borderColor: lessonTypeColor(card.lessonType) }}
          >
            <Typography.Label>{card.numberPair}</Typography.Label>
            <span>{formatTime(card.startTime)}</span>
          </div>
        }
        subtitle={
          <span>
            <Clock3 size={14} /> {formatTime(card.startTime)} –{" "}
            {formatTime(card.endTime)} · {lessonTypeLabel(card.lessonType)}
          </span>
        }
      />
    </CellList>
  )
}