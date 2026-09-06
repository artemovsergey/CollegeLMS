"use client"

import { cn } from "@/lib/utils"
import { ChevronDown } from "lucide-react"

interface NativeSelectProps {
  value: string
  onValueChange: (value: string) => void
  placeholder?: string
  className?: string
  children: React.ReactNode
}

export function NativeSelect({
  value,
  onValueChange,
  placeholder,
  className,
  children,
}: NativeSelectProps) {
  return (
    <div className={cn("relative", className)}>
      <select
        value={value}
        onChange={(e) => onValueChange(e.target.value)}
        className="h-9 w-full appearance-none rounded-md border border-input bg-transparent px-3 pr-8 text-sm shadow-xs transition-[color,box-shadow] outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute right-2 top-1/2 size-4 -translate-y-1/2 opacity-50" />
    </div>
  )
}

interface NativeSelectItemProps {
  value: string
  children: React.ReactNode
}

export function NativeSelectItem({ value, children }: NativeSelectItemProps) {
  return <option value={value}>{children}</option>
}
