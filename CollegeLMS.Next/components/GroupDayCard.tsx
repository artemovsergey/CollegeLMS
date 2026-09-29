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

/** Занятое занятие слота — то, что переносят. */
export interface MovedLesson {
  entry: CorrectionDayEntry
  numberPair: number
}

/** Пара занята, если её не трогает ни одна позиция пакета. */
export function isPairOccupied(
  entries: CorrectionDayEntry[],
  numberPair: number,
): boolean {
  return entries.some(
    (entry) =>
      entry.numberPair === numberPair && entry.pendingChangeType == null,
  )
}

interface GroupDayCardProps {
  groupName: string
  dateLabel: string
  entries: CorrectionDayEntry[]
  /** Выбранный слот назначения (добавление). */
  selectedPair: number | null
  onSelectPair: (numberPair: number) => void
  /** Занятие, выбранное для переноса. */
  movedLesson: MovedLesson | null
  onSelectLesson?: (lesson: MovedLesson) => void
  /** Клик по строке выбирает слот назначения. */
  selectable: boolean
  className?: string
}

/**
 * Карточка расписания группы на дату корректировки: все пары дня, и занятые,
 * и свободные. Слот назначения выбирается кликом, а для переноса занятие
 * перетаскивается на нужную строку прямо здесь — отдельный список не нужен.
 *
 * Занятый слот выбрать можно: в одной паре законно бывает несколько занятий
 * (преподаватель ведёт две группы подряд).
 */
export default function GroupDayCard({
  groupName,
  dateLabel,
  entries,
  selectedPair,
  onSelectPair,
  movedLesson,
  onSelectLesson,
  selectable,
  className,
}: GroupDayCardProps) {
  const [dragOver, setDragOver] = useState<number | null>(null)
  // Что именно тянут: обычный DragEvent не даёт достучаться до данных,
  // поэтому переносимое занятие держим в состоянии.
  const [dragged, setDragged] = useState<MovedLesson | null>(null)
  const moveMode = Boolean(onSelectLesson)
  const dragSourcePair = movedLesson?.numberPair ?? null

  return (
    <div className={cn("grid gap-2 rounded-md border", className)}>
      <p className="border-b bg-muted/40 px-3 py-2 text-xs font-medium text-muted-foreground">
        {groupName} · {dateLabel}
        {moveMode
          ? " — перетащите занятие на пару назначения"
          : " — выберите пару"}
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
            // Перенос в тот же слот — не перенос.
            const isSameSlot = dragSourcePair === numberPair
            // Тащить можно только занятое: свободный слот переносить нечего.
            const canDrag = Boolean(onSelectLesson) && occupied

            return (
              <li key={numberPair}>
                <div
                  onDragOver={(event) => {
                    if (!onSelectLesson || !dragged) return
                    if (dragged.numberPair === numberPair) return
                    event.preventDefault()
                    setDragOver(numberPair)
                  }}
                  onDragLeave={() => setDragOver(null)}
                  onDrop={(event) => {
                    setDragOver(null)
                    if (!onSelectLesson || !dragged) return
                    if (dragged.numberPair === numberPair) return
                    event.preventDefault()
                    // Перетаскивание задаёт перенос целиком: что переносим
                    // и в какую пару.
                    onSelectLesson(dragged)
                    onSelectPair(numberPair)
                    setDragged(null)
                  }}
                  className={cn(
                    "flex w-full items-start gap-3 rounded-md border px-3 py-2 text-left transition-colors",
                    selected
                      ? "border-primary bg-primary/10"
                      : "border-input bg-background",
                    selectable && !selected && "hover:bg-muted",
                    isDropTarget && "border-primary bg-primary/20 ring-2 ring-ring",
                    isSameSlot && "ring-1 ring-primary/40",
                  )}
                >
                  <button
                    type="button"
                    disabled={!selectable}
                    aria-pressed={selectable ? selected : undefined}
                    onClick={() => selectable && onSelectPair(numberPair)}
                    className={cn(
                      "flex size-7 shrink-0 items-center justify-center rounded-full text-sm font-semibold",
                      "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                      selected
                        ? "bg-primary text-primary-foreground"
                        : occupied
                          ? "bg-muted text-foreground hover:bg-muted-foreground/10"
                          : "bg-muted/60 text-muted-foreground",
                    )}
                  >
                    {numberPair}
                  </button>

                  <span className="min-w-0 flex-1">
                    {pairEntries.length === 0 ? (
                      <span className="text-sm text-muted-foreground">
                        Пара свободна
                      </span>
                    ) : (
                      <span className="grid gap-1">
                        {pairEntries.map((entry) => {
                          const picked =
                            movedLesson?.numberPair === entry.numberPair &&
                            movedLesson.entry.subject === entry.subject
                          return (
                            <span
                              key={`${entry.numberPair}-${entry.subject}-${entry.teacherName}`}
                              draggable={canDrag}
                              onDragStart={(event) => {
                                event.dataTransfer.effectAllowed = "move"
                                setDragged({ entry, numberPair })
                              }}
                              onDragEnd={() => {
                                setDragged(null)
                                setDragOver(null)
                              }}
                              className={cn(
                                "grid gap-0.5 rounded-sm",
                                canDrag && "cursor-grab",
                                picked && "bg-primary/10 px-1",
                              )}
                            >
                              <span className="flex items-center gap-1">
                                {canDrag && (
                                  <GripVertical
                                    className="size-3.5 shrink-0 text-muted-foreground"
                                    aria-hidden
                                  />
                                )}
                                <span
                                  className={cn(
                                    "truncate text-sm",
                                    picked && "font-semibold text-primary",
                                  )}
                                >
                                  {entry.subject}
                                </span>
                              </span>
                              <span className="flex flex-wrap items-center gap-x-3 gap-y-0.5 pl-0.5 text-[11px] text-muted-foreground">
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
                                    {
                                      PENDING_META[entry.pendingChangeType]
                                        ?.label
                                    }
                                  </Badge>
                                )}
                              </span>
                            </span>
                          )
                        })}
                      </span>
                    )}
                  </span>

                  {/* Бейдж на слот один: несколько операций над одной парой
                      не должны давать бейджи рядом друг с другом. */}
                  {pairEntries.length > 0 && (
                    <span className="mt-0.5 shrink-0 self-start">
                      <ChangeTagBadge
                        tags={pairEntries.flatMap((entry) => entry.changeTags)}
                      />
                    </span>
                  )}
                </div>
              </li>
            )
          },
        )}
      </ul>

      <p className="flex items-start gap-1.5 border-t px-3 py-2 text-xs text-muted-foreground">
        <CircleAlert className="mt-0.5 size-3.5 shrink-0" aria-hidden />
        {moveMode && movedLesson
          ? `Переносим пару ${movedLesson.numberPair} — выберите или перетащите на неё пару назначения.`
          : moveMode
            ? "Перетащите занятие на пару назначения или нажмите на его пару, а потом на нужную."
            : "Занятую пару выбрать можно: в слоте может быть несколько занятий."}
      </p>
    </div>
  )
}
