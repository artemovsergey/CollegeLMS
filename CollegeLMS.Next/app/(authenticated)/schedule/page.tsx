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
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import ScheduleFilterBar from "@/components/ScheduleFilterBar"
import ScheduleTable from "@/components/ScheduleTable"
import ScheduleEntryDialog from "@/components/ScheduleEntryDialog"
import ScheduleImportDialog from "@/components/ScheduleImportDialog"
import { CAN_MANAGE_ROLES } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { CalendarDays, Trash2 } from "lucide-react"
import { toast } from "sonner"

export default function SchedulePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])

  const [selectedGroupId, setSelectedGroupId] = useState("")
  const [selectedTeacherId, setSelectedTeacherId] = useState("")
  const [selectedDayOfWeek, setSelectedDayOfWeek] = useState("")

  const [entryDialogOpen, setEntryDialogOpen] = useState(false)
  const [editingEntry, setEditingEntry] = useState<ScheduleResponse | null>(null)
  const [importDialogOpen, setImportDialogOpen] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const canManage = user?.role ? CAN_MANAGE_ROLES.includes(user.role) : false

  const getFilters = useCallback(() => {
    const filters: Record<string, string | undefined> = {}
    if (selectedGroupId && selectedGroupId !== "all")
      filters.groupId = selectedGroupId
    if (selectedTeacherId && selectedTeacherId !== "all")
      filters.teacherId = selectedTeacherId
    if (selectedDayOfWeek && selectedDayOfWeek !== "all")
      filters.dayOfWeek = selectedDayOfWeek
    return filters
  }, [selectedGroupId, selectedTeacherId, selectedDayOfWeek])

  const loadSchedule = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const body = await fetchSchedule({
        ...getFilters(),
        pageSize: 100,
      } as Record<string, string | undefined> & {
        pageSize?: number
      })
      if (body.isSuccess && body.data) {
        setEntries(body.data.items)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки расписания")
      }
    } catch {
      setError("Ошибка загрузки расписания")
    } finally {
      setLoading(false)
    }
  }, [getFilters])

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
    if (token) {
      loadSchedule()
    }
  }, [token, loadSchedule])

  const handleToday = () => {
    const today = new Date().getDay()
    setSelectedDayOfWeek(String(today === 0 ? 0 : today))
  }

  const handleClear = () => {
    setSelectedGroupId("")
    setSelectedTeacherId("")
    setSelectedDayOfWeek("")
  }

  const handleExport = async (format: "pdf" | "xlsx") => {
    try {
      await exportSchedule(getFilters(), format)
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
        loadSchedule()
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
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-7xl">
      <div className="flex items-center gap-2">
        <CalendarDays className="size-5 text-primary" />
        <h2 className="text-xl font-semibold">Расписание</h2>
      </div>

      <ScheduleFilterBar
        groups={groups}
        teachers={teachers}
        selectedGroupId={selectedGroupId || "all"}
        selectedTeacherId={selectedTeacherId || "all"}
        selectedDayOfWeek={selectedDayOfWeek || "all"}
        onGroupChange={(v) => setSelectedGroupId(v === "all" ? "" : v)}
        onTeacherChange={(v) => setSelectedTeacherId(v === "all" ? "" : v)}
        onDayChange={(v) => setSelectedDayOfWeek(v === "all" ? "" : v)}
        onToday={handleToday}
        onClear={handleClear}
        onExport={handleExport}
        onImport={() => setImportDialogOpen(true)}
        canManage={canManage}
        onAdd={handleAdd}
      />

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
      ) : (
        <ScheduleTable
          entries={entries}
          onEntryClick={canManage ? handleEdit : undefined}
          onDeleteClick={canManage ? (id) => setDeleteConfirmId(id) : undefined}
        />
      )}

      <ScheduleEntryDialog
        open={entryDialogOpen}
        onOpenChange={setEntryDialogOpen}
        onSaved={loadSchedule}
        entry={editingEntry}
        groups={groups}
        teachers={teachers}
      />

      <ScheduleImportDialog
        open={importDialogOpen}
        onOpenChange={setImportDialogOpen}
        onImported={loadSchedule}
      />

      <AlertDialog open={!!deleteConfirmId} onOpenChange={(o) => !o && setDeleteConfirmId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить запись?</AlertDialogTitle>
            <AlertDialogDescription>
              Это действие нельзя отменить. Запись будет удалена из расписания.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
