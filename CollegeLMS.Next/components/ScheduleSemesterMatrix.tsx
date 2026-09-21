"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { CalendarRange, RefreshCw } from "lucide-react"
import type {
  ScheduleDayView as ScheduleDayData,
  ScheduleSemesterView as ScheduleSemesterData,
} from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import {
  fetchSemesterView,
  normalizeDateOnly,
  parseIsoDate,
} from "@/api/schedule"
import { InsertRow, PracticeCard } from "@/components/ScheduleLayers"
import ChangeTagBadge from "@/components/ChangeTagBadge"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Button } from "@/components/ui/button"

interface ScheduleSemesterMatrixProps {
  groupId?: string
  teacherId?: string
  refreshKey?: number
  onDayClick: (date: string) => void
}

function formatDate(iso: string): string {
  const date = parseIsoDate(iso)
  if (Number.isNaN(date.getTime())) return ""
  return `${String(date.getDate()).padStart(2, "0")}.${String(
    date.getMonth() + 1,
  ).padStart(2, "0")}`
}

function CellContent({ day }: { day: ScheduleDayData }) {
  if (day.isNonWorking) {
    return (
      <span className="text-[11px] text-amber-700 dark:text-amber-300">
        Не работает: {day.nonWorkingTitle}
      </span>
    )
  }

  if (day.isSunday) {
    return <span className="text-[11px] text-muted-foreground">Выходной</span>
  }

  if (day.practices.length > 0) {
    return (
      <>
        {day.practices.map((practice) => (
          <PracticeCard key={practice.id} practice={practice} compact />
        ))}
      </>
    )
  }

  if (day.entries.length === 0 && day.inserts.length === 0) {
    return <span className="text-muted-foreground">—</span>
  }

  return (
    <>
      {day.inserts.map((insert) => (
        <InsertRow key={insert.id} insert={insert} compact />
      ))}
      {day.entries.map((entry) => (
        <div key={entry.id} className="text-[11px] leading-tight">
          <span className="font-semibold">{entry.numberPair}.</span>{" "}
          <span className="font-medium">{entry.subject}</span>
          <span className="text-muted-foreground"> · {entry.room}</span>
          {entry.changeTags && entry.changeTags.length > 0 && (
            <span className="mt-0.5 flex flex-wrap gap-0.5">
              {entry.changeTags.map((tag, i) => (
                <ChangeTagBadge key={i} tag={tag} />
              ))}
            </span>
          )}
        </div>
      ))}
    </>
  )
}

/** Режим «Семестр»: матрица недель × дни (Пн–Сб) со sticky-колонкой недель. */
export default function ScheduleSemesterMatrix({
  groupId,
  teacherId,
  refreshKey,
  onDayClick,
}: ScheduleSemesterMatrixProps) {
  const hasExactlyOneFilter = Boolean(groupId) !== Boolean(teacherId)
  const [data, setData] = useState<ScheduleSemesterData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestIdRef = useRef(0)

  const load = useCallback(async () => {
    if (!hasExactlyOneFilter) return
    const requestId = ++requestIdRef.current
    setLoading(true)
    setError(null)
    try {
      const body = await fetchSemesterView({ groupId, teacherId })
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
  }, [groupId, teacherId, hasExactlyOneFilter])

  useEffect(() => {
    if (!hasExactlyOneFilter) {
      setData(null)
      setError(null)
      setLoading(false)
      return
    }
    load()
  }, [load, hasExactlyOneFilter, refreshKey])

  if (!hasExactlyOneFilter) {
    return (
      <div className="flex min-h-[40vh] flex-col items-center justify-center gap-3 rounded-lg border bg-card p-10 text-center text-muted-foreground">
        <CalendarRange className="size-10 opacity-40" aria-hidden />
        <p>Выберите группу или преподавателя</p>
      </div>
    )
  }

  const headerDays = data?.weeks[0]?.days ?? []

  return (
    <div className="flex flex-col gap-3">
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
        <div className="overflow-x-auto rounded-lg border bg-card">
          {error && <ErrorBanner message={error} className="m-2" />}
          <table className="w-full min-w-[900px] border-collapse text-xs">
            <thead>
              <tr className="border-b">
                <th
                  scope="col"
                  className="sticky left-0 z-10 min-w-[72px] border-r bg-card px-2 py-2 text-left font-medium"
                >
                  Неделя
                </th>
                {headerDays.map((day) => {
                  const iso = normalizeDateOnly(day.date)
                  const info = DAYS.find((d) => d.value === day.dayOfWeek)
                  return (
                    <th
                      key={iso}
                      scope="col"
                      className="min-w-[150px] border-r px-2 py-2 text-left font-medium last:border-r-0"
                    >
                      {info?.label ?? "—"}
                      <span className="block text-[10px] font-normal text-muted-foreground">
                        {formatDate(iso)}
                      </span>
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody>
              {data.weeks.map((week) => (
                <tr
                  key={week.week}
                  className="border-b last:border-b-0"
                >
                  <th
                    scope="row"
                    className="sticky left-0 z-10 border-r bg-card px-2 py-1.5 text-left font-medium"
                  >
                    {week.week}
                  </th>
                  {week.days.map((day) => {
                    const iso = normalizeDateOnly(day.date)
                    return (
                      <td
                        key={iso}
                        className="border-r p-0 align-top last:border-r-0"
                      >
                        <button
                          type="button"
                          onClick={() => iso && onDayClick(iso)}
                          aria-label={`Открыть день ${formatDate(iso)}`}
                          className="flex min-h-[56px] w-full flex-col gap-1 p-1.5 text-left transition-colors hover:bg-accent focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
                        >
                          <CellContent day={day} />
                        </button>
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
