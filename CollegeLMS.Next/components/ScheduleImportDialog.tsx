"use client"

import { useState, useRef } from "react"
import { toast } from "sonner"
import {
  previewScheduleImport,
  confirmScheduleImport,
  type SchedulePreviewResult,
  type SchedulePreviewEntry,
} from "@/api/schedule"
import { extractErrorMessage } from "@/lib/utils"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Check } from "lucide-react"
import {
  Upload,
  FileSpreadsheet,
  AlertCircle,
  CheckCircle,
  Eye,
  ArrowLeft,
} from "lucide-react"

interface ScheduleImportDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onImported: () => void
}

type Step = "upload" | "preview" | "result"

interface ConfirmResult {
  imported: number
  skipped: number
  errors: { row: number; message: string }[]
}

export default function ScheduleImportDialog({
  open,
  onOpenChange,
  onImported,
}: ScheduleImportDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [step, setStep] = useState<Step>("upload")
  const [file, setFile] = useState<File | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [preview, setPreview] = useState<SchedulePreviewResult | null>(null)
  const [confirmResult, setConfirmResult] = useState<ConfirmResult | null>(null)
  const [createMissingGroups, setCreateMissingGroups] = useState(false)
  const [createMissingTeachers, setCreateMissingTeachers] = useState(false)

  const reset = () => {
    setStep("upload")
    setFile(null)
    setPreview(null)
    setConfirmResult(null)
    setCreateMissingGroups(false)
    setCreateMissingTeachers(false)
  }

  const handleClose = () => {
    reset()
    onOpenChange(false)
  }

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
      setConfirmResult(null)
    }
  }

  const handlePreview = async () => {
    if (!file) return
    setPreviewing(true)
    try {
      const response = await previewScheduleImport(file)
      if (response.isSuccess && response.data) {
        setPreview(response.data)
        setStep("preview")
      } else {
        toast.error(response.errorMessage ?? "Ошибка превью")
      }
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
      const response = await confirmScheduleImport(preview.entries, {
        createMissingGroups,
        createMissingTeachers,
      })
      if (response.isSuccess && response.data) {
        setConfirmResult(response.data)
        setStep("result")
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
      setConfirming(false)
    }
  }

  const hasMissingGroups = preview?.warnings.some(w => w.type === "group_not_found") ?? false
  const hasMissingTeachers = preview?.warnings.some(w => w.type === "teacher_not_found") ?? false

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Импорт расписания</DialogTitle>
          <DialogDescription>
            Загрузите XLSX-файл расписания колледжа.
          </DialogDescription>
        </DialogHeader>

        {step === "upload" && (
          <div className="grid gap-4">
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
          </div>
        )}

        {step === "preview" && preview && (
          <div className="grid gap-4">
            <div className="grid grid-cols-3 gap-2 text-center">
              <div className="rounded-lg border bg-card p-3">
                <p className="text-2xl font-bold">{preview.totalEntries}</p>
                <p className="text-xs text-muted-foreground">Всего записей</p>
              </div>
              <div className="rounded-lg border bg-card p-3">
                <p className="text-2xl font-bold text-success">{preview.validEntries}</p>
                <p className="text-xs text-muted-foreground">Валидных</p>
              </div>
              <div className="rounded-lg border bg-card p-3">
                <p className={`text-2xl font-bold ${preview.warningsCount > 0 ? "text-warning" : "text-success"}`}>
                  {preview.warningsCount}
                </p>
                <p className="text-xs text-muted-foreground">Предупреждений</p>
              </div>
            </div>

            {preview.warnings.length > 0 && (
              <div className="rounded-md border p-3 text-xs space-y-2">
                <p className="font-semibold flex items-center gap-1 text-warning">
                  <AlertCircle className="size-3" />
                  Предупреждения
                </p>
                {preview.warnings.map((w, i) => (
                  <p key={i} className="text-muted-foreground">
                    {w.type === "group_not_found"
                      ? `Группы не найдены: ${w.value} (${w.count} записей)`
                      : `Преподаватели не найдены: ${w.value} (${w.count} записей)`}
                  </p>
                ))}
              </div>
            )}

            {hasMissingGroups && (
              <button
                type="button"
                onClick={() => setCreateMissingGroups(!createMissingGroups)}
                className="flex items-center gap-2 rounded-md border p-2 text-sm text-left hover:bg-muted/50 transition-colors"
              >
                <span className={`flex size-5 shrink-0 items-center justify-center rounded border ${createMissingGroups ? "bg-primary border-primary text-primary-foreground" : "border-input"}`}>
                  {createMissingGroups && <Check className="size-3" />}
                </span>
                Создать отсутствующие группы
              </button>
            )}

            {hasMissingTeachers && (
              <button
                type="button"
                onClick={() => setCreateMissingTeachers(!createMissingTeachers)}
                className="flex items-center gap-2 rounded-md border p-2 text-sm text-left hover:bg-muted/50 transition-colors"
              >
                <span className={`flex size-5 shrink-0 items-center justify-center rounded border ${createMissingTeachers ? "bg-primary border-primary text-primary-foreground" : "border-input"}`}>
                  {createMissingTeachers && <Check className="size-3" />}
                </span>
                Создать отсутствующих преподавателей
              </button>
            )}

            <div className="flex gap-2">
              <Button variant="outline" onClick={() => { reset(); }} className="flex-1">
                <ArrowLeft className="size-4 mr-2" />
                Назад
              </Button>
              <Button
                onClick={handleConfirm}
                disabled={confirming}
                className="flex-1"
              >
                {confirming ? "Импорт..." : "Импортировать"}
              </Button>
            </div>
          </div>
        )}

        {step === "result" && confirmResult && (
          <div className="grid gap-4">
            <div className="flex items-center gap-2 rounded-md bg-success/10 p-3 text-sm text-success">
              <CheckCircle className="size-4 shrink-0" />
              Импорт завершён
            </div>
            <div className="grid grid-cols-2 gap-3 text-center">
              <div className="rounded-lg border bg-card p-3">
                <p className="text-2xl font-bold text-success">{confirmResult.imported}</p>
                <p className="text-xs text-muted-foreground">Импортировано</p>
              </div>
              <div className="rounded-lg border bg-card p-3">
                <p className={`text-2xl font-bold ${confirmResult.skipped > 0 ? "text-warning" : "text-success"}`}>
                  {confirmResult.skipped}
                </p>
                <p className="text-xs text-muted-foreground">Пропущено</p>
              </div>
            </div>
            {confirmResult.errors.length > 0 && (
              <div className="max-h-40 overflow-y-auto rounded-md border p-3 text-xs">
                <p className="mb-2 font-semibold text-destructive flex items-center gap-1">
                  <AlertCircle className="size-3" />
                  Ошибки ({confirmResult.errors.length})
                </p>
                {confirmResult.errors.map((err, i) => (
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
