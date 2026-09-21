"use client"

import { useCallback, useEffect, useState, type ReactNode } from "react"
import {
  Activity,
  CalendarOff,
  ChevronLeft,
  ChevronRight,
  Clock,
  GraduationCap,
  RefreshCw,
  Users,
} from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { cn, extractErrorMessage } from "@/lib/utils"
import { formatDate } from "@/lib/reference"
import {
  LIVE_STATUS_LABELS,
  fetchLiveDashboard,
  type LiveDashboardResponse,
  type LiveEntityStatus,
  type LiveEntry,
  type LiveLessonStatus,
} from "@/api/live"

const REFRESH_INTERVAL_MS = 30_000

const STATUS_BADGE: Record<LiveLessonStatus, string> = {
  InLesson: "border-transparent bg-primary text-primary-foreground",
  Finished: "border-border bg-card text-muted-foreground",
  NoPairs: "border-transparent bg-muted text-muted-foreground",
  Waiting: "border-transparent bg-warning text-warning-foreground",
}

const COUNT_TILES: {
  key: keyof LiveDashboardResponse["counts"]
  label: string
  bar: string
  value: string
}[] = [
  { key: "inLesson", label: "Идут", bar: "bg-primary", value: "text-primary" },
  { key: "finished", label: "Закончились", bar: "bg-border", value: "text-fg" },
  {
    key: "noPairs",
    label: "Нет пар",
    bar: "bg-muted-foreground/50",
    value: "text-muted-foreground",
  },
  { key: "waiting", label: "Ожидание", bar: "bg-warning", value: "text-warning" },
]

function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, "0")
  const day = String(now.getDate()).padStart(2, "0")
  return `${now.getFullYear()}-${month}-${day}`
}

function shiftIsoDate(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map(Number)
  if (!year || !month || !day) return iso
  const base = new Date(Date.UTC(year, month - 1, day))
  base.setUTCDate(base.getUTCDate() + days)
  return base.toISOString().slice(0, 10)
}

function clockSeconds(value: string): number | null {
  const time = value.includes("T") ? value.slice(11, 19) : value.slice(0, 8)
  const match = /^(\d{2}):(\d{2}):(\d{2})$/.exec(time)
  if (!match) return null
  return Number(match[1]) * 3600 + Number(match[2]) * 60 + Number(match[3])
}

function shortClock(value: string): string {
  return value.slice(0, 5)
}

function clockOfMoment(value: string): string {
  const direct = /^(\d{2}:\d{2}:\d{2})/.exec(value)
  if (direct) return direct[1]
  const part = value.slice(11, 19)
  return /^\d{2}:\d{2}:\d{2}$/.test(part) ? part : "—"
}

function pairRange(start: string, end: string): string {
  return `${shortClock(start)}–${shortClock(end)}`
}

function pairProgress(
  now: string,
  start: string,
  end: string,
): number | null {
  const current = clockSeconds(now)
  const from = clockSeconds(start)
  const to = clockSeconds(end)
  if (current === null || from === null || to === null || to <= from) {
    return null
  }
  const ratio = ((current - from) / (to - from)) * 100
  return Math.min(100, Math.max(0, Math.round(ratio)))
}

function counterpart(entry: LiveEntry, view: "teacher" | "group"): string {
  if (view === "group") return entry.teacherName ?? "Преподаватель не указан"
  return entry.groupName
}

function StatusBadge({ status }: { status: LiveLessonStatus }) {
  return (
    <Badge variant="outline" className={cn("shrink-0", STATUS_BADGE[status])}>
      {status === "InLesson" && (
        <span
          className="size-1.5 animate-pulse rounded-full bg-current motion-reduce:animate-none"
          aria-hidden="true"
        />
      )}
      {LIVE_STATUS_LABELS[status]}
    </Badge>
  )
}

function EntityTile({
  entity,
  now,
  view,
}: {
  entity: LiveEntityStatus
  now: string
  view: "teacher" | "group"
}) {
  const current = entity.currentPair
  const next = entity.nextPair
  const progress =
    entity.status === "InLesson" && current
      ? pairProgress(now, current.startTime, current.endTime)
      : null

  return (
    <li className="flex flex-col rounded-xl border bg-card p-4">
      <div className="flex items-start justify-between gap-2">
        <p className="min-w-0 flex-1 text-sm font-medium break-words">
          {entity.name}
        </p>
        <StatusBadge status={entity.status} />
      </div>
      <p className="mt-1 text-xs text-muted-foreground">
        Пар: {entity.totalPairs}
      </p>

      {entity.status === "NoPairs" && (
        <p className="mt-3 text-xs text-muted-foreground">
          На выбранный день пар нет
        </p>
      )}

      {current && (
        <div className="mt-3 flex flex-col gap-1.5">
          <p className="flex items-center gap-1.5 text-xs">
            <Clock
              className="size-3.5 shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
            <span className="text-fg">
              Пара {current.numberPair} · {pairRange(current.startTime, current.endTime)}
            </span>
          </p>
          {progress !== null && (
            <div
              role="progressbar"
              aria-label={`Пара ${current.numberPair}: пройдено ${progress}%`}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={progress}
              className="h-1.5 w-full overflow-hidden rounded-full bg-muted"
            >
              <span
                className="block h-full rounded-full bg-primary"
                style={{ width: `${progress}%` }}
              />
            </div>
          )}
        </div>
      )}

      {next && (
        <p className="mt-1.5 text-xs text-muted-foreground">
          Следующая: пара {next.numberPair} · {pairRange(next.startTime, next.endTime)}
        </p>
      )}

      {entity.entries.length > 0 && (
        <details className="mt-3 border-t border-border pt-2">
          <summary className="flex min-h-11 cursor-pointer list-none items-center text-xs font-medium text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none sm:min-h-9">
            Занятия: {entity.entries.length}
          </summary>
          <ul className="flex flex-col pt-1">
            {entity.entries.map((entry, index) => (
              <li
                key={`${entry.groupId}-${entry.numberPair}-${index}`}
                className="flex flex-col gap-0.5 border-t border-border/60 py-2 first:border-t-0"
              >
                <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                  <span className="font-mono text-xs tabular-nums text-muted-foreground">
                    {shortClock(entry.startTime)}–{shortClock(entry.endTime)}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    пара {entry.numberPair}
                  </span>
                  {entry.isPractice && (
                    <Badge
                      variant="outline"
                      className="border-transparent bg-muted text-muted-foreground"
                    >
                      {entry.practiceName ?? "Практика"}
                    </Badge>
                  )}
                </div>
                <p className="text-sm font-medium break-words">{entry.subject}</p>
                <p className="text-xs text-muted-foreground break-words">
                  {counterpart(entry, view)}
                  {entry.room ? ` · ауд. ${entry.room}` : ""}
                </p>
              </li>
            ))}
          </ul>
        </details>
      )}
    </li>
  )
}

function EntitySection({
  title,
  icon,
  entities,
  now,
  view,
}: {
  title: string
  icon: ReactNode
  entities: LiveEntityStatus[]
  now: string
  view: "teacher" | "group"
}) {
  if (entities.length === 0) return null
  const headingId = `live-section-${view}`
  return (
    <section aria-labelledby={headingId} className="flex flex-col gap-3">
      <h2
        id={headingId}
        className="flex items-center gap-2 text-lg font-semibold"
      >
        {icon}
        {title}
        <span className="text-sm font-normal text-muted-foreground">
          {entities.length}
        </span>
      </h2>
      <ul className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {entities.map((entity) => (
          <EntityTile key={entity.id} entity={entity} now={now} view={view} />
        ))}
      </ul>
    </section>
  )
}

export default function DispatcherLivePage() {
  const [date, setDate] = useState<string>(() => todayIso())
  const [data, setData] = useState<LiveDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(
    async (mode: "initial" | "silent") => {
      if (mode === "initial") setLoading(true)
      else setRefreshing(true)
      try {
        const result = await fetchLiveDashboard(date)
        setData(result)
        setError(null)
      } catch (err) {
        setError(
          extractErrorMessage(err) ?? "Не удалось загрузить текущие пары",
        )
      } finally {
        setLoading(false)
        setRefreshing(false)
      }
    },
    [date],
  )

  useEffect(() => {
    void load("initial")
  }, [load])

  useEffect(() => {
    const timer = window.setInterval(() => {
      if (document.hidden) return
      void load("silent")
    }, REFRESH_INTERVAL_MS)
    const onVisibilityChange = () => {
      if (!document.hidden) void load("silent")
    }
    document.addEventListener("visibilitychange", onVisibilityChange)
    return () => {
      window.clearInterval(timer)
      document.removeEventListener("visibilitychange", onVisibilityChange)
    }
  }, [load])

  const isEmpty =
    data !== null && data.teachers.length === 0 && data.groups.length === 0

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-col gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="flex items-center gap-2 text-2xl font-semibold">
            <Activity className="size-6 text-primary" aria-hidden="true" />
            Текущие пары
          </h1>
          <p className="text-sm text-muted-foreground" aria-live="polite">
            {data
              ? `Дата: ${formatDate(data.date)} · неделя ${data.week} · обновлено в ${clockOfMoment(data.now)}`
              : "Загрузка состояния занятий…"}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            onClick={() => setDate((value) => shiftIsoDate(value, -1))}
            className="min-h-11 sm:min-h-9"
          >
            <ChevronLeft className="size-4" aria-hidden="true" />
            День назад
          </Button>
          <Button
            variant="outline"
            onClick={() => setDate(todayIso())}
            className="min-h-11 sm:min-h-9"
          >
            Сегодня
          </Button>
          <Button
            variant="outline"
            onClick={() => setDate((value) => shiftIsoDate(value, 1))}
            className="min-h-11 sm:min-h-9"
          >
            День вперёд
            <ChevronRight className="size-4" aria-hidden="true" />
          </Button>
          <Button
            variant="outline"
            onClick={() => void load("silent")}
            disabled={refreshing}
            aria-busy={refreshing}
            className="min-h-11 sm:ml-auto sm:min-h-9"
          >
            <RefreshCw
              className={cn("size-4", refreshing && "animate-spin motion-reduce:animate-none")}
              aria-hidden="true"
            />
            {refreshing ? "Обновление…" : "Обновить"}
          </Button>
          <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <span
              className="size-1.5 animate-pulse rounded-full bg-primary motion-reduce:animate-none"
              aria-hidden="true"
            />
            Автообновление 30 с
          </span>
        </div>
      </header>

      {data?.isNonWorking && (
        <div
          role="status"
          className="flex items-start gap-3 rounded-xl border border-warning/40 bg-warning/10 p-4 text-sm text-warning"
        >
          <CalendarOff className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <div className="flex flex-col gap-1">
            <p className="font-medium">
              {data.nonWorkingTitle ?? "Нерабочий день"}
            </p>
            <p>Занятий по расписанию нет. Пары отменены.</p>
          </div>
        </div>
      )}

      {data && !data.isNonWorking && data.isWorkingDay && data.workingDayTitle && (
        <div
          role="status"
          className="flex items-start gap-3 rounded-xl border border-primary/40 bg-primary/10 p-4 text-sm text-primary"
        >
          <Activity className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <div className="flex flex-col gap-1">
            <p className="font-medium">Рабочий выходной: {data.workingDayTitle}</p>
            <p>Занятия идут по расписанию заменяемого дня.</p>
          </div>
        </div>
      )}

      {error && (
        <div
          role="alert"
          className="flex flex-wrap items-center gap-3 rounded-xl border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive"
        >
          <span className="flex-1">{error}</span>
          <Button
            variant="outline"
            onClick={() => void load("silent")}
            className="min-h-11 border-destructive/40 bg-transparent text-destructive hover:bg-destructive/10 hover:text-destructive sm:min-h-9"
          >
            <RefreshCw className="size-4" aria-hidden="true" />
            Повторить
          </Button>
        </div>
      )}

      {loading && !data ? (
        <div
          role="status"
          aria-label="Загрузка текущих пар"
          className="grid grid-cols-2 gap-3 sm:grid-cols-4"
        >
          {Array.from({ length: 4 }).map((_, index) => (
            <div
              key={index}
              className="h-20 animate-pulse rounded-xl border bg-muted motion-reduce:animate-none"
            />
          ))}
        </div>
      ) : (
        <>
          {data && (
            <section aria-label="Сводка по статусам" className="flex flex-col gap-3">
              <ul className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                {COUNT_TILES.map((tile) => (
                  <li
                    key={tile.key}
                    className="flex flex-col gap-2 rounded-xl border bg-card p-4"
                  >
                    <span
                      className={cn("h-1.5 w-10 rounded-full", tile.bar)}
                      aria-hidden="true"
                    />
                    <div className="flex items-end gap-2">
                      <span
                        className={cn(
                          "font-mono text-2xl leading-none font-semibold tabular-nums",
                          tile.value,
                        )}
                      >
                        {data.counts[tile.key]}
                      </span>
                      <span className="text-sm text-muted-foreground">
                        {tile.label}
                      </span>
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {isEmpty && (
            <Card>
              <CardContent className="flex flex-col items-center gap-3 py-16 text-center">
                <CalendarOff
                  className="size-12 text-muted-foreground opacity-60"
                  aria-hidden="true"
                />
                <p className="text-base font-medium">На сегодня пар нет</p>
                <p className="text-sm text-muted-foreground">
                  На выбранную дату занятия не найдены. Проверьте соседние дни.
                </p>
              </CardContent>
            </Card>
          )}

          {data && (
            <EntitySection
              title="Преподаватели"
              icon={
                <GraduationCap
                  className="size-5 text-muted-foreground"
                  aria-hidden="true"
                />
              }
              entities={data.teachers}
              now={data.now}
              view="teacher"
            />
          )}

          {data && (
            <EntitySection
              title="Группы"
              icon={
                <Users
                  className="size-5 text-muted-foreground"
                  aria-hidden="true"
                />
              }
              entities={data.groups}
              now={data.now}
              view="group"
            />
          )}
        </>
      )}
    </div>
  )
}
