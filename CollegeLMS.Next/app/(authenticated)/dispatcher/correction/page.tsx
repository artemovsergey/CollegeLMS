"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { toast } from "sonner"
import type { LucideIcon } from "lucide-react"
import {
  Upload,
  FileSpreadsheet,
  AlertCircle,
  CheckCircle,
  Eye,
  ArrowLeft,
  History,
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
  RefreshCw,
} from "lucide-react"
import {
  previewCorrection,
  confirmCorrection,
  getHistory,
} from "@/api/correction"
import { extractErrorMessage } from "@/lib/utils"
import { DAYS } from "@/types/schedule"
import type {
  CorrectionChangeType,
  CorrectionPreviewResponse,
  CorrectionPreviewEntry,
  ScheduleHistoryItem,
} from "@/types/correction"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: {
    label: "Добавлено",
    icon: Plus,
    className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    icon: Minus,
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    icon: Repeat,
    className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    icon: ArrowRightLeft,
    className: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
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

function dayLabelFromInt(dayOfWeek: number): string {
  return DAYS.find(d => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
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

type Tab = "import" | "journal"

export default function DispatcherCorrectionPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [tab, setTab] = useState<Tab>("import")
  const [file, setFile] = useState<File | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [preview, setPreview] = useState<CorrectionPreviewResponse | null>(null)
  const [appliedCount, setAppliedCount] = useState<number | null>(null)

  const [history, setHistory] = useState<ScheduleHistoryItem[]>([])
  const [historyPage, setHistoryPage] = useState(1)
  const [historyTotalPages, setHistoryTotalPages] = useState(1)
  const [weekFilter, setWeekFilter] = useState("")
  const [loadingHistory, setLoadingHistory] = useState(false)

  const loadHistory = useCallback(async (page: number) => {
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
  }, [weekFilter])

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
      setPreview(null)
      setAppliedCount(null)
    }
  }

  const handlePreview = async () => {
    if (!file) return
    setPreviewing(true)
    try {
      const result = await previewCorrection(file)
      setPreview(result)
      setAppliedCount(null)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка превью")
    } finally {
      setPreviewing(false)
    }
  }

  const handleConfirm = async () => {
    if (!preview) return
    setConfirming(true)
    try {
      const result = await confirmCorrection(preview.entries)
      toast.success(`Применено изменений: ${result.applied}`)
      setAppliedCount(result.applied)
      setPreview(null)
      setFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ""
      if (tab === "journal") loadHistory(1)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка применения")
    } finally {
      setConfirming(false)
    }
  }

  const hasErrors = (preview?.errors.length ?? 0) > 0

  const renderTitle = (entry: CorrectionPreviewEntry) => {
    if (entry.changeType === "Replace" || entry.changeType === "Move") {
      return (
        <span className="flex items-center gap-1">
          <span className="line-through text-muted-foreground">
            {entry.removedSubject ?? "—"}
          </span>
          <ArrowLeft className="size-3 rotate-180 text-muted-foreground" />
          <span className="font-medium">{entry.subject}</span>
        </span>
      )
    }
    if (entry.changeType === "Remove") {
      return (
        <span className="line-through text-muted-foreground">
          {entry.removedSubject ?? "—"}
        </span>
      )
    }
    return <span className="font-medium">{entry.subject ?? "—"}</span>
  }

  const renderTeacher = (entry: CorrectionPreviewEntry) => {
    if (entry.changeType === "Replace" || entry.changeType === "Move") {
      const from = entry.removedTeacherName
      const to = entry.teacherName
      if (from && to)
        return (
          <span className="flex items-center gap-1">
            <span className="line-through text-muted-foreground">{from}</span>
            <ArrowLeft className="size-3 rotate-180 text-muted-foreground" />
            <span className="font-medium">{to}</span>
          </span>
        )
      return to ?? from ?? "—"
    }
    return (
      entry.teacherName ??
      (entry.changeType === "Remove" ? entry.removedTeacherName : null) ??
      "—"
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-6xl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">Корректировка расписания</h2>
        <div className="inline-flex rounded-lg border bg-card p-1">
          <Button
            variant={tab === "import" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setTab("import")}
          >
            <Upload className="size-4 mr-2" />
            Импорт
          </Button>
          <Button
            variant={tab === "journal" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setTab("journal")}
          >
            <History className="size-4 mr-2" />
            Журнал
          </Button>
        </div>
      </div>

      {appliedCount !== null && (
        <div className="flex items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300">
          <CheckCircle className="size-4 shrink-0" />
          Изменения применены: {appliedCount}
        </div>
      )}

      {tab === "import" ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <FileSpreadsheet className="size-4" /> Импорт корректировок из XLSX
            </CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4">
            {!preview ? (
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
            ) : (
              <>
                <div className="grid grid-cols-3 gap-2 text-center">
                  <div className="rounded-lg border bg-card p-3">
                    <p className="text-2xl font-bold">{preview.totalEntries}</p>
                    <p className="text-xs text-muted-foreground">
                      Всего записей
                    </p>
                  </div>
                  <div className="rounded-lg border bg-card p-3">
                    <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400">
                      {preview.entries.length}
                    </p>
                    <p className="text-xs text-muted-foreground">Валидных</p>
                  </div>
                  <div className="rounded-lg border bg-card p-3">
                    <p
                      className={`text-2xl font-bold ${hasErrors
                          ? "text-destructive"
                          : "text-emerald-600 dark:text-emerald-400"
                        }`}
                    >
                      {preview.errors.length}
                    </p>
                    <p className="text-xs text-muted-foreground">Ошибок</p>
                  </div>
                </div>

                {hasErrors && (
                  <div className="max-h-40 overflow-y-auto rounded-md border p-3 text-xs space-y-2">
                    <p className="font-semibold flex items-center gap-1 text-destructive">
                      <AlertCircle className="size-3" />
                      Ошибки ({preview.errors.length})
                    </p>
                    {preview.errors.map((err, i) => (
                      <p key={i} className="text-muted-foreground">
                        Строка {err.row}, стлб. {err.column}: {err.message}
                      </p>
                    ))}
                  </div>
                )}

                <div className="overflow-x-auto rounded-md border">
                  <table className="w-full min-w-[720px] text-sm">
                    <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                      <tr>
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
                      {preview.entries.map((entry, i) => (
                        <tr key={`${entry.row}-${i}`}>
                          <td className="px-3 py-2">
                            <ChangeTypeBadge type={entry.changeType} />
                          </td>
                          <td className="px-3 py-2 whitespace-nowrap">
                            {entry.groupName}
                          </td>
                          <td className="px-3 py-2 whitespace-nowrap">
                            {dayLabelFromInt(entry.dayOfWeek)}
                          </td>
                          <td className="px-3 py-2 whitespace-nowrap">
                            {entry.removedNumberPair != null &&
                              entry.removedNumberPair !== entry.numberPair
                              ? `${entry.removedNumberPair} → ${entry.numberPair}`
                              : entry.numberPair}
                          </td>
                          <td className="px-3 py-2">{renderTitle(entry)}</td>
                          <td className="px-3 py-2 max-w-[220px] truncate">
                            {renderTeacher(entry)}
                          </td>
                          <td className="px-3 py-2 max-w-[200px] truncate text-muted-foreground">
                            {entry.note ?? "—"}
                          </td>
                        </tr>
                      ))}
                      {preview.entries.length === 0 && (
                        <tr>
                          <td
                            colSpan={7}
                            className="px-3 py-8 text-center text-muted-foreground"
                          >
                            Нет валидных записей
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>

                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setPreview(null)
                      setAppliedCount(null)
                    }}
                    className="flex-1"
                  >
                    <ArrowLeft className="size-4 mr-2" />
                    Назад
                  </Button>
                  <Button
                    onClick={handleConfirm}
                    disabled={confirming || hasErrors || preview.entries.length === 0}
                    className="flex-1"
                  >
                    {confirming ? "Применение..." : "Применить"}
                  </Button>
                </div>
              </>
            )}

            {!preview && (
              <Button
                onClick={handlePreview}
                disabled={!file || previewing}
                className="w-full"
              >
                {previewing ? "Загрузка..." : (
                  <>
                    <Eye className="size-4 mr-2" />
                    Просмотр
                  </>
                )}
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
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
                  onChange={e => setWeekFilter(e.target.value)}
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
                      {history.map(item => (
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
                              (item.changeType === "Replace" || item.changeType === "Move") &&
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
      )}
    </div>
  )
}