"use client"

import type { GroupResponse, TeacherResponse } from "@/types"
import { DAYS } from "@/types/schedule"
import { Filter, SearchX, Calendar } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface ScheduleFilterBarProps {
  groups: GroupResponse[]
  teachers: TeacherResponse[]
  selectedGroupId: string
  selectedTeacherId: string
  selectedDayOfWeek: string
  onGroupChange: (v: string) => void
  onTeacherChange: (v: string) => void
  onDayChange: (v: string) => void
  onToday: () => void
  onClear: () => void
}

export default function ScheduleFilterBar({
  groups,
  teachers,
  selectedGroupId,
  selectedTeacherId,
  selectedDayOfWeek,
  onGroupChange,
  onTeacherChange,
  onDayChange,
  onToday,
  onClear,
}: ScheduleFilterBarProps) {
  const hasFilters = selectedGroupId || selectedTeacherId || selectedDayOfWeek

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-lg border bg-card p-4">
      <Filter className="size-4 text-muted-foreground shrink-0" />
      <div className="flex flex-wrap items-center gap-2">
        <Select value={selectedGroupId} onValueChange={onGroupChange}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Все группы" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Все группы</SelectItem>
            {groups.map(g => (
              <SelectItem key={g.id} value={g.id}>
                {g.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={selectedTeacherId} onValueChange={onTeacherChange}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Все преподаватели" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Все преподаватели</SelectItem>
            {teachers.map(t => (
              <SelectItem key={t.id} value={t.id}>
                {t.fullName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={selectedDayOfWeek} onValueChange={onDayChange}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Все дни" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Все дни</SelectItem>
            {DAYS.filter(d => d.value >= 1 && d.value <= 5).map(d => (
              <SelectItem key={d.value} value={String(d.value)}>
                {d.full}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Button variant="outline" size="sm" onClick={onToday}>
          <Calendar className="size-3.5" />
          Сегодня
        </Button>

        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={onClear}>
            <SearchX className="size-3.5" />
            Сбросить
          </Button>
        )}
      </div>
    </div>
  )
}
