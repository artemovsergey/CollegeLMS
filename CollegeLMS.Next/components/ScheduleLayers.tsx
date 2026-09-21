"use client"

import type { Practice } from "@/api/practices"
import { PRACTICE_KIND_LABELS, PRACTICE_KIND_SHORT } from "@/api/practices"
import type { ScheduleInsert } from "@/api/inserts"
import { BellRing, CalendarOff } from "lucide-react"
import { cn } from "@/lib/utils"

interface ScheduleLayersProps {
  nonWorkingTitle: string | null
  isSunday: boolean
  practices: Practice[]
}

const PRACTICE_BADGE: Record<Practice["kind"], string> = {
  Up: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  Pp: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
}

const PRACTICE_BORDER: Record<Practice["kind"], string> = {
  Up: "border-emerald-200 bg-emerald-50/60 dark:border-emerald-900 dark:bg-emerald-950/20",
  Pp: "border-amber-200 bg-amber-50/60 dark:border-amber-900 dark:bg-amber-950/20",
}

function formatTime(time: string): string {
  return time.slice(0, 5)
}

/** Строка вставки: «HH:mm–HH:mm Название» без номера пары. */
export function InsertRow({
  insert,
  compact = false,
}: {
  insert: ScheduleInsert
  compact?: boolean
}) {
  return (
    <div
      className={cn(
        "flex items-center gap-2 rounded-md border border-dashed bg-muted/40",
        compact ? "px-2 py-1 text-[11px]" : "px-3 py-2 text-sm",
      )}
    >
      <BellRing
        className={cn("shrink-0 text-primary", compact ? "size-3" : "size-4")}
        aria-hidden
      />
      <span className="whitespace-nowrap font-medium">
        {formatTime(insert.startTime)}–{formatTime(insert.endTime)}
      </span>
      <span className="truncate">{insert.title}</span>
    </div>
  )
}

/** Карточка практики: «УП/ПП: группа · преподаватель · организация». */
export function PracticeCard({
  practice,
  compact = false,
}: {
  practice: Practice
  compact?: boolean
}) {
  return (
    <div
      className={cn(
        "rounded-lg border",
        compact ? "p-2" : "p-3",
        PRACTICE_BORDER[practice.kind],
      )}
    >
      <span
        className={cn(
          "inline-flex items-center rounded-full px-2 py-0.5 font-medium",
          compact ? "text-[10px]" : "text-[11px]",
          PRACTICE_BADGE[practice.kind],
        )}
        title={PRACTICE_KIND_LABELS[practice.kind]}
      >
        {PRACTICE_KIND_SHORT[practice.kind]}
      </span>
      <p
        className={cn(
          "mt-1 break-words",
          compact ? "text-[11px]" : "text-sm",
        )}
      >
        <span className="font-medium">{practice.groupName}</span>
        {" · "}
        {practice.teacherName}
        {practice.organization ? ` · ${practice.organization}` : ""}
      </p>
      {practice.note && (
        <p
          className={cn(
            "mt-1 text-muted-foreground",
            compact ? "text-[10px]" : "text-xs",
          )}
        >
          Примечание: {practice.note}
        </p>
      )}
    </div>
  )
}

/** Слои дня: нерабочий день, воскресенье, практики. */
export default function ScheduleLayers({
  nonWorkingTitle,
  isSunday,
  practices,
}: ScheduleLayersProps) {
  const hasContent =
    Boolean(nonWorkingTitle) || isSunday || practices.length > 0
  if (!hasContent) return null

  return (
    <div className="flex flex-col gap-2">
      {nonWorkingTitle && (
        <div
          role="status"
          className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-3 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-200"
        >
          <CalendarOff className="size-4 shrink-0" aria-hidden />
          <span>
            <span className="font-medium">Нерабочий день:</span>{" "}
            {nonWorkingTitle}
          </span>
        </div>
      )}

      {!nonWorkingTitle && isSunday && (
        <div
          role="status"
          className="flex items-center gap-2 rounded-lg border bg-muted/40 px-3 py-3 text-sm text-muted-foreground"
        >
          <CalendarOff className="size-4 shrink-0" aria-hidden />
          Выходной
        </div>
      )}

      {practices.map((practice) => (
        <PracticeCard key={practice.id} practice={practice} />
      ))}
    </div>
  )
}
