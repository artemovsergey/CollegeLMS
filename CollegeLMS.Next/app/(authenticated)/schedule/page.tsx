"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, GroupResponse, TeacherResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import {
  fetchSchedule,
  exportSchedule,
  deleteSchedule,
} from "@/api/schedule"
import { Button } from "@/components/ui/button"
import {
  NativeDialog,
  NativeDialogHeader,
  NativeDialogTitle,
  NativeDialogDescription,
  NativeDialogFooter,
} from "@/components/ui/native-dialog"
import {
  NativeSelect,
  NativeSelectItem,
} from "@/components/ui/native-select"
import WeekNavigation from "@/components/WeekNavigation"
import DayTabs from "@/components/DayTabs"
import ScheduleTable from "@/components/ScheduleTable"
import SemesterView from "@/components/SemesterView"
import ScheduleEntryDialog from "@/components/ScheduleEntryDialog"
import ScheduleImportDialog from "@/components/ScheduleImportDialog"
import { CAN_MANAGE_ROLES } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import {
  CalendarDays,
  Filter,
  SearchX,
  FileDown,
  FileSpreadsheet,
  Upload,
  LayoutGrid,
} from "lucide-react"
import { toast } from "sonner"

const SEMESTER_START = new Date(2026, 8, 1)

function getMondayOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const offset = day === 0 ? 6 : day - 1
  d.setDate(d.getDate() - offset)
  d.setHours(0, 0, 0, 0)
  return d
}

function getCurrentWeek(): number {
  const now = new Date()
  const currentMonday = getMondayOfWeek(now)
  const semesterMonday = getMondayOfWeek(SEMESTER_START)
  const diffMs = currentMonday.getTime() - semesterMonday.getTime()
  const diffWeeks = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000))
  return Math.max(1, diffWeeks + 1)
}

function defaultDay(): number {
  const day = new Date().getDay()
  return day >= 1 && day <= 5 ? day : 1
}

export default function SchedulePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [allEntries, setAllEntries] = useState<ScheduleResponse[]>([])
  const [initialLoading, setInitialLoading] = useState(true)
  const [semesterLoading, setSemesterLoading] = useState(false)
  const [fetching, setFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])

  const [selectedGroupId, setSelectedGroupId] = useState("")
  const [selectedTeacherId, setSelectedTeacherId] = useState("")
  const [selectedWeek, setSelectedWeek] = useState(getCurrentWeek())
  const [selectedDay, setSelectedDay] = useState<number | null>(defaultDay())
  const [viewMode, setViewMode] = useState<"cards" | "semester">("cards")

  const [entryDialogOpen, setEntryDialogOpen] = useState(false)
  const [editingEntry, setEditingEntry] = useState<ScheduleResponse | null>(
    null,
  )
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const canManage = user?.role ? CAN_MANAGE_ROLES.includes(user.role) : false

  const loadSchedule = useCallback(async () => {
    if (entries.length === 0) {
      setInitialLoading(true)
    } else {
      setFetching(true)
    }
    setError(null)
    try {
      const params: Record<string, string | number | undefined> = {
        pageSize: 200,
      }
      if (selectedGroupId) params.groupId = selectedGroupId
      if (selectedTeacherId) params.teacherId = selectedTeacherId
      if (selectedWeek) params.week = selectedWeek
      const body = await fetchSchedule(params)
      if (body.isSuccess && body.data) {
        setEntries(body.data.items)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки расписания")
      }
    } catch {
      setError("Ошибка загрузки расписания")
    } finally {
      setInitialLoading(false)
      setFetching(false)
    }
  }, [selectedGroupId, selectedTeacherId, selectedWeek, entries.length])

  const loadAllEntries = useCallback(async () => {
    setSemesterLoading(true)
    setError(null)
    try {
      const params: Record<string, string | number | undefined> = {
        pageSize: 2000,
      }
      if (selectedGroupId) params.groupId = selectedGroupId
      if (selectedTeacherId) params.teacherId = selectedTeacherId
      const body = await fetchSchedule(params)
      if (body.isSuccess && body.data) {
        setAllEntries(body.data.items)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки расписания")
      }
    } catch {
      setError("Ошибка загрузки расписания")
    } finally {
      setSemesterLoading(false)
    }
  }, [selectedGroupId, selectedTeacherId])

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
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      loadGroups()
      loadTeachers()
    }
  }, [token, loadGroups, loadTeachers])

  useEffect(() => {
    if (token && viewMode === "cards") {
      loadSchedule()
    }
  }, [token, viewMode, loadSchedule])

  useEffect(() => {
    if (token && allEntries.length === 0) {
      loadAllEntries()
    }
  }, [token, allEntries.length, loadAllEntries])

  const handleViewModeChange = (mode: "cards" | "semester") => {
    setViewMode(mode)
    if (mode === "semester" && allEntries.length === 0) {
      loadAllEntries()
    }
  }

  const handleSemesterCellClick = (week: number, day: number) => {
    setSelectedWeek(week)
    setSelectedDay(day)
    setViewMode("cards")
  }

  const handleClear = () => {
    setSelectedGroupId("")
    setSelectedTeacherId("")
    setSelectedWeek(getCurrentWeek())
    setSelectedDay(defaultDay())
  }

  const handleExport = async (format: "pdf" | "xlsx", layout: "grid" | "daycards" = "grid") => {
    try {
      const params: Record<string, string | number | undefined> = {}
      if (selectedGroupId) params.groupId = selectedGroupId
      if (selectedTeacherId) params.teacherId = selectedTeacherId
      if (viewMode === "cards" && selectedWeek) params.week = selectedWeek
      await exportSchedule(params, format, layout)
      toast.success("Экспорт выполнен")
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Ошибка экспорта"
      toast.error(msg)
    }
  }

  const handleAdd = () => {
    setEditingEntry(null)
    setEntryDialogOpen(true)
  }

  const handleEdit = (entry: ScheduleResponse) => {
    setEditingEntry(entry)
    setEntryDialogOpen(true)
  }

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    const confirmed = window.confirm("Удалить запись? Это действие нельзя отменить.")
    if (!confirmed) {
      setDeleteConfirmId(null)
      return
    }
    try {
      const result = await deleteSchedule(deleteConfirmId)
      if (result.isSuccess) {
        toast.success("Запись удалена")
        if (viewMode === "cards") loadSchedule()
        else loadAllEntries()
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

  const displayEntries = viewMode === "semester" ? allEntries : entries
  const showCards = viewMode === "cards"

  return (
    <div className="flex flex-col gap-4 p-6 mx-auto max-w-7xl">
      <div className="flex items-center gap-2">
        <CalendarDays className="size-5 text-primary" />
        <h2 className="text-xl font-semibold">Расписание</h2>
      </div>

      <div className={`transition-all duration-200 ${showCards ? "opacity-100 max-h-[500px]" : "opacity-0 max-h-0 overflow-hidden pointer-events-none"}`}>
        <WeekNavigation
          currentWeek={selectedWeek}
          onChange={setSelectedWeek}
          totalWeeks={52}
        />
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border bg-card p-4">
        <Filter className="size-4 text-muted-foreground shrink-0" />
        <NativeSelect
          value={selectedGroupId || "all"}
          onValueChange={(v) => setSelectedGroupId(v === "all" ? "" : v)}
          placeholder="Все группы"
          className="w-44"
        >
          <NativeSelectItem value="all">Все группы</NativeSelectItem>
          {groups.map((g) => (
            <NativeSelectItem key={g.id} value={g.id}>
              {g.name}
            </NativeSelectItem>
          ))}
        </NativeSelect>

        <NativeSelect
          value={selectedTeacherId || "all"}
          onValueChange={(v) => setSelectedTeacherId(v === "all" ? "" : v)}
          placeholder="Все преподаватели"
          className="w-44"
        >
          <NativeSelectItem value="all">Все преподаватели</NativeSelectItem>
          {teachers.map((t) => (
            <NativeSelectItem key={t.id} value={t.id}>
              {t.fullName}
            </NativeSelectItem>
          ))}
        </NativeSelect>

        <Button
          variant="ghost"
          size="sm"
          onClick={handleClear}
          className={`shrink-0 transition-opacity ${!selectedGroupId && !selectedTeacherId ? "opacity-0 pointer-events-none" : "opacity-100"}`}
        >
          <SearchX className="size-3.5" />
          Сбросить
        </Button>

        <div className="flex-1" />

        <div className="flex items-center gap-2 shrink-0">
          <div className="flex rounded-md border overflow-hidden">
            <Button
              variant={showCards ? "default" : "ghost"}
              size="sm"
              className="rounded-r-none border-0"
              onClick={() => handleViewModeChange("cards")}
            >
              <CalendarDays className="size-3.5 mr-1" />
              Карточки
            </Button>
            <Button
              variant={!showCards ? "default" : "ghost"}
              size="sm"
              className="rounded-l-none border-0"
              onClick={() => handleViewModeChange("semester")}
            >
              <LayoutGrid className="size-3.5 mr-1" />
              Семестр
            </Button>
          </div>

          <NativeSelect
            value=""
            onValueChange={(v) => {
              if (v) {
                const [format, layout] = v.split(":") as ["pdf" | "xlsx", "grid" | "daycards"]
                handleExport(format, layout)
              }
            }}
            className="w-auto"
          >
            <option value="" disabled>
              Экспорт
            </option>
            <option value="pdf:grid">PDF — Сетка</option>
            <option value="pdf:daycards">PDF — По дням</option>
            <option value="xlsx:grid">Excel — Сетка</option>
            <option value="xlsx:daycards">Excel — По дням</option>
          </NativeSelect>
          {canManage && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setImportDialogOpen(true)}
              >
                <Upload className="size-3.5 mr-1" />
                Импорт
              </Button>
              <Button size="sm" onClick={handleAdd}>
                Добавить
              </Button>
            </>
          )}
        </div>
      </div>

      <div className={`transition-all duration-200 ${showCards ? "opacity-100 max-h-[500px]" : "opacity-0 max-h-0 overflow-hidden pointer-events-none"}`}>
        <DayTabs selectedDay={selectedDay} onChange={setSelectedDay} />
      </div>

      {error && <ErrorBanner message={error} />}

      {showCards && initialLoading ? (
        <div className="flex min-h-[60vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : !showCards && semesterLoading ? (
        <div className="flex min-h-[60vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : (
        <div className="relative">
          {fetching && (
            <div className="absolute inset-0 z-10 flex items-center justify-center bg-background/60">
              <LoadingSpinner size="lg" />
            </div>
          )}
          {showCards ? (
            <ScheduleTable
              entries={displayEntries}
              selectedDay={selectedDay}
              onEntryClick={canManage ? handleEdit : undefined}
              onDeleteClick={
                canManage ? (id) => setDeleteConfirmId(id) : undefined
              }
            />
          ) : (
            <SemesterView
              entries={displayEntries}
              selectedWeek={selectedWeek}
              onCellClick={handleSemesterCellClick}
            />
          )}
        </div>
      )}

      <ScheduleEntryDialog
        open={entryDialogOpen}
        onOpenChange={setEntryDialogOpen}
        onSaved={showCards ? loadSchedule : loadAllEntries}
        entry={editingEntry}
        groups={groups}
        teachers={teachers}
      />

      <ScheduleImportDialog
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        onImported={showCards ? loadSchedule : loadAllEntries}
      />
    </div>
  )
}
