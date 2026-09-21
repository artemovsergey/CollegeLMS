"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { AlertTriangle, ChevronLeft, ChevronRight, RefreshCw } from "lucide-react"
import type { ScheduleMonthDay, ScheduleMonthView } from "@/types/schedule"
import {
  fetchMonthView,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
  toIsoMonth,
} from "@/api/schedule"
import {
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  type PracticeKind,
} from "@/api/practices"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { WorkingDayBadge } from "@/components/ScheduleLayers"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface ScheduleMonthCalendarProps {
  /** Месяц в формате YYYY-MM. */
  month: string
  groupId?: string
  teacherId?: string
  refreshKey?: number
  onMonthChange: (month: string) => void
  onDayClick: (date: string) => void
}

const WEEKDAYS = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"]

const PRACTICE_BADGE: Record<PracticeKind, string> = {
  Up: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  Pp: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
}

function pluralPairs(count: number): string {
  const mod10 = count % 10
  const mod100 = count % 100
  if (mod10 === 1 && mod100 !== 11) return `${count} пара`
  if ([2, 3, 4].includes(mod10) && ![12, 13, 14].includes(mod100)) {
    return `${count} пары`
  }
  return `${count} пар`
}

function monthLabel(month: string): string {
  const date = parseIsoDate(`${month}-01`)
  if (Number.isNaN(date.getTime())) return month
  const label = new Intl.DateTimeFormat("ru-RU", {
    month: "long",
    year: "numeric",
  }).format(date)
  return label.charAt(0).toUpperCase() + label.slice(1)
}

function buildCells(days: ScheduleMonthDay[]): (ScheduleMonthDay | null)[] {
  if (days.length === 0) return []
  const first = days[0].dayOfWeek
  const leading = first === 0 ? 6 : first - 1
  const cells: (ScheduleMonthDay | null)[] = [
    ...Array.from({ length: leading }, () => null),
    ...days,
  ]
  while (cells.length % 7 !== 0) cells.push(null)
  return cells
}

/** Режим «Календарь»: сетка месяца с количеством пар и маркерами. */
export default function ScheduleMonthCalendar({
  month,
  groupId,
  teacherId,
  refreshKey,
  onMonthChange,
  onDayClick,
}: ScheduleMonthCalendarProps) {
  const [data, setData] = useState<ScheduleMonthView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestIdRef = useRef(0)

  const load = useCallback(async () => {
    const requestId = ++requestIdRef.current
    setLoading(true)
    setError(null)
    try {
      const body = await fetchMonthView({ month, groupId, teacherId })
      if (requestId !== requestIdRef.current) return
      if (body.isSuccess && body.data) {
        setData(body.data)
      } else {
        setData(null)
        setError(body.errorMessage ?? "Ошибка загрузки расписания")
      }
    } catch {
      if (requestId === requestIdRef.current) {
        setData(null)
        setError("Ошибка загрузки расписания")
      }
    } finally {
      if (requestId === requestIdRef.current) setLoading(false)
    }
  }, [month, groupId, teacherId])

  useEffect(() => {
    load()
  }, [load, refreshKey])

  const shiftMonth = (delta: number) => {
    const base = parseIsoDate(`${month}-01`)
    const target = Number.isNaN(base.getTime()) ? new Date() : base
    target.setMonth(target.getMonth() + delta)
    onMonthChange(toIsoMonth(target))
  }

  const isCurrentMonth = month === toIsoMonth(new Date())
  const cells = data ? buildCells(data.days) : []
  const todayIso = toIsoDate(new Date())

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-11"
          aria-label="Предыдущий месяц"
          onClick={() => shiftMonth(-1)}
        >
          <ChevronLeft className="size-4" aria-hidden />
        </Button>
        <span className="min-w-[170px] text-center text-base font-semibold sm:text-lg">
          {monthLabel(month)}
        </span>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-11"
          aria-label="Следующий месяц"
          onClick={() => shiftMonth(1)}
        >
          <ChevronRight className="size-4" aria-hidden />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-11"
          onClick={() => onMonthChange(toIsoMonth(new Date()))}
          disabled={isCurrentMonth}
        >
          Текущий
        </Button>
      </div>

      {loading && !data ? (
        <div className="flex min-h-[50vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : error && !data ? (
        <div className="flex flex-col items-center gap-3 py-10">
          <ErrorBanner message={error} />
          <Button variant="outline" size="sm" onClick={load}>
            <RefreshCw className="size-3.5" aria-hidden />
            Повторить
          </Button>
        </div>
      ) : data ? (
        <div className="rounded-lg border bg-card p-2 sm:p-3">
          {error && <ErrorBanner message={error} className="mb-2" />}
          <div
            className={cn(
              "grid grid-cols-7 gap-1 transition-opacity",
              loading && "opacity-60",
            )}
          >
            {WEEKDAYS.map((weekday) => (
              <div
                key={weekday}
                className="pb-1 text-center text-xs font-medium text-muted-foreground"
              >
                {weekday}
              </div>
            ))}

            {cells.map((day, index) => {
              if (!day) {
                return (
                  <div
                    key={`empty-${index}`}
                    className="min-h-[64px] rounded-md"
                  />
                )
              }

              const iso = normalizeDateOnly(day.date)
              const dayNumber = iso ? Number(iso.slice(8, 10)) : ""
              const muted =
                (day.isSunday || day.isOutOfSemester) && !day.isWorkingDay
              const isToday = iso !== "" && iso === todayIso
              const dayLabel = `${dayNumber} ${
                day.isNonWorking
                  ? `— нерабочий день: ${day.nonWorkingTitle}`
                  : day.isWorkingDay
                    ? "— рабочий день"
                    : day.isSunday
                      ? "— выходной"
                      : day.pairCount > 0
                        ? `— ${pluralPairs(day.pairCount)}`
                        : ""
              }`

              return (
                <button
                  key={iso}
                  type="button"
                  onClick={() => iso && onDayClick(iso)}
                  aria-label={`Открыть ${dayNumber}: ${dayLabel}`}
                  className={cn(
                    "flex min-h-[64px] flex-col items-start gap-1 rounded-md border p-1.5 text-left transition-colors hover:border-primary/30 hover:bg-primary/[0.04] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 dark:hover:bg-primary/[0.10]",
                    isToday &&
                      "border-primary/40 bg-primary/[0.04] ring-1 ring-primary/40 dark:bg-primary/[0.10]",
                    muted && "text-muted-foreground opacity-50",
                  )}
                >
                  <span className="flex items-center gap-1">
                    <span className="text-sm font-medium">{dayNumber}</span>
                    {isToday && (
                      <span className="rounded bg-primary/10 px-1 text-[10px] font-medium text-primary">
                        Сегодня
                      </span>
                    )}
                  </span>

                  {day.isNonWorking ? (
                    <span
                      title={day.nonWorkingTitle ?? "Нерабочий день"}
                      className="inline-flex items-center text-amber-700 dark:text-amber-300"
                    >
                      <AlertTriangle className="size-3.5 shrink-0" aria-hidden />
                    </span>
                  ) : day.pairCount > 0 ? (
                    <span className="text-[11px] text-muted-foreground">
                      {pluralPairs(day.pairCount)}
                    </span>
                  ) : null}

                  {day.isWorkingDay && (
                    <WorkingDayBadge
                      title={day.workingDayTitle}
                      substituteDayOfWeek={day.substituteDayOfWeek}
                      compact
                      className="max-w-full whitespace-normal text-left"
                    />
                  )}

                  {day.practiceKinds.length > 0 && (
                    <span className="flex flex-wrap gap-0.5">
                      {day.practiceKinds.map((kind) => (
                        <span
                          key={kind}
                          title={PRACTICE_KIND_LABELS[kind]}
                          className={cn(
                            "inline-flex items-center rounded-full px-1.5 py-0.5 text-[10px] font-medium",
                            PRACTICE_BADGE[kind],
                          )}
                        >
                          {PRACTICE_KIND_SHORT[kind]}
                        </span>
                      ))}
                    </span>
                  )}
                </button>
              )
            })}
          </div>
        </div>
      ) : null}
    </div>
  )
}
