"use client"

import { useRouter } from "next/navigation"
import { BellRing, CalendarCheck, CalendarOff, Clock3 } from "lucide-react"
import { CellList, CellSimple, Typography } from "@maxhub/max-ui"
import type { ScheduleResponse } from "@/types/schedule"
import type { Practice } from "@/api/practices"
import {
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  practiceName,
  practiceTeacherNames,
} from "@/api/practices"
import type { ScheduleInsert } from "@/api/inserts"
import {
  formatTime,
  lessonTypeColor,
  lessonTypeLabel,
} from "@/lib/max-lesson"
import { workingDayLabel } from "@/lib/reference"
import { toDateFromTime } from "@/api/schedule"
import { useMaxContext, type ViewContext } from "@/lib/max-context"
import { journalHref } from "@/lib/journal"
import ChangeBadge from "@/components/max/ChangeBadge"

type FeedRow =
  | { kind: "entry"; entry: ScheduleResponse }
  | { kind: "insert"; insert: ScheduleInsert }

function rowStartTime(row: FeedRow): string {
  return row.kind === "entry" ? row.entry.startTime : row.insert.startTime
}

/**
 * Пары и вставки дня — одним списком по времени начала, как в LMS.
 * При равном времени пара идёт раньше вставки.
 */
function mergeFeedRows(
  entries: ScheduleResponse[],
  inserts: ScheduleInsert[],
): FeedRow[] {
  const rows: FeedRow[] = [
    ...entries.map((entry): FeedRow => ({ kind: "entry", entry })),
    ...inserts.map((insert): FeedRow => ({ kind: "insert", insert })),
  ]

  return rows.sort((a, b) => {
    const aStart = rowStartTime(a)
    const bStart = rowStartTime(b)
    if (!aStart && !bStart) return 0
    if (!aStart) return 1
    if (!bStart) return -1
    if (aStart === bStart) {
      if (a.kind === b.kind) return 0
      return a.kind === "entry" ? -1 : 1
    }
    return aStart < bStart ? -1 : 1
  })
}

export default function DayFeed({
  entries,
  inserts = [],
  practices = [],
  isSunday = false,
  isNonWorking = false,
  nonWorkingTitle = null,
  nonWorkingLabel = "Нерабочий день",
  isWorkingDay = false,
  workingDayTitle = null,
  substituteDayOfWeek = null,
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
  isWorkingDay?: boolean
  workingDayTitle?: string | null
  substituteDayOfWeek?: number | null
  header?: React.ReactNode
  today?: boolean
}) {
  const { viewContext, profile, setViewContext } = useMaxContext()
  const router = useRouter()

  const sorted = [...entries].sort((a, b) => a.numberPair - b.numberPair)
  const feedRows = mergeFeedRows(sorted, inserts)
  const now = new Date()
  const currentId = today
    ? sorted.find((e) => {
        const start = toDateFromTime(e.startTime, now)
        const end = toDateFromTime(e.endTime, now)
        return now >= start && now <= end
      })?.id
    : undefined

  const isTeacherContext = Boolean(viewContext.teacherId)
  // Журнал доступен преподавателю и только по его собственным парам: свой
  // чужой запрос бэкенд не пропустит (Teacher видит лишь свой журнал).
  const ownTeacherId = profile?.role === "Teacher" ? profile.teacherId : null

  // Ссылка на контрагента пары: у преподавателя — группа, у студента —
  // преподаватель. По ней расписание открывается уже по этой цели.
  const counterpartContext = (
    entry: ScheduleResponse,
  ): ViewContext | null => {
    if (isTeacherContext) {
      if (!entry.groupId) return null
      return { groupId: entry.groupId, groupName: entry.groupName }
    }
    if (!entry.teacherId) return null
    return {
      teacherId: entry.teacherId,
      teacherName: entry.teacherName ?? undefined,
    }
  }

  const openCounterpart = (target: ViewContext) => {
    router.push("/max/schedule")
    setViewContext(target)
  }

  // Слои — всё, кроме пар и вставок: они идут общим списком по времени.
  const hasLayers =
    isNonWorking || isSunday || isWorkingDay || practices.length > 0
  // «Нет пар» — для обычного дня без пар, но со слоями (например, только практика).
  const showEmptyNote =
    sorted.length === 0 &&
    inserts.length === 0 &&
    practices.length === 0 &&
    !isNonWorking &&
    !isSunday &&
    !isWorkingDay

  return (
    <div className="max-feed">
      {header || hasLayers || showEmptyNote ? (
        <div className="max-feed__stack">
          {header}
          {hasLayers ? (
            <div className="max-layers">
              {isWorkingDay ? (
                <div role="status" className="max-layers__workingday">
                  <CalendarCheck size={14} aria-hidden />
                  <span>
                    {workingDayLabel(workingDayTitle, substituteDayOfWeek)}
                  </span>
                </div>
              ) : null}

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
              ) : isSunday && !isWorkingDay ? (
                <div role="status" className="max-layers__notice">
                  <CalendarOff size={16} aria-hidden />
                  <Typography.Body>Выходной</Typography.Body>
                </div>
              ) : null}

              {practices.map((practice) => {
                const name = practiceName(practice)
                const teachers = practiceTeacherNames(practice)
                return (
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
                        <strong>{name}</strong>
                      </Typography.Body>
                      <Typography.Body className="max-layers__practice-note">
                        {practice.groupName}
                        {teachers ? ` · ${teachers}` : ""}
                      </Typography.Body>
                      {practice.note ? (
                        <Typography.Body className="max-layers__practice-note">
                          Примечание: {practice.note}
                        </Typography.Body>
                      ) : null}
                    </span>
                  </div>
                )
              })}

            </div>
          ) : null}

          {showEmptyNote ? (
            <p className="max-app__note max-feed__empty">Нет пар</p>
          ) : null}
        </div>
      ) : null}

      {feedRows.length > 0 ? (
        <CellList mode="island">
          {feedRows.map((row) => {
            if (row.kind === "insert") {
              return (
                <CellSimple
                  key={`insert-${row.insert.id}`}
                  separator
                  title={row.insert.title}
                  before={
                    <div className="max-schedule__insert-badge">
                      <BellRing size={16} aria-hidden />
                    </div>
                  }
                  subtitle={
                    <span className="max-schedule__subtitle">
                      <Clock3 size={14} aria-hidden />{" "}
                      {formatTime(row.insert.startTime)}–{formatTime(row.insert.endTime)}
                    </span>
                  }
                />
              )
            }

            const entry = row.entry
            const typeLabel = lessonTypeLabel(entry.lessonType)
            const isCurrent = entry.id === currentId
            const counterpart = isTeacherContext
              ? entry.groupName
              : entry.teacherName
            const counterpartTarget = counterpartContext(entry)
            // Клик по паре открывает её карточку журнала: пара — это ровно
            // «группа + предмет», то есть и есть карточка журнала.
            const journalUrl =
              ownTeacherId && entry.teacherId === ownTeacherId
                ? journalHref("/max/journal", entry)
                : ""
            const cellClassName = [
              isCurrent ? "max-schedule__cell--current" : "",
              journalUrl ? "max-schedule__cell--link" : "",
            ]
              .filter(Boolean)
              .join(" ")
            return (
              <CellSimple
                key={entry.id}
                separator
                className={cellClassName || undefined}
                title={entry.subject}
                role={journalUrl ? "button" : undefined}
                tabIndex={journalUrl ? 0 : undefined}
                aria-label={
                  journalUrl
                    ? `Журнал: ${entry.groupName} · ${entry.subject}`
                    : undefined
                }
                showChevron={Boolean(journalUrl)}
                onClick={
                  journalUrl ? () => router.push(journalUrl) : undefined
                }
                onKeyDown={
                  journalUrl
                    ? (event) => {
                        if (event.key !== "Enter" && event.key !== " ") return
                        event.preventDefault()
                        router.push(journalUrl)
                      }
                    : undefined
                }
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
                      {counterpart ? (
                        <span>
                          {" · "}
                          {counterpartTarget ? (
                            <button
                              type="button"
                              className="max-schedule__link"
                              onClick={(event) => {
                                event.stopPropagation()
                                openCounterpart(counterpartTarget)
                              }}
                            >
                              {counterpart}
                            </button>
                          ) : (
                            counterpart
                          )}
                        </span>
                      ) : null}
                      {entry.room ? <span>{" · "}{entry.room}</span> : null}
                    </span>
                    {entry.practiceName &&
                    entry.practiceName.trim() !== entry.subject.trim() ? (
                      <span className="max-schedule__practice-name">
                        {entry.practiceName}
                      </span>
                    ) : null}
                    {entry.changeTags.length > 0 ? (
                      <span className="max-schedule__tag-row">
                        <ChangeBadge tags={entry.changeTags} />
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
