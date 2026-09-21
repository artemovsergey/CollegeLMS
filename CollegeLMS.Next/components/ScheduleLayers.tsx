"use client"

import type { Practice } from "@/api/practices"
import {
  PRACTICE_KIND_LABELS,
  PRACTICE_KIND_SHORT,
  practiceName,
  practiceTeacherNames,
} from "@/api/practices"
import type { ScheduleInsert } from "@/api/inserts"
import type { ScheduleBigBreak, ScheduleResponse } from "@/types/schedule"
import {
  BellRing,
  Briefcase,
  CalendarCheck,
  CalendarOff,
  Coffee,
} from "lucide-react"
import { cn } from "@/lib/utils"
import { workingDayLabel } from "@/lib/reference"

interface ScheduleLayersProps {
  nonWorkingTitle: string | null
  isSunday: boolean
  practices: Practice[]
  /** День сделан рабочим (перенос с другого дня недели). */
  isWorkingDay?: boolean
  workingDayTitle?: string | null
  substituteDayOfWeek?: number | null
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

/** Строка большой перемены: «HH:mm–HH:mm Большая перемена». */
export function BigBreakRow({
  bigBreak,
  compact = false,
  className,
}: {
  bigBreak: ScheduleBigBreak
  compact?: boolean
  className?: string
}) {
  return (
    <div
      role="note"
      className={cn(
        "flex items-center gap-2 rounded-md border border-warning/40 bg-warning/10 text-warning",
        compact ? "px-2 py-1 text-[11px]" : "px-3 py-2 text-sm",
        className,
      )}
    >
      <Coffee
        className={cn("shrink-0", compact ? "size-3" : "size-4")}
        aria-hidden
      />
      <span className="whitespace-nowrap font-medium">
        {formatTime(bigBreak.startTime)}–{formatTime(bigBreak.endTime)}
      </span>
      <span className="truncate">Большая перемена</span>
    </div>
  )
}

/** Бейдж рабочего дня: «Работа в субботу: за понедельник». */
export function WorkingDayBadge({
  title,
  substituteDayOfWeek,
  compact = false,
  className,
}: {
  title?: string | null
  substituteDayOfWeek?: number | null
  compact?: boolean
  className?: string
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full bg-warning/15 font-medium text-warning",
        compact ? "px-1.5 py-0.5 text-[10px]" : "px-2 py-0.5 text-xs",
        className,
      )}
    >
      <CalendarCheck
        className={cn("shrink-0", compact ? "size-3" : "size-3.5")}
        aria-hidden
      />
      {workingDayLabel(title, substituteDayOfWeek)}
    </span>
  )
}

/** Название практики у пары УП (если бэкенд его отдал). */
export function practicePairName(entry: ScheduleResponse): string | null {
  const name = entry.practiceName?.trim()
  return name && name.length > 0 ? name : null
}

/**
 * Бейдж пары УП. Показывает название практики, если оно не дублирует
 * предмет пары, иначе — общую метку «Практика».
 */
export function PracticePairBadge({
  name,
  subject,
  className,
}: {
  name: string
  subject?: string
  className?: string
}) {
  const trimmed = name.trim()
  const label = trimmed.length > 0 && trimmed !== subject?.trim()
    ? trimmed
    : "Практика"
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-medium text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
        className,
      )}
    >
      <Briefcase className="size-3" aria-hidden />
      {label}
    </span>
  )
}

/** Карточка практики: «УП/ПП · название · группа · преподаватели». */
export function PracticeCard({
  practice,
  compact = false,
}: {
  practice: Practice
  compact?: boolean
}) {
  const name = practiceName(practice)
  const teachers = practiceTeacherNames(practice)
  return (
    <div
      className={cn(
        "rounded-lg border",
        compact ? "p-2" : "p-3",
        PRACTICE_BORDER[practice.kind],
      )}
    >
      <div className="flex flex-wrap items-center gap-2">
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
        <span
          className={cn("font-medium", compact ? "text-[11px]" : "text-sm")}
        >
          {name}
        </span>
      </div>
      <p
        className={cn(
          "mt-1 break-words",
          compact ? "text-[11px]" : "text-sm",
        )}
      >
        <span className="font-medium">{practice.groupName}</span>
        {teachers ? ` · ${teachers}` : ""}
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

/** Слои дня: нерабочий день, воскресенье, рабочий день, практики. */
export default function ScheduleLayers({
  nonWorkingTitle,
  isSunday,
  practices,
  isWorkingDay = false,
  workingDayTitle = null,
  substituteDayOfWeek = null,
}: ScheduleLayersProps) {
  const hasContent =
    Boolean(nonWorkingTitle) ||
    isSunday ||
    isWorkingDay ||
    practices.length > 0
  if (!hasContent) return null

  return (
    <div className="flex flex-col gap-2">
      {isWorkingDay && (
        <WorkingDayBadge
          title={workingDayTitle}
          substituteDayOfWeek={substituteDayOfWeek}
        />
      )}

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
