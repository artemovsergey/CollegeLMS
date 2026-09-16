"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import {
  CalendarDays,
  Download,
  Eye,
  FolderPlus,
  Play,
  RefreshCw,
  Trash2,
} from "lucide-react"
import {
  applyBatch,
  createBatch,
  deleteBatch,
  exportBatch,
  getBatches,
} from "@/api/correction"
import type { CorrectionBatch, CorrectionBatchStatus } from "@/types/correction"
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
import { Input } from "@/components/ui/input"
import EmptyState from "@/components/EmptyState"

const STATUS_META: Record<
  CorrectionBatchStatus,
  { label: string; className: string }
> = {
  Draft: {
    label: "Черновик",
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
    className:
      "bg-muted text-muted-foreground",
  },
}

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

interface CorrectionBatchListProps {
  onOpen: (batchId: string) => void
  refreshKey: number
}

export default function CorrectionBatchList({
  onOpen,
  refreshKey,
}: CorrectionBatchListProps) {
  const [batches, setBatches] = useState<CorrectionBatch[]>([])
  const [statusFilter, setStatusFilter] = useState<
    CorrectionBatchStatus | "All" | "Draft"
  >("All")
  const [loading, setLoading] = useState(false)
  const [date, setDate] = useState(todayIso())
  const [creating, setCreating] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      setBatches(await getBatches())
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка загрузки черновиков")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load, refreshKey])

  const handleCreate = async () => {
    if (!date) {
      toast.error("Укажите дату корректировки")
      return
    }
    setCreating(true)
    try {
      const batch = await createBatch(date)
      toast.success("Черновик создан")
      await load()
      onOpen(batch.id)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось создать черновик")
    } finally {
      setCreating(false)
    }
  }

  const handleDelete = async (batch: CorrectionBatch) => {
    if (!window.confirm(`Удалить черновик за ${formatDate(batch.correctionDate)}?`))
      return
    setBusyId(batch.id)
    try {
      await deleteBatch(batch.id)
      toast.success("Черновик удалён")
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить черновик")
    } finally {
      setBusyId(null)
    }
  }

  const handleExport = async (batch: CorrectionBatch) => {
    setBusyId(batch.id)
    try {
      const blob = await exportBatch(batch.id)
      downloadBlob(blob, `Корректировка_${batchIdSuffix(batch)}.xlsx`)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сформировать файл")
    } finally {
      setBusyId(null)
    }
  }

  const handleApply = async (batch: CorrectionBatch) => {
    if (!window.confirm(`Применить корректировки за ${formatDate(batch.correctionDate)}?`))
      return
    setBusyId(batch.id)
    try {
      const result = await applyBatch(batch.id, crypto.randomUUID())
      toast.success(`Применено изменений: ${result.applied}`)
      let blob: Blob | null = null
      try {
        blob = await exportBatch(batch.id)
      } catch {
        blob = null
      }
      if (blob) {
        downloadBlob(blob, `Корректировка_${batchIdSuffix(batch)}.xlsx`)
      }
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось применить корректировки")
    } finally {
      setBusyId(null)
    }
  }

  const filtered =
    statusFilter === "All"
      ? batches
      : batches.filter((batch) => batch.status === statusFilter)

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <FolderPlus className="size-4" />
            Черновики корректировок
          </span>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void load()}
            aria-label="Обновить"
          >
            <RefreshCw className={`size-4 ${loading ? "animate-spin" : ""}`} />
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
              className="w-48"
            />
          </label>
          <Button onClick={() => void handleCreate()} disabled={creating}>
            {creating ? "Создание..." : "Создать черновик"}
          </Button>
        </div>

        <div className="flex flex-wrap gap-1">
          {(
            [
              ["All", "Все"],
              ["Draft", "Черновики"],
              ["Applied", "Применённые"],
            ] as const
          ).map(([key, label]) => (
            <Button
              key={key}
              variant={statusFilter === key ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setStatusFilter(key)}
            >
              {label}
            </Button>
          ))}
        </div>

        {filtered.length === 0 ? (
          <EmptyState message="Черновиков пока нет. Создайте черновик по дате или импортируйте XLSX на вкладке «Импорт»." />
        ) : (
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full min-w-[760px] text-sm">
              <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 text-left">Дата</th>
                  <th className="px-3 py-2 text-left">Неделя / день</th>
                  <th className="px-3 py-2 text-left">Позиций</th>
                  <th className="px-3 py-2 text-left">Статус</th>
                  <th className="px-3 py-2 text-right">Действия</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {filtered.map((batch) => {
                  const meta = STATUS_META[batch.status]
                  const busy = busyId === batch.id
                  return (
                    <tr key={batch.id}>
                      <td className="px-3 py-2 whitespace-nowrap">
                        <span className="flex items-center gap-1.5">
                          <CalendarDays className="size-4 text-muted-foreground" />
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
                            aria-label="Открыть пакет"
                          >
                            <Eye className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={busy}
                            onClick={() => void handleExport(batch)}
                            aria-label="Скачать XLSX"
                          >
                            <Download className="size-4" />
                          </Button>
                          {batch.status === "Draft" && (
                            <>
                              <Button
                                variant="ghost"
                                size="icon"
                                disabled={busy || batch.positionCount === 0}
                                onClick={() => void handleApply(batch)}
                                aria-label="Применить пакет"
                              >
                                <Play className="size-4 text-emerald-600 dark:text-emerald-400" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                disabled={busy}
                                onClick={() => void handleDelete(batch)}
                                aria-label="Удалить пакет"
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
        )}
      </CardContent>
    </Card>
  )
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("ru-RU")
}

function batchIdSuffix(batch: CorrectionBatch): string {
  const stamp = new Date().toISOString().slice(0, 19).replace(/[T:]/g, "-")
  const datePart = (batch.correctionDate ?? "").slice(0, 10)
  return `${datePart}_${stamp}`
}