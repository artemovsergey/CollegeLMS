"use client"

import { BellRing, CalendarOff, Clock3 } from "lucide-react"
import { CellList, CellSimple, Typography } from "@maxhub/max-ui"
import type { ScheduleResponse } from "@/types/schedule"
import type { Practice } from "@/api/practices"
import { PRACTICE_KIND_LABELS, PRACTICE_KIND_SHORT } from "@/api/practices"
import type { ScheduleInsert } from "@/api/inserts"
import {
  formatTime,
  lessonTypeColor,
  lessonTypeLabel,
} from "@/lib/max-lesson"
import { toDateFromTime } from "@/api/schedule"
import { useMaxContext } from "@/lib/max-context"
import ChangeBadge from "@/components/max/ChangeBadge"

export default function DayFeed({
  entries,
  inserts = [],
  practices = [],
  isSunday = false,
  isNonWorking = false,
  nonWorkingTitle = null,
  nonWorkingLabel = "Нерабочий день",
  header,
  today = false,
}: {
  entries: ScheduleResponse[]
  inserts?: ScheduleInsert[]
  practices?: Practice[]
  isSunday?: boolean
  isNonWorking?: boolean
  nonWorkingTitle?: string | null
  nonWorkingLabel?: string
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
  const hasLayers =
    isNonWorking || isSunday || practices.length > 0 || inserts.length > 0
  // «Нет пар» — для обычного дня без пар, но со слоями (например, только вставки).
  const showEmptyNote =
    sorted.length === 0 && practices.length === 0 && !isNonWorking && !isSunday

  return (
    <div className="max-feed">
      {header || hasLayers || showEmptyNote ? (
        <div className="max-feed__stack">
          {header}
          {hasLayers ? (
            <div className="max-layers">
              {isNonWorking ? (
                <div
                  role="status"
                  className="max-layers__notice max-layers__notice--attention"
                >
                  <CalendarOff size={16} aria-hidden />
                  <Typography.Body>
                    {nonWorkingLabel}: <strong>{nonWorkingTitle}</strong>
                  </Typography.Body>
                </div>
              ) : isSunday ? (
                <div role="status" className="max-layers__notice">
                  <CalendarOff size={16} aria-hidden />
                  <Typography.Body>Выходной</Typography.Body>
                </div>
              ) : null}

              {practices.map((practice) => (
                <div
                  key={practice.id}
                  className={`max-layers__practice max-layers__practice--${practice.kind.toLowerCase()}`}
                >
                  <span
                    className={`max-app__badge max-layers__practice-badge--${practice.kind.toLowerCase()}`}
                    title={PRACTICE_KIND_LABELS[practice.kind]}
                  >
                    {PRACTICE_KIND_SHORT[practice.kind]}
                  </span>
                  <span className="max-layers__practice-body">
                    <Typography.Body>
                      <strong>{practice.groupName}</strong>
                      {` · ${practice.teacherName}`}
                      {practice.organization
                        ? ` · ${practice.organization}`
                        : ""}
                    </Typography.Body>
                    {practice.note ? (
                      <Typography.Body className="max-layers__practice-note">
                        Примечание: {practice.note}
                      </Typography.Body>
                    ) : null}
                  </span>
                </div>
              ))}

              {inserts.length > 0 ? (
                <div className="max-layers__inserts">
                  <Typography.Label className="max-layers__label">
                    События
                  </Typography.Label>
                  {inserts.map((insert) => (
                    <div key={insert.id} className="max-layers__insert">
                      <BellRing size={14} aria-hidden />
                      <span className="max-layers__insert-time">
                        {formatTime(insert.startTime)}–
                        {formatTime(insert.endTime)}
                      </span>
                      <span className="max-layers__insert-title">
                        {insert.title}
                      </span>
                    </div>
                  ))}
                </div>
              ) : null}
            </div>
          ) : null}

          {showEmptyNote ? (
            <p className="max-app__note max-feed__empty">Нет пар</p>
          ) : null}
        </div>
      ) : null}

      {sorted.length > 0 ? (
        <CellList mode="island">
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
                  <span className="max-schedule__subtitle-wrap">
                    <span className="max-schedule__subtitle">
                      <Clock3 size={14} aria-hidden />{" "}
                      {formatTime(entry.startTime)} – {formatTime(entry.endTime)}
                      {counterpart ? ` · ${counterpart}` : ""}
                      {entry.room ? ` · ${entry.room}` : ""}
                    </span>
                    {entry.changeTags.length > 0 ? (
                      <span className="max-schedule__tag-row">
                        {entry.changeTags.map((tag, i) => (
                          <ChangeBadge
                            key={`${tag.changeType}:${tag.week}:${i}`}
                            tag={tag}
                          />
                        ))}
                      </span>
                    ) : null}
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
      ) : null}
    </div>
  )
}
