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
  fetchDayView,
  fetchScheduleMeta,
  fetchWeekView,
  isValidDate,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
} from "@/api/schedule"
import type { ScheduleMeta } from "@/api/schedule"
import type { ScheduleDayView, ScheduleWeekView } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import { useMaxContext, type ViewContext } from "@/lib/max-context"
import { parseMaxDeepLink } from "@/lib/max-deeplink"
import { formatDay, pluralPairs } from "@/lib/max-lesson"
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

// Сравниваем только по идентификатору цели — имена могли обновиться.
function sameSelection(a: ViewContext, b: ViewContext): boolean {
  if (a.groupId && b.groupId) return a.groupId === b.groupId
  if (a.teacherId && b.teacherId) return a.teacherId === b.teacherId
  return false
}

export default function ScheduleView() {
  const {
    viewContext,
    currentSelection,
    makeCurrentSelection,
    loading: contextLoading,
    deepLink,
  } = useMaxContext()
  const [view, setView] = useState<"day" | "week">("week")
  const [selectionPending, setSelectionPending] = useState(false)
  const [meta, setMeta] = useState<ScheduleMeta | null>(null)
  const [selectedDate, setSelectedDate] = useState(() => toIsoDate(new Date()))
  const [selectedWeek, setSelectedWeek] = useState<number | null>(null)
  const [dayData, setDayData] = useState<ScheduleDayView | null>(null)
  const [weekData, setWeekData] = useState<ScheduleWeekView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchOpen, setSearchOpen] = useState(false)
  const requestIdRef = useRef(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      const res = await fetchScheduleMeta()
      if (cancelled || !res.isSuccess || !res.data) return
      setMeta(res.data)
      const link =
        deepLink ??
        parseMaxDeepLink(
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
  }, [deepLink])

  const load = useCallback(async () => {
    const requestId = ++requestIdRef.current
    setLoading(true)
    setError(null)
    try {
      if (view === "week" && selectedWeek !== null) {
        if (meta && (selectedWeek < 1 || selectedWeek > meta.totalWeeks)) {
          setWeekData(null)
          return
        }
        const res = await fetchWeekView({
          week: selectedWeek,
          groupId: viewContext.groupId,
          teacherId: viewContext.teacherId,
        })
        if (requestId !== requestIdRef.current) return
        if (!res.isSuccess) {
          throw new Error(res.errorMessage ?? "Не удалось загрузить расписание")
        }
        setWeekData(res.data ?? null)
      } else {
        const res = await fetchDayView({
          date: selectedDate,
          groupId: viewContext.groupId,
          teacherId: viewContext.teacherId,
        })
        if (requestId !== requestIdRef.current) return
        if (!res.isSuccess) {
          throw new Error(res.errorMessage ?? "Не удалось загрузить расписание")
        }
        setDayData(res.data ?? null)
      }
    } catch (err) {
      if (requestId === requestIdRef.current) {
        setError(
          err instanceof Error ? err.message : "Не удалось загрузить расписание",
        )
      }
    } finally {
      if (requestId === requestIdRef.current) setLoading(false)
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
    // Конец диапазона — последний день недели из ответа (Пн–Пт + выходные при контенте).
    const lastDay =
      weekData && weekData.week === selectedWeek && weekData.days.length > 0
        ? weekData.days[weekData.days.length - 1]
        : undefined
    const lastIso = lastDay ? normalizeDateOnly(lastDay.date) : ""
    const end = lastIso || toIsoDate(addDays(start, 4))
    return `${formatDay(toIsoDate(start))} – ${formatDay(end)}`
  }, [meta, selectedWeek, weekData])

  const dateIsToday = selectedDate === toIsoDate(new Date())
  const isCurrentWeek =
    meta !== null && selectedWeek === meta.currentWeek

  const dayHasContent =
    dayData !== null &&
    (dayData.entries.length > 0 ||
      dayData.inserts.length > 0 ||
      dayData.practices.length > 0 ||
      dayData.isNonWorking ||
      dayData.isSunday ||
      dayData.isWorkingDay === true)

  const weekHasContent =
    weekData !== null &&
    weekData.days.some(
      (day) =>
        day.entries.length > 0 ||
        day.inserts.length > 0 ||
        day.practices.length > 0 ||
        day.isNonWorking ||
        day.isSunday ||
        day.isWorkingDay === true,
    )

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
          meta?.totalWeeks ?? 17,
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
          meta?.totalWeeks ?? 17,
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

  // Пользователь смотрит расписание выбора, который ещё не сохранён в боте.
  const viewIsUnsaved =
    Object.keys(viewContext).length > 0 &&
    !sameSelection(viewContext, currentSelection)

  const saveViewedSelection = async () => {
    if (selectionPending) return
    setSelectionPending(true)
    try {
      await makeCurrentSelection(viewContext)
    } catch {
      // Строка остаётся — пользователь может повторить попытку.
    } finally {
      setSelectionPending(false)
    }
  }

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
          <div className="max-schedule__page-actions">
            <Button variant="secondary" size="small" onClick={goToday}>
              Сегодня
            </Button>
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
          </div>
        </header>

        {viewIsUnsaved ? (
          <div className="max-schedule__notice">
            <Typography.Body className="max-app__muted">
              Просмотр без сохранения
            </Typography.Body>
            <Button
              size="small"
              variant="secondary"
              disabled={selectionPending}
              onClick={() => void saveViewedSelection()}
            >
              Сделать текущим
            </Button>
          </div>
        ) : null}

        <div className="max-schedule__nav">
          <div className="max-schedule__nav-info">
            {view === "week" ? (
              <Typography.Body className="max-schedule__nav-label">
                <strong>{selectedWeek ? `Неделя ${selectedWeek}` : ""}</strong>
                <span className="max-app__note">{weekRange}</span>
              </Typography.Body>
            ) : (
              <div className="max-schedule__date-field">
                <Button
                  variant="secondary"
                  size="small"
                  aria-label="Выбрать дату"
                  iconBefore={<CalendarDays size={16} aria-hidden />}
                />
                <input
                  type="date"
                  value={selectedDate}
                  onClick={(e) => {
                    // Нативный пикер открывается только при получении фокуса.
                    // showPicker() принудительно открывает на каждый тап,
                    // даже когда input уже сфокусирован после прошлого открытия.
                    try {
                      e.currentTarget.showPicker()
                    } catch {
                      // Без поддержки showPicker пикер откроется нативным кликом.
                    }
                  }}
                  onChange={(e) => {
                    if (e.target.value) {
                      setView("day")
                      setSelectedDate(e.target.value)
                    }
                  }}
                  aria-label="Выбрать дату"
                />
              </div>
            )}
          </div>
          <div className="max-schedule__nav-actions">
            <Button
              variant="secondary"
              size="small"
              aria-label="Предыдущий период"
              onClick={() => navigate(-1)}
              iconBefore={<ChevronLeft size={16} aria-hidden />}
            />
            <Button
              variant="secondary"
              size="small"
              aria-label="Следующий период"
              onClick={() => navigate(1)}
              iconAfter={<ChevronRight size={16} aria-hidden />}
            />
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
              Выбор ещё не задан. Найдите группу или преподавателя — выбор
              сохранится в боте, и уведомления начнут приходить.
            </Typography.Body>
            <Button onClick={() => setSearchOpen(true)}>Поиск</Button>
          </div>
        ) : view === "week" ? (
          weekData && weekHasContent ? (
            <WeekFeed data={weekData} highlightToday={isCurrentWeek} />
          ) : (
            <ScheduleEmpty />
          )
        ) : dayData && dayHasContent ? (
            <DayFeed
              entries={dayData.entries}
              inserts={dayData.inserts}
              practices={dayData.practices}
              isSunday={dayData.isSunday}
              isNonWorking={dayData.isNonWorking}
              nonWorkingTitle={dayData.nonWorkingTitle}
              isWorkingDay={dayData.isWorkingDay ?? false}
              workingDayTitle={dayData.workingDayTitle ?? null}
              substituteDayOfWeek={dayData.substituteDayOfWeek ?? null}
              today={dateIsToday}
            header={
              <div className="max-week__day">
                <span className="max-week__day--strong">
                  {DAYS.find((d) => d.value === dayData.dayOfWeek)?.full ?? ""}
                </span>
                <span className="max-app__note">
                  {formatDay(selectedDate)}
                  {dayData.entries.length > 0
                    ? ` · ${dayData.entries.length} ${pluralPairs(dayData.entries.length)}`
                    : ""}
                </span>
              </div>
            }
          />
        ) : (
          <ScheduleEmpty />
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