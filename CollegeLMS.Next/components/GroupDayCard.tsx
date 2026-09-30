"use client"

import type { CorrectionDayEntry } from "@/types/correction"
import { CircleAlert, MapPin, UserRound } from "lucide-react"
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

/** Первое занятие слота, которое форма считает занятым. */
export function occupiedEntryFor(
  entries: CorrectionDayEntry[],
  numberPair: number,
): CorrectionDayEntry | null {
  return (
    entries.find(
      (entry) =>
        entry.numberPair === numberPair && entry.pendingChangeType == null,
    ) ?? null
  )
}

/** Пара занята, если её не трогает ни одна позиция пакета. */
export function isPairOccupied(
  entries: CorrectionDayEntry[],
  numberPair: number,
): boolean {
  return occupiedEntryFor(entries, numberPair) != null
}

interface GroupDayCardProps {
  groupName: string
  dateLabel: string
  entries: CorrectionDayEntry[]
  /** Выбранный слот. */
  selectedPair: number | null
  onSelectPair: (numberPair: number) => void
  /**
   * Задание внутри выбранной пары. Передаётся при снятии и замене: там нужно
   * указать не пару, а конкретное занятие, и лишний отдельный шаг только мешает —
   * клик по занятию в этой же карточке выбирает и пару, и занятие сразу.
   */
  onSelectEntry?: (entry: CorrectionDayEntry) => void
  /** Ключ выбранного занятия: пара, предмет и преподаватель. */
  selectedEntryKey?: string | null
  className?: string
}

/** Ключ занятия: пара, предмет и преподаватель. */
export function entryKey(entry: CorrectionDayEntry): string {
  return `${entry.numberPair}-${entry.subject}-${entry.teacherName ?? ""}`
}

/**
 * Карточка расписания группы на дату корректировки: все пары дня, и занятые,
 * и свободные. Слот выбирается прямо здесь. Занятый слот выбрать можно —
 * в одной паре законно бывает несколько занятий (преподаватель ведёт две
 * группы подряд, ин.язык делится между двумя преподавателями).
 */
export default function GroupDayCard({
  groupName,
  dateLabel,
  entries,
  selectedPair,
  onSelectPair,
  onSelectEntry,
  selectedEntryKey,
  className,
}: GroupDayCardProps) {
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

            return (
              <li key={numberPair}>
                {/* Пара — без вложенных кнопок: номер выбирает слот, а занятия
                    ниже выбираются отдельно, когда нужно указать конкретное. */}
                <div
                  className={cn(
                    "flex w-full items-start gap-3 rounded-md border px-3 py-2 text-left transition-colors",
                    selected
                      ? "border-primary bg-primary/10"
                      : "border-input bg-background",
                  )}
                >
                  <button
                    type="button"
                    aria-pressed={selected}
                    aria-label={`Пара ${numberPair}`}
                    onClick={() => onSelectPair(numberPair)}
                    className={cn(
                      "flex size-7 shrink-0 items-center justify-center rounded-full text-sm font-semibold",
                      "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                      selected
                        ? "bg-primary text-primary-foreground"
                        : occupied
                          ? "bg-muted text-foreground hover:bg-muted/70"
                          : "bg-muted/60 text-muted-foreground",
                    )}
                  >
                    {numberPair}
                  </button>

                  <div className="min-w-0 flex-1">
                    {pairEntries.length === 0 ? (
                      <span className="text-sm text-muted-foreground">
                        Пара свободна
                      </span>
                    ) : (
                      <span className="grid gap-1">
                        {pairEntries.map((entry) => {
                          const key = entryKey(entry)
                          const detail = (
                            <>
                              <span className="truncate text-sm">
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
                              </span>
                            </>
                          )

                          if (!onSelectEntry) return <span key={key}>{detail}</span>

                          return (
                            <button
                              key={key}
                              type="button"
                              aria-pressed={selectedEntryKey === key}
                              onClick={() => onSelectEntry(entry)}
                              className={cn(
                                "grid w-full gap-0.5 rounded-md border px-2 py-1.5 text-left transition-colors",
                                "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                                selectedEntryKey === key
                                  ? "border-primary bg-primary/10"
                                  : "border-transparent hover:bg-muted",
                              )}
                            >
                              {detail}
                            </button>
                          )
                        })}
                      </span>
                    )}
                  </div>

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
        {onSelectEntry
          ? "Кликните по занятию — оно и пара выберутся сразу."
          : "Занятую пару выбрать можно: в слоте может быть несколько занятий."}
      </p>
    </div>
  )
}
