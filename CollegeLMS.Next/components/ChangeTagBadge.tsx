"use client"

import type { ChangeTag } from "@/types/correction"
import type { LucideIcon } from "lucide-react"
import { Plus, Minus, BookOpen } from "lucide-react"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import {
  changeTagKind,
  changeTagLabel,
  changeTagTooltip,
  isInformationalNote,
  isSelfStudyNote,
  primaryChangeTag,
  showsSelfStudyTag,
} from "@/lib/change-tags"
import { cn } from "@/lib/utils"

/** Исходов два: добавлено и снято. */
const CHANGE_META: Record<"Add" | "Remove", { icon: LucideIcon; className: string }> =
  {
    Add: {
      icon: Plus,
      className:
        "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300",
    },
    Remove: {
      icon: Minus,
      className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300",
    },
  }

const SELF_STUDY_META = {
  icon: BookOpen,
  iconClass: "text-violet-600 dark:text-violet-400",
  className:
    "bg-violet-100 text-violet-700 dark:bg-violet-950/60 dark:text-violet-300",
}

interface ChangeTagBadgeProps {
  tags: ChangeTag[]
  className?: string
}

/**
 * Бейдж изменений в паре расписания.
 *
 * Показывается один бейдж на пару: остальные изменения (неделя, предмет
 * «вместо», примечание) уходят в подсказку, иначе несколько операций над одной
 * парой дают наложенные друг на друга бейджи. Исключение — «Сам.р.»: он
 * приходит вторым бейджом рядом, потому что это дополнительная пометка, а не
 * отдельная корректировка пары.
 */
export default function ChangeTagBadge({ tags, className }: ChangeTagBadgeProps) {
  const tag = primaryChangeTag(tags)
  if (!tag) return null

  // Подпись «Сам.р.» вместо «Снято» — значит пара осталась, и красный цвет
  // снятия сбивал бы с толку: красим бейдж в цвет самостоятельной работы.
  const selfStudyReplacesLabel =
    isSelfStudyNote(tag.note) && tag.changeType === "Remove"
  // Пара, которой в расписании нет, а есть только пометка, — тоже самостоятельная
  // работа, а не изменение пары.
  const informationalOnly =
    !selfStudyReplacesLabel && isInformationalNote(tag.note)
  const meta =
    selfStudyReplacesLabel || informationalOnly
      ? SELF_STUDY_META
      : CHANGE_META[changeTagKind(tag.changeType)]
  const Icon = meta.icon
  const label = changeTagLabel(tag)
  const SelfStudyIcon = SELF_STUDY_META.icon

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
            {showsSelfStudyTag(tag) && (
              <>
                <SelfStudyIcon
                  className={cn("size-3", SELF_STUDY_META.iconClass)}
                  aria-hidden
                />
                <span className="sr-only">самостоятельная работа</span>
              </>
            )}
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
