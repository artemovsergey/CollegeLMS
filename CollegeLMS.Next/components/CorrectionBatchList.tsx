"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import {
  CalendarDays,
  CircleAlert,
  Download,
  Eye,
  FolderPlus,
  LoaderCircle,
  Play,
  RefreshCw,
  RotateCcw,
  Trash2,
} from "lucide-react"
import {
  applyBatch,
  createBatch,
  deleteBatch,
  exportBatch,
  getBatches,
} from "@/api/correction"
import type {
  CorrectionBatch,
  CorrectionBatchStatus,
} from "@/types/correction"
import { DAYS } from "@/types/schedule"
import { cn, extractErrorMessage } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import EmptyState from "@/components/EmptyState"

const PAGE_SIZE = 20

const STATUS_META: Record<
  CorrectionBatchStatus,
  { label: string; className: string }
> = {
  Draft: {
    label: "Подготовлен",
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
  Applied: {
    label: "Применён",
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Cancelled: {
    label: "Отменён",
    className: "bg-muted text-muted-foreground",
  },
}

const STATUS_FILTERS: { key: CorrectionBatchStatus | "All"; label: string }[] = [
  { key: "All", label: "Все" },
  { key: "Draft", label: "Подготовленные" },
  { key: "Applied", label: "Применённые" },
  { key: "Cancelled", label: "Отменённые" },
]

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

function todayIso(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, "0")
  const day = String(now.getDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("ru-RU")
}

function canApply(batch: CorrectionBatch): boolean {
  return (
    batch.status === "Draft" &&
    batch.positionCount > 0 &&
    batch.errors.length === 0
  )
}

function applyBlockReason(batch: CorrectionBatch): string | undefined {
  if (batch.status !== "Draft") return "Пакет уже применён или отменён"
  if (batch.positionCount === 0) return "В пакете нет позиций"
  if (batch.errors.length > 0)
    return `В пакете ${batch.errors.length} ошибок — исправьте их в редакторе`
  return undefined
}

interface CorrectionBatchListProps {
  onOpen: (batchId: string) => void
  refreshKey: number
}

export default function CorrectionBatchList({
  onOpen,
  refreshKey,
}: CorrectionBatchListProps) {
  const [batches, setBatches] = useState<CorrectionBatch[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState<
    CorrectionBatchStatus | "All"
  >("All")
  const [from, setFrom] = useState("")
  const [to, setTo] = useState("")
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [date, setDate] = useState(todayIso())
  const [creating, setCreating] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await getBatches({
        status: statusFilter === "All" ? undefined : statusFilter,
        from: from || undefined,
        to: to || undefined,
        page,
        pageSize: PAGE_SIZE,
      })
      // Страница могла «съехать» после удаления — возвращаемся к последней доступной.
      if (res.items.length === 0 && page > 1 && res.totalCount > 0) {
        setPage(Math.max(1, res.totalPages))
        return
      }
      setBatches(res.items)
      setTotalCount(res.totalCount)
      setTotalPages(Math.max(1, res.totalPages))
    } catch (err) {
      setError(extractErrorMessage(err) ?? "Не удалось загрузить пакеты")
      setBatches([])
      setTotalCount(0)
      setTotalPages(1)
    } finally {
      setLoading(false)
    }
    // refreshKey намеренно в зависимостях: родитель бампает его после мутаций,
    // чтобы перезагрузить список, не сбрасывая фильтры и страницу.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, from, to, page, refreshKey])

  useEffect(() => {
    void load()
  }, [load])

  const hasFilters = statusFilter !== "All" || Boolean(from) || Boolean(to)

  const resetFilters = () => {
    setStatusFilter("All")
    setFrom("")
    setTo("")
    setPage(1)
  }

  const handleCreate = async () => {
    if (!date) {
      toast.error("Укажите дату корректировки")
      return
    }
    setCreating(true)
    try {
      const batch = await createBatch(date)
      toast.success("Пакет создан")
      onOpen(batch.id)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось создать пакет")
    } finally {
      setCreating(false)
    }
  }

  const handleDelete = async (batch: CorrectionBatch) => {
    if (!window.confirm(`Удалить пакет за ${formatDate(batch.correctionDate)}?`))
      return
    setBusyId(batch.id)
    try {
      await deleteBatch(batch.id)
      toast.success("Пакет удалён")
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить пакет")
    } finally {
      setBusyId(null)
    }
  }

  const handleExport = async (batch: CorrectionBatch) => {
    setBusyId(batch.id)
    try {
      const { blob, fileName } = await exportBatch(batch.id)
      downloadBlob(blob, fileName)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сформировать файл")
    } finally {
      setBusyId(null)
    }
  }

  const handleApply = async (batch: CorrectionBatch) => {
    if (
      !window.confirm(
        `Применить корректировки за ${formatDate(batch.correctionDate)}?`,
      )
    )
      return
    setBusyId(batch.id)
    try {
      const result = await applyBatch(batch.id, crypto.randomUUID())
      toast.success(`Применено изменений: ${result.applied}`)
      try {
        const { blob, fileName } = await exportBatch(batch.id)
        downloadBlob(blob, fileName)
      } catch {
        toast.message("Пакет применён, но файл не удалось сформировать")
      }
      await load()
    } catch (err) {
      const message = extractErrorMessage(err)
      if (message) {
        toast.error("Не удалось применить пакет", {
          description: message,
          duration: 8000,
        })
      } else {
        toast.error("Не удалось применить корректировки")
      }
    } finally {
      setBusyId(null)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <FolderPlus className="size-4" aria-hidden />
            Пакеты корректировок
          </span>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void load()}
            aria-label="Обновить список пакетов"
            disabled={loading}
          >
            <RefreshCw className={cn("size-4", loading && "animate-spin")} />
          </Button>
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <div className="flex flex-wrap items-end gap-2">
          <label className="grid gap-1 text-sm font-medium">
            Дата корректировки
            <Input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-44"
            />
          </label>
          <Button onClick={() => void handleCreate()} disabled={creating}>
            {creating ? (
              <>
                <LoaderCircle className="size-4 animate-spin" aria-hidden />
                Создание...
              </>
            ) : (
              "Создать пакет"
            )}
          </Button>
        </div>

        <div className="grid gap-3 rounded-md border p-3 sm:grid-cols-[auto_1fr] sm:items-end">
          <div className="grid gap-1.5">
            <span className="text-xs font-medium text-muted-foreground">
              Статус
            </span>
            <div
              className="flex flex-wrap gap-1"
              role="group"
              aria-label="Фильтр по статусу пакета"
            >
              {STATUS_FILTERS.map(({ key, label }) => (
                <Button
                  key={key}
                  variant={statusFilter === key ? "secondary" : "ghost"}
                  size="sm"
                  aria-pressed={statusFilter === key}
                  onClick={() => {
                    setStatusFilter(key)
                    setPage(1)
                  }}
                >
                  {label}
                </Button>
              ))}
            </div>
          </div>

          <div className="flex flex-wrap items-end gap-2">
            <label className="grid gap-1 text-sm font-medium">
              Период с
              <Input
                type="date"
                value={from}
                max={to || undefined}
                onChange={(e) => {
                  setFrom(e.target.value)
                  setPage(1)
                }}
                className="w-40"
              />
            </label>
            <label className="grid gap-1 text-sm font-medium">
              по
              <Input
                type="date"
                value={to}
                min={from || undefined}
                onChange={(e) => {
                  setTo(e.target.value)
                  setPage(1)
                }}
                className="w-40"
              />
            </label>
            <Button
              variant="ghost"
              size="sm"
              onClick={resetFilters}
              disabled={!hasFilters}
            >
              <RotateCcw className="size-4" aria-hidden />
              Сбросить
            </Button>
          </div>
        </div>

        {loading && batches.length === 0 ? (
          <BatchListSkeleton />
        ) : error ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive"
          >
            <span className="flex items-center gap-2">
              <CircleAlert className="size-4 shrink-0" aria-hidden />
              {error}
            </span>
            <Button variant="outline" size="sm" onClick={() => void load()}>
              Повторить
            </Button>
          </div>
        ) : batches.length === 0 ? (
          <EmptyState
            message={
              hasFilters
                ? "По выбранным фильтрам пакетов не найдено. Измените статус или период."
                : "Пакетов пока нет. Создайте пакет по дате или импортируйте XLSX на вкладке «Импорт»."
            }
          />
        ) : (
          <>
            <div className="hidden overflow-x-auto rounded-md border md:block">
              <table className="w-full min-w-[860px] text-sm">
                <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 text-left">Дата</th>
                    <th className="px-3 py-2 text-left">Неделя / день</th>
                    <th className="px-3 py-2 text-left">Позиций</th>
                    <th className="px-3 py-2 text-left">Ошибки</th>
                    <th className="px-3 py-2 text-left">Статус</th>
                    <th className="px-3 py-2 text-right">Действия</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {batches.map((batch) => {
                    const meta = STATUS_META[batch.status]
                    const busy = busyId === batch.id
                    const blocked = applyBlockReason(batch)
                    return (
                      <tr key={batch.id}>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className="flex items-center gap-1.5">
                            <CalendarDays
                              className="size-4 text-muted-foreground"
                              aria-hidden
                            />
                            {formatDate(batch.correctionDate)}
                          </span>
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap text-muted-foreground">
                          {batch.week} неделя ·{" "}
                          {DAYS.find((d) => d.value === batch.dayOfWeek)?.full ??
                            batch.dayOfWeek}
                        </td>
                        <td className="px-3 py-2">{batch.positionCount}</td>
                        <td className="px-3 py-2">
                          {batch.errors.length > 0 ? (
                            <span className="inline-flex items-center gap-1 text-destructive">
                              <CircleAlert className="size-3.5" aria-hidden />
                              {batch.errors.length}
                            </span>
                          ) : (
                            <span className="text-muted-foreground">—</span>
                          )}
                        </td>
                        <td className="px-3 py-2">
                          <Badge variant="outline" className={meta.className}>
                            {meta.label}
                          </Badge>
                        </td>
                        <td className="px-3 py-2">
                          <div className="flex justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => onOpen(batch.id)}
                              aria-label={`Открыть пакет за ${formatDate(batch.correctionDate)}`}
                            >
                              <Eye className="size-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              disabled={busy}
                              onClick={() => void handleExport(batch)}
                              aria-label={`Скачать XLSX за ${formatDate(batch.correctionDate)}`}
                            >
                              <Download className="size-4" />
                            </Button>
                            {batch.status === "Draft" && (
                              <>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  disabled={busy || !canApply(batch)}
                                  title={blocked}
                                  onClick={() => void handleApply(batch)}
                                  aria-label={`Применить пакет за ${formatDate(batch.correctionDate)}`}
                                >
                                  <Play className="size-4 text-emerald-600 dark:text-emerald-400" />
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  disabled={busy}
                                  onClick={() => void handleDelete(batch)}
                                  aria-label={`Удалить пакет за ${formatDate(batch.correctionDate)}`}
                                >
                                  <Trash2 className="size-4 text-destructive" />
                                </Button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            <ul className="grid gap-3 md:hidden">
              {batches.map((batch) => {
                const meta = STATUS_META[batch.status]
                const blocked = applyBlockReason(batch)
                const busy = busyId === batch.id
                return (
                  <li
                    key={batch.id}
                    className="grid gap-3 rounded-md border p-3"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="flex items-center gap-2 text-sm font-medium">
                        <CalendarDays
                          className="size-4 text-muted-foreground"
                          aria-hidden
                        />
                        {formatDate(batch.correctionDate)}
                      </span>
                      <Badge variant="outline" className={meta.className}>
                        {meta.label}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {batch.week} неделя ·{" "}
                      {DAYS.find((d) => d.value === batch.dayOfWeek)?.full ??
                        batch.dayOfWeek}{" "}
                      · позиций: {batch.positionCount}
                      {batch.errors.length > 0 && (
                        <span className="text-destructive">
                          {" "}
                          · ошибок: {batch.errors.length}
                        </span>
                      )}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        variant="outline"
                        className="h-11 flex-1"
                        onClick={() => onOpen(batch.id)}
                      >
                        <Eye className="size-4" aria-hidden />
                        Открыть
                      </Button>
                      <Button
                        variant="outline"
                        className="h-11 flex-1"
                        disabled={busy}
                        onClick={() => void handleExport(batch)}
                      >
                        <Download className="size-4" aria-hidden />
                        XLSX
                      </Button>
                      {batch.status === "Draft" && (
                        <>
                          <Button
                            className="h-11 flex-1"
                            disabled={busy || !canApply(batch)}
                            title={blocked}
                            onClick={() => void handleApply(batch)}
                          >
                            <Play className="size-4" aria-hidden />
                            Применить
                          </Button>
                          <Button
                            variant="outline"
                            className="h-11 text-destructive"
                            disabled={busy}
                            onClick={() => void handleDelete(batch)}
                          >
                            <Trash2 className="size-4" aria-hidden />
                            Удалить
                          </Button>
                        </>
                      )}
                    </div>
                  </li>
                )
              })}
            </ul>

            <div className="flex flex-wrap items-center justify-between gap-2">
              <p className="text-xs text-muted-foreground">
                Стр. {page} из {totalPages} · всего {totalCount}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1 || loading}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  ← Назад
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages || loading}
                  onClick={() =>
                    setPage((current) => Math.min(totalPages, current + 1))
                  }
                >
                  Далее →
                </Button>
              </div>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  )
}

function BatchListSkeleton() {
  return (
    <div
      className="grid gap-2"
      role="status"
      aria-label="Загрузка пакетов"
    >
      {Array.from({ length: 4 }).map((_, index) => (
        <div
          key={index}
          className="h-12 animate-pulse rounded-md bg-muted"
        />
      ))}
    </div>
  )
}
