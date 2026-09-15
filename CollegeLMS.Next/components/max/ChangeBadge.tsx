"use client"

import { Plus, Minus, Repeat, ArrowRightLeft, BookOpen } from "lucide-react"
import type { LucideIcon } from "lucide-react"
import type { ChangeTag, CorrectionChangeType } from "@/types/correction"

const META: Record<CorrectionChangeType, { label: string; icon: LucideIcon }> = {
  Add: { label: "Добавление", icon: Plus },
  Remove: { label: "Снятие", icon: Minus },
  Replace: { label: "Замена", icon: Repeat },
  Move: { label: "Перенос", icon: ArrowRightLeft },
}

export default function ChangeBadge({ tag }: { tag: ChangeTag }) {
  const selfStudy =
    tag.changeType === "Add" && tag.note?.trim().toLowerCase() === "сам.р."

  if (selfStudy) {
    return (
      <span className="max-app__badge max-app__badge--selfstudy">
        <BookOpen size={12} aria-hidden />
        сам.р. · нед. {tag.week}
      </span>
    )
  }

  const meta = META[tag.changeType]
  const Icon = meta.icon
  return (
    <span
      className={`max-app__badge max-app__badge--${tag.changeType.toLowerCase()}`}
    >
      <Icon size={12} aria-hidden />
      {meta.label} · нед. {tag.week}
    </span>
  )
}