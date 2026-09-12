"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import {
  Button,
  CellHeader,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import {
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  Clock3,
  GraduationCap,
  MapPin,
} from "lucide-react"
import { fetchSchedule } from "@/api/schedule"
import type { ScheduleResponse } from "@/types/schedule"

const SEMESTER_START = new Date(2026, 8, 1)
const TOTAL_WEEKS = 52
const WEEKDAYS = [
  { value: 1, label: "Пн", full: "Понедельник" },
  { value: 2, label: "Вт", full: "Вторник" },
  { value: 3, label: "Ср", full: "Среда" },
  { value: 4, label: "Чт", full: "Четверг" },
  { value: 5, label: "Пт", full: "Пятница" },
  { value: 6, label: "Сб", full: "Суббота" },
]

function mondayOf(date: Date) {
  const result = new Date(date)
  const day = result.getDay()
  result.setDate(result.getDate() - (day === 0 ? 6 : day - 1))
  result.setHours(0, 0, 0, 0)
  return result
}

function currentWeek() {
  const diff = mondayOf(new Date()).getTime() - mondayOf(SEMESTER_START).getTime()
  return Math.max(1, Math.floor(diff / (7 * 24 * 60 * 60 * 1000)) + 1)
}

function weekDates(week: number) {
  const start = mondayOf(SEMESTER_START)
  start.setDate(start.getDate() + (week - 1) * 7)
  const end = new Date(start)
  end.setDate(end.getDate() + 6)
  return `${start.toLocaleDateString("ru-RU", { day: "numeric", month: "short" })} – ${end.toLocaleDateString("ru-RU", { day: "numeric", month: "short" })}`
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function lessonTypeLabel(type: ScheduleResponse["lessonType"]) {
  return {
    Lecture: "Лекция",
    Practice: "Практика",
    Lab: "Лабораторная",
    Exam: "Экзамен",
    None: "Занятие",
  }[type] ?? "Занятие"
}

function lessonTypeColor(type: ScheduleResponse["lessonType"]) {
  return {
    Lecture: "#3478f6",
    Practice: "#28a745",
    Lab: "#e6a700",
    Exam: "#e04f5f",
    None: "#8b929a",
  }[type] ?? "#8b929a"
}

export default function MaxScheduleView() {
  const [identity] = useState(() => {
    if (typeof window === "undefined") return { groupId: undefined, teacherId: undefined }
    const params = new URLSearchParams(window.location.search)
    return {
      groupId: params.get("groupId") ?? undefined,
      teacherId: params.get("teacherId") ?? undefined,
    }
  })
  const [week, setWeek] = useState(currentWeek)
  const [day, setDay] = useState(() => {
    const today = new Date().getDay()
    return today === 0 ? 1 : today
  })
  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadSchedule = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      if (!identity.groupId && !identity.teacherId) {
        setEntries([])
        setError("Откройте расписание из меню MAX после выбора группы или преподавателя")
        return
      }

      const result = await fetchSchedule({
        ...identity,
        week,
        pageSize: 200,
      })
      if (!result.isSuccess || !result.data) {
        setError(result.errorMessage ?? "Не удалось загрузить расписание")
        return
      }
      setEntries(result.data.items)
    } catch {
      setError("Не удалось загрузить расписание")
    } finally {
      setLoading(false)
    }
  }, [identity, week])

  useEffect(() => {
    void loadSchedule()
  }, [loadSchedule])

  const visibleEntries = useMemo(
    () =>
      entries
        .filter((entry) => entry.dayOfWeek === day)
        .sort((a, b) => a.numberPair - b.numberPair),
    [day, entries],
  )

  const selectedDay = WEEKDAYS.find((item) => item.value === day) ?? WEEKDAYS[0]

  return (
    <MaxUI className="max-schedule">
      <main className="max-schedule__content">
        <header className="max-schedule__hero">
          <div className="max-schedule__brand">
            <div className="max-schedule__icon" aria-hidden>
              <CalendarDays size={22} />
            </div>
            <div>
              <Typography.Title>Расписание</Typography.Title>
              <Typography.Body className="max-schedule__muted">
                {identity.groupId ? "Расписание группы" : identity.teacherId ? "Расписание преподавателя" : "Открытое расписание"}
              </Typography.Body>
            </div>
          </div>
          <div className="max-schedule__week">
            <Button
              variant="ghost"
              size="small"
              iconBefore={<ChevronLeft size={18} />}
              aria-label="Предыдущая неделя"
              disabled={week <= 1}
              onClick={() => setWeek((value) => Math.max(1, value - 1))}
            />
            <div className="max-schedule__week-label">
              <Typography.Label>Неделя {week}</Typography.Label>
              <Typography.Body className="max-schedule__muted">{weekDates(week)}</Typography.Body>
            </div>
            <Button
              variant="ghost"
              size="small"
              iconBefore={<ChevronRight size={18} />}
              aria-label="Следующая неделя"
              disabled={week >= TOTAL_WEEKS}
              onClick={() => setWeek((value) => Math.min(TOTAL_WEEKS, value + 1))}
            />
          </div>
        </header>

        <nav className="max-schedule__days" aria-label="Дни недели">
          {WEEKDAYS.map((item) => (
            <Button
              key={item.value}
              size="small"
              variant={item.value === day ? "primary" : "secondary"}
              stretched
              onClick={() => setDay(item.value)}
              aria-label={item.full}
            >
              {item.label}
            </Button>
          ))}
        </nav>

        <CellList mode="island" header={<CellHeader titleStyle="normal">{selectedDay.full}</CellHeader>}>
          {loading ? (
            <div className="max-schedule__state">
              <Spinner size={24} />
              <Typography.Body>Загрузка расписания…</Typography.Body>
            </div>
          ) : error ? (
            <div className="max-schedule__state">
              <Typography.Body>{error}</Typography.Body>
              {identity.groupId || identity.teacherId ? (
                <Button size="small" onClick={() => void loadSchedule()}>Повторить</Button>
              ) : null}
            </div>
          ) : visibleEntries.length === 0 ? (
            <div className="max-schedule__state">
              <Typography.Title>Пар нет</Typography.Title>
              <Typography.Body className="max-schedule__muted">На этот день занятий нет</Typography.Body>
            </div>
          ) : (
            visibleEntries.map((entry) => (
              <CellSimple
                key={entry.id}
                separator
                before={
                  <div
                    className="max-schedule__pair"
                    style={{ borderColor: lessonTypeColor(entry.lessonType) }}
                  >
                    <Typography.Label>{entry.numberPair}</Typography.Label>
                    <span>{formatTime(entry.startTime)}</span>
                  </div>
                }
                title={entry.subject}
                subtitle={
                  <span className="max-schedule__details">
                    <span><Clock3 size={14} /> {formatTime(entry.startTime)} – {formatTime(entry.endTime)}</span>
                    <span><MapPin size={14} /> {entry.room || "Кабинет не указан"}</span>
                    {entry.teacherName ? (
                      <span><GraduationCap size={14} /> {entry.teacherName}</span>
                    ) : null}
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
            ))
          )}
        </CellList>
      </main>
    </MaxUI>
  )
}
