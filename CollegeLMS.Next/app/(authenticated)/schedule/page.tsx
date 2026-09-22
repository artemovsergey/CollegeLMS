"use client"

import { useEffect, useState, useCallback, useRef } from "react"
import { useRouter } from "next/navigation"
import type { Result, GroupResponse, TeacherResponse } from "@/types"
import type { ScheduleResponse, ScheduleViewMode } from "@/types/schedule"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import {
  fetchScheduleContext,
  fetchScheduleMeta,
  exportSchedule,
  deleteSchedule,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
  toIsoMonth,
  type ScheduleFilters,
  type ScheduleMeta,
} from "@/api/schedule"
import { Button } from "@/components/ui/button"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import ScheduleViewSwitcher from "@/components/ScheduleViewSwitcher"
import WeekNavigation from "@/components/WeekNavigation"
import ScheduleDayView from "@/components/ScheduleDayView"
import ScheduleWeekView from "@/components/ScheduleWeekView"
import ScheduleMonthCalendar from "@/components/ScheduleMonthCalendar"
import ScheduleSemesterMatrix from "@/components/ScheduleSemesterMatrix"
import ScheduleFilterPrompt from "@/components/ScheduleFilterPrompt"
import ScheduleEntryDialog from "@/components/ScheduleEntryDialog"
import ScheduleImportDialog from "@/components/ScheduleImportDialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { CAN_MANAGE_ROLES } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import { CalendarDays, Download, Filter, SearchX, Upload } from "lucide-react"
import { toast } from "sonner"

function mondayOf(date: Date): Date {
  const d = new Date(date)
  const offset = (d.getDay() + 6) % 7
  d.setDate(d.getDate() - offset)
  d.setHours(0, 0, 0, 0)
  return d
}

export default function SchedulePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [view, setView] = useState<ScheduleViewMode>("day")
  const [selectedDate, setSelectedDate] = useState(() => toIsoDate(new Date()))
  const [selectedWeek, setSelectedWeek] = useState(1)
  const [selectedMonth, setSelectedMonth] = useState(() =>
    toIsoMonth(new Date()),
  )
  const [meta, setMeta] = useState<ScheduleMeta | null>(null)

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])

  const [selectedGroupId, setSelectedGroupId] = useState("")
  const [selectedTeacherId, setSelectedTeacherId] = useState("")
  const [defaultGroupId, setDefaultGroupId] = useState("")
  const [defaultTeacherId, setDefaultTeacherId] = useState("")

  const [refreshKey, setRefreshKey] = useState(0)
  const [urlReady, setUrlReady] = useState(false)
  const [contextReady, setContextReady] = useState(false)

  const [entryDialogOpen, setEntryDialogOpen] = useState(false)
  const [editingEntry, setEditingEntry] = useState<ScheduleResponse | null>(
    null,
  )
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const legacyRef = useRef<{ week: number; dayOffset: number } | null>(null)
  const hasUrlDateRef = useRef(false)
  const hasUrlWeekRef = useRef(false)
  const hasUrlMonthRef = useRef(false)
  const urlGroupRef = useRef<string | null>(null)
  const urlTeacherRef = useRef<string | null>(null)
  // Embed-режим для превью: страница открыта в телефонной рамке,
  // в этом режиме URL не перезаписывается.
  const [embed, setEmbed] = useState(false)

  const canManage = user?.roles
    ? user.roles.some((role) => CAN_MANAGE_ROLES.includes(role))
    : false
  const hasCustomFilters =
    selectedGroupId !== defaultGroupId ||
    selectedTeacherId !== defaultTeacherId
  const hasSelectedFilter = Boolean(selectedGroupId || selectedTeacherId)

  const exportLabel =
    view === "day"
      ? "Экспорт: день"
      : view === "week"
        ? "Экспорт: неделя"
        : "Экспорт: семестр"

  // Разбор URL: view|date|week|month и миграция старых ?week=&day=.
  useEffect(() => {
    if (typeof window === "undefined") return
    const sp = new URLSearchParams(window.location.search)

    const viewParam = sp.get("view")
    const dateParam = normalizeDateOnly(sp.get("date"))
    const monthParam = sp.get("month")
    const weekParam = Number(sp.get("week"))
    const dayParam = Number(sp.get("day"))
    const hasValidWeek = Number.isFinite(weekParam) && weekParam >= 1

    if (
      viewParam === "week" ||
      viewParam === "calendar" ||
      viewParam === "semester"
    ) {
      setView(viewParam)
    }

    if (dateParam) {
      setSelectedDate(dateParam)
      hasUrlDateRef.current = true
    }
    if (monthParam && /^\d{4}-\d{2}$/.test(monthParam)) {
      setSelectedMonth(monthParam)
      hasUrlMonthRef.current = true
    }
    if (viewParam === "week" && hasValidWeek) {
      setSelectedWeek(weekParam)
      hasUrlWeekRef.current = true
    }

    // Цель просмотра из query (превью мини-аппа) приоритетнее контекста роли.
    const groupParam = sp.get("groupId")
    const teacherParam = sp.get("teacherId")
    if (groupParam) {
      urlGroupRef.current = groupParam
      setSelectedGroupId(groupParam)
      setSelectedTeacherId("")
    } else if (teacherParam) {
      urlTeacherRef.current = teacherParam
      setSelectedTeacherId(teacherParam)
      setSelectedGroupId("")
    }
    if (sp.get("embed") === "1") setEmbed(true)

    // Переход из раздела «Изменения»: ?week=5&day=3 → «День» с вычисленной датой.
    // ChangeCard формирует day как смещение от понедельника (Пн=0…Вс=6),
    // значение 7 также трактуем как воскресенье.
    if (!viewParam && hasValidWeek) {
      const dayOffset =
        Number.isFinite(dayParam) && dayParam >= 0 && dayParam <= 6
          ? dayParam
          : dayParam === 7
            ? 6
            : 0
      legacyRef.current = { week: weekParam, dayOffset }
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  // Мета семестра: дефолтная дата/неделя и миграция легаси-ссылок.
  useEffect(() => {
    if (!token) return
    let cancelled = false
    fetchScheduleMeta()
      .then((body) => {
        if (cancelled) return
        if (body.isSuccess && body.data) {
          setMeta(body.data)
          const legacy = legacyRef.current
          if (legacy) {
            legacyRef.current = null
            const semesterMonday = mondayOf(
              parseIsoDate(normalizeDateOnly(body.data.semesterStart)),
            )
            const date = new Date(semesterMonday)
            date.setDate(
              date.getDate() + (legacy.week - 1) * 7 + legacy.dayOffset,
            )
            setView("day")
            setSelectedDate(toIsoDate(date))
            setSelectedWeek(body.data.currentWeek)
          } else {
            if (!hasUrlDateRef.current) {
              setSelectedDate(
                normalizeDateOnly(body.data.currentDate) ||
                  toIsoDate(new Date()),
              )
            }
            if (!hasUrlWeekRef.current) setSelectedWeek(body.data.currentWeek)
          }
        }
        setUrlReady(true)
      })
      .catch(() => {
        if (!cancelled) setUrlReady(true)
      })
    return () => {
      cancelled = true
    }
  }, [token])

  // Дефолт пользователя: студент → своя группа, преподаватель → он сам.
  useEffect(() => {
    if (!token) return
    let cancelled = false
    fetchScheduleContext()
      .then((body) => {
        if (cancelled || !body.isSuccess || !body.data) return
        const groupId = body.data.groupId ?? ""
        const teacherId = body.data.teacherId ?? ""
        setDefaultGroupId(groupId)
        setDefaultTeacherId(teacherId)
        setSelectedGroupId(groupId)
        setSelectedTeacherId(teacherId)
        // Значения из query (превью) важнее дефолта роли.
        if (urlGroupRef.current) {
          setSelectedGroupId(urlGroupRef.current)
          setSelectedTeacherId("")
        } else if (urlTeacherRef.current) {
          setSelectedTeacherId(urlTeacherRef.current)
          setSelectedGroupId("")
        }
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setContextReady(true)
      })
    return () => {
      cancelled = true
    }
  }, [token])

  const loadGroups = useCallback(async () => {
    try {
      const { data } = await api.get<Result<GroupResponse[]>>("/api/groups")
      if (data.isSuccess && data.data) setGroups(data.data)
    } catch {
      /* ignore */
    }
  }, [])

  const loadTeachers = useCallback(async () => {
    try {
      const { data } = await api.get<Result<TeacherResponse[]>>("/api/teachers")
      if (data.isSuccess && data.data) setTeachers(data.data)
    } catch {
      /* ignore */
    }
  }, [])

  useEffect(() => {
    if (token) {
      loadGroups()
      loadTeachers()
    }
  }, [token, loadGroups, loadTeachers])

  // Синхронизация состояния с URL без перезагрузки страницы.
  useEffect(() => {
    // В embed-режиме URL не трогаем: иначе потеряются embed и фильтр превью.
    if (!urlReady || embed || typeof window === "undefined") return
    const params = new URLSearchParams()
    params.set("view", view)
    if (view === "day") params.set("date", selectedDate)
    if (view === "week") params.set("week", String(selectedWeek))
    if (view === "calendar") params.set("month", selectedMonth)
    window.history.replaceState(
      null,
      "",
      `${window.location.pathname}?${params.toString()}`,
    )
  }, [urlReady, embed, view, selectedDate, selectedWeek, selectedMonth])

  const handleViewChange = (mode: ScheduleViewMode) => {
    setView(mode)
  }

  const handleDateChange = (date: string) => {
    const normalized = normalizeDateOnly(date)
    if (normalized) setSelectedDate(normalized)
  }

  const handleDayOpen = (date: string) => {
    const normalized = normalizeDateOnly(date)
    if (normalized) setSelectedDate(normalized)
    setView("day")
  }

  const handleClear = () => {
    setSelectedGroupId(defaultGroupId)
    setSelectedTeacherId(defaultTeacherId)
  }

  const handleExport = async (
    format: "pdf" | "xlsx",
    layout: "grid" | "daycards" = "grid",
  ) => {
    const filters: ScheduleFilters = {}
    if (selectedGroupId) filters.groupId = selectedGroupId
    if (selectedTeacherId) filters.teacherId = selectedTeacherId
    try {
      if (view === "day") {
        await exportSchedule(filters, format, layout, "day", {
          date: selectedDate,
        })
      } else if (view === "week") {
        await exportSchedule(filters, format, layout, "week", {
          week: selectedWeek,
        })
      } else {
        await exportSchedule(filters, format, layout, "semester")
      }
      toast.success("Экспорт выполнен")
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Ошибка экспорта"
      toast.error(msg)
    }
  }

  const handleEdit = (entry: ScheduleResponse) => {
    setEditingEntry(entry)
    setEntryDialogOpen(true)
  }

  const handleDelete = async (id: string) => {
    try {
      const result = await deleteSchedule(id)
      if (result.isSuccess) {
        toast.success("Запись удалена")
        setRefreshKey((k) => k + 1)
      } else {
        toast.error(result.errorMessage ?? "Ошибка удаления")
      }
    } catch {
      toast.error("Ошибка удаления")
    } finally {
      setDeleteConfirmId(null)
    }
  }

  if (authLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token) return null

  return (
    <div className="mx-auto flex w-full min-w-0 max-w-7xl flex-col gap-4 p-6">
      <div className="flex items-center gap-2">
        <CalendarDays className="size-5 text-primary" aria-hidden />
        <h2 className="text-xl font-semibold">Расписание</h2>
      </div>

      <div className="flex flex-col gap-3 rounded-lg border bg-card p-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap items-center gap-3">
          <Filter
            className="size-4 shrink-0 text-muted-foreground"
            aria-hidden
          />
          <NativeSelect
            value={selectedGroupId || "all"}
            onValueChange={(v) => setSelectedGroupId(v === "all" ? "" : v)}
            placeholder="Группы"
            className="w-44 [&>select]:h-11"
          >
            <NativeSelectItem value="all">Группы</NativeSelectItem>
            {groups.map((g) => (
              <NativeSelectItem key={g.id} value={g.id}>
                {g.name}
              </NativeSelectItem>
            ))}
          </NativeSelect>

          <NativeSelect
            value={selectedTeacherId || "all"}
            onValueChange={(v) => setSelectedTeacherId(v === "all" ? "" : v)}
            placeholder="Преподаватели"
            className="w-44 [&>select]:h-11"
          >
            <NativeSelectItem value="all">Преподаватели</NativeSelectItem>
            {teachers.map((t) => (
              <NativeSelectItem key={t.id} value={t.id}>
                {t.fullName}
              </NativeSelectItem>
            ))}
          </NativeSelect>

          {hasCustomFilters && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClear}
              className="h-11 shrink-0"
            >
              <SearchX className="size-3.5" aria-hidden />
              Сбросить
            </Button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <ScheduleViewSwitcher value={view} onChange={handleViewChange} />

          {view !== "calendar" && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-11"
                  aria-label="Экспорт расписания"
                >
                  <Download className="size-3.5" aria-hidden />
                  {exportLabel}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  onSelect={() => {
                    void handleExport("pdf", "grid")
                  }}
                >
                  PDF — сетка
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => {
                    void handleExport("pdf", "daycards")
                  }}
                >
                  PDF — по дням
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => {
                    void handleExport("xlsx", "grid")
                  }}
                >
                  Excel — сетка
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => {
                    void handleExport("xlsx", "daycards")
                  }}
                >
                  Excel — по дням
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}

          {canManage && (
            <Button
              variant="outline"
              size="sm"
              className="h-11"
              onClick={() => setImportDialogOpen(true)}
            >
              <Upload className="size-3.5" aria-hidden />
              Импорт
            </Button>
          )}
        </div>
      </div>

      {view === "week" && meta && (
        <WeekNavigation
          currentWeek={selectedWeek}
          onChange={setSelectedWeek}
          totalWeeks={meta.totalWeeks}
          semesterStart={parseIsoDate(normalizeDateOnly(meta.semesterStart))}
          todayWeek={meta.currentWeek}
        />
      )}

      {!contextReady ? (
        <div className="flex min-h-[50vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : !hasSelectedFilter ? (
        <ScheduleFilterPrompt />
      ) : (
        <>
          {view === "day" && (
            <ScheduleDayView
              date={selectedDate}
              groupId={selectedGroupId || undefined}
              teacherId={selectedTeacherId || undefined}
              refreshKey={refreshKey}
              onDateChange={handleDateChange}
              onEntryClick={canManage ? handleEdit : undefined}
              onDeleteClick={
                canManage ? (id) => setDeleteConfirmId(id) : undefined
              }
            />
          )}

          {view === "week" && (
            <ScheduleWeekView
              week={selectedWeek}
              groupId={selectedGroupId || undefined}
              teacherId={selectedTeacherId || undefined}
              refreshKey={refreshKey}
              onDayClick={handleDayOpen}
            />
          )}

          {view === "calendar" && (
            <ScheduleMonthCalendar
              month={selectedMonth}
              groupId={selectedGroupId || undefined}
              teacherId={selectedTeacherId || undefined}
              refreshKey={refreshKey}
              onMonthChange={setSelectedMonth}
              onDayClick={handleDayOpen}
            />
          )}

          {view === "semester" && (
            <ScheduleSemesterMatrix
              groupId={selectedGroupId || undefined}
              teacherId={selectedTeacherId || undefined}
              refreshKey={refreshKey}
              onDayClick={handleDayOpen}
            />
          )}
        </>
      )}

      <ScheduleEntryDialog
        open={entryDialogOpen}
        onOpenChange={setEntryDialogOpen}
        onSaved={() => setRefreshKey((k) => k + 1)}
        entry={editingEntry}
        groups={groups}
        teachers={teachers}
        totalWeeks={meta?.totalWeeks ?? 0}
      />

      <ScheduleImportDialog
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        onImported={() => setRefreshKey((k) => k + 1)}
      />

      <AlertDialog
        open={deleteConfirmId !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteConfirmId(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить пару?</AlertDialogTitle>
            <AlertDialogDescription>
              Запись будет удалена из расписания. Это действие нельзя отменить.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (deleteConfirmId) void handleDelete(deleteConfirmId)
              }}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
