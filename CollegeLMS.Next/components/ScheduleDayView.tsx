"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { RefreshCw } from "lucide-react"
import type {
  ScheduleDayView as ScheduleDayData,
  ScheduleResponse,
} from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import {
  fetchDayView,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
} from "@/api/schedule"
import DayNavigation from "@/components/DayNavigation"
import ScheduleLayers from "@/components/ScheduleLayers"
import ScheduleTable from "@/components/ScheduleTable"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Button } from "@/components/ui/button"

interface ScheduleDayViewProps {
  date: string
  groupId?: string
  teacherId?: string
  refreshKey?: number
  onDateChange: (date: string) => void
  onEntryClick?: (entry: ScheduleResponse) => void
  onDeleteClick?: (id: string) => void
}

function formatDayMonth(iso: string): string {
  const d = parseIsoDate(iso)
  if (Number.isNaN(d.getTime())) return ""
  return `${String(d.getDate()).padStart(2, "0")}.${String(
    d.getMonth() + 1,
  ).padStart(2, "0")}`
}

/** Режим «День»: навигация по дате, слои и карточки пар. */
export default function ScheduleDayView({
  date,
  groupId,
  teacherId,
  refreshKey,
  onDateChange,
  onEntryClick,
  onDeleteClick,
}: ScheduleDayViewProps) {
  const [data, setData] = useState<ScheduleDayData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestIdRef = useRef(0)

  const load = useCallback(async () => {
    const requestId = ++requestIdRef.current
    setLoading(true)
    setError(null)
    try {
      const body = await fetchDayView({ date, groupId, teacherId })
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
  }, [date, groupId, teacherId])

  useEffect(() => {
    load()
  }, [load, refreshKey])

  const dayInfo = data ? DAYS.find((d) => d.value === data.dayOfWeek) : undefined
  const isoDate = data ? normalizeDateOnly(data.date) : date
  const isToday = isoDate === toIsoDate(new Date())
  const title = data
    ? `${dayInfo?.full ?? "День"}, ${formatDayMonth(isoDate)} · неделя ${data.week}`
    : ""
  const isOrdinaryDay =
    data && !data.isSunday && !data.isNonWorking && data.practices.length === 0

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <DayNavigation date={date} onChange={onDateChange} />
        {title && (
          <h3 className="text-base font-semibold sm:text-lg">{title}</h3>
        )}
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
        <div
          className={`flex flex-col gap-3 transition-opacity ${
            loading ? "opacity-60" : ""
          }`}
        >
          {error && <ErrorBanner message={error} />}
          <ScheduleLayers
            nonWorkingTitle={data.nonWorkingTitle}
            isSunday={data.isSunday}
            practices={data.practices}
          />
          {isOrdinaryDay && (
            <ScheduleTable
              entries={data.entries}
              inserts={data.inserts}
              selectedDay={null}
              currentWeek={isToday ? data.week : undefined}
              onEntryClick={onEntryClick}
              onDeleteClick={onDeleteClick}
            />
          )}
        </div>
      ) : null}
    </div>
  )
}
