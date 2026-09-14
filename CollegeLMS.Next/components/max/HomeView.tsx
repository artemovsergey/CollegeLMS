"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import Link from "next/link"
import { CalendarDays, ArrowRight } from "lucide-react"
import {
  Button,
  CellHeader,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { fetchDaySchedule, fetchScheduleMeta, isValidDate, normalizeDateOnly, parseIsoDate, toIsoDate } from "@/api/schedule"
import { getHistory } from "@/api/correction"
import type { ScheduleResponse } from "@/types/schedule"
import type { ScheduleHistoryItem } from "@/types/correction"
import { useMaxContext } from "@/lib/max-context"
import { parseMaxDeepLink } from "@/lib/max-deeplink"
import { currentPair, dayLabelFromString, formatTime, lessonTypeColor, lessonTypeLabel } from "@/lib/max-lesson"
import ChangeCard from "@/components/max/ChangeCard"
import CurrentPairCard from "@/components/max/CurrentPairCard"
import ScheduleEmpty from "@/components/max/ScheduleEmpty"
import ScheduleError from "@/components/max/ScheduleError"
import SearchSheet from "@/components/max/SearchSheet"

export default function HomeView() {
  const { viewContext, isAuthed, loading: contextLoading } = useMaxContext()
  const [week, setWeek] = useState<number | null>(null)
  const [today, setToday] = useState(() => toIsoDate(new Date()))
  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [changes, setChanges] = useState<ScheduleHistoryItem[]>([])
  const [changesError, setChangesError] = useState(false)
  const [searchOpen, setSearchOpen] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const meta = await fetchScheduleMeta()
      if (!meta.isSuccess) {
        throw new Error(meta.errorMessage ?? "Не удалось загрузить календарь")
      }
      setWeek(meta.data!.currentWeek)
      const currentDate = normalizeDateOnly(meta.data!.currentDate)
      if (currentDate) setToday(currentDate)

      const schedule = await fetchDaySchedule({
        date: currentDate || toIsoDate(new Date()),
        groupId: viewContext.groupId,
        teacherId: viewContext.teacherId,
      })
      if (!schedule.isSuccess) {
        throw new Error(schedule.errorMessage ?? "Не удалось загрузить расписание")
      }
      setEntries(schedule.data?.items ?? [])
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось загрузить расписание")
    } finally {
      setLoading(false)
    }
  }, [viewContext.groupId, viewContext.teacherId])

  useEffect(() => {
    void load()
  }, [load])

  const loadChanges = useCallback(async () => {
    if (!isAuthed) return
    try {
      const history = await getHistory({
        groupId: viewContext.groupId,
        teacherId: viewContext.teacherId,
        page: 1,
        pageSize: 3,
      })
      setChanges(history.items)
    } catch {
      setChangesError(true)
    }
  }, [isAuthed, viewContext.groupId, viewContext.teacherId])

  useEffect(() => {
    void loadChanges()
  }, [loadChanges])

  useEffect(() => {
    const link = parseMaxDeepLink(
      typeof window !== "undefined" ? window.location.search : "",
    )
    if (link.date) {
      const normalized = normalizeDateOnly(link.date)
      if (normalized) setToday(normalized)
    }
  }, [])

  const pair = useMemo(() => currentPair(entries), [entries])

  const dateLabel = useMemo(() => {
    const date = parseIsoDate(today)
    if (!isValidDate(date)) return null
    const weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"]
    return `${date.toLocaleDateString("ru-RU", { day: "numeric", month: "long" })}, ${weekdays[(date.getDay() + 6) % 7]}`
  }, [today])

  const hasContext = Boolean(viewContext.groupId || viewContext.teacherId)

  const sorted = useMemo(
    () => [...entries].sort((a, b) => a.numberPair - b.numberPair),
    [entries],
  )

  const contextName = viewContext.groupName ?? viewContext.teacherName ?? null

  return (
    <MaxUI className="max-home">
      <main className="max-app__page">
        <header className="max-app__page-title">
          <div>
            <Typography.Title>
              {dateLabel ? `Сегодня, ${dateLabel.toLowerCase()}` : "Сегодня"}
            </Typography.Title>
            {contextName ? (
              <Typography.Body className="max-app__muted">
                {contextName}
              </Typography.Body>
            ) : null}
          </div>
          {week !== null ? (
            <span className="max-app__badge max-app__badge--replace">
              Неделя {week}
            </span>
          ) : null}
        </header>

        {contextLoading || loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
            <Typography.Body>Загрузка…</Typography.Body>
          </div>
        ) : error ? (
          <ScheduleError message={error} onRetry={() => void load()} />
        ) : !hasContext ? (
          <div className="max-app__login-prompt">
            <CalendarDays size={32} className="max-app__state-icon" aria-hidden />
            <Typography.Title>Выберите расписание</Typography.Title>
            <Typography.Body className="max-app__muted">
              Откройте группу или преподавателя через поиск
            </Typography.Body>
            <Link href="/max/schedule" passHref>
              <Button>Расписание</Button>
            </Link>
          </div>
        ) : sorted.length === 0 ? (
          <ScheduleEmpty />
        ) : (
          <>
            <CurrentPairCard current={pair.current} next={pair.next} />

            <CellList
              mode="island"
              header={<CellHeader titleStyle="normal">День</CellHeader>}
            >
              {sorted.map((entry) => (
                <CellSimple
                  key={entry.id}
                  separator
                  title={entry.subject}
                  before={
                    <div
                      className="max-schedule__pair"
                      style={{ borderColor: lessonTypeColor(entry.lessonType) }}
                    >
                      <Typography.Label>{entry.numberPair}</Typography.Label>
                      <span>{formatTime(entry.startTime)}</span>
                    </div>
                  }
                  subtitle={`${formatTime(entry.startTime)} – ${formatTime(entry.endTime)} · ${lessonTypeLabel(entry.lessonType)}`}
                  after={
                    <span style={{ color: lessonTypeColor(entry.lessonType) }}>
                      {lessonTypeLabel(entry.lessonType)}
                    </span>
                  }
                />
              ))}
            </CellList>
          </>
        )}

        {!isAuthed ? null : changesError ? null : changes.length > 0 ? (
          <CellList
            mode="island"
            header={
              <CellHeader titleStyle="normal">
                <span className="max-app__page-title">
                  Изменения
                  <Link href="/max/changes" aria-label="Все изменения">
                    <ArrowRight size={18} aria-hidden />
                  </Link>
                </span>
              </CellHeader>
            }
          >
            {changes.map((item) => (
              <ChangeCard key={item.id} item={item} />
            ))}
          </CellList>
        ) : (
          !contextLoading && (
            <CellList mode="island">
              <CellSimple
                title="Изменений сегодня нет"
                subtitle="О них расскажем в ленте"
              />
            </CellList>
          )
        )}

        <Button
          variant="secondary"
          stretched
          onClick={() => setSearchOpen(true)}
        >
          Сменить просмотр
        </Button>
      </main>

      <SearchSheet open={searchOpen} onClose={() => setSearchOpen(false)} />
    </MaxUI>
  )
}