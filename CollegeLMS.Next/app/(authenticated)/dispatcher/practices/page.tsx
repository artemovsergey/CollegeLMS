"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { toast } from "sonner"
import {
  AlertTriangle,
  Briefcase,
  CalendarDays,
  Download,
  FileText,
  Inbox,
  Pencil,
  Plus,
  SearchX,
  Trash2,
  Upload,
  UploadCloud,
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
  type ScheduleValidationError,
} from "@/api/schedule"
import {
  fetchNonWorkingDays,
  type NonWorkingDay,
} from "@/api/nonWorkingDays"
import type { GroupResponse, TeacherResponse, Result } from "@/types"
import {
  confirmPracticeGraphImport,
  createPractice,
  deletePractice,
  exportPracticeGraph,
  fetchPractices,
  normalizePairNumbers,
  practiceDays,
  practiceName,
  practiceTeacherNames,
  practiceTeachers,
  practiceTotalPairs,
  previewPracticeGraphImport,
  updatePractice,
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  PRACTICE_PAIR_NUMBERS,
  type Practice,
  type PracticeDay,
  type PracticeGraphPreviewResponse,
  type PracticeKind,
} from "@/api/practices"

const PAGE_SIZE = 20
const MAX_RANGE_DAYS = 400
const MAX_IMPORT_SIZE = 10 * 1024 * 1024
const DEFAULT_PAIR_NUMBERS = [1, 2, 3, 4, 5, 6]

const WEEKDAY_SHORT = ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"]

/** Черновик дня УП: включён ли день и какие пары в нём отмечены. */
interface DayDraft {
  date: string
  included: boolean
  pairNumbers: number[]
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

/** Ошибка импорта графика: «Лист, строка N: сообщение». */
function formatGraphError(error: ScheduleValidationError): string {
  const message = error.message ?? ""
  if (/^Строка\s+\d+/i.test(message)) return message
  const parts: string[] = []
  if (error.sheet?.trim()) parts.push(error.sheet.trim())
  if (error.row > 0) parts.push(`строка ${error.row}`)
  const prefix = parts.join(", ")
  return prefix ? `${prefix}: ${message}` : message
}

/** Проверка файла графика УП до отправки на сервер. */
function validateGraphFile(file: File): string | null {
  if (!file.name.toLowerCase().endsWith(".docx"))
    return "Поддерживается только формат DOCX"
  if (file.size > MAX_IMPORT_SIZE)
    return "Файл слишком большой. Максимум 10 МБ"
  return null
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
  const [exporting, setExporting] = useState(false)

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formKind, setFormKind] = useState<PracticeKind>("Up")
  const [formName, setFormName] = useState("")
  const [formGroup, setFormGroup] = useState("")
  const [formTeacherIds, setFormTeacherIds] = useState<string[]>([])
  const [formDateFrom, setFormDateFrom] = useState("")
  const [formDateTo, setFormDateTo] = useState("")
  const [formRoom, setFormRoom] = useState("")
  const [formSubgroup, setFormSubgroup] = useState("")
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
  const [preview, setPreview] = useState<PracticeGraphPreviewResponse | null>(
    null,
  )
  // Правки шапки графика перед подтверждением: группа, тема, период.
  const [previewGroup, setPreviewGroup] = useState("")
  const [previewName, setPreviewName] = useState("")
  const [previewFrom, setPreviewFrom] = useState("")
  const [previewTo, setPreviewTo] = useState("")
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [dragging, setDragging] = useState(false)

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
      daySeedRef.current.map((day) => [
        toDateInput(day.date),
        normalizePairNumbers(day.pairNumbers),
      ]),
    )
    const drafts: DayDraft[] = candidates.map((date) => {
      const seeded = seed.get(date)
      return {
        date,
        included: true,
        pairNumbers:
          seeded && seeded.length > 0 ? seeded : [...DEFAULT_PAIR_NUMBERS],
      }
    })
    for (const [date, pairNumbers] of seed) {
      if (!drafts.some((draft) => draft.date === date)) {
        drafts.push({
          date,
          included: true,
          pairNumbers:
            pairNumbers.length > 0 ? pairNumbers : [...DEFAULT_PAIR_NUMBERS],
        })
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
    setFormRoom("")
    setFormSubgroup("")
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
    setFormRoom(practice.room ?? "")
    setFormSubgroup(practice.subgroup != null ? String(practice.subgroup) : "")
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

  const toggleDayIncluded = (index: number, included: boolean) => {
    setDayDrafts((prev) =>
      prev.map((day, i) => (i === index ? { ...day, included } : day)),
    )
  }

  const toggleDayPair = (index: number, pair: number, checked: boolean) => {
    setDayDrafts((prev) =>
      prev.map((day, i) => {
        if (i !== index) return day
        const next = checked
          ? [...day.pairNumbers, pair]
          : day.pairNumbers.filter((value) => value !== pair)
        return { ...day, pairNumbers: normalizePairNumbers(next) }
      }),
    )
  }

  const fillAllDays = () => {
    setDayDrafts((prev) =>
      prev.map((day) => ({
        ...day,
        included: true,
        pairNumbers: [...PRACTICE_PAIR_NUMBERS],
      })),
    )
  }

  const clearAllDays = () => {
    setDayDrafts((prev) =>
      prev.map((day) => ({ ...day, included: false, pairNumbers: [] })),
    )
  }

  const includedDays = dayDrafts.filter((day) => day.included)
  const totalDraftPairs = includedDays.reduce(
    (sum, day) => sum + day.pairNumbers.length,
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
    const subgroupValue = formSubgroup.trim()
    if (subgroupValue) {
      const parsed = Number(subgroupValue)
      if (!Number.isInteger(parsed) || parsed <= 0) {
        setFormError("Номер подгруппы должен быть целым числом больше нуля.")
        return
      }
    }
    if (formKind === "Up") {
      if (includedDays.length === 0) {
        setFormError("Для УП выберите хотя бы один день практики.")
        return
      }
      if (includedDays.some((day) => day.pairNumbers.length === 0)) {
        setFormError("У каждого выбранного дня отметьте хотя бы одну пару (1–8).")
        return
      }
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
              pairNumbers: normalizePairNumbers(day.pairNumbers),
            }))
          : undefined,
      note: formNote.trim() || null,
      room: formKind === "Up" ? formRoom.trim() || null : null,
      subgroup:
        formKind === "Up" && subgroupValue ? Number(subgroupValue) : null,
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

  const handleExport = async () => {
    if (filterGroup === "all") {
      toast.error("Выберите группу для экспорта графика УП")
      return
    }
    const groupName = groups.find((g) => g.id === filterGroup)?.name ?? ""
    setExporting(true)
    try {
      await exportPracticeGraph(filterGroup, groupName)
      toast.success("График УП сформирован")
    } catch (err) {
      toast.error(
        extractErrorMessage(err) ??
          (err instanceof Error ? err.message : null) ??
          "Не удалось сформировать график УП",
      )
    } finally {
      setExporting(false)
    }
  }

  const closeImport = () => {
    setImportOpen(false)
    setImportFile(null)
    setPreview(null)
    setPreviewGroup("")
    setPreviewName("")
    setPreviewFrom("")
    setPreviewTo("")
    setDragging(false)
    if (fileInputRef.current) fileInputRef.current.value = ""
  }

  const applyImportFile = (file: File) => {
    const problem = validateGraphFile(file)
    if (problem) {
      toast.error(problem)
      return
    }
    setImportFile(file)
    setPreview(null)
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    applyImportFile(file)
  }

  const handleDrop = (e: React.DragEvent<HTMLButtonElement>) => {
    e.preventDefault()
    setDragging(false)
    const file = e.dataTransfer.files?.[0]
    if (!file) return
    applyImportFile(file)
  }

  const runPreview = async () => {
    if (!importFile) return
    setPreviewing(true)
    try {
      const result = await previewPracticeGraphImport(importFile)
      setPreview(result)
      setPreviewGroup(result.groupName?.trim() ?? "")
      setPreviewName(result.practiceName?.trim() ?? "")
      setPreviewFrom(toDateInput(result.dateFrom))
      setPreviewTo(toDateInput(result.dateTo))
      if (result.errors.length > 0) {
        toast.error(`Найдены ошибки: ${result.errors.length}`, {
          description: "Проверьте оформление документа и повторите проверку.",
        })
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось прочитать документ")
    } finally {
      setPreviewing(false)
    }
  }

  const graphHeaderReady = Boolean(
    previewGroup.trim() &&
      previewName.trim() &&
      previewFrom &&
      previewTo &&
      previewFrom <= previewTo,
  )

  const runConfirm = async () => {
    if (!preview) return
    if (preview.errors.length > 0) {
      toast.error("Сначала исправьте ошибки в документе")
      return
    }
    if (!graphHeaderReady) {
      toast.error("Укажите группу, тему УП и корректный период")
      return
    }
    setConfirming(true)
    try {
      const result = await confirmPracticeGraphImport({
        groupName: previewGroup.trim(),
        name: previewName.trim(),
        dateFrom: previewFrom,
        dateTo: previewTo,
        rows: preview.rows,
      })
      toast.success(`Импортировано практик: ${result.imported}`)
      closeImport()
      await load(1)
      setPage(1)
    } catch (err) {
      toast.error(
        extractErrorMessage(err) ?? "Не удалось импортировать практики",
      )
    } finally {
      setConfirming(false)
    }
  }

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold">Практики</h1>
          <p className="text-sm text-muted-foreground">
            Периоды УП и ПП: в это время обычные пары группы заменяются
            карточкой практики. Для УП задаются учебные дни и номера пар.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            onClick={() => setImportOpen(true)}
            className="min-h-11 sm:min-h-9"
          >
            <Upload className="size-4" aria-hidden="true" />
            Импорт графика УП
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
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-medium">Экспорт</span>
          <Button
            type="button"
            variant="outline"
            onClick={() => void handleExport()}
            disabled={filterGroup === "all" || exporting}
            title={
              filterGroup === "all"
                ? "Выберите группу, чтобы скачать график УП"
                : "Скачать график УП выбранной группы в DOCX"
            }
            className="min-h-11 sm:min-h-9"
          >
            <Download className="size-4" aria-hidden="true" />
            {exporting ? "Формирование…" : "Скачать график УП"}
          </Button>
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
                      : "Добавьте практику вручную или импортируйте график УП из DOCX."
                  }
                />
                {!hasFilters && (
                  <div className="flex flex-wrap justify-center gap-2">
                    <Button variant="outline" onClick={() => setImportOpen(true)}>
                      <Upload className="size-4" aria-hidden="true" />
                      Импорт графика УП
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
                      const totalPairs = practiceTotalPairs(practice)
                      const teacherList = practiceTeachers(practice)
                      const meta = [
                        practice.subgroup != null
                          ? `подгруппа ${practice.subgroup}`
                          : null,
                        practice.room ? `каб. ${practice.room}` : null,
                      ]
                        .filter(Boolean)
                        .join(" · ")
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
                                {days.length} дн. · {totalPairs} пар ·{" "}
                                {totalPairs * 2} акад. ч
                              </span>
                            )}
                            {practice.kind === "Up" && meta && (
                              <span className="block text-xs font-normal text-muted-fg">
                                {meta}
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

              {formKind === "Up" && (
                <>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="pr-room">Кабинет</Label>
                    <Input
                      id="pr-room"
                      value={formRoom}
                      onChange={(e) => setFormRoom(e.target.value)}
                      maxLength={20}
                      placeholder="Например, 305"
                      className="h-11 bg-card sm:h-9"
                    />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="pr-subgroup">№ подгруппы</Label>
                    <Input
                      id="pr-subgroup"
                      type="number"
                      min={1}
                      step={1}
                      value={formSubgroup}
                      onChange={(e) => setFormSubgroup(e.target.value)}
                      placeholder="Например, 1"
                      className="h-11 bg-card sm:h-9"
                    />
                  </div>
                </>
              )}

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
                    <div className="flex flex-wrap gap-1.5">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-9"
                        onClick={fillAllDays}
                        disabled={dayDrafts.length === 0}
                      >
                        Заполнить все пары
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
                    Учебные дни периода (Пн–Сб без нерабочих). Отметьте точные
                    номера пар 1–8; по умолчанию выбраны пары 1–6.
                  </p>
                  {dayDrafts.length === 0 ? (
                    <p className="rounded-md border border-dashed bg-card px-3 py-4 text-center text-sm text-muted-foreground">
                      {!formDateFrom || !formDateTo
                        ? "Укажите период, чтобы раскрыть учебные дни."
                        : "В выбранном периоде нет учебных дней."}
                    </p>
                  ) : (
                    <div className="max-h-72 overflow-y-auto rounded-md border bg-card">
                      <ul className="divide-y">
                        {dayDrafts.map((day, index) => {
                          const label = formatDayLabel(day.date)
                          return (
                            <li
                              key={day.date}
                              className="flex flex-col gap-2 px-3 py-3 sm:flex-row sm:items-start sm:gap-3"
                            >
                              <div className="flex items-center justify-between gap-2 sm:w-56 sm:shrink-0">
                                <label className="flex flex-1 cursor-pointer items-center gap-2 text-sm">
                                  <input
                                    type="checkbox"
                                    checked={day.included}
                                    onChange={(e) =>
                                      toggleDayIncluded(
                                        index,
                                        e.target.checked,
                                      )
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
                                <Badge
                                  variant="secondary"
                                  className="tabular-nums"
                                >
                                  {day.pairNumbers.length}
                                </Badge>
                              </div>
                              <div
                                role="group"
                                aria-label={`Пары: ${label}`}
                                className={cn(
                                  "grid flex-1 grid-cols-4 gap-1.5 sm:grid-cols-8",
                                  !day.included && "opacity-50",
                                )}
                              >
                                {PRACTICE_PAIR_NUMBERS.map((pair) => {
                                  const active =
                                    day.pairNumbers.includes(pair)
                                  return (
                                    <label
                                      key={pair}
                                      className={cn(
                                        "flex min-h-11 cursor-pointer items-center justify-center gap-1.5 rounded-md border px-1.5 text-sm tabular-nums transition-colors",
                                        active
                                          ? "border-primary bg-primary/[0.06]"
                                          : "border-border hover:bg-muted",
                                        !day.included &&
                                          "cursor-not-allowed hover:bg-transparent",
                                      )}
                                    >
                                      <input
                                        type="checkbox"
                                        checked={active}
                                        disabled={!day.included}
                                        onChange={(e) =>
                                          toggleDayPair(
                                            index,
                                            pair,
                                            e.target.checked,
                                          )
                                        }
                                        aria-label={`Пара ${pair}, ${label}`}
                                        className="size-4 accent-primary"
                                      />
                                      {pair}
                                    </label>
                                  )
                                })}
                              </div>
                            </li>
                          )
                        })}
                      </ul>
                    </div>
                  )}
                  {includedDays.length > 0 && (
                    <p className="text-xs text-muted-foreground">
                      Выбрано дней: {includedDays.length} · {totalDraftPairs} пар
                      = {totalDraftPairs * 2} акад. часов
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

      {/* Импорт графика УП из DOCX */}
      <Dialog
        open={importOpen}
        onOpenChange={(open) => {
          if (!open) closeImport()
          else setImportOpen(true)
        }}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-5xl">
          <DialogHeader>
            <DialogTitle>Импорт графика УП из DOCX</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <p className="text-sm text-muted-foreground">
              Загрузите документ «График проведения занятий». Из шапки
              определяются группа, тема и период, из таблицы — подгруппы,
              кабинеты, даты с номерами пар и преподаватели. На основе строк
              будут созданы практики УП.
            </p>

            <input
              ref={fileInputRef}
              type="file"
              accept=".docx"
              className="hidden"
              onChange={handleFileChange}
              aria-hidden="true"
              tabIndex={-1}
            />

            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              onDragOver={(e) => {
                e.preventDefault()
                setDragging(true)
              }}
              onDragLeave={() => setDragging(false)}
              onDrop={handleDrop}
              className={cn(
                "flex w-full cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed p-6 text-center transition-colors focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50",
                dragging
                  ? "border-primary bg-primary/[0.06]"
                  : "border-border hover:bg-muted/50",
              )}
            >
              {importFile ? (
                <>
                  <FileText
                    className="size-10 text-accent"
                    aria-hidden="true"
                  />
                  <span className="flex flex-col gap-0.5">
                    <span className="font-medium break-all">
                      {importFile.name}
                    </span>
                    <span className="text-sm text-muted-foreground">
                      {(importFile.size / 1024).toFixed(1)} КБ
                    </span>
                  </span>
                </>
              ) : (
                <>
                  <UploadCloud
                    className="size-10 text-muted-foreground"
                    aria-hidden="true"
                  />
                  <span className="flex flex-col gap-0.5">
                    <span className="font-medium">
                      Перетащите график УП сюда
                    </span>
                    <span className="text-sm text-muted-foreground">
                      DOCX, до 10 МБ — или нажмите, чтобы выбрать
                    </span>
                  </span>
                </>
              )}
            </button>

            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                onClick={() => void runPreview()}
                disabled={!importFile || previewing}
                className="min-h-11 sm:min-h-9"
              >
                {previewing ? "Чтение…" : "Проверить документ"}
              </Button>
              {importFile && (
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setImportFile(null)
                    setPreview(null)
                    if (fileInputRef.current) fileInputRef.current.value = ""
                  }}
                  className="min-h-11 sm:min-h-9"
                >
                  Убрать файл
                </Button>
              )}
            </div>

            {preview && (
              <>
                <div
                  role="group"
                  aria-labelledby="pr-graph-head-label"
                  className="flex flex-col gap-3 rounded-lg border bg-muted/30 p-3"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span
                      id="pr-graph-head-label"
                      className="text-sm font-medium"
                    >
                      Шапка графика
                    </span>
                    <span className="text-xs text-muted-foreground tabular-nums">
                      Строк: {preview.totalRows}
                    </span>
                  </div>
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="pr-graph-group">Группа *</Label>
                      <Input
                        id="pr-graph-group"
                        value={previewGroup}
                        onChange={(e) => setPreviewGroup(e.target.value)}
                        maxLength={100}
                        className="h-11 bg-card sm:h-9"
                      />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="pr-graph-name">Тема УП *</Label>
                      <Input
                        id="pr-graph-name"
                        value={previewName}
                        onChange={(e) => setPreviewName(e.target.value)}
                        maxLength={100}
                        className="h-11 bg-card sm:h-9"
                      />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="pr-graph-from">Период с *</Label>
                      <Input
                        id="pr-graph-from"
                        type="date"
                        value={previewFrom}
                        onChange={(e) => setPreviewFrom(e.target.value)}
                        className="h-11 bg-card sm:h-9"
                      />
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <Label htmlFor="pr-graph-to">по *</Label>
                      <Input
                        id="pr-graph-to"
                        type="date"
                        value={previewTo}
                        onChange={(e) => setPreviewTo(e.target.value)}
                        className="h-11 bg-card sm:h-9"
                      />
                    </div>
                  </div>
                  {previewFrom && previewTo && previewFrom > previewTo && (
                    <p className="text-xs text-destructive">
                      Дата начала не может быть позже даты окончания.
                    </p>
                  )}
                </div>

                {preview.errors.length > 0 ? (
                  <div className="flex flex-col gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-3">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-destructive">
                      <AlertTriangle className="size-4" aria-hidden="true" />
                      Ошибок: {preview.errors.length}
                    </p>
                    <div className="max-h-40 overflow-y-auto text-xs">
                      <ul className="flex flex-col gap-1">
                        {preview.errors.map((err, i) => (
                          <li key={i} className="text-destructive">
                            {formatGraphError(err)}
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                ) : (
                  <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
                    <span className="text-success">Ошибок нет</span> — можно
                    импортировать.
                  </p>
                )}

                {!graphHeaderReady && (
                  <div className="flex items-start gap-2 rounded-md border border-amber-300/60 bg-amber-50 p-3 text-xs text-amber-800 dark:border-amber-900/60 dark:bg-amber-950/40 dark:text-amber-200">
                    <AlertTriangle
                      className="mt-0.5 size-4 shrink-0"
                      aria-hidden="true"
                    />
                    <p>
                      Заполните группу, тему УП и корректный период — без них
                      импорт недоступен.
                    </p>
                  </div>
                )}

                {preview.rows.length > 0 && (
                  <div className="max-h-80 overflow-auto rounded-md border">
                    <table className="w-full min-w-[900px] text-xs">
                      <thead className="sticky top-0 bg-muted text-left">
                        <tr>
                          <th className="px-3 py-2 font-medium">№</th>
                          <th className="px-3 py-2 font-medium">Подгруппа</th>
                          <th className="px-3 py-2 font-medium">Тема</th>
                          <th className="px-3 py-2 font-medium">Кабинет</th>
                          <th className="px-3 py-2 font-medium">
                            Дни (даты и пары)
                          </th>
                          <th className="px-3 py-2 font-medium">
                            Преподаватель
                          </th>
                          <th className="px-3 py-2 font-medium">Примечание</th>
                        </tr>
                      </thead>
                      <tbody>
                        {preview.rows.map((row, index) => (
                          <tr
                            key={`${row.row}-${index}`}
                            className="border-b align-top last:border-0"
                          >
                            <td className="px-3 py-2 font-mono tabular-nums text-muted-fg">
                              {row.row}
                            </td>
                            <td className="px-3 py-2 tabular-nums">
                              {row.subgroup ?? "—"}
                            </td>
                            <td className="min-w-[180px] px-3 py-2">
                              {row.name || "—"}
                            </td>
                            <td className="whitespace-nowrap px-3 py-2">
                              {row.room || "—"}
                            </td>
                            <td className="min-w-[220px] px-3 py-2">
                              {row.days.length === 0 ? (
                                <span className="text-muted-fg">—</span>
                              ) : (
                                <ul className="flex flex-col gap-0.5">
                                  {row.days.map((day, dayIndex) => (
                                    <li
                                      key={`${day.date}-${dayIndex}`}
                                      className="whitespace-nowrap"
                                    >
                                      <span className="font-mono tabular-nums">
                                        {formatDate(day.date)}
                                      </span>
                                      {" — "}
                                      <span>
                                        пары{" "}
                                        {day.pairNumbers.length > 0
                                          ? day.pairNumbers.join(", ")
                                          : "—"}
                                      </span>
                                    </li>
                                  ))}
                                </ul>
                              )}
                            </td>
                            <td className="min-w-[160px] px-3 py-2">
                              {row.teacherName || "—"}
                            </td>
                            <td className="min-w-[140px] px-3 py-2 text-muted-fg">
                              {row.note || "—"}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <p className="text-xs text-muted-foreground">
                  Импорт выполняется одной транзакцией: по одной практике УП на
                  каждую подгруппу. Существующие практики группы за этот период
                  будут заменены.
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
                !graphHeaderReady ||
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
