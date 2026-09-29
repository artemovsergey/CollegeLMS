"use client"

import type { ChangeTag } from "@/types/correction"
import type { LucideIcon } from "lucide-react"
import { Plus, Minus, Repeat, ArrowRightLeft, BookOpen } from "lucide-react"
import { changeTagLabel, isSelfStudyNote, primaryChangeTag } from "@/lib/change-tags"

const META: Record<ChangeTag["changeType"], { icon: LucideIcon }> = {
  Add: { icon: Plus },
  Remove: { icon: Minus },
  Replace: { icon: Repeat },
  Move: { icon: ArrowRightLeft },
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

  const selfStudy = isSelfStudyNote(tag.note)
  const Icon = selfStudy ? SELF_STUDY_ICON : META[tag.changeType].icon
  const modifier = selfStudy
    ? "selfstudy"
    : tag.changeType.toLowerCase()

  return (
    <span className={`max-app__badge max-app__badge--${modifier}`}>
      <Icon size={12} aria-hidden />
      {changeTagLabel(tag)}
    </span>
  )
}
