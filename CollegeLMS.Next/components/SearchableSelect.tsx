"use client"

import { useEffect, useId, useMemo, useRef, useState } from "react"
import { Check, ChevronDown, Search, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { Input } from "@/components/ui/input"

export interface SearchableOption {
  value: string
  label: string
  /** Дополнительная подсказка под названием — например, предметы преподавателя. */
  description?: string
}

interface SearchableSelectProps {
  options: SearchableOption[]
  value: string
  onValueChange: (value: string) => void
  placeholder?: string
  emptyLabel?: string
  searchPlaceholder?: string
  disabled?: boolean
  className?: string
  id?: string
  "aria-label"?: string
}

/**
 * Список с выбором и встроенным поиском по тексту. Нужен там, где групп или
 * преподавателей много: нативный выпадающий список не даёт найти нужное без
 * прокрутки, а примитив `Select` не умеет фильтровать. Поэтому контрол
 * собственный, но на тех же токенах и с теми же правилами доступности.
 */
export function SearchableSelect({
  options,
  value,
  onValueChange,
  placeholder = "Выберите значение",
  emptyLabel = "Ничего не найдено",
  searchPlaceholder = "Поиск…",
  disabled,
  className,
  id,
  ...aria
}: SearchableSelectProps) {
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")
  const [activeIndex, setActiveIndex] = useState(0)
  const rootRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return options
    return options.filter(
      (option) =>
        option.label.toLowerCase().includes(q) ||
        (option.description ?? "").toLowerCase().includes(q),
    )
  }, [options, query])

  const selected = useMemo(
    () => options.find((option) => option.value === value) ?? null,
    [options, value],
  )

  useEffect(() => {
    if (!open) return
    setQuery("")
    setActiveIndex(0)
    const focus = window.setTimeout(() => searchRef.current?.focus(), 0)
    return () => window.clearTimeout(focus)
  }, [open])

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener("mousedown", onPointerDown)
    return () => document.removeEventListener("mousedown", onPointerDown)
  }, [open])

  useEffect(() => {
    if (!open) return
    if (activeIndex >= filtered.length) setActiveIndex(0)
  }, [filtered.length, activeIndex, open])

  const commit = (next: string) => {
    onValueChange(next)
    setOpen(false)
  }

  const onKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === "ArrowDown") {
      event.preventDefault()
      setActiveIndex((index) => (filtered.length ? (index + 1) % filtered.length : 0))
    } else if (event.key === "ArrowUp") {
      event.preventDefault()
      setActiveIndex((index) =>
        filtered.length ? (index - 1 + filtered.length) % filtered.length : 0,
      )
    } else if (event.key === "Enter") {
      event.preventDefault()
      const option = filtered[activeIndex]
      if (option) commit(option.value)
    } else if (event.key === "Escape") {
      event.preventDefault()
      setOpen(false)
    }
  }

  return (
    <div ref={rootRef} className={cn("relative", className)}>
      <button
        type="button"
        id={id}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listId : undefined}
        aria-label={aria["aria-label"]}
        onClick={() => setOpen((current) => !current)}
        className={cn(
          "flex h-9 w-full items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 text-left text-sm shadow-xs transition-[color,box-shadow] outline-none",
          "focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50",
          "disabled:cursor-not-allowed disabled:opacity-50",
          "dark:bg-input/30",
          open && "border-ring ring-[3px] ring-ring/50",
        )}
      >
        <span className={cn("truncate", !selected && "text-muted-foreground")}>
          {selected?.label ?? placeholder}
        </span>
        <ChevronDown
          className={cn(
            "size-4 shrink-0 opacity-50 transition-transform",
            open && "rotate-180",
          )}
          aria-hidden
        />
      </button>

      {open && (
        <div className="absolute z-40 mt-1 w-full min-w-[16rem] rounded-md border bg-popover p-1 shadow-lg">
          <div className="relative">
            <Search
              className="pointer-events-none absolute left-2 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden
            />
            <Input
              ref={searchRef}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={onKeyDown}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
              aria-controls={listId}
              className="h-8 pl-8"
            />
            {query && (
              <button
                type="button"
                onClick={() => setQuery("")}
                aria-label="Очистить поиск"
                className="absolute right-1 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground hover:bg-muted"
              >
                <X className="size-3.5" />
              </button>
            )}
          </div>

          <ul
            id={listId}
            role="listbox"
            aria-label={aria["aria-label"] ?? placeholder}
            className="mt-1 max-h-64 overflow-y-auto"
          >
            {filtered.length === 0 && (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">
                {emptyLabel}
              </li>
            )}
            {filtered.map((option, index) => {
              const isSelected = option.value === value
              return (
                <li key={option.value}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={isSelected}
                    onMouseEnter={() => setActiveIndex(index)}
                    onClick={() => commit(option.value)}
                    className={cn(
                      "flex w-full items-start gap-2 rounded-sm px-2 py-1.5 text-left text-sm transition-colors",
                      index === activeIndex ? "bg-accent text-accent-foreground" : "",
                    )}
                  >
                    <Check
                      className={cn(
                        "mt-0.5 size-4 shrink-0",
                        isSelected ? "opacity-100" : "opacity-0",
                      )}
                      aria-hidden
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-medium">
                        {option.label}
                      </span>
                      {option.description && (
                        <span className="block truncate text-xs text-muted-foreground">
                          {option.description}
                        </span>
                      )}
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </div>
  )
}

/** Множественный выбор с тем же поиском: чипы выбранного + список доступных. */
export function SearchableMultiSelect({
  options,
  values,
  onValuesChange,
  placeholder = "Выберите значения",
  emptyLabel = "Ничего не найдено",
  searchPlaceholder = "Поиск…",
  disabled,
  className,
  id,
  ...aria
}: Omit<SearchableSelectProps, "value" | "onValueChange"> & {
  values: string[]
  onValuesChange: (values: string[]) => void
}) {
  const listId = useId()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")
  const rootRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return options
    return options.filter(
      (option) =>
        option.label.toLowerCase().includes(q) ||
        (option.description ?? "").toLowerCase().includes(q),
    )
  }, [options, query])

  const selectedLabels = useMemo(
    () => values.map((value) => options.find((o) => o.value === value)?.label ?? value),
    [options, values],
  )

  useEffect(() => {
    if (!open) return
    setQuery("")
    const focus = window.setTimeout(() => searchRef.current?.focus(), 0)
    return () => window.clearTimeout(focus)
  }, [open])

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener("mousedown", onPointerDown)
    return () => document.removeEventListener("mousedown", onPointerDown)
  }, [open])

  const toggle = (value: string) => {
    onValuesChange(
      values.includes(value)
        ? values.filter((item) => item !== value)
        : [...values, value],
    )
  }

  return (
    <div ref={rootRef} className={cn("relative", className)}>
      <button
        type="button"
        id={id}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listId : undefined}
        aria-label={aria["aria-label"]}
        onClick={() => setOpen((current) => !current)}
        className={cn(
          "flex min-h-9 w-full items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 py-1 text-left text-sm shadow-xs transition-[color,box-shadow] outline-none",
          "focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50",
          "disabled:cursor-not-allowed disabled:opacity-50",
          "dark:bg-input/30",
          open && "border-ring ring-[3px] ring-ring/50",
        )}
      >
        <span
          className={cn(
            "truncate",
            selectedLabels.length === 0 && "text-muted-foreground",
          )}
        >
          {selectedLabels.length === 0
            ? placeholder
            : selectedLabels.join(", ")}
        </span>
        <ChevronDown
          className={cn(
            "size-4 shrink-0 opacity-50 transition-transform",
            open && "rotate-180",
          )}
          aria-hidden
        />
      </button>

      {open && (
        <div className="absolute z-40 mt-1 w-full min-w-[16rem] rounded-md border bg-popover p-1 shadow-lg">
          <div className="relative">
            <Search
              className="pointer-events-none absolute left-2 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden
            />
            <Input
              ref={searchRef}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  event.preventDefault()
                  setOpen(false)
                }
              }}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
              aria-controls={listId}
              className="h-8 pl-8"
            />
          </div>

          <ul
            id={listId}
            role="listbox"
            aria-multiselectable
            aria-label={aria["aria-label"] ?? placeholder}
            className="mt-1 max-h-64 overflow-y-auto"
          >
            {filtered.length === 0 && (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">
                {emptyLabel}
              </li>
            )}
            {filtered.map((option) => {
              const isSelected = values.includes(option.value)
              return (
                <li key={option.value}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={isSelected}
                    onClick={() => toggle(option.value)}
                    className="flex w-full items-start gap-2 rounded-sm px-2 py-1.5 text-left text-sm transition-colors hover:bg-accent hover:text-accent-foreground"
                  >
                    <Check
                      className={cn(
                        "mt-0.5 size-4 shrink-0",
                        isSelected ? "opacity-100" : "opacity-0",
                      )}
                      aria-hidden
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-medium">
                        {option.label}
                      </span>
                      {option.description && (
                        <span className="block truncate text-xs text-muted-foreground">
                          {option.description}
                        </span>
                      )}
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </div>
  )
}
