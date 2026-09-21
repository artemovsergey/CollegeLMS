"use client"

import { useState } from "react"
import { AlertCircle, X } from "lucide-react"
import { Button, Typography } from "@maxhub/max-ui"
import { addPosition, applyBatch, createBatch } from "@/api/correction"
import { handleDispatcherAuthError } from "@/api/dispatcher"
import type {
  CorrectionApplyResult,
  CorrectionPreviewEntry,
  CreateCorrectionPosition,
} from "@/types/correction"
import { extractErrorMessage } from "@/lib/utils"
import { WEEKDAYS } from "@/lib/max-lesson"

const CHANGE_LABEL: Record<string, string> = {
  Add: "Добавление",
  Remove: "Снятие",
  Replace: "Замена",
  Move: "Перенос",
}

function dayLabel(dayOfWeek: number): string {
  return WEEKDAYS.find((d) => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
}

export default function ConfirmOpsSheet({
  open,
  ops,
  batchId,
  correctionDate,
  onCancel,
  onApplied,
}: {
  open: boolean
  ops: CorrectionPreviewEntry[]
  batchId?: string
  correctionDate?: string
  onCancel: () => void
  onApplied: (result: CorrectionApplyResult) => void
}) {
  const [confirming, setConfirming] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  if (!open) return null

  const apply = async () => {
    setConfirming(true)
    setFormError(null)
    try {
      let currentBatchId = batchId
      if (!currentBatchId) {
        if (!correctionDate) throw new Error("Не удалось определить дату корректировки")
        currentBatchId = (await createBatch(correctionDate)).id
        for (const op of ops) {
          const position: CreateCorrectionPosition = {
            changeType: op.changeType,
            groupId: op.groupId,
            groupName: op.groupName,
            numberPair: op.numberPair,
            subject: op.subject,
            teacherId: op.teacherId,
            teacherName: op.teacherName,
            removedSubject: op.removedSubject,
            removedTeacherId: op.removedTeacherId,
            removedTeacherName: op.removedTeacherName,
            removedNumberPair: op.removedNumberPair,
            note: op.note || null,
          }
          await addPosition(currentBatchId, position)
        }
      }
      const result = await applyBatch(currentBatchId, crypto.randomUUID())
      onApplied(result)
    } catch (err) {
      if (handleDispatcherAuthError(err)) return
      setFormError(
        extractErrorMessage(err) ??
          "Ошибка применения. Проверьте операции и повторите",
      )
    } finally {
      setConfirming(false)
    }
  }

  return (
    <div
      className="max-app__sheet"
      role="dialog"
      aria-modal="true"
      aria-label="Подтверждение корректировки"
    >
      <div className="max-app__sheet-head">
        <Typography.Title>Подтверждение ({ops.length})</Typography.Title>
        <Button
          size="small"
          variant="ghost"
          iconBefore={<X size={18} aria-hidden />}
          aria-label="Закрыть подтверждение"
          onClick={onCancel}
        />
      </div>

      <div className="max-app__ops">
        {ops.map((op, i) => (
          <div className="max-app__op" key={`${op.row}:${i}`}>
            <span
              className={`max-app__badge max-app__badge--${op.changeType.toLowerCase()}`}
            >
              {CHANGE_LABEL[op.changeType] ?? op.changeType}
            </span>
            <span className="max-app__op-text">
              {op.groupName} · {dayLabel(op.dayOfWeek)} {op.week}-я нед. ·{" "}
              {op.changeType === "Move"
                ? `${op.removedNumberPair ?? "—"} → ${op.numberPair}`
                : `${op.numberPair} пара`}
              {op.changeType === "Remove" && op.removedSubject
                ? ` · ${op.removedSubject}`
                : ""}
              {op.subject ? ` · ${op.subject}` : ""}
              {op.teacherName ? ` · ${op.teacherName}` : ""}
            </span>
          </div>
        ))}
      </div>

      {formError ? (
        <div className="max-app__confirm-error">
          <AlertCircle size={16} aria-hidden className="max-app__confirm-error-icon" />
          <Typography.Body className="max-app__error">
            {formError}
          </Typography.Body>
        </div>
      ) : null}

      <div className="max-app__sheet-actions">
        <Button stretched variant="secondary" onClick={onCancel} disabled={confirming}>
          Вернуться к редактированию
        </Button>
        <Button stretched loading={confirming} onClick={() => void apply()}>
          Применить изменения
        </Button>
      </div>
    </div>
  )
}
