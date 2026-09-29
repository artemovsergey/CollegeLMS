"use client"

import type { ChangeTag } from "@/types/correction"
import type { LucideIcon } from "lucide-react"
import { Plus, Minus, Repeat, ArrowRightLeft, BookOpen } from "lucide-react"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import {
  changeTagLabel,
  changeTagTooltip,
  isSelfStudyNote,
  primaryChangeTag,
} from "@/lib/change-tags"
import { cn } from "@/lib/utils"

const CHANGE_META: Record<
  ChangeTag["changeType"],
  { icon: LucideIcon; className: string }
> = {
  Add: {
    icon: Plus,
    className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
  },
  Remove: {
    icon: Minus,
    className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
  },
  Replace: {
    icon: Repeat,
    className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300",
  },
  Move: {
    icon: ArrowRightLeft,
    className: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300",
  },
}

const SELF_STUDY_META = {
  icon: BookOpen,
  className:
    "bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300",
}

interface ChangeTagBadgeProps {
  tags: ChangeTag[]
  className?: string
}

/**
 * Бейдж изменений в паре расписания. Показывается ровно один бейдж на пару:
 * остальные изменения (неделя, предмет «вместо», примечание) уходят в подсказку,
 * иначе несколько операций над одной парой дают наложенные друг на друга бейджи.
 */
export default function ChangeTagBadge({ tags, className }: ChangeTagBadgeProps) {
  const tag = primaryChangeTag(tags)
  if (!tag) return null

  const meta = isSelfStudyNote(tag.note) ? SELF_STUDY_META : CHANGE_META[tag.changeType]
  const Icon = meta.icon
  const label = changeTagLabel(tag)

  return (
    <TooltipProvider delayDuration={0}>
      <Tooltip>
        <TooltipTrigger asChild>
          <span
            className={cn(
              "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium leading-none",
              meta.className,
              className,
            )}
          >
            <Icon className="size-3" aria-hidden />
            {label}
          </span>
        </TooltipTrigger>
        <TooltipContent>
          <span className="block whitespace-pre-line text-left">
            {changeTagTooltip(tags)}
          </span>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
