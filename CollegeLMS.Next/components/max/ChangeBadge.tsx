"use client"

import type { ChangeTag } from "@/types/correction"
import type { LucideIcon } from "lucide-react"
import { Plus, Minus, BookOpen } from "lucide-react"
import {
  changeTagKind,
  changeTagLabel,
  isInformationalNote,
  isSelfStudyNote,
  primaryChangeTag,
} from "@/lib/change-tags"

/** Исходов два: добавлено и снято. */
const META: Record<"Add" | "Remove", { icon: LucideIcon }> = {
  Add: { icon: Plus },
  Remove: { icon: Minus },
}

const SELF_STUDY_ICON = BookOpen

interface ChangeBadgeProps {
  tags: ChangeTag[]
}

/**
 * Бейдж изменений в паре расписания мини-приложения. Ровно один бейдж на пару:
 * остальные изменения перечислены в подсказке, чтобы бейджи не наезжали.
 */
export default function ChangeBadge({ tags }: ChangeBadgeProps) {
  const tag = primaryChangeTag(tags)
  if (!tag) return null

  const kind = changeTagKind(tag.changeType)
  // Пара, которой в расписании нет, а есть только пометка, — самостоятельная
  // работа, а не изменение пары.
  const selfStudy =
    isSelfStudyNote(tag.note) &&
    (isInformationalNote(tag.note) || tag.changeType === "Remove")
  const Icon = selfStudy ? SELF_STUDY_ICON : META[kind].icon
  const modifier = selfStudy ? "selfstudy" : kind.toLowerCase()

  return (
    <span className={`max-app__badge max-app__badge--${modifier}`}>
      <Icon size={12} aria-hidden />
      {changeTagLabel(tag)}
    </span>
  )
}
