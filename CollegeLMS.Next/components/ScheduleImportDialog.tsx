"use client"

import { useState, useRef } from "react"
import { toast } from "sonner"
import { importSchedule, type ScheduleImportResult } from "@/api/schedule"
import { extractErrorMessage } from "@/lib/utils"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Upload, FileSpreadsheet, AlertCircle, CheckCircle, X } from "lucide-react"

interface ScheduleImportDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onImported: () => void
}

export default function ScheduleImportDialog({
  open,
  onOpenChange,
  onImported,
}: ScheduleImportDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)
  const [result, setResult] = useState<ScheduleImportResult | null>(null)

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
      setResult(null)
    }
  }

  const handleImport = async () => {
    if (!file) return
    setImporting(true)
    setResult(null)
    try {
      const response = await importSchedule(file)
      if (response.isSuccess && response.data) {
        setResult(response.data)
        if (response.data.imported > 0) {
          toast.success(`Импортировано: ${response.data.imported}`)
          onImported()
        }
      } else {
        toast.error(response.errorMessage ?? "Ошибка импорта")
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка импорта")
    } finally {
      setImporting(false)
    }
  }

  const handleClose = () => {
    setFile(null)
    setResult(null)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Импорт расписания</DialogTitle>
          <DialogDescription>
            Загрузите XLSX-файл с расписанием.
            <br />
            Колонки: Группа, Преподаватель, Предмет, Аудитория, День, Начало, Конец, Тип занятия.
          </DialogDescription>
        </DialogHeader>

        {!result ? (
          <div className="grid gap-4">
            <div
              className="flex flex-col items-center gap-3 rounded-lg border-2 border-dashed p-8 text-center cursor-pointer hover:bg-muted/50 transition-colors"
              onClick={() => fileInputRef.current?.click()}
            >
              {file ? (
                <>
                  <FileSpreadsheet className="size-10 text-college-navy" />
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

            <Button
              onClick={handleImport}
              disabled={!file || importing}
              className="w-full"
            >
              {importing ? "Импорт..." : "Импортировать"}
            </Button>
          </div>
        ) : (
          <div className="grid gap-4">
            <div className="flex items-center gap-2 rounded-md bg-green-50 dark:bg-green-950/20 p-3 text-sm text-green-700 dark:text-green-400">
              <CheckCircle className="size-4 shrink-0" />
              Импорт завершён
            </div>
            <div className="grid grid-cols-2 gap-3 text-center">
              <div className="rounded-lg border bg-card p-3">
                <p className="text-2xl font-bold text-green-600">{result.imported}</p>
                <p className="text-xs text-muted-foreground">Импортировано</p>
              </div>
              <div className="rounded-lg border bg-card p-3">
                <p className={`text-2xl font-bold ${result.skipped > 0 ? "text-amber-600" : "text-green-600"}`}>
                  {result.skipped}
                </p>
                <p className="text-xs text-muted-foreground">Пропущено</p>
              </div>
            </div>
            {result.errors.length > 0 && (
              <div className="max-h-40 overflow-y-auto rounded-md border p-3 text-xs">
                <p className="mb-2 font-semibold text-destructive flex items-center gap-1">
                  <AlertCircle className="size-3" />
                  Ошибки ({result.errors.length})
                </p>
                {result.errors.map((err, i) => (
                  <p key={i} className="text-muted-foreground">
                    Строка {err.row}: {err.message}
                  </p>
                ))}
              </div>
            )}
            <Button variant="outline" onClick={handleClose} className="w-full">
              Закрыть
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
