"use client"

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
import { dateForLesson } from "@/lib/semester"
import { dayLabelFromString } from "@/lib/max-lesson"
import { toIsoDate } from "@/api/schedule"
import {
  changeTagKind,
  isInformationalNote,
  movePairFromNote,
} from "@/lib/change-tags"

/** Исходов два: добавлено и снято. */
const CHANGE_KIND_META: Record<
  "Add" | "Remove",
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: { label: "Добавлено", icon: Plus, className: "max-app__badge--add" },
  Remove: { label: "Снято", icon: Minus, className: "max-app__badge--remove" },
}

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

/** Ссылка на день расписания с контекстом изменения. */
function buildDayHref(item: ScheduleHistoryItem, date: Date): string {
  const params = new URLSearchParams({ route: "day", date: toIsoDate(date) })
  if (item.teacherId) params.set("teacherId", item.teacherId)
  else if (item.groupId) params.set("groupId", item.groupId)
  return `/max/schedule?${params.toString()}`
}

/** «Предмет Преподаватель» одной строкой. */
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
  highlighted?: boolean
}

/**
 * Карточка изменения расписания в мини-приложении — та же структура, что и в вебе:
 * тип, «Сам.р.», дата занятия с днём недели и номером недели, группа, пара, аудитория,
 * зачёркнутый старый предмет → новый, преподаватель, примечание, «Применено» и ссылка на день.
 */
export default function ChangeCard({
  item,
  semesterStartIso,
  highlighted = false,
}: ChangeCardProps) {
  const isRemove = item.changeType === "Remove"
  const meta = CHANGE_KIND_META[changeTagKind(item.changeType)]
  const Icon = meta.icon
  const selfStudy = isSelfStudy(item)
  const informational = isInformationalNote(item.note)
  const dayIndex = DAY_OFFSET[item.dayOfWeek] ?? 0
  const date = dateForLesson(semesterStartIso, item.week, dayIndex)
  const appliedAt = formatAppliedAt(item.appliedAt)

  const dayLabel = dayLabelFromString(item.dayOfWeek)
  // Разделитель тот же, что в вебе и в боте: тонкий «=>».
  // Пара «откуда» берётся из примечания: после объединения операций перенос —
  // это добавление с отметкой «вм.X».
  const movedFrom = isRemove
    ? null
    : (movePairFromNote(item.note) ?? item.removedNumberPair)
  const pairLabel =
    movedFrom != null && movedFrom !== item.numberPair
      ? `пара ${movedFrom} => ${item.numberPair}`
      : `пара ${item.numberPair}`

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
  // «Предмет Преподаватель => Предмет Преподаватель» — так же, как в вебе и в боте.
  // Ссылка «Открыть день» ведёт сразу на нужный день с нужной группой
  // или преподавателем: иначе расписание открывается пустым.
  const dayHref = buildDayHref(item, date)
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

  return (
    <article
      className={`max-app__change-card${
        highlighted ? " max-app__change-card--highlight" : ""
      }`}
      aria-label={`${meta.label}: ${item.subject}, ${item.groupName}`}
    >
      <div className="max-app__change-card-head">
        <span className={`max-app__badge ${meta.className}`}>
          <Icon size={12} aria-hidden /> {meta.label}
        </span>
        {selfStudy && (
          <span className="max-app__badge max-app__badge--selfstudy">
            {informational ? (
              <>
                <Info size={12} aria-hidden /> Только информация
              </>
            ) : (
              <>
                <BookOpen size={12} aria-hidden /> Сам.р.
              </>
            )}
          </span>
        )}
      </div>

      <div className="max-app__change-card-row">
        <span className="max-app__change-card-date">
          <CalendarDays size={14} aria-hidden /> {formatDate(date)}
        </span>
        <span className="max-app__change-card-muted">
          {dayLabel}, {item.week}-я неделя
        </span>
      </div>

      <div className="max-app__change-card-row">
        <span className="max-app__change-card-group">{item.groupName}</span>
        <span className="max-app__change-card-muted max-app__change-card-icon">
          <Clock size={14} aria-hidden /> {pairLabel}
        </span>
        {item.room && (
          <span className="max-app__change-card-muted max-app__change-card-icon">
            <DoorOpen size={14} aria-hidden /> ауд. {item.room}
          </span>
        )}
      </div>

      <div className="max-app__change-card-subject">
        {replaces ? (
          <>
            <span className="max-app__change-card-removed">{removedLesson}</span>
            <span className="max-app__change-card-arrow" aria-hidden>
              {"=>"}
            </span>
            <span className="max-app__change-card-new">{newLesson}</span>
          </>
        ) : isRemove ? (
          // При снятии ничего не вводится, поэтому предмет показываем тот,
          // который уходит из пары: иначе строка предмета была бы пустой.
          <span className="max-app__change-card-new">{removedLesson}</span>
        ) : (
          <span className="max-app__change-card-new">{newLesson}</span>
        )}
      </div>

      <div className="max-app__change-card-row">
        {!item.teacherName && (
          <span className="max-app__change-card-muted max-app__change-card-icon">
            <User size={14} aria-hidden /> Преподаватель не указан
          </span>
        )}
        {movedFromLesson && (
          <span className="max-app__change-card-muted max-app__change-card-icon">
            <ArrowRightLeft size={14} aria-hidden /> С пары {movedFromLesson.pair}:{" "}
            {movedFromLesson.lesson}
          </span>
        )}
        {informational && (
          <span className="max-app__change-card-muted max-app__change-card-icon">
            <Info size={14} aria-hidden /> Пара в расписание не вставала
          </span>
        )}
        {item.note && (
          <span className="max-app__change-card-muted max-app__change-card-note">
            Примечание: {item.note}
          </span>
        )}
      </div>

      <div className="max-app__change-card-foot">
        <span className="max-app__change-card-applied">
          Применено: {appliedAt || "—"}
        </span>
        <Link
          href={dayHref}
          className="max-app__change-card-link"
          aria-label={`Открыть расписание на ${toIsoDate(date)}`}
        >
          <CalendarDays size={16} aria-hidden /> Открыть день
        </Link>
      </div>
    </article>
  )
}
