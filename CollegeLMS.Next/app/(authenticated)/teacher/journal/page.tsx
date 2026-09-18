"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import type { LucideIcon } from "lucide-react"
import {
  BookOpen,
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
  CalendarDays,
  Inbox,
  RefreshCw,
} from "lucide-react"
import type { Result, TeacherResponse } from "@/types"
import {
  fetchJournal,
  fetchScheduleContext,
  type JournalResponse,
} from "@/api/schedule"
import { useAuth } from "@/lib/auth"
import { cn, extractErrorMessage } from "@/lib/utils"
import { dayLabelFromInt } from "@/lib/max-lesson"
import { CAN_MANAGE_ROLES } from "@/lib/constants"
import api from "@/lib/api"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import FilterSelect from "@/components/FilterSelect"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

const CHANGE_TYPE_META: Record<
  string,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: {
    label: "Добавлено",
    icon: Plus,
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    icon: Minus,
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    icon: Repeat,
    className:
      "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    icon: ArrowRightLeft,
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
  SelfStudy: {
    label: "Сам.р.",
    icon: BookOpen,
    className:
      "bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300",
  },
}

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  })
}

function JournalBadges({ types }: { types: string[] }) {
  if (types.length === 0) return null
  return (
    <span className="flex flex-wrap gap-1">
      {types.map((type) => {
        const meta = CHANGE_TYPE_META[type] ?? {
          label: type,
          icon: BookOpen,
          className: "bg-muted text-muted-foreground",
        }
        const Icon = meta.icon
        return (
          <Badge key={type} variant="outline" className={meta.className}>
            <Icon aria-hidden /> {meta.label}
          </Badge>
        )
      })}
    </span>
  )
}

export default function TeacherJournalPage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [selectedTeacherId, setSelectedTeacherId] = useState("")
  const [ownTeacherId, setOwnTeacherId] = useState<string | undefined>()
  const [ownTeacherName, setOwnTeacherName] = useState<string | undefined>()
  const [contextReady, setContextReady] = useState(false)
  const [journal, setJournal] = useState<JournalResponse | null>(null)
  const [subject, setSubject] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const roles = user?.roles ?? []
  const canPickTeacher = roles.some((role) => CAN_MANAGE_ROLES.includes(role))

  useEffect(() => {
    if (!authLoading && !token) router.push("/login")
  }, [authLoading, token, router])

  // Teacher — свой журнал через /api/schedule/context.
  useEffect(() => {
    if (!token) return
    let cancelled = false
    fetchScheduleContext()
      .then((res) => {
        if (cancelled || !res.isSuccess || !res.data) return
        setOwnTeacherId(res.data.teacherId ?? undefined)
        setOwnTeacherName(res.data.teacherName ?? undefined)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setContextReady(true)
      })
    return () => {
      cancelled = true
    }
  }, [token])

  // Admin/Dispatcher — выбор преподавателя.
  useEffect(() => {
    if (!token || !canPickTeacher) return
    let cancelled = false
    api
      .get<Result<TeacherResponse[]>>("/api/teachers")
      .then(({ data }) => {
        if (cancelled || !data.isSuccess || !data.data) return
        setTeachers(data.data)
        setSelectedTeacherId((prev) => (prev ? prev : (data.data![0]?.id ?? "")))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [token, canPickTeacher])

  const effectiveTeacherId = canPickTeacher ? selectedTeacherId : ownTeacherId

  const load = useCallback(async () => {
    if (!effectiveTeacherId) return
    setLoading(true)
    setError(null)
    try {
      const res = await fetchJournal(effectiveTeacherId)
      if (!res.isSuccess || !res.data) {
        throw new Error(res.errorMessage ?? "Не удалось загрузить журнал")
      }
      setJournal(res.data)
      setSubject((prev) => {
        const subjects = res.data!.subjects.map((s) => s.subject)
        if (prev && subjects.includes(prev)) return prev
        return subjects[0] ?? null
      })
    } catch (err) {
      setError(extractErrorMessage(err) ?? "Не удалось загрузить журнал")
    } finally {
      setLoading(false)
    }
  }, [effectiveTeacherId])

  useEffect(() => {
    void load()
  }, [load])

  const subjectGroup = useMemo(
    () => journal?.subjects.find((s) => s.subject === subject) ?? null,
    [journal, subject],
  )

  const items = useMemo(
    () =>
      subjectGroup
        ? [...subjectGroup.items].sort((a, b) =>
            a.week !== b.week ? a.week - b.week : a.dayOfWeek - b.dayOfWeek,
          )
        : [],
    [subjectGroup],
  )

  if (authLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token) return null

  const waitingForTeacher = canPickTeacher && !selectedTeacherId
  const noTeacherAssigned = !canPickTeacher && !ownTeacherId && !journal

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 p-4 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <BookOpen className="size-5 text-primary" aria-hidden />
          <h2 className="text-xl font-semibold">Журнал преподавателя</h2>
        </div>
        <div className="flex items-center gap-2">
          {journal && (
            <span className="text-sm text-muted-foreground">
              Всего пар: {journal.totalPairCount}
            </span>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={() => void load()}
            disabled={loading || !effectiveTeacherId}
          >
            <RefreshCw className={cn("size-3.5", loading && "animate-spin")} aria-hidden />
            Обновить
          </Button>
        </div>
      </div>

      {canPickTeacher && (
        <FilterSelect
          id="journal-teacher"
          label="Преподаватель"
          containerClassName="sm:max-w-sm"
          value={selectedTeacherId || "none"}
          onChange={(e) =>
            setSelectedTeacherId(e.target.value === "none" ? "" : e.target.value)
          }
        >
          <option value="none">Выберите преподавателя</option>
          {teachers.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.fullName}
            </option>
          ))}
        </FilterSelect>
      )}

      {!canPickTeacher && journal && (
        <p className="text-sm text-muted-foreground">
          {journal.teacherName || ownTeacherName || "Преподаватель"}
        </p>
      )}

      {error && (
        <div className="flex flex-wrap items-center gap-3">
          <ErrorBanner message={error} className="flex-1" />
          <Button variant="outline" size="sm" onClick={() => void load()}>
            Повторить
          </Button>
        </div>
      )}

      {waitingForTeacher || noTeacherAssigned ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
            <Inbox className="size-10 text-muted-foreground" aria-hidden />
            <p className="text-base font-medium">
              {waitingForTeacher
                ? "Выберите преподавателя"
                : "Журнал пока недоступен"}
            </p>
            <p className="text-sm text-muted-foreground">
              {waitingForTeacher
                ? "Журнал будет загружен после выбора преподавателя"
                : "Для вашей учётной записи не найден профиль преподавателя"}
            </p>
          </CardContent>
        </Card>
      ) : (loading || (!canPickTeacher && !contextReady)) && !journal ? (
        <div className="flex min-h-[40vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : !journal || journal.subjects.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
            <Inbox className="size-10 text-muted-foreground" aria-hidden />
            <p className="text-base font-medium">Проведённых занятий нет</p>
            <p className="text-sm text-muted-foreground">
              Журнал формируется из прошедших занятий расписания
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className={cn("flex flex-col gap-4", loading && "opacity-60 transition-opacity")}>
          <div
            className="flex flex-wrap gap-2"
            role="group"
            aria-label="Фильтр по предмету"
          >
            {journal.subjects.map((group) => {
              const active = group.subject === subject
              return (
                <button
                  key={group.subject}
                  type="button"
                  onClick={() => setSubject(group.subject)}
                  aria-pressed={active}
                  className={cn(
                    "inline-flex min-h-9 items-center gap-1.5 rounded-full border px-3 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    active
                      ? "border-primary bg-primary/10 text-primary"
                      : "border-border text-muted-foreground hover:bg-muted hover:text-foreground",
                  )}
                >
                  {group.subject}
                  <span className="text-xs opacity-70">{group.pairCount}</span>
                </button>
              )
            })}
          </div>

          {subjectGroup && (
            <Card className="gap-0">
              <CardContent className="flex flex-col gap-3 py-4">
                <div className="flex items-center justify-between gap-2">
                  <h3 className="text-base font-semibold">{subjectGroup.subject}</h3>
                  <span className="text-sm text-muted-foreground">
                    {subjectGroup.pairCount} пар по расписанию
                  </span>
                </div>
                <ul className="divide-y">
                  {items.map((entry) => (
                    <li
                      key={`${entry.week}:${entry.dayOfWeek}`}
                      className="flex flex-wrap items-center justify-between gap-2 py-3"
                    >
                      <div className="flex flex-col gap-1">
                        <span className="flex items-center gap-2 text-sm font-medium">
                          <CalendarDays className="size-3.5 text-muted-foreground" aria-hidden />
                          {dayLabelFromInt(entry.dayOfWeek)}, {formatDate(entry.date)}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          Неделя {entry.week}
                        </span>
                        <JournalBadges types={entry.changeTypes ?? []} />
                      </div>
                      <span className="text-sm text-muted-foreground">
                        Пар: {entry.numberPairs.length}
                      </span>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}
        </div>
      )}
    </div>
  )
}
