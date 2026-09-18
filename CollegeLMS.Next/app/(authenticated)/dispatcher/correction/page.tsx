"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { toast } from "sonner"
import type { LucideIcon } from "lucide-react"
import {
  Upload,
  FileSpreadsheet,
  AlertCircle,
  History,
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
  RefreshCw,
  ArrowLeft,
  Package,
} from "lucide-react"
import { importCorrection, getHistory } from "@/api/correction"
import { extractErrorMessage } from "@/lib/utils"
import { DAYS } from "@/types/schedule"
import type {
  CorrectionChangeType,
  CorrectionImportResponse,
  ScheduleHistoryItem,
  ScheduleValidationError,
} from "@/types/correction"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import CorrectionBatchList from "@/components/CorrectionBatchList"
import CorrectionPositionEditor from "@/components/CorrectionPositionEditor"

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: {
    label: "Добавлено",
    icon: Plus,
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    icon: Minus,
    className:
      "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    icon: Repeat,
    className:
      "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    icon: ArrowRightLeft,
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

const DAY_RU: Record<string, string> = {
  Monday: "Понедельник",
  Tuesday: "Вторник",
  Wednesday: "Среда",
  Thursday: "Четверг",
  Friday: "Пятница",
  Saturday: "Суббота",
  Sunday: "Воскресенье",
}

function dayLabelFromString(dayOfWeek: string): string {
  return DAY_RU[dayOfWeek] ?? dayOfWeek
}

function ChangeTypeBadge({ type }: { type: CorrectionChangeType }) {
  const meta = CHANGE_TYPE_META[type]
  const Icon = meta.icon
  return (
    <Badge variant="outline" className={meta.className}>
      <Icon /> {meta.label}
    </Badge>
  )
}

function formatValidationError(error: ScheduleValidationError): string {
  const message = error.message ?? ""
  if (/^Строка\s+\d+/i.test(message)) return message
  const row = error.row ? `Строка ${error.row}` : ""
  return row ? `${row}: ${message}` : message
}

type Tab = "batches" | "editor" | "import" | "journal"

const TABS: { key: Tab; label: string; icon: LucideIcon }[] = [
  { key: "batches", label: "Пакеты", icon: Package },
  { key: "editor", label: "Редактор", icon: FileSpreadsheet },
  { key: "import", label: "Импорт", icon: Upload },
  { key: "journal", label: "Журнал", icon: History },
]

export default function DispatcherCorrectionPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [tab, setTab] = useState<Tab>("batches")
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)

  const [file, setFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)
  const [importResult, setImportResult] =
    useState<CorrectionImportResponse | null>(null)

  const [history, setHistory] = useState<ScheduleHistoryItem[]>([])
  const [historyPage, setHistoryPage] = useState(1)
  const [historyTotalPages, setHistoryTotalPages] = useState(1)
  const [weekFilter, setWeekFilter] = useState("")
  const [loadingHistory, setLoadingHistory] = useState(false)

  const loadHistory = useCallback(
    async (page: number) => {
      setLoadingHistory(true)
      try {
        const week = weekFilter ? Number(weekFilter) : undefined
        const res = await getHistory({
          week,
          page,
          pageSize: 20,
        })
        setHistory(res.items)
        setHistoryPage(res.page)
        setHistoryTotalPages(res.totalPages)
      } catch (err) {
        toast.error(extractErrorMessage(err) ?? "Ошибка загрузки журнала")
      } finally {
        setLoadingHistory(false)
      }
    },
    [weekFilter],
  )

  useEffect(() => {
    if (tab === "journal") loadHistory(1)
  }, [tab, loadHistory])

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (f) {
      if (!f.name.endsWith(".xlsx")) {
        toast.error("Поддерживается только формат XLSX")
        return
      }
      if (f.size > 10 * 1024 * 1024) {
        toast.error("Файл слишком большой. Максимум 10MB")
        return
      }
      setFile(f)
      setImportResult(null)
    }
  }

  const handleImport = async () => {
    if (!file) return
    setImporting(true)
    try {
      const result = await importCorrection(file)
      if (result.batchId) {
        // Строчные ошибки не мешают созданию пакета — их покажет редактор.
        if (result.errors.length > 0) {
          toast.error(`Пакет создан с ошибками (${result.errors.length})`, {
            description: "Открываем редактор — исправьте позиции и примените пакет",
            duration: 6000,
          })
        } else {
          toast.success(`Импортировано позиций: ${result.totalEntries}`)
        }
        setFile(null)
        setImportResult(null)
        if (fileInputRef.current) fileInputRef.current.value = ""
        setSelectedBatchId(result.batchId)
        setRefreshKey((k) => k + 1)
        setTab("editor")
      } else {
        setImportResult(result)
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка импорта")
    } finally {
      setImporting(false)
    }
  }

  const openBatch = (id: string) => {
    setSelectedBatchId(id)
    setTab("editor")
  }

  const handleEditorExit = () => {
    setRefreshKey((k) => k + 1)
    setSelectedBatchId(null)
    setTab("batches")
  }

  const renderJournal = () => (
    <Card>
      <CardHeader>
        <CardTitle className="text-base flex items-center justify-between gap-2">
          <span className="flex items-center gap-2">
            <History className="size-4" />
            Журнал изменений
          </span>
          <div className="flex items-center gap-2">
            <Input
              type="number"
              min={1}
              placeholder="Неделя"
              value={weekFilter}
              onChange={(e) => setWeekFilter(e.target.value)}
              className="w-28"
              aria-label="Фильтр по неделе"
            />
            <Button
              variant="outline"
              size="icon"
              onClick={() => loadHistory(1)}
              aria-label="Обновить журнал"
            >
              <RefreshCw
                className={`size-4 ${loadingHistory ? "animate-spin" : ""}`}
              />
            </Button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        {history.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Журнал пуст.
          </p>
        ) : (
          <>
            <div className="overflow-x-auto rounded-md border">
              <table className="w-full min-w-[820px] text-sm">
                <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2 text-left">Дата</th>
                    <th className="px-3 py-2 text-left">Тип</th>
                    <th className="px-3 py-2 text-left">Группа</th>
                    <th className="px-3 py-2 text-left">День</th>
                    <th className="px-3 py-2 text-left">Пара</th>
                    <th className="px-3 py-2 text-left">Предмет</th>
                    <th className="px-3 py-2 text-left">Преподаватель</th>
                    <th className="px-3 py-2 text-left">Примечание</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {history.map((item) => (
                    <tr key={item.id}>
                      <td className="px-3 py-2 whitespace-nowrap text-muted-foreground">
                        {new Date(item.appliedAt).toLocaleString("ru-RU")}
                      </td>
                      <td className="px-3 py-2">
                        <ChangeTypeBadge type={item.changeType} />
                      </td>
                      <td className="px-3 py-2 whitespace-nowrap">
                        {item.groupName}
                      </td>
                      <td className="px-3 py-2 whitespace-nowrap">
                        {dayLabelFromString(item.dayOfWeek)}
                      </td>
                      <td className="px-3 py-2">
                        {item.removedNumberPair != null &&
                          (item.changeType === "Replace" ||
                            item.changeType === "Move") &&
                          item.removedNumberPair !== item.numberPair
                          ? `${item.removedNumberPair} → ${item.numberPair}`
                          : item.numberPair}
                      </td>
                      <td className="px-3 py-2">
                        {(item.changeType === "Replace" ||
                          item.changeType === "Move") &&
                          item.removedSubject ? (
                          <span className="flex items-center gap-1">
                            <span className="line-through text-muted-foreground">
                              {item.removedSubject}
                            </span>
                            <ArrowLeft className="size-3 rotate-180 text-muted-foreground" />
                            <span className="font-medium">
                              {item.subject}
                            </span>
                          </span>
                        ) : (
                          item.subject
                        )}
                      </td>
                      <td className="px-3 py-2 max-w-[220px] truncate">
                        {item.teacherName ?? "—"}
                      </td>
                      <td className="px-3 py-2 max-w-[200px] truncate text-muted-foreground">
                        {item.note ?? "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex items-center justify-between">
              <p className="text-xs text-muted-foreground">
                Стр. {historyPage} из {historyTotalPages}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={historyPage <= 1}
                  onClick={() => loadHistory(historyPage - 1)}
                >
                  ← Назад
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={historyPage >= historyTotalPages}
                  onClick={() => loadHistory(historyPage + 1)}
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

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-6xl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">Корректировка расписания</h2>
        <div className="inline-flex rounded-lg border bg-card p-1">
          {TABS.map(({ key, label, icon: Icon }) => (
            <Button
              key={key}
              variant={tab === key ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setTab(key)}
              disabled={key === "editor" && !selectedBatchId}
            >
              <Icon className="size-4 mr-2" />
              {label}
            </Button>
          ))}
        </div>
      </div>

      {tab === "batches" && (
        <CorrectionBatchList onOpen={openBatch} refreshKey={refreshKey} />
      )}

      {tab === "editor" &&
        (selectedBatchId ? (
          <CorrectionPositionEditor
            batchId={selectedBatchId}
            onBack={handleEditorExit}
            onApplied={handleEditorExit}
          />
        ) : (
          <Card>
            <CardContent className="py-8 text-center text-sm text-muted-foreground">
              Откройте пакет из списка, чтобы редактировать его позиции.
            </CardContent>
          </Card>
        ))}

      {tab === "import" && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <FileSpreadsheet className="size-4" /> Импорт корректировок из XLSX
            </CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div
              className="flex flex-col items-center gap-3 rounded-lg border-2 border-dashed p-8 text-center cursor-pointer hover:bg-muted/50 transition-colors"
              onClick={() => fileInputRef.current?.click()}
            >
              {file ? (
                <>
                  <FileSpreadsheet className="size-10 text-accent" />
                  <div>
                    <p className="font-medium">{file.name}</p>
                    <p className="text-sm text-muted-foreground">
                      {(file.size / 1024).toFixed(1)} KB
                    </p>
                  </div>
                </>
              ) : (
                <>
                  <Upload className="size-10 text-muted-foreground" />
                  <div>
                    <p className="font-medium">Нажмите для выбора файла</p>
                    <p className="text-sm text-muted-foreground">
                      XLSX, до 10MB
                    </p>
                  </div>
                </>
              )}
              <input
                ref={fileInputRef}
                type="file"
                accept=".xlsx"
                className="hidden"
                onChange={handleFileChange}
              />
            </div>

            {importResult && (
              <div className="rounded-md border border-destructive/40 bg-destructive/5 p-4 grid gap-2">
                <p className="flex items-center gap-1 text-sm font-semibold text-destructive">
                  <AlertCircle className="size-3" aria-hidden />
                  Пакет не создан: структурные ошибки ({importResult.errors.length}
                  )
                </p>
                <p className="text-xs text-muted-foreground">
                  Не удалось определить дату, шапку или данные файла. Исправьте
                  файл и повторите импорт.
                </p>
                <div className="max-h-40 overflow-y-auto text-xs space-y-1">
                  {importResult.errors.map((err, i) => (
                    <p key={i} className="text-muted-foreground">
                      {formatValidationError(err)}
                    </p>
                  ))}
                </div>
              </div>
            )}

            <Button
              onClick={() => void handleImport()}
              disabled={!file || importing}
              className="w-full"
            >
              {importing
                ? "Импорт..."
                : file
                  ? "Создать пакет из файла"
                  : "Импорт"}
            </Button>
            {!importResult && file && (
              <p className="text-xs text-muted-foreground">
                После импорта будет создан пакет — вы сможете отредактировать
                позиции и применить его.
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {tab === "journal" && renderJournal()}
    </div>
  )
}