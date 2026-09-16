"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { ChevronLeft, ChevronRight, Search, CalendarDays } from "lucide-react"
import {
  Button,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import {
  fetchDaySchedule,
  fetchSchedule,
  fetchScheduleMeta,
  isValidDate,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
} from "@/api/schedule"
import type { ScheduleMeta } from "@/api/schedule"
import type { ScheduleResponse } from "@/types/schedule"
import { useMaxContext } from "@/lib/max-context"
import { parseMaxDeepLink } from "@/lib/max-deeplink"
import DayFeed from "@/components/max/DayFeed"
import WeekFeed from "@/components/max/WeekFeed"
import ScheduleEmpty from "@/components/max/ScheduleEmpty"
import ScheduleError from "@/components/max/ScheduleError"
import SearchSheet from "@/components/max/SearchSheet"

function mondayOf(date: Date): Date {
  const result = new Date(date)
  result.setDate(result.getDate() - ((date.getDay() + 6) % 7))
  return result
}

function addDays(date: Date, days: number): Date {
  const result = new Date(date)
  result.setDate(result.getDate() + days)
  return result
}

function weekOfDate(date: string, semesterStart: string): number {
  const w1 = mondayOf(parseIsoDate(semesterStart))
  const currentMonday = mondayOf(parseIsoDate(date))
  return Math.floor((currentMonday.getTime() - w1.getTime()) / 604800000) + 1
}

function dateInWeek(date: string, week: number, semesterStart: string): string {
  const w1 = mondayOf(parseIsoDate(semesterStart))
  const weekStart = addDays(w1, (week - 1) * 7)
  const weekdayIndex = (parseIsoDate(date).getDay() + 6) % 7
  return toIsoDate(addDays(weekStart, weekdayIndex))
}

function formatDay(value: string): string {
  const date = parseIsoDate(value)
  if (!isValidDate(date)) return ""
  return date.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
  })
}

export default function ScheduleView() {
  const { viewContext, loading: contextLoading } = useMaxContext()
  const [view, setView] = useState<"day" | "week">("week")
  const [meta, setMeta] = useState<ScheduleMeta | null>(null)
  const [selectedDate, setSelectedDate] = useState(() => toIsoDate(new Date()))
  const [selectedWeek, setSelectedWeek] = useState<number | null>(null)
  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchOpen, setSearchOpen] = useState(false)
  const dateInputRef = useRef<HTMLInputElement>(null)

  const openDatePicker = () => {
    const el = dateInputRef.current
    if (!el) return
    if (typeof el.showPicker === "function") {
      try {
        el.showPicker()
      } catch {
        el.focus()
      }
    } else {
      el.focus()
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      const res = await fetchScheduleMeta()
      if (cancelled || !res.isSuccess || !res.data) return
      setMeta(res.data)
      const link = parseMaxDeepLink(
        typeof window !== "undefined" ? window.location.search : "",
      )
      let initialWeek = res.data.currentWeek
      let initialDate =
        normalizeDateOnly(res.data.currentDate) || toIsoDate(new Date())
      if (link.route === "today" || link.route === "schedule") {
        setView("day")
      } else if (link.route === "week" || link.view === "week") {
        setView("week")
        const linkDate = link.date ? normalizeDateOnly(link.date) : ""
        if (linkDate) {
          const now = parseIsoDate(linkDate)
          const semesterStart = normalizeDateOnly(res.data.semesterStart)
          if (isValidDate(now) && semesterStart) {
            const w1 = mondayOf(parseIsoDate(semesterStart))
            initialWeek = Math.floor(
              (mondayOf(now).getTime() - w1.getTime()) / 604800000,
            ) + 1
          }
        }
        initialWeek = Math.min(
          Math.max(1, initialWeek),
          res.data.totalWeeks,
        )
      } else if (link.route === "day" && link.date) {
        setView("day")
        const normalized = normalizeDateOnly(link.date)
        if (normalized) initialDate = normalized
      }
      setSelectedWeek(initialWeek)
      setSelectedDate(initialDate)
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      if (view === "week" && selectedWeek !== null) {
        if (meta && (selectedWeek < 1 || selectedWeek > meta.totalWeeks)) {
          setEntries([])
          return
        }
        const res = await fetchSchedule({
          week: selectedWeek,
          groupId: viewContext.groupId,
          teacherId: viewContext.teacherId,
          pageSize: 300,
        })
        if (!res.isSuccess) {
          throw new Error(res.errorMessage ?? "Не удалось загрузить расписание")
        }
        setEntries(res.data?.items ?? [])
      } else {
        const res = await fetchDaySchedule({
          date: selectedDate,
          groupId: viewContext.groupId,
          teacherId: viewContext.teacherId,
        })
        if (!res.isSuccess) {
          throw new Error(res.errorMessage ?? "Не удалось загрузить расписание")
        }
        setEntries(res.data?.items ?? [])
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Не удалось загрузить расписание",
      )
    } finally {
      setLoading(false)
    }
  }, [view, selectedWeek, selectedDate, meta, viewContext.groupId, viewContext.teacherId])

  useEffect(() => {
    void load()
  }, [load])

  const weekRange = useMemo(() => {
    if (!meta || selectedWeek === null) return ""
    const semesterStart = normalizeDateOnly(meta.semesterStart)
    if (!semesterStart) return ""
    const w1 = mondayOf(parseIsoDate(semesterStart))
    const start = addDays(w1, (selectedWeek - 1) * 7)
    const end = addDays(start, 6)
    return `${formatDay(toIsoDate(start))} – ${formatDay(toIsoDate(end))}`
  }, [meta, selectedWeek])

  const dateIsToday = selectedDate === toIsoDate(new Date())
  const isCurrentWeek =
    meta !== null && selectedWeek === meta.currentWeek

  const isOutsideSemester =
    view === "week" &&
    meta !== null &&
    selectedWeek !== null &&
    (selectedWeek < 1 || selectedWeek > meta.totalWeeks)

  const switchView = (next: "day" | "week") => {
    setView(next)
    const semesterStart = meta ? normalizeDateOnly(meta.semesterStart) : ""
    if (next === "week") {
      // День → Неделя: показать неделю, которой принадлежит выбранная дата.
      if (semesterStart && selectedDate) {
        const week = Math.min(
          Math.max(1, weekOfDate(selectedDate, semesterStart)),
          meta?.totalWeeks ?? 16,
        )
        setSelectedWeek(week)
      } else if (selectedWeek === null) {
        setSelectedWeek(meta?.currentWeek ?? 1)
      }
    } else if (semesterStart && selectedWeek !== null) {
      // Неделя → День: сохранить день недели, перенеся его в выбранную неделю,
      // чтобы день и неделя показывали один и тот же период расписания.
      setSelectedDate(dateInWeek(selectedDate, selectedWeek, semesterStart))
    }
  }

  const navigate = (delta: number) => {
    if (view === "day") {
      setSelectedDate((prev) => toIsoDate(addDays(parseIsoDate(prev), delta)))
    } else if (selectedWeek !== null) {
      setSelectedWeek((prev) =>
        Math.min(
          Math.max(1, (prev ?? 1) + delta),
          meta?.totalWeeks ?? 16,
        ),
      )
    }
  }

  const goToday = () => {
    if (!meta) return
    const currentDate = normalizeDateOnly(meta.currentDate)
    if (currentDate) setSelectedDate(currentDate)
    setSelectedWeek(meta.currentWeek)
  }

  const contextName = viewContext.groupName ?? viewContext.teacherName ?? null

  return (
    <MaxUI className="max-schedule">
      <main className="max-app__page">
        <header className="max-app__page-title">
          <div className="max-schedule__context">
            {contextName ? (
              <Typography.Body className="max-app__muted">
                {contextName}
              </Typography.Body>
            ) : null}
          </div>
          <div className="max-schedule__view-switch" role="tablist" aria-label="Вид расписания">
            <button
              type="button"
              role="tab"
              aria-selected={view === "week"}
              className={`max-schedule__view-tab ${
                view === "week" ? "max-schedule__view-tab--active" : ""
              }`}
              onClick={() => switchView("week")}
            >
              Неделя
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={view === "day"}
              className={`max-schedule__view-tab ${
                view === "day" ? "max-schedule__view-tab--active" : ""
              }`}
              onClick={() => switchView("day")}
            >
              День
            </button>
          </div>
        </header>

        <div className="max-schedule__nav">
          <div className="max-schedule__nav-side">
            <Button
              variant="secondary"
              size="small"
              aria-label="Предыдущий период"
              onClick={() => navigate(-1)}
              iconBefore={<ChevronLeft size={16} aria-hidden />}
            />
          </div>
          <div className="max-schedule__nav-title">
            {view === "week" ? (
              <Typography.Body className="max-schedule__nav-label">
                <strong>{selectedWeek ? `Неделя ${selectedWeek}` : ""}</strong>
                <span className="max-app__note">{weekRange}</span>
              </Typography.Body>
            ) : (
              <>
                <Button
                  variant="secondary"
                  size="small"
                  aria-label="Выбрать дату"
                  onClick={openDatePicker}
                  iconBefore={<CalendarDays size={16} aria-hidden />}
                />
                <input
                  ref={dateInputRef}
                  type="date"
                  value={selectedDate}
                  onChange={(e) => {
                    if (e.target.value) {
                      setView("day")
                      setSelectedDate(e.target.value)
                    }
                  }}
                  className="max-schedule__date-input"
                  tabIndex={-1}
                  aria-hidden="true"
                />
              </>
            )}
          </div>
          <div className="max-schedule__nav-actions">
            <Button
              variant="secondary"
              size="small"
              aria-label="Следующий период"
              onClick={() => navigate(1)}
              iconAfter={<ChevronRight size={16} aria-hidden />}
            />
            <Button variant="secondary" size="small" onClick={goToday}>
              Сегодня
            </Button>
          </div>
        </div>

        {contextLoading ? (
          <div className="max-app__state">
            <Spinner size={24} />
          </div>
        ) : isOutsideSemester ? (
          <div className="max-app__state">
            <Typography.Title>Не учебная неделя</Typography.Title>
            <Typography.Body className="max-app__muted">
              Расписание есть только на 1–{meta?.totalWeeks} недели семестра
            </Typography.Body>
            <Button size="small" onClick={goToday}>
              К ближайшей учебной неделе
            </Button>
          </div>
        ) : loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
            <Typography.Body>Загрузка…</Typography.Body>
          </div>
        ) : error ? (
          <ScheduleError message={error} onRetry={() => void load()} />
        ) : !contextName ? (
          <div className="max-app__login-prompt">
            <CalendarDays size={32} className="max-app__state-icon" aria-hidden />
            <Typography.Title>Выберите расписание</Typography.Title>
            <Typography.Body className="max-app__muted">
              Откройте группу или преподавателя через поиск
            </Typography.Body>
            <Button onClick={() => setSearchOpen(true)}>Поиск</Button>
          </div>
        ) : entries.length === 0 ? (
          <ScheduleEmpty />
        ) : view === "week" ? (
          <WeekFeed
            entries={entries}
            rangeLabel={weekRange}
            highlightToday={isCurrentWeek}
          />
        ) : (
          <DayFeed
            entries={entries}
            today={dateIsToday}
            header={<span>{formatDay(selectedDate)}</span>}
          />
        )}

        {contextName ? (
          <Button
            variant="secondary"
            stretched
            onClick={() => setSearchOpen(true)}
            iconBefore={<Search size={18} aria-hidden />}
          >
            Поиск
          </Button>
        ) : null}
      </main>

      <SearchSheet open={searchOpen} onClose={() => setSearchOpen(false)} />
    </MaxUI>
  )
}