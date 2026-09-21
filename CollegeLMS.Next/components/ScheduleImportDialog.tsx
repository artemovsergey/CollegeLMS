"use client"

import { useState, useRef } from "react"
import { toast } from "sonner"
import {
  previewScheduleImport,
  confirmScheduleImport,
  ScheduleImportError,
  type SchedulePreviewResult,
  type ScheduleImportResult,
  type ScheduleValidationError,
} from "@/api/schedule"
import { extractErrorMessage } from "@/lib/utils"
import {
  NativeDialog,
  NativeDialogHeader,
  NativeDialogTitle,
  NativeDialogDescription,
  NativeDialogFooter,
  NativeDialogClose,
} from "@/components/ui/native-dialog"
import { Button } from "@/components/ui/button"
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
  const [confirmResult, setConfirmResult] = useState<ScheduleImportResult | null>(null)
  const [confirmErrors, setConfirmErrors] = useState<ScheduleValidationError[]>([])

  const reset = () => {
    setStep("upload")
    setFile(null)
    setPreview(null)
    setConfirmResult(null)
    setConfirmErrors([])
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
      setConfirmErrors([])
    }
  }

  const handlePreview = async () => {
    if (!file) return
    setPreviewing(true)
    try {
      const response = await previewScheduleImport(file)
      if (response.isSuccess && response.data) {
        setPreview(response.data)
        setConfirmErrors([])
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
      const result = await confirmScheduleImport(preview.entries)
      if (result.isSuccess) {
        setConfirmResult(result)
        setConfirmErrors(result.errors)
        setStep("result")
        toast.success(`Загружено пар: ${result.imported}`)
        onImported()
      } else {
        setConfirmErrors(result.errors)
        toast.error(result.errors[0]?.message ?? "Ошибка импорта")
      }
    } catch (err) {
      if (err instanceof ScheduleImportError) {
        setConfirmErrors(err.result.errors)
        toast.error(err.message)
      } else {
        toast.error(extractErrorMessage(err) ?? "Ошибка импорта")
      }
    } finally {
      setConfirming(false)
    }
  }

  const importErrors = [...(preview?.errors ?? []), ...confirmErrors]

  return (
    <NativeDialog open={open} onOpenChange={handleClose} className="sm:max-w-lg w-full">
      <NativeDialogClose onClick={handleClose} />
      <NativeDialogHeader>
        <NativeDialogTitle>Импорт расписания</NativeDialogTitle>
        <NativeDialogDescription>
          Загрузите XLSX-файл расписания колледжа.
        </NativeDialogDescription>
      </NativeDialogHeader>

      <div className="p-6">
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
                <p className="text-2xl font-bold text-success">{preview.entries.length}</p>
                <p className="text-xs text-muted-foreground">Валидных</p>
              </div>
              <div className="rounded-lg border bg-card p-3">
                <p className={`text-2xl font-bold ${importErrors.length > 0 ? "text-destructive" : "text-success"}`}>
                  {importErrors.length}
                </p>
                <p className="text-xs text-muted-foreground">Ошибок</p>
              </div>
            </div>

            {importErrors.length > 0 && (
              <div className="max-h-40 overflow-y-auto rounded-md border p-3 text-xs space-y-2">
                <p className="font-semibold flex items-center gap-1 text-destructive">
                  <AlertCircle className="size-3" />
                  Ошибки ({importErrors.length})
                </p>
                {importErrors.map((err, i) => (
                  <p key={i} className="text-muted-foreground">
                    {err.message}
                  </p>
                ))}
              </div>
            )}

            <div className="flex gap-2">
              <Button variant="outline" onClick={() => { reset(); }} className="flex-1">
                <ArrowLeft className="size-4 mr-2" />
                Назад
              </Button>
              <Button
                onClick={handleConfirm}
                disabled={confirming || importErrors.length > 0}
                className="flex-1"
              >
                {confirming ? "Загрузка..." : "Загрузить"}
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
            <p className="text-center text-sm">
              Загружено пар:{" "}
              <span className="font-semibold text-success">{confirmResult.imported}</span>
              {" · "}Группы:{" "}
              <span className="font-semibold">{confirmResult.groups}</span>
              {" · "}Преподаватели:{" "}
              <span className="font-semibold">{confirmResult.teachers}</span>
            </p>
            <Button variant="outline" onClick={handleClose} className="w-full">
              Закрыть
            </Button>
          </div>
        )}
      </div>
    </NativeDialog>
  )
}
