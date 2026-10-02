"use client"

import { FOCUS_RING } from "@/lib/focus"
import { RotateCw } from "lucide-react"
import type {
  CorrectionChangeType,
  CorrectionDayEntry,
} from "@/types/correction"
import { cn } from "@/lib/utils"
import { CHANGE_TYPE_STYLE, SELF_STUDY_STYLE } from "@/lib/status-style"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import ChangeTagBadge from "@/components/ChangeTagBadge"

export interface RemovedPairSelection {
  numberPair: number
  removedSubject: string
  removedTeacherId: string | null
  removedTeacherName: string | null
}

const PENDING_META: Record<
  CorrectionChangeType,
  { label: string; className: string }
> = {
  Add: { label: "Добавлено", className: CHANGE_TYPE_STYLE.Add },
  Remove: { label: "Снято", className: CHANGE_TYPE_STYLE.Remove },
  Replace: { label: "Замена", className: CHANGE_TYPE_STYLE.Replace },
  Move: { label: "Перенос", className: CHANGE_TYPE_STYLE.Move },
}

interface RemovePairPickerProps {
  /** Занятое занятие дня — снимаемое или заменяемое. */
  value: RemovedPairSelection | null
  onChange: (value: RemovedPairSelection | null) => void
  entries: CorrectionDayEntry[]
  loading?: boolean
  error?: string | null
  onRetry?: () => void
  disabled?: boolean
  className?: string
}

/** Список занятий выбранного дня: один клик — выбор позиции для снятия/замены. */
export default function RemovePairPicker({
  value,
  onChange,
  entries,
  loading,
  error,
  onRetry,
  disabled,
  className,
}: RemovePairPickerProps) {
  if (loading) {
    return (
      <p className="text-sm text-muted-foreground">Загрузка расписания дня…</p>
    )
  }

  if (error) {
    return (
      <div className="flex flex-wrap items-center gap-2 text-sm text-destructive-text">
        <span>{error}</span>
        {onRetry && (
          <Button variant="outline" size="sm" onClick={onRetry}>
            <RotateCw className="size-4" aria-hidden />
            Повторить
          </Button>
        )}
      </div>
    )
  }

  if (entries.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        В этот день у группы нет занятий. Добавьте позицию типа «Добавлено».
      </p>
    )
  }

  return (
    <div className={cn("grid gap-1.5", className)} role="group" aria-label="Занятие дня">
      {entries.map((entry, index) => {
        const selected =
          value != null &&
          value.numberPair === entry.numberPair &&
          value.removedSubject === entry.subject
        const pending = entry.pendingChangeType
          ? PENDING_META[entry.pendingChangeType]
          : null
        return (
          <button
            key={`${entry.numberPair}-${entry.subject}-${index}`}
            type="button"
            disabled={disabled}
            aria-pressed={selected}
            onClick={() => {
              if (disabled) return
              if (selected) {
                onChange(null)
                return
              }
              onChange({
                numberPair: entry.numberPair,
                removedSubject: entry.subject,
                removedTeacherId: entry.teacherId,
                removedTeacherName: entry.teacherName,
              })
            }}
            className={cn(
              "flex items-start justify-between gap-3 rounded-md border px-3 py-2 text-left text-sm transition-colors",
"",
              selected
                ? "border-primary bg-primary/5"
                : "border-input hover:bg-muted",
              disabled && "cursor-not-allowed opacity-60",
            )}
          >
            <span className="min-w-0">
              <span className="flex flex-wrap items-center gap-1.5 font-medium">
                {entry.numberPair} пара
                {entry.isSelfStudy && (
                  <Badge variant="outline" className={SELF_STUDY_STYLE}>
                    Сам.р.
                  </Badge>
                )}
              </span>
              <span className="block truncate text-muted-foreground">
                {entry.isSelfStudy ? "Самостоятельная работа" : entry.subject}
                {entry.room ? ` · каб. ${entry.room}` : ""}
              </span>
              {entry.teacherName && (
                <span className="block truncate text-xs text-muted-foreground">
                  {entry.teacherName}
                </span>
              )}
            </span>
            <span className="flex shrink-0 flex-wrap justify-end gap-1">
              {pending && (
                <Badge variant="outline" className={pending.className}>
                  {pending.label}
                </Badge>
              )}
              <ChangeTagBadge tags={entry.changeTags} />
            </span>
          </button>
        )
      })}
    </div>
  )
}
