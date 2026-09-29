"use client"

import { useState } from "react"
import { LoaderCircle } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export interface CreateBatchDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Начальная дата: для создания — сегодня, для правки — дата пакета. */
  initialDate: string
  /** null — создание пакета, иначе меняется дата существующего. */
  batchId?: string | null
  onSubmit: (date: string) => Promise<void>
}

/**
 * Модальное окно создания пакета корректировки: дата выбирается здесь же,
 * а не в поле на странице, чтобы её нельзя было потерять из виду.
 */
export function CreateBatchDialog({
  open,
  onOpenChange,
  initialDate,
  batchId,
  onSubmit,
}: CreateBatchDialogProps) {
  const [date, setDate] = useState(initialDate)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Значение извне подставляем только при открытии окна.
  const [lastOpen, setLastOpen] = useState(false)
  if (open !== lastOpen) {
    setLastOpen(open)
    if (open) {
      setDate(initialDate)
      setError(null)
    }
  }

  const handleSubmit = async () => {
    if (!date) {
      setError("Укажите дату корректировки")
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onSubmit(date)
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось сохранить дату")
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={busy ? () => {} : onOpenChange}>
      <DialogContent className="sm:max-w-md" showCloseButton={!busy}>
        <DialogHeader>
          <DialogTitle>
            {batchId ? "Изменить дату пакета" : "Новый пакет корректировки"}
          </DialogTitle>
          <DialogDescription>
            По дате определяются учебная неделя и день недели. Выходные и
            нерабочие дни недоступны.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-2">
          <Label htmlFor="batch-correction-date">Дата корректировки</Label>
          <Input
            id="batch-correction-date"
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            disabled={busy}
          />
          {error && (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={busy}
          >
            Отмена
          </Button>
          <Button onClick={() => void handleSubmit()} disabled={busy || !date}>
            {busy ? (
              <>
                <LoaderCircle className="size-4 animate-spin" aria-hidden />
                Сохранение...
              </>
            ) : batchId ? (
              "Сохранить дату"
            ) : (
              "Создать пакет"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
