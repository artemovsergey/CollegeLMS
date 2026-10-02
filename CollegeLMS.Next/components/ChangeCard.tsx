"use client"

import { FOCUS_RING } from "@/lib/focus"
import Link from "next/link"
import type { LucideIcon } from "lucide-react"
import {
  Plus,
  Minus,
  ArrowRightLeft,
  BookOpen,
  CalendarDays,
  Clock,
  DoorOpen,
  Info,
  User,
} from "lucide-react"
import type { ScheduleHistoryItem } from "@/types/correction"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent } from "@/components/ui/card"
import { cn } from "@/lib/utils"
import { dateForLesson } from "@/lib/semester"
import { toIsoDate } from "@/api/schedule"
import {
  REPLACE_ARROW_CLASS,
  changeTagKind,
  isInformationalNote,
  movePairFromNote,
} from "@/lib/change-tags"
import { dayLabelFromString } from "@/lib/max-lesson"
import {
  CHANGE_KIND_STYLE,
  SELF_STUDY_BADGE_STYLE,
} from "@/lib/status-style"

/** Исходов два: добавлено и снято. */
const CHANGE_KIND_META: Record<
  "Add" | "Remove",
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: { label: "Добавлено", icon: Plus, className: CHANGE_KIND_STYLE.Add },
  Remove: { label: "Снято", icon: Minus, className: CHANGE_KIND_STYLE.Remove },
}

const SELF_STUDY_BADGE = SELF_STUDY_BADGE_STYLE

const DAY_OFFSET: Record<string, number> = {
  Monday: 0,
  Tuesday: 1,
  Wednesday: 2,
  Thursday: 3,
  Friday: 4,
  Saturday: 5,
  Sunday: 6,
}

const SELF_STUDY_NOTE_RE = /сам[\s./-]*р/i

function isSelfStudy(item: ScheduleHistoryItem): boolean {
  return SELF_STUDY_NOTE_RE.test(item.note ?? "")
}

/** «Предмет Преподаватель» одной строкой — так же, как в файле корректировки. */
function lessonLine(
  subject: string | null | undefined,
  teacher: string | null | undefined,
): string {
  return [subject, teacher].filter(Boolean).join(" ") || "—"
}

function formatDate(date: Date): string {
  return date.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  })
}

/**
 * Ссылка на день расписания: дата занятия + контекст (преподаватель, если
 * изменение его, иначе группа). Страница расписания читает их из query.
 */
function buildDayHref(
  item: ScheduleHistoryItem,
  date: Date,
  dayIndex: number,
): string {
  const params = new URLSearchParams({ view: "day" })
  const dateValue = toIsoDate(date)
  if (dateValue) params.set("date", dateValue)
  else {
    params.set("week", String(item.week))
    params.set("day", String(dayIndex))
  }
  if (item.teacherId) params.set("teacherId", item.teacherId)
  else if (item.groupId) params.set("groupId", item.groupId)
  return `/schedule?${params.toString()}`
}

function formatAppliedAt(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ""
  return date.toLocaleString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}

interface ChangeCardProps {
  item: ScheduleHistoryItem
  semesterStartIso?: string
  className?: string
}

export default function ChangeCard({
  item,
  semesterStartIso,
  className,
}: ChangeCardProps) {
  const isRemove = item.changeType === "Remove"
  const meta = CHANGE_KIND_META[changeTagKind(item.changeType)]
  const Icon = meta.icon
  const selfStudy = isSelfStudy(item)
  const informational = isInformationalNote(item.note)
  // Перенос — это добавление с отметкой «вм.X», поэтому пара «откуда» берётся
  // из примечания, а не из отдельного поля.
  const movedFrom = isRemove
    ? null
    : (movePairFromNote(item.note) ?? item.removedNumberPair)
  const dayIndex = DAY_OFFSET[item.dayOfWeek] ?? 0
  const date = dateForLesson(semesterStartIso, item.week, dayIndex)
  const appliedAt = formatAppliedAt(item.appliedAt)

  const dayLabel = dayLabelFromString(item.dayOfWeek)
  // Ссылка «Открыть день» ведёт сразу на нужный день с нужной группой
  // или преподавателем: иначе расписание открывается пустым.
  const dayHref = buildDayHref(item, date, dayIndex)
  const pairLabel =
    movedFrom != null && movedFrom !== item.numberPair
      ? `пара ${movedFrom} → ${item.numberPair}`
      : `пара ${item.numberPair}`

  // Преподаватель снимаемого занятия — из Removed*, иначе в «вместо» стоял бы
  // преподаватель нового занятия.
  const removedLesson = lessonLine(
    item.removedSubject ?? item.subject,
    item.removedTeacherName ?? item.teacherName,
  )
  const newLesson = lessonLine(
    isRemove ? null : item.subject,
    isRemove ? null : item.teacherName,
  )
  // При переносе снимается само вводимое занятие — стрелка «X => X» ничего бы
  // не сказала, поэтому показываем пару один раз.
  const replaces =
    !isRemove &&
    item.removedSubject != null &&
    item.removedSubject.trim().toLowerCase() !==
      (item.subject ?? "").trim().toLowerCase()
  // Перенос: занятие само переезжает, поэтому стрелки «старое => новое» нет —
  // называем освобождаемую пару отдельной строкой.
  const movedFromLesson =
    !isRemove && movedFrom != null && !replaces
      ? { pair: movedFrom, lesson: lessonLine(item.removedSubject, item.removedTeacherName) }
      : null

  return (
    <Card
      className={cn("gap-0 py-4", className)}
      role="article"
      aria-label={`${meta.label}: ${item.subject}, ${item.groupName}`}
    >
      <CardContent className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline" className={meta.className}>
            <Icon aria-hidden /> {meta.label}
          </Badge>
          {selfStudy && (
            <Badge variant="outline" className={SELF_STUDY_BADGE}>
              {informational ? (
                <>
                  <Info aria-hidden /> Только информация
                </>
              ) : (
                <>
                  <BookOpen aria-hidden /> Сам.р.
                </>
              )}
            </Badge>
          )}
          <span className="inline-flex items-center gap-1 text-sm font-semibold text-foreground">
            <CalendarDays className="size-3.5 text-muted-foreground" aria-hidden />
            {formatDate(date)}
          </span>
          <span className="text-sm text-muted-foreground">
            {dayLabel}, {item.week}-я неделя
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{item.groupName}</span>
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5" aria-hidden />
            {pairLabel}
          </span>
          {item.room && (
            <span className="inline-flex items-center gap-1">
              <DoorOpen className="size-3.5" aria-hidden />
              ауд. {item.room}
            </span>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2 text-base">
          {replaces ? (
            <span className="flex flex-wrap items-center gap-1.5">
              <span className="text-muted-foreground">{removedLesson}</span>
              <span className={REPLACE_ARROW_CLASS} aria-hidden>
                {"=>"}
              </span>
              <span className="font-medium">{newLesson}</span>
            </span>
          ) : isRemove ? (
            // При снятии ничего не вводится, поэтому предмет показываем тот,
            // который уходит из пары: иначе строка предмета была бы пустой.
            <span className="font-medium">{removedLesson}</span>
          ) : (
            <span className="font-medium">{newLesson}</span>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          {!item.teacherName && (
            <span className="inline-flex items-center gap-1">
              <User className="size-3.5" aria-hidden />
              Преподаватель не указан
            </span>
          )}
          {movedFromLesson && (
            <span className="inline-flex items-center gap-1">
              <ArrowRightLeft className="size-3.5" aria-hidden />
              С пары {movedFromLesson.pair}: {movedFromLesson.lesson}
            </span>
          )}
          {informational && (
            <span className="inline-flex items-center gap-1">
              <Info className="size-3.5" aria-hidden />
              Пара в расписание не вставала — только пометка
            </span>
          )}
          {item.note && <span>Примечание: {item.note}</span>}
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-xs text-muted-foreground">
            Применено: {appliedAt || "—"}
          </span>
          <Link
            href={dayHref}
 className={cn(FOCUS_RING, "inline-flex items-center gap-1 text-sm font-medium text-primary underline-offset-4 hover:underline rounded")}
          >
            Открыть день расписания
          </Link>
        </div>
      </CardContent>
    </Card>
  )
}
