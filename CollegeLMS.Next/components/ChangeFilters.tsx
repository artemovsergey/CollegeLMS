"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { Filter, Search, SearchX, X } from "lucide-react"
import type { Result, TeacherResponse } from "@/types"
import type { CorrectionChangeType } from "@/types/correction"
import { searchSchedule } from "@/api/schedule"
import type { ScheduleSearchGroup } from "@/api/schedule"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import FilterSelect from "@/components/FilterSelect"

export interface ChangeFiltersValue {
  week?: number
  date?: string
  from?: string
  to?: string
  groupId?: string
  groupName?: string
  teacherId?: string
  changeType?: CorrectionChangeType
}

const CHANGE_TYPE_OPTIONS: { value: CorrectionChangeType; label: string }[] = [
  { value: "Add", label: "Добавлено" },
  { value: "Remove", label: "Снято" },
  { value: "Replace", label: "Замена" },
  { value: "Move", label: "Перенос" },
]

export function hasActiveChangeFilters(value: ChangeFiltersValue): boolean {
  return Boolean(
    value.week ||
      value.date ||
      value.from ||
      value.to ||
      value.groupId ||
      value.teacherId ||
      value.changeType,
  )
}

interface ChangeFiltersProps {
  value: ChangeFiltersValue
  onChange: (patch: Partial<ChangeFiltersValue>) => void
  onReset: () => void
  totalWeeks: number
}

export default function ChangeFilters({
  value,
  onChange,
  onReset,
  totalWeeks,
}: ChangeFiltersProps) {
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [groupQuery, setGroupQuery] = useState("")
  const [groupResults, setGroupResults] = useState<ScheduleSearchGroup[]>([])
  const [groupLoading, setGroupLoading] = useState(false)
  const [groupOpen, setGroupOpen] = useState(false)
  const groupBoxRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    let cancelled = false
    api
      .get<Result<TeacherResponse[]>>("/api/teachers")
      .then(({ data }) => {
        if (!cancelled && data.isSuccess && data.data) {
          setTeachers(data.data)
        }
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    const q = groupQuery.trim()
    if (q.length < 2) {
      setGroupResults([])
      setGroupLoading(false)
      return
    }
    let cancelled = false
    const timer = window.setTimeout(async () => {
      setGroupLoading(true)
      try {
        const res = await searchSchedule(q)
        if (!cancelled && res.isSuccess && res.data) {
          setGroupResults(res.data.groups)
        }
      } catch {
        if (!cancelled) setGroupResults([])
      } finally {
        if (!cancelled) setGroupLoading(false)
      }
    }, 300)
    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [groupQuery])

  useEffect(() => {
    function onMouseDown(event: MouseEvent) {
      if (!groupBoxRef.current?.contains(event.target as Node)) {
        setGroupOpen(false)
      }
    }
    document.addEventListener("mousedown", onMouseDown)
    return () => document.removeEventListener("mousedown", onMouseDown)
  }, [])

  const pickGroup = useCallback(
    (group: ScheduleSearchGroup) => {
      onChange({ groupId: group.id, groupName: group.name })
      setGroupQuery("")
      setGroupResults([])
      setGroupOpen(false)
    },
    [onChange],
  )

  const weekOptions =
    totalWeeks > 0 ? Array.from({ length: totalWeeks }, (_, i) => i + 1) : []

  return (
    <section
      className="flex flex-col gap-4 rounded-lg border bg-card p-4"
      aria-label="Фильтры изменений"
    >
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Filter className="size-4" aria-hidden />
        Фильтры
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        <FilterSelect
          id="changes-week"
          label="Неделя"
          value={value.week ? String(value.week) : "all"}
          onChange={(e) =>
            onChange({
              week: e.target.value === "all" ? undefined : Number(e.target.value),
            })
          }
        >
          <option value="all">Все недели</option>
          {weekOptions.map((week) => (
            <option key={week} value={week}>
              {week}
            </option>
          ))}
        </FilterSelect>

        <div className="flex flex-col">
          <Label htmlFor="changes-date" className="mb-1.5">
            Дата проведения
          </Label>
          <Input
            id="changes-date"
            type="date"
            value={value.date ?? ""}
            onChange={(e) => onChange({ date: e.target.value || undefined })}
          />
        </div>

        <div className="flex flex-col">
          <Label htmlFor="changes-from" className="mb-1.5">
            Период: с
          </Label>
          <Input
            id="changes-from"
            type="date"
            value={value.from ?? ""}
            onChange={(e) => onChange({ from: e.target.value || undefined })}
          />
        </div>

        <div className="flex flex-col">
          <Label htmlFor="changes-to" className="mb-1.5">
            Период: по
          </Label>
          <Input
            id="changes-to"
            type="date"
            value={value.to ?? ""}
            onChange={(e) => onChange({ to: e.target.value || undefined })}
          />
        </div>

        <FilterSelect
          id="changes-teacher"
          label="Преподаватель"
          value={value.teacherId ?? "all"}
          onChange={(e) =>
            onChange({
              teacherId: e.target.value === "all" ? undefined : e.target.value,
            })
          }
        >
          <option value="all">Все преподаватели</option>
          {teachers.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.fullName}
            </option>
          ))}
        </FilterSelect>

        <FilterSelect
          id="changes-type"
          label="Тип изменения"
          value={value.changeType ?? "all"}
          onChange={(e) =>
            onChange({
              changeType:
                e.target.value === "all"
                  ? undefined
                  : (e.target.value as CorrectionChangeType),
            })
          }
        >
          <option value="all">Все типы</option>
          {CHANGE_TYPE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </FilterSelect>
      </div>

      <div className="flex flex-col sm:max-w-md">
        <Label htmlFor="changes-group" className="mb-1.5">
          Группа
        </Label>
        {value.groupId ? (
          <div className="flex h-9 items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 text-sm dark:bg-input/30">
            <span className="truncate">
              {value.groupName ?? "Выбранная группа"}
            </span>
            <button
              type="button"
              onClick={() =>
                onChange({ groupId: undefined, groupName: undefined })
              }
              className="inline-flex size-6 items-center justify-center rounded-md text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              aria-label="Сбросить группу"
            >
              <X className="size-4" aria-hidden />
            </button>
          </div>
        ) : (
          <div className="relative" ref={groupBoxRef}>
            <Search
              className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden
            />
            <Input
              id="changes-group"
              type="search"
              placeholder="Начните вводить название группы"
              value={groupQuery}
              onChange={(e) => {
                setGroupQuery(e.target.value)
                setGroupOpen(true)
              }}
              onFocus={() => setGroupOpen(true)}
              className="pl-9"
              autoComplete="off"
              aria-controls="changes-group-results"
              aria-expanded={groupOpen && groupQuery.trim().length >= 2}
            />
            {groupOpen && groupQuery.trim().length >= 2 && (
              <ul
                id="changes-group-results"
                role="listbox"
                aria-label="Найденные группы"
                className="absolute z-20 mt-1 max-h-64 w-full overflow-auto rounded-md border bg-popover p-1 shadow-md"
              >
                {groupLoading ? (
                  <li className="px-3 py-2 text-sm text-muted-foreground">
                    Поиск…
                  </li>
                ) : groupResults.length === 0 ? (
                  <li className="px-3 py-2 text-sm text-muted-foreground">
                    Ничего не найдено
                  </li>
                ) : (
                  groupResults.map((group) => (
                    <li key={group.id}>
                      <button
                        type="button"
                        role="option"
                        aria-selected={false}
                        onClick={() => pickGroup(group)}
                        className="w-full rounded-sm px-3 py-2 text-left text-sm hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      >
                        {group.name}
                      </button>
                    </li>
                  ))
                )}
              </ul>
            )}
          </div>
        )}
      </div>

      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={onReset}
          disabled={!hasActiveChangeFilters(value)}
        >
          <SearchX className="size-3.5" aria-hidden />
          Сбросить фильтры
        </Button>
      </div>
    </section>
  )
}
