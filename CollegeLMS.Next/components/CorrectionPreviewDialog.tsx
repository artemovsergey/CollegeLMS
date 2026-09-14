"use client"

import { useState } from "react"
import { AlertCircle, ListChecks, XCircle } from "lucide-react"

import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { confirmCorrection } from "@/api/correction"
import type { CorrectionPreviewEntry, ConfirmResult } from "@/types/correction"
import { extractErrorMessage } from "@/lib/utils"
import { dayLabelFromInt } from "@/lib/max-lesson"

const CHANGE_LABEL: Record<string, string> = {
  Add: "Добавление",
  Remove: "Снятие",
  Replace: "Замена",
  Move: "Перенос",
}

interface CorrectionPreviewDialogProps {
  open: boolean
  entries: CorrectionPreviewEntry[]
  errors: string[]
  onConfirm: (result: ConfirmResult) => void
  onCancel: () => void
}

export function CorrectionPreviewDialog({
  open,
  entries,
  errors,
  onConfirm,
  onCancel,
}: CorrectionPreviewDialogProps) {
  const [busy, setBusy] = useState(false)
  const [applyError, setApplyError] = useState<string | null>(null)

  const close = () => {
    if (!busy) onCancel()
  }

  const apply = async () => {
    setBusy(true)
    setApplyError(null)
    try {
      const result = await confirmCorrection(entries, crypto.randomUUID())
      onConfirm(result)
    } catch (err) {
      setApplyError(
        extractErrorMessage(err) ?? "Не удалось применить корректировку",
      )
    } finally {
      setBusy(false)
    }
  }

  const hasErrors = errors.length > 0

  return (
    <Dialog open={open} onOpenChange={(next) => !next && close()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {hasErrors ? "Некорректные строки файла" : "Предпросмотр корректировки"}
          </DialogTitle>
        </DialogHeader>

        {hasErrors ? (
          <ul className="grid max-h-[50vh] gap-2 overflow-y-auto text-sm">
            {errors.map((message, index) => (
              <li
                key={index}
                className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-destructive"
              >
                <XCircle className="mt-0.5 size-4 shrink-0" />
                <span>{message}</span>
              </li>
            ))}
          </ul>
        ) : (
          <>
            <ul className="grid max-h-[50vh] divide-y overflow-y-auto rounded-md border text-sm">
              {entries.map((entry, index) => (
                <li key={index} className="flex flex-col gap-1 px-3 py-2">
                  <div className="flex flex-wrap items-center justify-between gap-x-2 gap-y-1">
                    <span className="flex items-center gap-1.5 font-medium">
                      <ListChecks className="size-4 text-primary" />
                      {CHANGE_LABEL[entry.changeType] ?? entry.changeType}
                    </span>
                    <span className="text-muted-foreground">
                      {entry.groupName} · {dayLabelFromInt(entry.dayOfWeek)},{entry.week} нед., пара {entry.numberPair}
                    </span>
                  </div>
                  <div className="text-muted-foreground">
                    {entry.changeType === "Add"
                      ? `Вводится: ${entry.subject ?? "—"} ${
                          entry.teacherName ? `(${entry.teacherName})` : ""
                        }`
                      : entry.changeType === "Remove"
                        ? `Снимается: ${entry.removedSubject ?? "—"}`
                        : `Снимается: ${entry.removedSubject ?? "—"} → Вводится: ${entry.subject ?? "—"}`}
                    {entry.note ? ` · ${entry.note}` : ""}
                  </div>
                </li>
              ))}
            </ul>

            {applyError && (
              <div className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                <AlertCircle className="mt-0.5 size-4 shrink-0" />
                <span>{applyError}</span>
              </div>
            )}
          </>
        )}

        <DialogFooter>
          <Button variant="outline" disabled={busy} onClick={close}>
            {hasErrors ? "Закрыть" : "Отмена"}
          </Button>
          {!hasErrors && (
            <Button disabled={busy} onClick={apply}>
              {busy ? "Применение..." : "Применить"}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}