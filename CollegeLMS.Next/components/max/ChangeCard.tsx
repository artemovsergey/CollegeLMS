"use client"

import Link from "next/link"
import type { LucideIcon } from "lucide-react"
import {
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
  ArrowLeft,
  BookOpen,
  CalendarDays,
  Clock,
  DoorOpen,
  User,
} from "lucide-react"
import type { ScheduleHistoryItem, CorrectionChangeType } from "@/types/correction"
import { dateForLesson } from "@/lib/semester"
import { dayLabelFromString } from "@/lib/max-lesson"
import { toIsoDate } from "@/api/schedule"

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: { label: "Добавлено", icon: Plus, className: "max-app__badge--add" },
  Remove: { label: "Снято", icon: Minus, className: "max-app__badge--remove" },
  Replace: { label: "Замена", icon: Repeat, className: "max-app__badge--replace" },
  Move: { label: "Перенос", icon: ArrowRightLeft, className: "max-app__badge--move" },
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

function isSelfStudy(item: ScheduleHistoryItem): boolean {
  return item.note?.trim().toLowerCase() === "сам.р."
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
  const meta = CHANGE_TYPE_META[item.changeType]
  const Icon = meta.icon
  const selfStudy = isSelfStudy(item)
  const dayIndex = DAY_OFFSET[item.dayOfWeek] ?? 0
  const date = dateForLesson(semesterStartIso, item.week, dayIndex)
  const appliedAt = formatAppliedAt(item.appliedAt)

  const dayLabel = dayLabelFromString(item.dayOfWeek)
  const pairLabel =
    item.removedNumberPair != null &&
    item.removedNumberPair !== item.numberPair &&
    (item.changeType === "Replace" || item.changeType === "Move")
      ? `пара ${item.removedNumberPair} → ${item.numberPair}`
      : `пара ${item.numberPair}`

  const isMoveOrReplace =
    item.changeType === "Replace" || item.changeType === "Move"
  const primarySubject =
    item.changeType === "Remove"
      ? item.removedSubject ?? item.subject
      : item.subject

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
            <BookOpen size={12} aria-hidden /> Сам.р.
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
        {isMoveOrReplace && item.removedSubject && (
          <>
            <span className="max-app__change-card-removed">
              {item.removedSubject}
            </span>
            <ArrowLeft size={16} className="max-app__change-card-arrow" aria-hidden />
          </>
        )}
        <span className="max-app__change-card-new">{primarySubject}</span>
      </div>

      <div className="max-app__change-card-row">
        <span className="max-app__change-card-muted max-app__change-card-icon">
          <User size={14} aria-hidden /> {item.teacherName ?? "Преподаватель не указан"}
        </span>
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
          href={`/max/schedule?route=day&date=${toIsoDate(date)}`}
          className="max-app__change-card-link"
          aria-label={`Открыть расписание на ${toIsoDate(date)}`}
        >
          <CalendarDays size={16} aria-hidden /> Открыть день
        </Link>
      </div>
    </article>
  )
}
