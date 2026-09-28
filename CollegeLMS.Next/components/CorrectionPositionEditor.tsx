"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import {
  ArrowLeft,
  CheckCircle,
  CircleAlert,
  Download,
  Pencil,
  Play,
  Plus,
  Trash2,
  WandSparkles,
} from "lucide-react"
import api, { unwrap } from "@/lib/api"
import { normalizeDateOnly } from "@/api/schedule"
import {
  addPosition,
  applyBatch,
  deletePosition,
  exportBatch,
  getBatch,
  updatePosition,
} from "@/api/correction"
import type {
  CorrectionBatch,
  CorrectionChangeType,
  CorrectionPosition,
  CreateCorrectionPosition,
  ScheduleValidationError,
} from "@/types/correction"
import type { GroupResponse, Result } from "@/types"
import { DAYS } from "@/types/schedule"
import { extractErrorMessage } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { SearchableSelect } from "@/components/SearchableSelect"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import Pagination from "@/components/ui/pagination"
import { ConfirmDialog } from "@/components/ConfirmDialog"
import { CorrectionPositionDialog } from "@/components/CorrectionPositionDialog"
import EmptyState from "@/components/EmptyState"

const POSITION_PAGE_SIZE = 20

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; className: string }
> = {
  Add: {
    label: "Добавлено",
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

const ALL_GROUPS = "__all__"
const ALL_TYPES = "__all__"

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

function PositionTypeBadge({ type }: { type: CorrectionChangeType }) {
  const meta = CHANGE_TYPE_META[type]
  return (
    <Badge variant="outline" className={meta.className}>
      {meta.label}
    </Badge>
  )
}

function formatValidationError(error: ScheduleValidationError): string {
  const message = error.message ?? ""
  if (/^Строка\s+\d+/i.test(message)) return message
  const row = error.row ? `Строка ${error.row}` : ""
  return row ? `${row}: ${message}` : message
}

function renderTitle(position: CorrectionPosition) {
  if (position.changeType === "Replace" || position.changeType === "Move") {
    return (
      <span className="flex items-center gap-1">
        <span className="line-through text-muted-foreground">
          {position.removedSubject ?? "—"}
        </span>
        <span className="font-medium">{position.subject ?? "—"}</span>
      </span>
    )
  }
  if (position.changeType === "Remove") {
    return (
      <span className="line-through text-muted-foreground">
        {position.removedSubject ?? "—"}
      </span>
    )
  }
  return <span className="font-medium">{position.subject ?? "—"}</span>
}

function renderTeacher(position: CorrectionPosition) {
  if (position.changeType === "Replace" || position.changeType === "Move") {
    const from = position.removedTeacherName
    const to = position.teacherName
    if (from && to) return `${from} → ${to}`
    return to ?? from ?? "—"
  }
  return (
    position.teacherName ??
    (position.changeType === "Remove" ? position.removedTeacherName : null) ??
    "—"
  )
}

interface CorrectionPositionEditorProps {
  batchId: string
  onBack: () => void
  onApplied: () => void
}

export default function CorrectionPositionEditor({
  batchId,
  onBack,
  onApplied,
}: CorrectionPositionEditorProps) {
  const [batch, setBatch] = useState<CorrectionBatch | null>(null)
  const [loading, setLoading] = useState(true)
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [applyErrors, setApplyErrors] = useState<string[]>([])
  const [busyRowId, setBusyRowId] = useState<string | null>(null)

  // Постраничный просмотр позиций: после импорта их могут быть десятки.
  const [page, setPage] = useState(1)
  const [groupFilter, setGroupFilter] = useState(ALL_GROUPS)
  const [typeFilter, setTypeFilter] = useState<
    CorrectionChangeType | typeof ALL_TYPES
  >(ALL_TYPES)

  // Модальные окна: форма позиции и подтверждения.
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<CorrectionPosition | null>(null)
  const [confirmApply, setConfirmApply] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<CorrectionPosition | null>(
    null,
  )

  const load = useCallback(async () => {
    try {
      setBatch(await getBatch(batchId))
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка загрузки пакета")
    } finally {
      setLoading(false)
    }
  }, [batchId])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    api
      .get<Result<GroupResponse[]>>("/api/groups")
      .then(unwrap)
      .then(setGroups)
      .catch(() => toast.error("Не удалось загрузить справочник групп"))
  }, [])

  const positions = useMemo(() => batch?.positions ?? [], [batch])

  const groupOptions = useMemo(() => {
    const names = new Set(positions.map((item) => item.groupName).filter(Boolean))
    return [...names].sort((a, b) => a.localeCompare(b, "ru"))
  }, [positions])

  const filtered = useMemo(
    () =>
      positions.filter(
        (position) =>
          (groupFilter === ALL_GROUPS ||
            position.groupName === groupFilter) &&
          (typeFilter === ALL_TYPES || position.changeType === typeFilter),
      ),
    [positions, groupFilter, typeFilter],
  )

  const totalPages = Math.max(1, Math.ceil(filtered.length / POSITION_PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const visible = useMemo(
    () =>
      filtered.slice(
        (currentPage - 1) * POSITION_PAGE_SIZE,
        currentPage * POSITION_PAGE_SIZE,
      ),
    [filtered, currentPage],
  )

  const handleSubmit = async (payload: CreateCorrectionPosition) => {
    if (editing) {
      await updatePosition(batchId, editing.id, payload)
      toast.success("Позиция обновлена")
    } else {
      await addPosition(batchId, payload)
      toast.success("Позиция добавлена")
    }
    setEditing(null)
    setPage(1)
    await load()
  }

  const handleDelete = async () => {
    if (!pendingDelete) return false
    setBusyRowId(pendingDelete.id)
    try {
      await deletePosition(batchId, pendingDelete.id)
      toast.success("Позиция удалена")
      setPendingDelete(null)
      await load()
      return true
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить позицию")
      return false
    } finally {
      setBusyRowId(null)
    }
  }

  const handleExport = async () => {
    if (!batch) return
    try {
      const { blob, fileName } = await exportBatch(batch.id)
      downloadBlob(blob, fileName)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сформировать файл")
    }
  }

  const applyBatchNow = async () => {
    if (!batch) return false
    setApplyErrors([])
    try {
      const result = await applyBatch(batch.id, crypto.randomUUID())
      toast.success(`Применено изменений: ${result.applied}`)
      try {
        const { blob, fileName } = await exportBatch(batch.id)
        downloadBlob(blob, fileName)
      } catch {
        toast.message("Пакет применён, но файл не удалось сформировать")
      }
      onApplied()
      await load()
      return true
    } catch (err) {
      const message = extractErrorMessage(err)
      const lines = message
        ? message
            .split(/\r?\n/)
            .map((line) => line.trim())
            .filter(Boolean)
        : []
      if (lines.length > 0) {
        setApplyErrors(lines)
        toast.error("Не удалось применить пакет", {
          description: lines[0],
          duration: 8000,
        })
      } else {
        toast.error("Не удалось применить пакет")
      }
      return false
    }
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="py-8 text-center text-sm text-muted-foreground">
          Загрузка пакета...
        </CardContent>
      </Card>
    )
  }

  if (!batch) {
    return (
      <Card>
        <CardContent className="py-8">
          <EmptyState message="Пакет не найден." />
          <Button variant="outline" className="mt-4" onClick={onBack}>
            <ArrowLeft className="size-4 mr-2" aria-hidden /> К пакетам
          </Button>
        </CardContent>
      </Card>
    )
  }

  const batchIsDraft = batch.status === "Draft"
  const batchErrors = batch.errors ?? []
  const dateOnly = normalizeDateOnly(batch.correctionDate)
  const applyDisabled =
    !batchIsDraft || batch.positionCount === 0 || batchErrors.length > 0
  const applyHint = !batchIsDraft
    ? "Пакет уже применён или отменён"
    : batchErrors.length > 0
      ? "Сначала исправьте ошибки пакета"
      : batch.positionCount === 0
        ? "В пакете нет позиций"
        : "Проверьте позиции и примените пакет"
  const hasFilters = groupFilter !== ALL_GROUPS || typeFilter !== ALL_TYPES

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <WandSparkles className="size-4" aria-hidden />
            Редактор пакета
            <Badge
              variant="outline"
              className={
                batchIsDraft
                  ? "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300"
                  : "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300"
              }
            >
              {batchIsDraft ? "Подготовлен" : "Применён"}
            </Badge>
          </span>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={onBack}>
              <ArrowLeft className="size-4 mr-2" aria-hidden /> Назад
            </Button>
            <Button variant="outline" onClick={() => void handleExport()}>
              <Download className="size-4 mr-2" aria-hidden /> XLSX
            </Button>
            <Button
              disabled={applyDisabled}
              title={applyDisabled ? applyHint : undefined}
              onClick={() => setConfirmApply(true)}
            >
              <Play className="size-4 mr-2" aria-hidden /> Применить
            </Button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <div className="flex flex-wrap items-center gap-x-6 gap-y-1 rounded-md border bg-muted/30 px-4 py-3 text-sm">
          <span>
            Дата:{" "}
            <span className="font-medium">
              {new Date(batch.correctionDate).toLocaleDateString("ru-RU")}
            </span>
          </span>
          <span>
            Неделя: <span className="font-medium">{batch.week}</span>
          </span>
          <span>
            День:{" "}
            <span className="font-medium">
              {DAYS.find((d) => d.value === batch.dayOfWeek)?.full ??
                batch.dayOfWeek}
            </span>
          </span>
          <span>
            Позиций: <span className="font-medium">{batch.positionCount}</span>
          </span>
          <span className="text-muted-foreground">{applyHint}</span>
        </div>

        {batchErrors.length > 0 && (
          <div
            role="alert"
            className="grid gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-4"
          >
            <p className="flex items-center gap-2 text-sm font-semibold text-destructive">
              <CircleAlert className="size-4 shrink-0" aria-hidden />
              Ошибки пакета ({batchErrors.length}) — применить нельзя
            </p>
            <ul className="grid max-h-48 gap-1 overflow-y-auto text-xs text-muted-foreground">
              {batchErrors.map((error, index) => (
                <li key={index}>{formatValidationError(error)}</li>
              ))}
            </ul>
          </div>
        )}

        {applyErrors.length > 0 && (
          <div
            role="alert"
            className="grid gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-4"
          >
            <p className="flex items-center gap-2 text-sm font-semibold text-destructive">
              <CircleAlert className="size-4 shrink-0" aria-hidden />
              Пакет не применён — бэкенд вернул ошибки
            </p>
            <ul className="grid max-h-48 gap-1 overflow-y-auto text-xs text-muted-foreground">
              {applyErrors.map((line, index) => (
                <li key={index}>{line}</li>
              ))}
            </ul>
          </div>
        )}

        {positions.length === 0 ? (
          <EmptyState message="Позиций пока нет — добавьте первую." />
        ) : (
          <>
            <div className="grid gap-3 rounded-md border p-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
              <label className="grid gap-1 text-sm font-medium">
                Группа
                <SearchableSelect
                  aria-label="Фильтр по группе"
                  value={groupFilter}
                  onValueChange={(value) => {
                    setGroupFilter(value)
                    setPage(1)
                  }}
                  options={[
                    { value: ALL_GROUPS, label: "Все группы" },
                    ...groupOptions.map((name) => ({ value: name, label: name })),
                  ]}
                  placeholder="Все группы"
                  searchPlaceholder="Поиск группы"
                />
              </label>
              <label className="grid gap-1 text-sm font-medium">
                Тип операции
                <SearchableSelect
                  aria-label="Фильтр по типу операции"
                  value={typeFilter}
                  onValueChange={(value) => {
                    setTypeFilter(value as CorrectionChangeType | typeof ALL_TYPES)
                    setPage(1)
                  }}
                  options={[
                    { value: ALL_TYPES, label: "Все типы" },
                    ...(Object.keys(CHANGE_TYPE_META) as CorrectionChangeType[]).map(
                      (type) => ({
                        value: type,
                        label: CHANGE_TYPE_META[type].label,
                      }),
                    ),
                  ]}
                  placeholder="Все типы"
                />
              </label>
              <p className="text-xs text-muted-foreground sm:pb-2">
                Показано {visible.length} из {filtered.length}
                {hasFilters ? " (с учётом фильтра)" : ""}
              </p>
            </div>

            {visible.length === 0 ? (
              <EmptyState message="По выбранным фильтрам позиций нет." />
            ) : (
              <div className="overflow-x-auto rounded-md border">
                <Table className="min-w-[900px]">
                  <TableHeader className="bg-muted/50 text-xs uppercase text-muted-foreground [&_th]:text-muted-foreground [&_th]:font-bold [&_th]:h-auto [&_tr]:border-b-0">
                    <TableRow>
                      <TableHead className="px-3 py-2 text-left">№</TableHead>
                      <TableHead className="px-3 py-2 text-left">Тип</TableHead>
                      <TableHead className="px-3 py-2 text-left">Группа</TableHead>
                      <TableHead className="px-3 py-2 text-left">Пара</TableHead>
                      <TableHead className="px-3 py-2 text-left">Предмет</TableHead>
                      <TableHead className="px-3 py-2 text-left">
                        Преподаватель
                      </TableHead>
                      <TableHead className="px-3 py-2 text-left">
                        Примечание
                      </TableHead>
                      <TableHead className="px-3 py-2" />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {visible.map((position) => (
                      <PositionRow
                        key={position.id}
                        position={position}
                        errors={position.errors ?? []}
                        batchIsDraft={batchIsDraft}
                        busy={busyRowId === position.id}
                        onEdit={() => {
                          setEditing(position)
                          setFormOpen(true)
                        }}
                        onDelete={() => setPendingDelete(position)}
                      />
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}

            {totalPages > 1 && (
              <Pagination
                page={currentPage}
                totalPages={totalPages}
                onPageChange={setPage}
              />
            )}
          </>
        )}

        {batchIsDraft && (
          <div className="flex flex-wrap gap-2">
            <Button
              onClick={() => {
                setEditing(null)
                setFormOpen(true)
              }}
            >
              <Plus className="size-4 mr-2" aria-hidden /> Добавить позицию
            </Button>
          </div>
        )}

        {batch.status === "Applied" && (
          <div className="flex items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300">
            <CheckCircle className="size-4 shrink-0" aria-hidden />
            <span>
              Применён
              {batch.appliedByName ? `: ${batch.appliedByName}` : ""}
              {batch.appliedAt
                ? `, ${new Date(batch.appliedAt).toLocaleString("ru-RU")}`
                : ""}
              . Позиции зафиксированы в журнале изменений.
            </span>
          </div>
        )}
      </CardContent>

      <CorrectionPositionDialog
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open)
          if (!open) setEditing(null)
        }}
        batchId={batchId}
        batchDate={dateOnly}
        groups={groups}
        position={editing}
        onSubmit={handleSubmit}
      />

      <ConfirmDialog
        open={confirmApply}
        onOpenChange={setConfirmApply}
        title="Применить корректировку?"
        description={
          <>
            <p>
              Будет применено позиций:{" "}
              <span className="font-medium">{batch.positionCount}</span>. Изменения
              попадут в расписание и в журнал, а преподавателям уйдёт уведомление.
            </p>
            <p className="mt-2">
              Дата:{" "}
              {new Date(batch.correctionDate).toLocaleDateString("ru-RU")}, неделя{" "}
              {batch.week}. Отменить применение нельзя.
            </p>
          </>
        }
        confirmLabel="Применить"
        onConfirm={applyBatchNow}
      />

      <ConfirmDialog
        open={pendingDelete != null}
        onOpenChange={(open) => {
          if (!open) setPendingDelete(null)
        }}
        title="Удалить позицию?"
        description={
          pendingDelete
            ? `Позиция ${pendingDelete.row} (${pendingDelete.groupName ?? "—"}) будет удалена из пакета.`
            : null
        }
        confirmLabel="Удалить"
        confirmVariant="destructive"
        onConfirm={handleDelete}
      />
    </Card>
  )
}

interface PositionRowProps {
  position: CorrectionPosition
  errors: ScheduleValidationError[]
  batchIsDraft: boolean
  busy: boolean
  onEdit: () => void
  onDelete: () => void
}

function PositionRow({
  position,
  errors,
  batchIsDraft,
  busy,
  onEdit,
  onDelete,
}: PositionRowProps) {
  const hasErrors = errors.length > 0
  return (
    <>
      <TableRow
        className={
          hasErrors
            ? "bg-destructive/5 hover:bg-destructive/5"
            : position.status === "Applied"
              ? "opacity-60 hover:bg-transparent"
              : "hover:bg-transparent"
        }
      >
        <TableCell className="px-3 py-2 text-muted-foreground">
          {position.row}
        </TableCell>
        <TableCell className="px-3 py-2">
          <PositionTypeBadge type={position.changeType} />
        </TableCell>
        <TableCell className="px-3 py-2 whitespace-nowrap">
          {position.groupName || (
            <span className="text-destructive">Группа не указана</span>
          )}
        </TableCell>
        <TableCell className="px-3 py-2 whitespace-nowrap">
          {position.removedNumberPair != null &&
          position.changeType !== "Remove" &&
          position.removedNumberPair !== position.numberPair
            ? `${position.removedNumberPair} → ${position.numberPair}`
            : position.numberPair || <span className="text-destructive">—</span>}
        </TableCell>
        <TableCell className="px-3 py-2 whitespace-normal">
          {renderTitle(position)}
        </TableCell>
        <TableCell className="px-3 py-2 max-w-[220px] truncate">
          {renderTeacher(position)}
        </TableCell>
        <TableCell className="px-3 py-2 max-w-[160px] truncate text-muted-foreground">
          {position.note ?? "—"}
        </TableCell>
        <TableCell className="px-3 py-2">
          {batchIsDraft && (
            <div className="flex justify-end gap-1">
              <Button
                variant="ghost"
                size="icon"
                onClick={onEdit}
                aria-label={`Редактировать позицию ${position.row}`}
              >
                <Pencil className="size-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                disabled={busy}
                onClick={onDelete}
                aria-label={`Удалить позицию ${position.row}`}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </div>
          )}
        </TableCell>
      </TableRow>
      {hasErrors && (
        <TableRow className="bg-destructive/5 hover:bg-destructive/5">
          <TableCell colSpan={8} className="px-3 pt-0 pb-2">
            <ul className="grid gap-1 text-xs text-destructive">
              {errors.map((error, index) => (
                <li key={index} className="flex items-start gap-1.5">
                  <CircleAlert className="mt-0.5 size-3.5 shrink-0" aria-hidden />
                  {formatValidationError(error)}
                </li>
              ))}
            </ul>
          </TableCell>
        </TableRow>
      )}
    </>
  )
}
