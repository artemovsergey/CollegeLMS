"use client"

import { Fragment, useCallback, useEffect, useRef, useState } from "react"
import { Radio, RefreshCw } from "lucide-react"
import type {
  ScheduleDayView as ScheduleDayData,
  ScheduleWeekView as ScheduleWeekData,
} from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import {
  fetchWeekView,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
} from "@/api/schedule"
import {
  BigBreakRow,
  InsertRow,
  PracticeCard,
  WorkingDayBadge,
} from "@/components/ScheduleLayers"
import ChangeTagBadge from "@/components/ChangeTagBadge"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Button } from "@/components/ui/button"
import { isEntryNow, mergeDayRows } from "@/lib/schedule-merge"
import { cn } from "@/lib/utils"

interface ScheduleWeekViewProps {
  week: number
  groupId?: string
  teacherId?: string
  refreshKey?: number
  onDayClick: (date: string) => void
}

function formatDate(d: Date): string {
  if (Number.isNaN(d.getTime())) return ""
  return `${String(d.getDate()).padStart(2, "0")}.${String(
    d.getMonth() + 1,
  ).padStart(2, "0")}`
}

function formatTime(time: string): string {
  return time.slice(0, 5)
}

function DayColumn({
  day,
  week,
  onOpen,
}: {
  day: ScheduleDayData
  week: number
  onOpen: (date: string) => void
}) {
  const iso = normalizeDateOnly(day.date)
  const info = DAYS.find((d) => d.value === day.dayOfWeek)
  const dateLabel = formatDate(parseIsoDate(iso))
  const isToday = iso !== "" && iso === toIsoDate(new Date())
  const rows = mergeDayRows(day.entries, day.inserts)

  return (
    <div
      role="button"
      tabIndex={0}
      aria-label={`Открыть день: ${info?.full ?? "день"} ${dateLabel}`}
      onClick={() => onOpen(iso)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault()
          onOpen(iso)
        }
      }}
      className="flex min-h-11 cursor-pointer flex-col gap-2 rounded-lg border bg-card p-2 text-left transition-colors hover:border-primary/30 hover:bg-primary/[0.04] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 dark:hover:bg-primary/[0.10]"
    >
      <div className="flex items-baseline justify-between gap-1">
        <span className="text-sm font-semibold">{info?.label ?? "—"}</span>
        <span className="text-xs text-muted-foreground">{dateLabel}</span>
      </div>

      {day.isWorkingDay && (
        <WorkingDayBadge
          title={day.workingDayTitle}
          substituteDayOfWeek={day.substituteDayOfWeek}
          compact
          className="self-start"
        />
      )}

      {day.isNonWorking ? (
        <p className="text-xs text-amber-700 dark:text-amber-300">
          Не работает: {day.nonWorkingTitle}
        </p>
      ) : day.isSunday && !day.isWorkingDay ? (
        <p className="text-xs text-muted-foreground">Выходной</p>
      ) : (
        <>
          {day.practices.map((practice) => (
            <PracticeCard key={practice.id} practice={practice} compact />
          ))}

          {rows.length === 0 && day.practices.length === 0 && (
            <p className="text-xs text-muted-foreground">Нет пар</p>
          )}

          {rows.map((row) => {
            if (row.kind === "insert") {
              return (
                <InsertRow key={`insert-${row.insert.id}`} insert={row.insert} compact />
              )
            }

            const entry = row.entry
            const isNow = isToday && isEntryNow(entry, day.dayOfWeek, week)
            const showBreak =
              day.bigBreak != null && day.bigBreak.afterPair === entry.numberPair

            return (
              <Fragment key={entry.id}>
              <div
                className={cn(
                  "rounded-md border-l-2 px-2 py-1.5 text-xs",
                  isNow
                    ? "border-primary bg-primary/[0.06] dark:bg-primary/[0.12]"
                    : "border-primary/40 bg-muted/30",
                )}
              >
                <div className="flex items-center gap-1.5">
                  <span className="font-semibold">{entry.numberPair}</span>
                  <span className="whitespace-nowrap text-muted-foreground">
                    {formatTime(entry.startTime)}–
                    {formatTime(entry.endTime)}
                  </span>
                </div>
                <p className="mt-0.5 font-medium leading-tight">
                  {entry.subject}
                </p>
                <p className="mt-0.5 truncate text-muted-foreground">
                  {entry.room}
                  {entry.teacherName ? ` · ${entry.teacherName}` : ""}
                </p>
                {isNow && (
                  <span className="mt-0.5 inline-flex items-center gap-1 text-[10px] font-medium text-primary">
                    <Radio className="size-2.5 animate-pulse" aria-hidden />
                    Сейчас идёт
                  </span>
                )}
                {entry.changeTags && entry.changeTags.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {entry.changeTags.map((tag, i) => (
                      <ChangeTagBadge key={i} tag={tag} />
                    ))}
                  </div>
                )}
              </div>
              {showBreak && day.bigBreak && (
                <BigBreakRow bigBreak={day.bigBreak} compact />
              )}
              </Fragment>
            )
          })}
        </>
      )}
    </div>
  )
}

/** Режим «Неделя»: Пн–Пт (Сб/Вс — при контенте), карточки дней со слоями. */
export default function ScheduleWeekView({
  week,
  groupId,
  teacherId,
  refreshKey,
  onDayClick,
}: ScheduleWeekViewProps) {
  const [data, setData] = useState<ScheduleWeekData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestIdRef = useRef(0)

  const load = useCallback(async () => {
    const requestId = ++requestIdRef.current
    setLoading(true)
    setError(null)
    try {
      const body = await fetchWeekView({ week, groupId, teacherId })
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
  }, [week, groupId, teacherId])

  useEffect(() => {
    load()
  }, [load, refreshKey])

  const firstDay = data?.days[0]
  const lastDay = data?.days[data.days.length - 1]
  const title = data
    ? `Неделя ${data.week} · ${formatDate(parseIsoDate(normalizeDateOnly(firstDay?.date ?? "")))}–${formatDate(parseIsoDate(normalizeDateOnly(lastDay?.date ?? "")))}`
    : ""
  // Пн–Пт всегда; Сб/Вс backend добавляет при контенте или рабочем дне.
  const dayCount = data?.days.length ?? 5
  const gridCols =
    dayCount === 5
      ? "lg:grid-cols-5"
      : dayCount === 7
        ? "lg:grid-cols-7"
        : "lg:grid-cols-6"
  const gridSpan =
    dayCount === 5
      ? "lg:col-span-5"
      : dayCount === 7
        ? "lg:col-span-7"
        : "lg:col-span-6"

  return (
    <div className="flex flex-col gap-3">
      {title && <h3 className="text-base font-semibold sm:text-lg">{title}</h3>}

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
        <div
          className={cn(
            "grid grid-cols-1 gap-2 transition-opacity",
            gridCols,
            loading && "opacity-60",
          )}
        >
          {error && (
            <div className={gridSpan}>
              <ErrorBanner message={error} />
            </div>
          )}
          {data.days.map((day) => (
            <DayColumn
              key={normalizeDateOnly(day.date)}
              day={day}
              week={data.week}
              onOpen={onDayClick}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}
