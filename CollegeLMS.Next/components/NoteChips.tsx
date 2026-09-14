"use client"

import { cn } from "@/lib/utils"

export const NOTE_CHIPS = ["сам.р.", "замена", "перенос"]

interface NoteChipsProps {
  value: string
  onChange: (value: string) => void
  hints?: string[]
  className?: string
}

export function NoteChips({
  value,
  onChange,
  hints = NOTE_CHIPS,
  className,
}: NoteChipsProps) {
  return (
    <div className={cn("flex flex-wrap gap-1.5", className)}>
      {hints.map((hint) => {
        const active = value.trim() === hint
        return (
          <button
            key={hint}
            type="button"
            onClick={() => onChange(active ? "" : hint)}
            aria-pressed={active}
            className={cn(
              "h-8 rounded-full border px-3 text-xs font-medium transition-colors",
              active
                ? "border-primary bg-primary text-primary-foreground"
                : "border-input bg-background text-muted-foreground hover:bg-muted",
            )}
          >
            {hint}
          </button>
        )
      })}
    </div>
  )
}