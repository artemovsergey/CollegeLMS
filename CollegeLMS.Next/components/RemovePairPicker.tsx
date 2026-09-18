"use client"

import { useCallback, useEffect, useState } from "react"
import { LoaderCircle, RotateCw } from "lucide-react"
import { getDaySchedule } from "@/api/correction"
import type {
  CorrectionChangeType,
  CorrectionDayEntry,
} from "@/types/correction"
import { cn, extractErrorMessage } from "@/lib/utils"
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
  Add: {
    label: "Добавлено",
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    className:
      "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    className:
      "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

interface RemovePairPickerProps {
  groupId: string | null
  /** Дата корректировки в формате yyyy-MM-dd. */
  date: string
  /** Пакет — чтобы показать неприменённые (pending) позиции. */
  batchId?: string | null
  value: RemovedPairSelection | null
  onChange: (value: RemovedPairSelection | null) => void
  disabled?: boolean
}

export default function RemovePairPicker({
  groupId,
  date,
  batchId,
  value,
  onChange,
  disabled,
}: RemovePairPickerProps) {
  const [entries, setEntries] = useState<CorrectionDayEntry[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  const load = useCallback(() => {
    if (!groupId || !date) {
      setEntries([])
      setError(null)
      return () => {}
    }
    let cancelled = false
    setLoading(true)
    setError(null)
    getDaySchedule({ groupId, date, batchId: batchId ?? undefined })
      .then((res) => {
        if (cancelled) return
        setEntries(
          [...res.entries].sort((a, b) => a.numberPair - b.numberPair),
        )
      })
      .catch((err) => {
        if (cancelled) return
        setEntries([])
        setError(
          extractErrorMessage(err) ?? "Не удалось загрузить расписание дня",
        )
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [groupId, date, batchId])

  useEffect(() => {
    const cleanup = load()
    return cleanup
  }, [load, reloadKey])

  if (!groupId) {
    return (
      <p className="text-sm text-muted-foreground">
        Сначала выберите группу
      </p>
    )
  }

  if (loading) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <LoaderCircle className="size-4 animate-spin" aria-hidden />
        Загрузка расписания дня...
      </p>
    )
  }

  if (error) {
    return (
      <div className="flex flex-wrap items-center gap-2 text-sm text-destructive">
        <span>{error}</span>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setReloadKey((key) => key + 1)}
        >
          <RotateCw className="size-4" aria-hidden />
          Повторить
        </Button>
      </div>
    )
  }

  if (entries.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Нет занятий в этот день. Добавьте позицию типа «Добавлено» или
        проверьте дату.
      </p>
    )
  }

  return (
    <div className="grid gap-1.5" role="group" aria-label="Снимаемая пара">
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
              "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
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
                  <Badge
                    variant="outline"
                    className="bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300"
                  >
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
              {entry.changeTags.map((tag, tagIndex) => (
                <ChangeTagBadge key={tagIndex} tag={tag} />
              ))}
            </span>
          </button>
        )
      })}
    </div>
  )
}
