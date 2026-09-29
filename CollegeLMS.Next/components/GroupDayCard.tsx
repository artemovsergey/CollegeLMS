"use client"

import { useState } from "react"
import { CircleAlert, GripVertical, MapPin, UserRound } from "lucide-react"
import type { CorrectionDayEntry } from "@/types/correction"
import { Badge } from "@/components/ui/badge"
import ChangeTagBadge from "@/components/ChangeTagBadge"
import { cn } from "@/lib/utils"

const MAX_PAIR = 8

const PENDING_META: Record<string, { label: string; className: string }> = {
  Add: {
    label: "в пакете",
    className:
      "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "снимается",
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "заменяется",
    className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "переносится",
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

/** Пара считается занятой, если её не трогает ни одна позиция пакета. */
function isPairOccupied(
  entries: CorrectionDayEntry[],
  numberPair: number,
): boolean {
  return entries.some(
    (entry) => entry.numberPair === numberPair && entry.pendingChangeType == null,
  )
}

export function occupiedEntryFor(
  entries: CorrectionDayEntry[],
  numberPair: number,
): CorrectionDayEntry | null {
  return (
    entries.find(
      (entry) => entry.numberPair === numberPair && entry.pendingChangeType == null,
    ) ?? null
  )
}

interface GroupDayCardProps {
  groupName: string
  dateLabel: string
  entries: CorrectionDayEntry[]
  selectedPair: number | null
  onSelectPair: (numberPair: number) => void
  /** Пара назначения выбирается кликом по строке; false — строки не выбираются. */
  selectable: boolean
  /** Разрешить перетаскивание занятия в другой слот (операция «Перенос»). */
  dragSource?: boolean
  onDropPair?: (numberPair: number) => void
  className?: string
}

/**
 * Карточка расписания группы на дату корректировки: полный день со всеми
 * восемью парами, включая свободные. По ней выбирается слот, в том числе
 * занятый — поставить пару в занятую позицию разрешено.
 */
export default function GroupDayCard({
  groupName,
  dateLabel,
  entries,
  selectedPair,
  onSelectPair,
  selectable,
  dragSource,
  onDropPair,
  className,
}: GroupDayCardProps) {
  const [dragOver, setDragOver] = useState<number | null>(null)

  return (
    <div className={cn("grid gap-2 rounded-md border", className)}>
      <p className="border-b bg-muted/40 px-3 py-2 text-xs font-medium text-muted-foreground">
        {groupName} · {dateLabel} — выберите пару
      </p>

      <ul className="grid gap-1 p-2">
        {Array.from({ length: MAX_PAIR }, (_, index) => index + 1).map(
          (numberPair) => {
            const pairEntries = entries.filter(
              (entry) => entry.numberPair === numberPair,
            )
            const occupied = isPairOccupied(entries, numberPair)
            const selected = selectedPair === numberPair
            const isDropTarget = dragOver === numberPair

            return (
              <li key={numberPair}>
                <button
                  type="button"
                  disabled={!selectable}
                  aria-pressed={selectable ? selected : undefined}
                  onClick={() => selectable && onSelectPair(numberPair)}
                  onDragOver={(event) => {
                    if (!onDropPair) return
                    event.preventDefault()
                    setDragOver(numberPair)
                  }}
                  onDragLeave={() => setDragOver(null)}
                  onDrop={(event) => {
                    setDragOver(null)
                    if (!onDropPair) return
                    event.preventDefault()
                    onDropPair(numberPair)
                  }}
                  className={cn(
                    "flex w-full items-start gap-3 rounded-md border px-3 py-2 text-left transition-colors",
                    "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                    selected
                      ? "border-primary bg-primary/10"
                      : "border-input bg-background",
                    !selectable && "cursor-default",
                    selectable && !selected && "hover:bg-muted",
                    isDropTarget && "border-primary bg-primary/20 ring-2 ring-ring",
                    onDropPair && "border-dashed",
                  )}
                >
                  <span
                    className={cn(
                      "flex size-7 shrink-0 items-center justify-center rounded-full text-sm font-semibold",
                      selected
                        ? "bg-primary text-primary-foreground"
                        : occupied
                          ? "bg-muted text-foreground"
                          : "bg-muted/60 text-muted-foreground",
                    )}
                  >
                    {numberPair}
                  </span>

                  <span className="min-w-0 flex-1">
                    {pairEntries.length === 0 ? (
                      <span className="text-sm text-muted-foreground">
                        Пара свободна
                      </span>
                    ) : (
                      <span className="grid gap-1">
                        {pairEntries.map((entry) => (
                          <span key={`${entry.numberPair}-${entry.subject}-${entry.teacherName}`} className="grid gap-0.5">
                            <span className="truncate text-sm font-medium">
                              {entry.subject}
                            </span>
                            <span className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11px] text-muted-foreground">
                              {entry.teacherName && (
                                <span className="inline-flex items-center gap-1">
                                  <UserRound className="size-3" aria-hidden />
                                  {entry.teacherName}
                                </span>
                              )}
                              {entry.room && (
                                <span className="inline-flex items-center gap-1">
                                  <MapPin className="size-3" aria-hidden />
                                  {entry.room}
                                </span>
                              )}
                            </span>
                            <span className="flex flex-wrap items-center gap-1">
                              {entry.isSelfStudy && (
                                <Badge
                                  variant="outline"
                                  className="bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300"
                                >
                                  Сам.р.
                                </Badge>
                              )}
                              {entry.pendingChangeType && (
                                <Badge
                                  variant="outline"
                                  className={
                                    PENDING_META[entry.pendingChangeType]
                                      ?.className
                                  }
                                >
                                  {PENDING_META[entry.pendingChangeType]?.label}
                                </Badge>
                              )}
                              <ChangeTagBadge tags={entry.changeTags} />
                            </span>
                          </span>
                        ))}
                      </span>
                    )}
                  </span>

                  {onDropPair && selectable && (
                    <span
                      className="flex shrink-0 items-center gap-1 self-center text-[11px] text-muted-foreground"
                      aria-hidden
                    >
                      <GripVertical className="size-3.5" />
                      {dragSource ? "Перетащите сюда" : null}
                    </span>
                  )}
                </button>
              </li>
            )
          },
        )}
      </ul>

      <p className="flex items-start gap-1.5 border-t px-3 py-2 text-xs text-muted-foreground">
        <CircleAlert className="mt-0.5 size-3.5 shrink-0" aria-hidden />
        Занятую пару выбрать можно: пара встанет в неё вместо текущей («вм.N»)
        либо рядом, если занятие переносится из другой пары.
      </p>
    </div>
  )
}
