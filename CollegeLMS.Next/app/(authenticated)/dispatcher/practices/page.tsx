"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { toast } from "sonner"
import {
  AlertTriangle,
  Briefcase,
  CalendarDays,
  FileSpreadsheet,
  Inbox,
  Minus,
  Pencil,
  Plus,
  SearchX,
  Trash2,
  Upload,
} from "lucide-react"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
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
import Pagination from "@/components/ui/pagination"
import ErrorBanner from "@/components/ErrorBanner"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import api from "@/lib/api"
import { cn, extractErrorMessage } from "@/lib/utils"
import { formatDate, formatDateRange, toDateInput } from "@/lib/reference"
import {
  fetchScheduleMeta,
  normalizeDateOnly,
  parseIsoDate,
  toIsoDate,
} from "@/api/schedule"
import {
  fetchNonWorkingDays,
  type NonWorkingDay,
} from "@/api/nonWorkingDays"
import type { GroupResponse, TeacherResponse, Result } from "@/types"
import {
  confirmPracticeImport,
  createPractice,
  deletePractice,
  fetchPractices,
  practiceDays,
  practiceName,
  practiceTeacherNames,
  practiceTeachers,
  previewPracticeImport,
  updatePractice,
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  type Practice,
  type PracticeDay,
  type PracticeImportPreview,
  type PracticeImportRow,
  type PracticeKind,
} from "@/api/practices"

const PAGE_SIZE = 20
const DEFAULT_PAIR_COUNT = 6
const MIN_PAIR_COUNT = 1
const MAX_PAIR_COUNT = 8
const MAX_RANGE_DAYS = 400

const WEEKDAY_SHORT = ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"]

function normalizeImportMessage(message: string, row: number): string {
  return /^Строка\s+\d+/i.test(message) ? message : `Строка ${row}: ${message}`
}

const IMPORT_FIELD_LABELS: Record<keyof PracticeImportRow, string> = {
  row: "Строка",
  kind: "Вид",
  name: "Название",
  groupName: "Группа",
  dateFrom: "Дата начала",
  dateTo: "Дата окончания",
  teacherName: "Преподаватель",
  note: "Примечание",
}

const IMPORT_EDITABLE_FIELDS: (keyof PracticeImportRow)[] = [
  "kind",
  "name",
  "groupName",
  "dateFrom",
  "dateTo",
  "teacherName",
  "note",
]

/** Черновик дня УП: включён ли день и сколько в нём пар. */
interface DayDraft {
  date: string
  included: boolean
  pairCount: number
}

function clampPairs(value: number): number {
  if (!Number.isFinite(value)) return DEFAULT_PAIR_COUNT
  return Math.min(MAX_PAIR_COUNT, Math.max(MIN_PAIR_COUNT, Math.round(value)))
}

function addDaysIso(iso: string, days: number): string {
  const date = parseIsoDate(iso)
  if (Number.isNaN(date.getTime())) return iso
  date.setDate(date.getDate() + days)
  return toIsoDate(date)
}

function isNonWorkingDate(iso: string, ranges: NonWorkingDay[]): boolean {
  return ranges.some((range) => {
    const from = toDateInput(range.dateFrom)
    const to = toDateInput(range.dateTo)
    return iso >= from && iso <= to
  })
}

/** Учебные дни периода: Пн–Сб без нерабочих и внесеместровых дат. */
function buildStudyDays(
  from: string,
  to: string,
  ranges: NonWorkingDay[],
  semester: { start: string; end: string } | null,
): string[] {
  const start = toDateInput(from)
  const end = toDateInput(to)
  if (!start || !end || start > end) return []
  const result: string[] = []
  let cursor = start
  let guard = 0
  while (cursor <= end && guard < MAX_RANGE_DAYS) {
    guard += 1
    const date = parseIsoDate(cursor)
    const inSemester =
      semester === null || (cursor >= semester.start && cursor <= semester.end)
    if (
      date.getDay() !== 0 &&
      inSemester &&
      !isNonWorkingDate(cursor, ranges)
    ) {
      result.push(cursor)
    }
    cursor = addDaysIso(cursor, 1)
  }
  return result
}

function formatDayLabel(iso: string): string {
  const date = parseIsoDate(iso)
  if (Number.isNaN(date.getTime())) return iso
  return `${WEEKDAY_SHORT[date.getDay()]} · ${formatDate(iso)}`
}

export default function DispatcherPracticesPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const daySeedRef = useRef<PracticeDay[]>([])

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])

  const [items, setItems] = useState<Practice[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)

  const [filterGroup, setFilterGroup] = useState("all")
  const [filterTeacher, setFilterTeacher] = useState("all")
  const [filterKind, setFilterKind] = useState("all")
  const [filterFrom, setFilterFrom] = useState("")
  const [filterTo, setFilterTo] = useState("")

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formKind, setFormKind] = useState<PracticeKind>("Up")
  const [formName, setFormName] = useState("")
  const [formGroup, setFormGroup] = useState("")
  const [formTeacherIds, setFormTeacherIds] = useState<string[]>([])
  const [formDateFrom, setFormDateFrom] = useState("")
  const [formDateTo, setFormDateTo] = useState("")
  const [formNote, setFormNote] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [dayDrafts, setDayDrafts] = useState<DayDraft[]>([])
  const [nonWorking, setNonWorking] = useState<NonWorkingDay[]>([])
  const [semester, setSemester] = useState<{
    start: string
    end: string
  } | null>(null)
  const [daysLoading, setDaysLoading] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<Practice | null>(null)
  const [deleting, setDeleting] = useState(false)

  const [importOpen, setImportOpen] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<PracticeImportPreview | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)

  const load = useCallback(
    async (targetPage: number) => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchPractices({
          groupId: filterGroup === "all" ? undefined : filterGroup,
          teacherId: filterTeacher === "all" ? undefined : filterTeacher,
          kind: filterKind === "all" ? undefined : (filterKind as PracticeKind),
          from: filterFrom || undefined,
          to: filterTo || undefined,
          page: targetPage,
          pageSize: PAGE_SIZE,
        })
        setItems(result.items)
        setTotalPages(Math.max(result.totalPages, 1))
      } catch (err) {
        setError(extractErrorMessage(err) ?? "Не удалось загрузить практики")
      } finally {
        setLoading(false)
      }
    },
    [filterGroup, filterTeacher, filterKind, filterFrom, filterTo],
  )

  useEffect(() => {
    void load(page)
  }, [page, load])

  useEffect(() => {
    void (async () => {
      try {
        const groupRes = await api.get<Result<GroupResponse[]>>("/api/groups")
        if (groupRes.data.isSuccess && groupRes.data.data)
          setGroups(groupRes.data.data)
        const teacherRes =
          await api.get<Result<TeacherResponse[]>>("/api/teachers")
        if (teacherRes.data.isSuccess && teacherRes.data.data)
          setTeachers(teacherRes.data.data)
      } catch {
        // Справочники нужны только для формы — тихо игнорируем сбой.
      }
    })()
  }, [])

  useEffect(() => {
    void (async () => {
      try {
        const res = await fetchScheduleMeta()
        const start = normalizeDateOnly(res.data?.semesterStart)
        if (res.isSuccess && start && res.data) {
          setSemester({
            start,
            end: addDaysIso(start, Math.max(res.data.totalWeeks, 1) * 7 - 1),
          })
        }
      } catch {
        // Метаданные семестра необязательны: без них дни строятся без фильтра.
      }
    })()
  }, [])

  useEffect(() => {
    if (!dialogOpen || !formDateFrom || !formDateTo || formDateFrom > formDateTo) {
      setNonWorking([])
      return
    }
    let cancelled = false
    setDaysLoading(true)
    void (async () => {
      try {
        const result = await fetchNonWorkingDays({
          from: formDateFrom,
          to: formDateTo,
          pageSize: 200,
        })
        if (!cancelled) setNonWorking(result.items)
      } catch {
        if (!cancelled) setNonWorking([])
      } finally {
        if (!cancelled) setDaysLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [dialogOpen, formDateFrom, formDateTo])

  useEffect(() => {
    if (!dialogOpen || formKind !== "Up") {
      setDayDrafts([])
      return
    }
    const candidates = buildStudyDays(
      formDateFrom,
      formDateTo,
      nonWorking,
      semester,
    )
    const seed = new Map(
      daySeedRef.current.map((day) => [toDateInput(day.date), day.pairCount]),
    )
    const drafts: DayDraft[] = candidates.map((date) => ({
      date,
      included: true,
      pairCount: clampPairs(seed.get(date) ?? DEFAULT_PAIR_COUNT),
    }))
    for (const [date, pairCount] of seed) {
      if (!drafts.some((draft) => draft.date === date)) {
        drafts.push({ date, included: true, pairCount: clampPairs(pairCount) })
      }
    }
    drafts.sort((a, b) => a.date.localeCompare(b.date))
    setDayDrafts(drafts)
  }, [dialogOpen, formKind, formDateFrom, formDateTo, nonWorking, semester])

  const resetPageAnd = (setter: (value: string) => void) => (value: string) => {
    setter(value)
    setPage(1)
  }

  const resetFilters = () => {
    setFilterGroup("all")
    setFilterTeacher("all")
    setFilterKind("all")
    setFilterFrom("")
    setFilterTo("")
    setPage(1)
  }

  const hasFilters =
    filterGroup !== "all" ||
    filterTeacher !== "all" ||
    filterKind !== "all" ||
    filterFrom !== "" ||
    filterTo !== ""

  const openCreate = () => {
    setEditingId(null)
    setFormKind("Up")
    setFormName("")
    setFormGroup(groups[0]?.id ?? "")
    setFormTeacherIds([])
    setFormDateFrom("")
    setFormDateTo("")
    setFormNote("")
    daySeedRef.current = []
    setFormError(null)
    setDialogOpen(true)
  }

  const openEdit = (practice: Practice) => {
    setEditingId(practice.id)
    setFormKind(practice.kind)
    setFormName(practice.name ?? "")
    setFormGroup(practice.groupId)
    const teacherIds = practiceTeachers(practice)
      .map((teacher) => teacher.id)
      .filter((id) => id.length > 0)
    setFormTeacherIds(
      teacherIds.length > 0 ? teacherIds : (practice.teacherIds ?? []),
    )
    setFormDateFrom(toDateInput(practice.dateFrom))
    setFormDateTo(toDateInput(practice.dateTo))
    setFormNote(practice.note ?? "")
    daySeedRef.current = practiceDays(practice)
    setFormError(null)
    setDialogOpen(true)
  }

  const toggleTeacher = (id: string, checked: boolean) => {
    setFormTeacherIds((prev) =>
      checked ? [...prev, id] : prev.filter((value) => value !== id),
    )
  }

  const updateDay = (index: number, patch: Partial<DayDraft>) => {
    setDayDrafts((prev) =>
      prev.map((day, i) => (i === index ? { ...day, ...patch } : day)),
    )
  }

  const fillAllDays = () => {
    setDayDrafts((prev) =>
      prev.map((day) => ({
        ...day,
        included: true,
        pairCount: DEFAULT_PAIR_COUNT,
      })),
    )
  }

  const clearAllDays = () => {
    setDayDrafts((prev) => prev.map((day) => ({ ...day, included: false })))
  }

  const includedDays = dayDrafts.filter((day) => day.included)
  const totalDraftPairs = includedDays.reduce(
    (sum, day) => sum + day.pairCount,
    0,
  )

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const name = formName.trim()
    if (!name) {
      setFormError("Укажите название практики (например, «УП 01»).")
      return
    }
    if (!formGroup) {
      setFormError("Выберите группу.")
      return
    }
    if (formTeacherIds.length === 0) {
      setFormError("Выберите хотя бы одного преподавателя.")
      return
    }
    if (!formDateFrom || !formDateTo) {
      setFormError("Укажите даты начала и окончания практики.")
      return
    }
    if (formDateFrom > formDateTo) {
      setFormError("Дата начала не может быть позже даты окончания.")
      return
    }
    if (formKind === "Up" && includedDays.length === 0) {
      setFormError("Для УП выберите хотя бы один день практики с числом пар.")
      return
    }

    setSubmitting(true)
    setFormError(null)
    const body = {
      kind: formKind,
      name,
      groupId: formGroup,
      teacherIds: formTeacherIds,
      dateFrom: formDateFrom,
      dateTo: formDateTo,
      days:
        formKind === "Up"
          ? includedDays.map((day) => ({
              date: day.date,
              pairCount: clampPairs(day.pairCount),
            }))
          : undefined,
      note: formNote.trim() || null,
    }
    try {
      if (editingId) {
        await updatePractice(editingId, body)
        toast.success("Практика обновлена")
      } else {
        await createPractice(body)
        toast.success("Практика создана")
      }
      setDialogOpen(false)
      if (editingId) {
        await load(page)
      } else if (page !== 1) {
        setPage(1)
      } else {
        await load(1)
      }
    } catch (err) {
      const message =
        extractErrorMessage(err) ?? "Не удалось сохранить практику"
      setFormError(message)
      toast.error(message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deletePractice(deleteTarget.id)
      toast.success("Практика удалена")
      setDeleteTarget(null)
      if (items.length === 1 && page > 1) {
        setPage(page - 1)
      } else {
        await load(page)
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить практику")
    } finally {
      setDeleting(false)
    }
  }

  const closeImport = () => {
    setImportOpen(false)
    setImportFile(null)
    setPreview(null)
    if (fileInputRef.current) fileInputRef.current.value = ""
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    if (!file.name.toLowerCase().endsWith(".xlsx")) {
      toast.error("Поддерживается только формат XLSX")
      return
    }
    if (file.size > 10 * 1024 * 1024) {
      toast.error("Файл слишком большой. Максимум 10 МБ")
      return
    }
    setImportFile(file)
    setPreview(null)
  }

  const runPreview = async () => {
    if (!importFile) return
    setPreviewing(true)
    try {
      const result = await previewPracticeImport(importFile)
      setPreview(result)
      if (result.errors.length > 0) {
        toast.error(`Найдены ошибки: ${result.errors.length}`, {
          description: "Исправьте строки в таблице и повторите подтверждение.",
        })
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось прочитать файл")
    } finally {
      setPreviewing(false)
    }
  }

  const updatePreviewRow = (
    index: number,
    field: keyof PracticeImportRow,
    value: string,
  ) => {
    setPreview((prev) => {
      if (!prev) return prev
      const rows = prev.rows.map((row, i) =>
        i === index ? { ...row, [field]: value } : row,
      )
      return { ...prev, rows }
    })
  }

  const runConfirm = async () => {
    if (!preview) return
    if (preview.errors.length > 0) {
      toast.error("Сначала исправьте ошибки в строках")
      return
    }
    setConfirming(true)
    try {
      const result = await confirmPracticeImport(preview.rows)
      toast.success(`Импортировано практик: ${result.imported}`)
      closeImport()
      await load(1)
      setPage(1)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось импортировать практики")
    } finally {
      setConfirming(false)
    }
  }

  const errorRows = useMemo(
    () => new Set((preview?.errors ?? []).map((e) => e.row)),
    [preview],
  )

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold">Практики</h1>
          <p className="text-sm text-muted-foreground">
            Периоды УП и ПП: в это время обычные пары группы заменяются
            карточкой практики. Для УП задаются учебные дни и число пар.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            onClick={() => setImportOpen(true)}
            className="min-h-11 sm:min-h-9"
          >
            <Upload className="size-4" aria-hidden="true" />
            Импорт XLSX
          </Button>
          <Button onClick={openCreate} className="min-h-11 sm:min-h-9">
            <Plus className="size-4" aria-hidden="true" />
            Добавить практику
          </Button>
        </div>
      </header>

      <div className="flex flex-wrap items-end gap-3 rounded-xl border bg-card p-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pr-filter-group">Группа</Label>
          <NativeSelect
            value={filterGroup}
            onValueChange={resetPageAnd(setFilterGroup)}
            className="w-48"
          >
            <NativeSelectItem value="all">Все группы</NativeSelectItem>
            {groups.map((g) => (
              <NativeSelectItem key={g.id} value={g.id}>
                {g.name}
              </NativeSelectItem>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pr-filter-teacher">Преподаватель</Label>
          <NativeSelect
            value={filterTeacher}
            onValueChange={resetPageAnd(setFilterTeacher)}
            className="w-56"
          >
            <NativeSelectItem value="all">Все преподаватели</NativeSelectItem>
            {teachers.map((t) => (
              <NativeSelectItem key={t.id} value={t.id}>
                {t.fullName}
              </NativeSelectItem>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pr-filter-kind">Вид</Label>
          <NativeSelect
            value={filterKind}
            onValueChange={resetPageAnd(setFilterKind)}
            className="w-44"
          >
            <NativeSelectItem value="all">Все виды</NativeSelectItem>
            <NativeSelectItem value="Up">УП — учебная</NativeSelectItem>
            <NativeSelectItem value="Pp">ПП — производственная</NativeSelectItem>
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pr-filter-from">Период с</Label>
          <Input
            id="pr-filter-from"
            type="date"
            value={filterFrom}
            onChange={(e) => {
              setFilterFrom(e.target.value)
              setPage(1)
            }}
            className="h-11 w-40 bg-card sm:h-9"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pr-filter-to">по</Label>
          <Input
            id="pr-filter-to"
            type="date"
            value={filterTo}
            onChange={(e) => {
              setFilterTo(e.target.value)
              setPage(1)
            }}
            className="h-11 w-40 bg-card sm:h-9"
          />
        </div>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={resetFilters}
            className="min-h-11 sm:min-h-9"
          >
            <SearchX className="size-4" aria-hidden="true" />
            Сбросить
          </Button>
        )}
      </div>

      {error && (
        <>
          <ErrorBanner message={error} />
          <Button
            variant="outline"
            onClick={() => void load(page)}
            className="w-fit min-h-11 sm:min-h-9"
          >
            Повторить загрузку
          </Button>
        </>
      )}

      {!error && (
        <Card className="gap-0 py-0">
          <CardHeader className="border-b py-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <Briefcase className="size-4" aria-hidden="true" />
              Список практик
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {loading ? (
              <div role="status" aria-label="Загрузка практик">
                <LoadingSpinner size="lg" className="py-20" />
              </div>
            ) : items.length === 0 ? (
              <div className="flex flex-col items-center gap-3 px-6 py-16 text-center text-muted-foreground">
                <Inbox className="size-12 opacity-40" aria-hidden="true" />
                <p className="text-base font-medium text-fg">
                  {hasFilters ? "Практик не найдено" : "Практик пока нет"}
                </p>
                <EmptyState
                  message={
                    hasFilters
                      ? "Измените фильтры или сбросьте их."
                      : "Добавьте практику вручную или импортируйте список из XLSX."
                  }
                />
                {!hasFilters && (
                  <div className="flex flex-wrap justify-center gap-2">
                    <Button variant="outline" onClick={() => setImportOpen(true)}>
                      <Upload className="size-4" aria-hidden="true" />
                      Импорт XLSX
                    </Button>
                    <Button onClick={openCreate}>
                      <Plus className="size-4" aria-hidden="true" />
                      Добавить практику
                    </Button>
                  </div>
                )}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[980px] text-sm">
                  <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-fg">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium">Вид</th>
                      <th className="px-4 py-3 text-left font-medium">Название</th>
                      <th className="px-4 py-3 text-left font-medium">Группа</th>
                      <th className="px-4 py-3 text-left font-medium">
                        Преподаватели
                      </th>
                      <th className="px-4 py-3 text-left font-medium">Период</th>
                      <th className="px-4 py-3 text-left font-medium">
                        Примечание
                      </th>
                      <th className="px-4 py-3 text-right font-medium">
                        Действия
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((practice) => {
                      const days = practiceDays(practice)
                      const totalPairs = days.reduce(
                        (sum, day) => sum + day.pairCount,
                        0,
                      )
                      const teacherList = practiceTeachers(practice)
                      return (
                        <tr key={practice.id} className="border-b last:border-0">
                          <td className="px-4 py-3">
                            <Badge variant="outline">
                              {PRACTICE_KIND_SHORT[practice.kind] ??
                                practice.kind}
                            </Badge>
                          </td>
                          <td className="px-4 py-3 font-medium">
                            {practiceName(practice)}
                            {practice.kind === "Up" && days.length > 0 && (
                              <span className="block text-xs font-normal text-muted-fg">
                                {days.length} дн. · {totalPairs} пар
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3">{practice.groupName}</td>
                          <td className="px-4 py-3">
                            {teacherList.length > 0 ? (
                              <div className="flex max-w-[240px] flex-wrap gap-1">
                                {teacherList.map((teacher, index) => (
                                  <Badge
                                    key={`${teacher.id}-${index}`}
                                    variant="secondary"
                                  >
                                    {teacher.name}
                                  </Badge>
                                ))}
                              </div>
                            ) : (
                              <span className="text-muted-fg">—</span>
                            )}
                          </td>
                          <td className="px-4 py-3 font-mono text-xs tabular-nums whitespace-nowrap">
                            {formatDateRange(practice.dateFrom, practice.dateTo)}
                          </td>
                          <td className="max-w-[220px] truncate px-4 py-3 text-muted-fg">
                            {practice.note ?? "—"}
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex justify-end gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="size-11"
                                onClick={() => openEdit(practice)}
                                aria-label={`Редактировать практику «${practiceName(practice)}»`}
                              >
                                <Pencil className="size-4" aria-hidden="true" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                                onClick={() => setDeleteTarget(practice)}
                                aria-label={`Удалить практику «${practiceName(practice)}»`}
                              >
                                <Trash2 className="size-4" aria-hidden="true" />
                              </Button>
                            </div>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {!loading && !error && items.length > 0 && (
        <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
      )}

      {/* Форма создания/правки практики */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {editingId ? "Изменить практику" : "Добавить практику"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {formError && <ErrorBanner message={formError} />}
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="pr-kind">Вид практики *</Label>
                <NativeSelect
                  value={formKind}
                  onValueChange={(v) => setFormKind(v as PracticeKind)}
                  className="w-full"
                >
                  <NativeSelectItem value="Up">УП — учебная</NativeSelectItem>
                  <NativeSelectItem value="Pp">
                    ПП — производственная
                  </NativeSelectItem>
                </NativeSelect>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="pr-group">Группа *</Label>
                <NativeSelect
                  value={formGroup}
                  onValueChange={setFormGroup}
                  className="w-full"
                >
                  <NativeSelectItem value="">Выберите группу</NativeSelectItem>
                  {groups.map((g) => (
                    <NativeSelectItem key={g.id} value={g.id}>
                      {g.name}
                    </NativeSelectItem>
                  ))}
                </NativeSelect>
              </div>
              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label htmlFor="pr-name">Название *</Label>
                <Input
                  id="pr-name"
                  required
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  maxLength={100}
                  placeholder="Например, «УП 01» или «ПП 09»"
                  className="h-11 bg-card sm:h-9"
                />
              </div>

              <div
                role="group"
                aria-labelledby="pr-teachers-label"
                className="flex flex-col gap-1.5 sm:col-span-2"
              >
                <span id="pr-teachers-label" className="text-sm font-medium">
                  Преподаватели *
                </span>
                {teachers.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    Справочник преподавателей пуст.
                  </p>
                ) : (
                  <div className="grid max-h-44 gap-1.5 overflow-y-auto rounded-md border bg-card p-2 sm:grid-cols-2">
                    {teachers.map((teacher) => {
                      const checked = formTeacherIds.includes(teacher.id)
                      return (
                        <label
                          key={teacher.id}
                          className={cn(
                            "flex min-h-11 cursor-pointer items-center gap-2 rounded-md border px-3 text-sm transition-colors",
                            checked
                              ? "border-primary bg-primary/[0.06]"
                              : "border-border hover:bg-muted",
                          )}
                        >
                          <input
                            type="checkbox"
                            checked={checked}
                            onChange={(e) =>
                              toggleTeacher(teacher.id, e.target.checked)
                            }
                            className="size-4 accent-primary"
                          />
                          {teacher.fullName}
                        </label>
                      )
                    })}
                  </div>
                )}
                <p className="text-xs text-muted-foreground">
                  Можно выбрать несколько преподавателей.
                </p>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="pr-date-from">Дата начала *</Label>
                <Input
                  id="pr-date-from"
                  type="date"
                  required
                  value={formDateFrom}
                  onChange={(e) => setFormDateFrom(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="pr-date-to">Дата окончания *</Label>
                <Input
                  id="pr-date-to"
                  type="date"
                  required
                  value={formDateTo}
                  onChange={(e) => setFormDateTo(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>

              {formKind === "Up" && (
                <div
                  role="group"
                  aria-labelledby="pr-days-label"
                  className="flex flex-col gap-2 rounded-lg border bg-muted/30 p-3 sm:col-span-2"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div
                      id="pr-days-label"
                      className="flex items-center gap-1.5 text-sm font-medium"
                    >
                      <CalendarDays className="size-4" aria-hidden="true" />
                      Дни практики *
                      {daysLoading && (
                        <LoadingSpinner size="sm" className="ml-1" />
                      )}
                    </div>
                    <div className="flex gap-1.5">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-9"
                        onClick={fillAllDays}
                        disabled={dayDrafts.length === 0}
                      >
                        Заполнить все
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="h-9"
                        onClick={clearAllDays}
                        disabled={dayDrafts.length === 0}
                      >
                        Очистить
                      </Button>
                    </div>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Учебные дни периода (Пн–Сб без нерабочих). По умолчанию 6
                    пар, допустимо 1–8.
                  </p>
                  {dayDrafts.length === 0 ? (
                    <p className="rounded-md border border-dashed bg-card px-3 py-4 text-center text-sm text-muted-foreground">
                      {!formDateFrom || !formDateTo
                        ? "Укажите период, чтобы раскрыть учебные дни."
                        : "В выбранном периоде нет учебных дней."}
                    </p>
                  ) : (
                    <div className="max-h-64 overflow-y-auto rounded-md border bg-card">
                      <ul className="divide-y">
                        {dayDrafts.map((day, index) => {
                          const label = formatDayLabel(day.date)
                          return (
                            <li
                              key={day.date}
                              className="flex items-center gap-2 px-3 py-2"
                            >
                              <label className="flex flex-1 cursor-pointer items-center gap-2 text-sm">
                                <input
                                  type="checkbox"
                                  checked={day.included}
                                  onChange={(e) =>
                                    updateDay(index, {
                                      included: e.target.checked,
                                    })
                                  }
                                  className="size-4 accent-primary"
                                />
                                <span
                                  className={cn(
                                    !day.included &&
                                      "text-muted-foreground line-through",
                                  )}
                                >
                                  {label}
                                </span>
                              </label>
                              <div className="flex items-center gap-1">
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="icon"
                                  className="size-9"
                                  aria-label={`Уменьшить число пар: ${label}`}
                                  disabled={!day.included || day.pairCount <= 1}
                                  onClick={() =>
                                    updateDay(index, {
                                      pairCount: Math.max(
                                        1,
                                        day.pairCount - 1,
                                      ),
                                    })
                                  }
                                >
                                  <Minus className="size-4" aria-hidden="true" />
                                </Button>
                                <Input
                                  type="number"
                                  min={MIN_PAIR_COUNT}
                                  max={MAX_PAIR_COUNT}
                                  step={1}
                                  value={day.pairCount}
                                  disabled={!day.included}
                                  onChange={(e) =>
                                    updateDay(index, {
                                      pairCount: clampPairs(
                                        Number(e.target.value),
                                      ),
                                    })
                                  }
                                  aria-label={`Число пар: ${label}`}
                                  className="h-9 w-16 text-center"
                                />
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="icon"
                                  className="size-9"
                                  aria-label={`Увеличить число пар: ${label}`}
                                  disabled={!day.included || day.pairCount >= 8}
                                  onClick={() =>
                                    updateDay(index, {
                                      pairCount: Math.min(
                                        8,
                                        day.pairCount + 1,
                                      ),
                                    })
                                  }
                                >
                                  <Plus className="size-4" aria-hidden="true" />
                                </Button>
                              </div>
                            </li>
                          )
                        })}
                      </ul>
                    </div>
                  )}
                  {includedDays.length > 0 && (
                    <p className="text-xs text-muted-foreground">
                      Выбрано дней: {includedDays.length} · всего пар:{" "}
                      {totalDraftPairs}
                    </p>
                  )}
                </div>
              )}

              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label htmlFor="pr-note">Примечание</Label>
                <Textarea
                  id="pr-note"
                  value={formNote}
                  onChange={(e) => setFormNote(e.target.value)}
                  maxLength={500}
                  rows={3}
                  placeholder="Дополнительная информация"
                />
              </div>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setDialogOpen(false)}
              >
                Отмена
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting ? "Сохранение…" : "Сохранить"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Импорт XLSX */}
      <Dialog
        open={importOpen}
        onOpenChange={(open) => {
          if (!open) closeImport()
          else setImportOpen(true)
        }}
      >
        <DialogContent className="sm:max-w-5xl">
          <DialogHeader>
            <DialogTitle>Импорт практик из XLSX</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              Шапка в строке 1: «Вид | Название | Группа | Дата начала | Дата
              окончания | Преподаватель | Примечание». Даты — в формате
              ДД.ММ.ГГГГ, вид — УП или ПП, несколько преподавателей — через «;».
            </p>

            <div className="flex flex-wrap items-center gap-2">
              <input
                ref={fileInputRef}
                type="file"
                accept=".xlsx"
                className="hidden"
                onChange={handleFileChange}
                aria-hidden="true"
                tabIndex={-1}
              />
              <Button
                type="button"
                variant="outline"
                onClick={() => fileInputRef.current?.click()}
                className="min-h-11 sm:min-h-9"
              >
                <FileSpreadsheet className="size-4" aria-hidden="true" />
                {importFile ? "Выбрать другой файл" : "Выбрать файл XLSX"}
              </Button>
              {importFile && (
                <span className="text-sm text-muted-foreground">
                  {importFile.name} · {(importFile.size / 1024).toFixed(1)} КБ
                </span>
              )}
              <Button
                type="button"
                onClick={() => void runPreview()}
                disabled={!importFile || previewing}
                className="min-h-11 sm:min-h-9"
              >
                {previewing ? "Чтение…" : "Проверить файл"}
              </Button>
            </div>

            {preview && (
              <>
                <div className="flex flex-wrap items-center gap-3 text-sm">
                  <span>
                    Строк в файле: <strong>{preview.totalRows}</strong>
                  </span>
                  {preview.errors.length > 0 ? (
                    <span className="flex items-center gap-1 text-destructive">
                      <AlertTriangle className="size-4" aria-hidden="true" />
                      Ошибок: {preview.errors.length}
                    </span>
                  ) : (
                    <span className="text-success">
                      Ошибок нет — можно импортировать.
                    </span>
                  )}
                </div>

                {preview.errors.length > 0 && (
                  <div className="max-h-40 overflow-y-auto rounded-md border border-destructive/40 bg-destructive/5 p-3 text-xs">
                    <ul className="flex flex-col gap-1">
                      {preview.errors.map((err, i) => (
                        <li key={i} className="text-destructive">
                          {normalizeImportMessage(err.message, err.row)}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {preview.rows.length > 0 && (
                  <div className="max-h-80 overflow-auto rounded-md border">
                    <table className="w-full min-w-[1000px] text-xs">
                      <thead className="sticky top-0 bg-muted text-left">
                        <tr>
                          <th className="px-2 py-2 font-medium">Строка</th>
                          <th className="px-2 py-2 font-medium">Вид</th>
                          <th className="px-2 py-2 font-medium">Название</th>
                          <th className="px-2 py-2 font-medium">Группа</th>
                          <th className="px-2 py-2 font-medium">Дата начала</th>
                          <th className="px-2 py-2 font-medium">
                            Дата окончания
                          </th>
                          <th className="px-2 py-2 font-medium">
                            Преподаватель
                          </th>
                          <th className="px-2 py-2 font-medium">Примечание</th>
                        </tr>
                      </thead>
                      <tbody>
                        {preview.rows.map((row, index) => (
                          <tr
                            key={`${row.row}-${index}`}
                            className={
                              errorRows.has(row.row)
                                ? "bg-destructive/5"
                                : undefined
                            }
                          >
                            <td className="px-2 py-1 font-mono tabular-nums text-muted-fg">
                              {row.row}
                            </td>
                            {IMPORT_EDITABLE_FIELDS.map((field) => (
                              <td key={field} className="px-2 py-1">
                                <Input
                                  value={(row[field] as string | null) ?? ""}
                                  onChange={(e) =>
                                    updatePreviewRow(index, field, e.target.value)
                                  }
                                  aria-label={`Строка ${row.row}, поле «${IMPORT_FIELD_LABELS[field]}»`}
                                  className="h-9 min-w-28 bg-card"
                                />
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <p className="text-xs text-muted-foreground">
                  Можно исправить значения прямо в таблице, затем подтвердить
                  импорт. Импорт выполняется одной транзакцией. Дни УП задаются
                  вручную после импорта (по умолчанию 6 пар).
                </p>
              </>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={closeImport}>
              Отмена
            </Button>
            <Button
              type="button"
              onClick={() => void runConfirm()}
              disabled={
                !preview ||
                preview.errors.length > 0 ||
                preview.rows.length === 0 ||
                confirming
              }
            >
              {confirming ? "Импорт…" : "Импортировать"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AlertDialog
        open={deleteTarget !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить практику?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `${PRACTICE_KIND_LABELS[deleteTarget.kind]} · ${practiceName(deleteTarget)} · ${deleteTarget.groupName} (${formatDateRange(deleteTarget.dateFrom, deleteTarget.dateTo)}). Действие необратимо.`
                : ""}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting}>Отмена</AlertDialogCancel>
            <AlertDialogAction
              disabled={deleting}
              onClick={(e) => {
                e.preventDefault()
                void handleDelete()
              }}
              className="bg-destructive text-white hover:bg-destructive/90"
            >
              {deleting ? "Удаление…" : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
