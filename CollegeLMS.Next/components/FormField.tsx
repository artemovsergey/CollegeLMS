"use client"

import { type ReactNode, useState } from "react"
import { CircleHelp } from "lucide-react"
import { Label } from "@/components/ui/label"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"

interface FormFieldProps {
  id: string
  label: string
  error?: string
  hint?: string
  required?: boolean
  showAsterisk?: boolean
  children: ReactNode
}

export default function FormField({ id, label, error, hint, required, showAsterisk = true, children }: FormFieldProps) {
  const [focused, setFocused] = useState(false)

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>
        {label}
        {required && showAsterisk && <span className="text-destructive"> *</span>}
        {hint && (
          <Tooltip delayDuration={0}>
            <TooltipTrigger asChild>
              <button
                type="button"
                tabIndex={-1}
                aria-label={`Подсказка: ${hint}`}
                className="ml-1.5 inline-flex h-4 w-4 items-center justify-center rounded-full text-muted-foreground hover:text-fg"
              >
                <CircleHelp className="h-3.5 w-3.5" />
              </button>
            </TooltipTrigger>
            <TooltipContent>{hint}</TooltipContent>
          </Tooltip>
        )}
      </Label>
      <div
        onFocusCapture={() => setFocused(true)}
        onBlurCapture={() => setFocused(false)}
      >
        {children}
      </div>
      {hint && focused && !error && <p className="text-xs text-muted-foreground">{hint}</p>}
      {error && (
        <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  )
}
