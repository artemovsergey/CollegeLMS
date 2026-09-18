"use client"

import * as React from "react"
import { ChevronDown } from "lucide-react"
import { cn } from "@/lib/utils"

interface FilterSelectProps
  extends React.SelectHTMLAttributes<HTMLSelectElement> {
  /** Видимый заголовок поля — привязывается к select через htmlFor/id. */
  label: string
  id: string
  containerClassName?: string
  children: React.ReactNode
}

/**
 * Нативный select с видимым label — для фильтров, где важны доступное имя
 * и сохраняемая разметка проекта (в отличие от Radio-комбобоксов shadcn).
 */
export default function FilterSelect({
  label,
  id,
  containerClassName,
  className,
  children,
  ...props
}: FilterSelectProps) {
  return (
    <div className={containerClassName}>
      <label
        htmlFor={id}
        className="mb-1.5 block text-sm font-medium text-foreground"
      >
        {label}
      </label>
      <div className="relative">
        <select
          id={id}
          className={cn(
            "h-9 w-full appearance-none rounded-md border border-input bg-transparent px-3 pr-8 text-sm shadow-xs transition-[color,box-shadow] outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
            className,
          )}
          {...props}
        >
          {children}
        </select>
        <ChevronDown
          className="pointer-events-none absolute right-2 top-1/2 size-4 -translate-y-1/2 opacity-50"
          aria-hidden
        />
      </div>
    </div>
  )
}
