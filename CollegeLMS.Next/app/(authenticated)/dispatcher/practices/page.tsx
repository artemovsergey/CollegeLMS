"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { toast } from "sonner"
import {
  AlertTriangle,
  Briefcase,
  FileSpreadsheet,
  Inbox,
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
import { extractErrorMessage } from "@/lib/utils"
import { formatDateRange, toDateInput } from "@/lib/reference"
import type { GroupResponse, TeacherResponse, Result } from "@/types"
import {
  confirmPracticeImport,
  createPractice,
  deletePractice,
  fetchPractices,
  previewPracticeImport,
  updatePractice,
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  type Practice,
  type PracticeImportPreview,
  type PracticeImportRow,
  type PracticeKind,
} from "@/api/practices"

const PAGE_SIZE = 20

function normalizeImportMessage(message: string, row: number): string {
  return /^Строка\s+\d+/i.test(message) ? message : `Строка ${row}: ${message}`
}

const IMPORT_FIELD_LABELS: Record<keyof PracticeImportRow, string> = {
  row: "Строка",
  kind: "Вид",
  groupName: "Группа",
  dateFrom: "Дата начала",
  dateTo: "Дата окончания",
  teacherName: "Преподаватель",
  organization: "Организация",
  note: "Примечание",
}

export default function DispatcherPracticesPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)

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
  const [formGroup, setFormGroup] = useState("")
  const [formTeacher, setFormTeacher] = useState("")
  const [formDateFrom, setFormDateFrom] = useState("")
  const [formDateTo, setFormDateTo] = useState("")
  const [formOrganization, setFormOrganization] = useState("")
  const [formNote, setFormNote] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

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
    setFormGroup(groups[0]?.id ?? "")
    setFormTeacher(teachers[0]?.id ?? "")
    setFormDateFrom("")
    setFormDateTo("")
    setFormOrganization("")
    setFormNote("")
    setFormError(null)
    setDialogOpen(true)
  }

  const openEdit = (practice: Practice) => {
    setEditingId(practice.id)
    setFormKind(practice.kind)
    setFormGroup(practice.groupId)
    setFormTeacher(practice.teacherId)
    setFormDateFrom(toDateInput(practice.dateFrom))
    setFormDateTo(toDateInput(practice.dateTo))
    setFormOrganization(practice.organization ?? "")
    setFormNote(practice.note ?? "")
    setFormError(null)
    setDialogOpen(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formGroup) {
      setFormError("Выберите группу.")
      return
    }
    if (!formTeacher) {
      setFormError("Выберите преподавателя.")
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

    setSubmitting(true)
    setFormError(null)
    const body = {
      kind: formKind,
      groupId: formGroup,
      teacherId: formTeacher,
      dateFrom: formDateFrom,
      dateTo: formDateTo,
      organization: formOrganization.trim() || null,
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
          <h1 className="text-2xl font-semibold">Учебные практики</h1>
          <p className="text-sm text-muted-foreground">
            Периоды УП и ПП: в это время обычные пары группы заменяются
            карточкой практики.
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
                <table className="w-full min-w-[860px] text-sm">
                  <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-fg">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium">Вид</th>
                      <th className="px-4 py-3 text-left font-medium">Группа</th>
                      <th className="px-4 py-3 text-left font-medium">
                        Преподаватель
                      </th>
                      <th className="px-4 py-3 text-left font-medium">Период</th>
                      <th className="px-4 py-3 text-left font-medium">
                        Организация
                      </th>
                      <th className="px-4 py-3 text-left font-medium">
                        Примечание
                      </th>
                      <th className="px-4 py-3 text-right font-medium">
                        Действия
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((practice) => (
                      <tr key={practice.id} className="border-b last:border-0">
                        <td className="px-4 py-3">
                          <Badge variant="outline">
                            {PRACTICE_KIND_SHORT[practice.kind] ??
                              practice.kind}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 font-medium">
                          {practice.groupName}
                        </td>
                        <td className="px-4 py-3">{practice.teacherName}</td>
                        <td className="px-4 py-3 font-mono text-xs tabular-nums whitespace-nowrap">
                          {formatDateRange(practice.dateFrom, practice.dateTo)}
                        </td>
                        <td className="max-w-[200px] truncate px-4 py-3 text-muted-fg">
                          {practice.organization ?? "—"}
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
                              aria-label={`Редактировать практику «${practice.groupName}»`}
                            >
                              <Pencil className="size-4" aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                              onClick={() => setDeleteTarget(practice)}
                              aria-label={`Удалить практику «${practice.groupName}»`}
                            >
                              <Trash2 className="size-4" aria-hidden="true" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
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
        <DialogContent className="sm:max-w-xl">
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
                <Label htmlFor="pr-teacher">Преподаватель *</Label>
                <NativeSelect
                  value={formTeacher}
                  onValueChange={setFormTeacher}
                  className="w-full"
                >
                  <NativeSelectItem value="">
                    Выберите преподавателя
                  </NativeSelectItem>
                  {teachers.map((t) => (
                    <NativeSelectItem key={t.id} value={t.id}>
                      {t.fullName}
                    </NativeSelectItem>
                  ))}
                </NativeSelect>
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
              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label htmlFor="pr-organization">Организация</Label>
                <Input
                  id="pr-organization"
                  value={formOrganization}
                  onChange={(e) => setFormOrganization(e.target.value)}
                  maxLength={200}
                  placeholder="Например, ООО «Связь-Сервис»"
                  className="h-11 bg-card sm:h-9"
                />
              </div>
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
              Шапка в строке 1: «Вид | Группа | Дата начала | Дата окончания |
              Преподаватель | Организация | Примечание». Даты — в формате
              ДД.ММ.ГГГГ, вид — УП или ПП.
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
                          <th className="px-2 py-2 font-medium">Группа</th>
                          <th className="px-2 py-2 font-medium">Дата начала</th>
                          <th className="px-2 py-2 font-medium">
                            Дата окончания
                          </th>
                          <th className="px-2 py-2 font-medium">
                            Преподаватель
                          </th>
                          <th className="px-2 py-2 font-medium">Организация</th>
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
                            {(
                              [
                                "kind",
                                "groupName",
                                "dateFrom",
                                "dateTo",
                                "teacherName",
                                "organization",
                                "note",
                              ] as (keyof PracticeImportRow)[]
                            ).map((field) => (
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
                  импорт. Импорт выполняется одной транзакцией.
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
                ? `${PRACTICE_KIND_LABELS[deleteTarget.kind]} · ${deleteTarget.groupName} (${formatDateRange(deleteTarget.dateFrom, deleteTarget.dateTo)}). Действие необратимо.`
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
