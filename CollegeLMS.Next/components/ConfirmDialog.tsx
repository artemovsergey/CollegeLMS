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

export interface ConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: React.ReactNode
  confirmLabel: string
  cancelLabel?: string
  confirmVariant?: "default" | "destructive"
  disabled?: boolean
  /**
   * Действие, выполняемое после подтверждения. Возвращает `true`, если операция
   * прошла успешно: при `false` окно остаётся открытым (например, показана ошибка).
   */
  onConfirm: () => Promise<boolean | void>
}

/**
 * Модальное подтверждение вместо `window.confirm`. Пока выполняется операция,
 * кнопка показывает лоадер и блокирует повторные клики — применение корректировки
 * занимает заметное время.
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel = "Отмена",
  confirmVariant = "default",
  disabled,
  onConfirm,
}: ConfirmDialogProps) {
  const [busy, setBusy] = useState(false)

  const handleOpenChange = (next: boolean) => {
    if (busy) return
    onOpenChange(next)
  }

  const handleConfirm = async () => {
    if (busy) return
    setBusy(true)
    try {
      const result = await onConfirm()
      if (result !== false) handleOpenChange(false)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        showCloseButton={!busy}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault()
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault()
        }}
        className="sm:max-w-md"
      >
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription asChild>
            <div className="text-sm text-muted-foreground">{description}</div>
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => handleOpenChange(false)}
            disabled={busy}
          >
            {cancelLabel}
          </Button>
          <Button
            variant={confirmVariant}
            onClick={() => void handleConfirm()}
            disabled={disabled || busy}
          >
            {busy ? (
              <>
                <LoaderCircle className="size-4 animate-spin" aria-hidden />
                Выполняется...
              </>
            ) : (
              confirmLabel
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
