"use client"

import { PageTitle } from "@/components/ui/heading"
import { useRef, useState } from "react"
import { toast } from "sonner"
import type { LucideIcon } from "lucide-react"
import {
  Upload,
  FileSpreadsheet,
  AlertCircle,
  BookMarked,
  Package,
} from "lucide-react"
import { importCorrection } from "@/api/correction"
import { extractErrorMessage } from "@/lib/utils"
import type {
  CorrectionImportResponse,
  ScheduleValidationError,
} from "@/types/correction"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import CorrectionBatchList from "@/components/CorrectionBatchList"
import CorrectionPositionEditor from "@/components/CorrectionPositionEditor"
import { CorrectionRules } from "@/components/CorrectionRules"

function formatValidationError(error: ScheduleValidationError): string {
  const message = error.message ?? ""
  if (/^Строка\s+\d+/i.test(message)) return message
  const row = error.row ? `Строка ${error.row}` : ""
  return row ? `${row}: ${message}` : message
}

// Журнала здесь нет: просмотр изменений живёт на отдельной странице «Изменения».
type Tab = "batches" | "editor" | "import"

const TABS: { key: Tab; label: string; icon: LucideIcon }[] = [
  { key: "batches", label: "Пакеты", icon: Package },
  { key: "editor", label: "Редактор", icon: FileSpreadsheet },
  { key: "import", label: "Импорт", icon: Upload },
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

  const [rulesOpen, setRulesOpen] = useState(false)

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

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-6xl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <PageTitle>Корректировка расписания</PageTitle>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setRulesOpen(true)}
          >
            <BookMarked className="size-4" aria-hidden />
            Правила
          </Button>
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
                <p className="flex items-center gap-1 text-sm font-semibold text-destructive-text">
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

      <CorrectionRules open={rulesOpen} onOpenChange={setRulesOpen} />
    </div>
  )
}