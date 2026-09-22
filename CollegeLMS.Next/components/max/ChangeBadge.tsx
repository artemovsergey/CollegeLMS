"use client"

import { Plus, Minus, Repeat, ArrowRightLeft, BookOpen } from "lucide-react"
import type { LucideIcon } from "lucide-react"
import type { ChangeTag, CorrectionChangeType } from "@/types/correction"

const META: Record<CorrectionChangeType, { label: string; icon: LucideIcon }> = {
  Add: { label: "Добавлено", icon: Plus },
  Remove: { label: "Снято", icon: Minus },
  Replace: { label: "Замена", icon: Repeat },
  Move: { label: "Перенос", icon: ArrowRightLeft },
}

export default function ChangeBadge({ tag }: { tag: ChangeTag }) {
  const selfStudy =
    (tag.changeType === "Add" || tag.changeType === "Remove") &&
    tag.note?.trim().toLowerCase() === "сам.р."

  const meta = META[tag.changeType]
  const Icon = meta.icon
  const mainBadge = (
    <span
      className={`max-app__badge max-app__badge--${tag.changeType.toLowerCase()}`}
    >
      <Icon size={12} aria-hidden />
      {meta.label}
    </span>
  )

  if (selfStudy) {
    return (
      <>
        {mainBadge}
        <span className="max-app__badge max-app__badge--selfstudy">
          <BookOpen size={12} aria-hidden />
          Сам.р.
        </span>
      </>
    )
  }

  return mainBadge
}