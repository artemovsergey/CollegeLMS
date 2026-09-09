"use client"

import type { ChangeTag, CorrectionChangeType } from "@/types/correction"
import type { LucideIcon } from "lucide-react"
import { Plus, Minus, Repeat, ArrowRightLeft } from "lucide-react"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import { cn } from "@/lib/utils"

const CHANGE_META: Record<
  CorrectionChangeType,
  { label: string; icon: LucideIcon; className: string }
> = {
  Add: {
    label: "Добавлено",
    icon: Plus,
    className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    label: "Снято",
    icon: Minus,
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    label: "Замена",
    icon: Repeat,
    className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    label: "Перенос",
    icon: ArrowRightLeft,
    className: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

function tagDetail(tag: ChangeTag): string {
  const parts = [`${CHANGE_META[tag.changeType].label} — неделя ${tag.week}`]
  if (tag.changeType === "Move" && tag.removedNumberPair != null) {
    parts.push(`перенос с пары ${tag.removedNumberPair}`)
  }
  if (tag.removedSubject) {
    parts.push(`вместо: ${tag.removedSubject}`)
  }
  if (tag.note) {
    parts.push(`примечание: ${tag.note}`)
  }
  return parts.join("\n")
}

export default function ChangeTagBadge({ tag }: { tag: ChangeTag }) {
  const meta = CHANGE_META[tag.changeType]
  const Icon = meta.icon
  return (
    <TooltipProvider delayDuration={0}>
      <Tooltip>
        <TooltipTrigger asChild>
          <span
            className={cn(
              "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium leading-none",
              meta.className,
            )}
          >
            <Icon className="size-3" aria-hidden />
            {meta.label} · нед. {tag.week}
          </span>
        </TooltipTrigger>
        <TooltipContent>
          <span className="block whitespace-pre-line text-left">{tagDetail(tag)}</span>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}