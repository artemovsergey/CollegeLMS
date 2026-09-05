"use client"

import { useEffect, useState, useCallback, useRef } from "react"
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
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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
  Calendar,
} from "lucide-react"
import { toast } from "sonner"

const SEMESTER_START = new Date(2026, 0, 12)

function getCurrentWeek(): number {
  const now = new Date()
  const diff = Math.floor(
    (now.getTime() - SEMESTER_START.getTime()) / (7 * 24 * 60 * 60 * 1000),
  )
  return Math.max(1, diff + 1)
}

export default function SchedulePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [allEntries, setAllEntries] = useState<ScheduleResponse[]>([])
  const [initialLoading, setInitialLoading] = useState(true)
  const [fetching, setFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])

  const [selectedGroupId, setSelectedGroupId] = useState("")
  const [selectedTeacherId, setSelectedTeacherId] = useState("")
  const [selectedWeek, setSelectedWeek] = useState(getCurrentWeek())
  const [selectedDay, setSelectedDay] = useState<number | null>(
    (() => {
      const day = new Date().getDay()
      return day >= 1 && day <= 5 ? day : 1
    })(),
  )
  const [viewMode, setViewMode] = useState<"cards" | "semester">("cards")

  const [entryDialogOpen, setEntryDialogOpen] = useState(false)
  const [editingEntry, setEditingEntry] = useState<ScheduleResponse | null>(
    null,
  )
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const canManage = user?.role ? CAN_MANAGE_ROLES.includes(user.role) : false

  const loadAllEntries = useCallback(async () => {
    setFetching(true)
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
      setFetching(false)
    }
  }, [selectedGroupId, selectedTeacherId])

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
    if (token && viewMode === "semester") {
      loadAllEntries()
    }
  }, [token, viewMode, loadAllEntries])

  const handleViewModeChange = (mode: "cards" | "semester") => {
    setViewMode(mode)
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
    setSelectedDay(
      (() => {
        const day = new Date().getDay()
        return day >= 1 && day <= 5 ? day : 1
      })(),
    )
  }

  const handleExport = async (format: "pdf" | "xlsx") => {
    try {
      const params: Record<string, string | number | undefined> = {}
      if (selectedGroupId) params.groupId = selectedGroupId
      if (selectedTeacherId) params.teacherId = selectedTeacherId
      if (viewMode === "cards" && selectedWeek) params.week = selectedWeek
      await exportSchedule(params, format)
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

  return (
    <div className="flex flex-col gap-4 p-6 mx-auto max-w-7xl">
      <div className="flex items-center gap-2">
        <CalendarDays className="size-5 text-primary" />
        <h2 className="text-xl font-semibold">Расписание</h2>
      </div>

      {viewMode === "cards" && (
        <WeekNavigation
          currentWeek={selectedWeek}
          onChange={setSelectedWeek}
          totalWeeks={52}
        />
      )}

      <div className="flex flex-wrap items-center gap-3 rounded-lg border bg-card p-4">
        <Filter className="size-4 text-muted-foreground shrink-0" />
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={selectedGroupId || "all"}
            onValueChange={(v) => setSelectedGroupId(v === "all" ? "" : v)}
          >
            <SelectTrigger className="w-44">
              <SelectValue placeholder="Все группы" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Все группы</SelectItem>
              {groups.map((g) => (
                <SelectItem key={g.id} value={g.id}>
                  {g.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select
            value={selectedTeacherId || "all"}
            onValueChange={(v) => setSelectedTeacherId(v === "all" ? "" : v)}
          >
            <SelectTrigger className="w-44">
              <SelectValue placeholder="Все преподаватели" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Все преподаватели</SelectItem>
              {teachers.map((t) => (
                <SelectItem key={t.id} value={t.id}>
                  {t.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {(selectedGroupId || selectedTeacherId) && (
            <Button variant="ghost" size="sm" onClick={handleClear}>
              <SearchX className="size-3.5" />
              Сбросить
            </Button>
          )}
        </div>

        <div className="ml-auto flex flex-wrap items-center gap-2">
          <div className="flex rounded-md border">
            <Button
              variant={viewMode === "cards" ? "default" : "ghost"}
              size="sm"
              className="rounded-r-none"
              onClick={() => handleViewModeChange("cards")}
            >
              <CalendarDays className="size-3.5 mr-1" />
              Карточки
            </Button>
            <Button
              variant={viewMode === "semester" ? "default" : "ghost"}
              size="sm"
              className="rounded-l-none"
              onClick={() => handleViewModeChange("semester")}
            >
              <LayoutGrid className="size-3.5 mr-1" />
              Семестр
            </Button>
          </div>

          <Button variant="outline" size="sm" onClick={() => handleExport("pdf")}>
            <FileDown className="size-3.5 mr-1" />
            PDF
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport("xlsx")}
          >
            <FileSpreadsheet className="size-3.5 mr-1" />
            Excel
          </Button>
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

      {viewMode === "cards" && (
        <DayTabs selectedDay={selectedDay} onChange={setSelectedDay} />
      )}

      {error && <ErrorBanner message={error} />}

      {initialLoading && viewMode === "cards" ? (
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
          {viewMode === "cards" ? (
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
        onSaved={viewMode === "cards" ? loadSchedule : loadAllEntries}
        entry={editingEntry}
        groups={groups}
        teachers={teachers}
      />

      <ScheduleImportDialog
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        onImported={viewMode === "cards" ? loadSchedule : loadAllEntries}
      />

      <AlertDialog
        open={!!deleteConfirmId}
        onOpenChange={(o) => !o && setDeleteConfirmId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить запись?</AlertDialogTitle>
            <AlertDialogDescription>
              Это действие нельзя отменить. Запись будет удалена из расписания.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
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
