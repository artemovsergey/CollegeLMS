"use client"

import { useEffect, useState } from "react"
import { fetchSchedule } from "@/api/schedule"
import type { ScheduleResponse } from "@/types/schedule"
import { cn } from "@/lib/utils"
import {
  NativeSelect,
  NativeSelectItem,
} from "@/components/ui/native-select"

export interface RemovedPairSelection {
  numberPair: number
  removedSubject: string
  removedTeacherId: string | null
  removedTeacherName: string | null
}

interface RemovePairPickerProps {
  groupId: string | null
  week: number
  dayOfWeek: number
  value: RemovedPairSelection | null
  onChange: (value: RemovedPairSelection | null) => void
  disabled?: boolean
}

export default function RemovePairPicker({
  groupId,
  week,
  dayOfWeek,
  value,
  onChange,
  disabled,
}: RemovePairPickerProps) {
  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!groupId) {
      setEntries([])
      onChange(null)
      return
    }
    let cancelled = false
    setLoading(true)
    fetchSchedule({ groupId, dayOfWeek, pageSize: 200 })
      .then((res) => {
        if (cancelled) return
        const data = res.data
        setEntries(
          data?.items?.filter((entry) => entry.weeks.includes(week)) ?? [],
        )
      })
      .catch(() => {
        if (!cancelled) setEntries([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupId, week, dayOfWeek])

  const options = [...entries].sort(
    (a, b) => a.numberPair - b.numberPair,
  )

  const selectedKey = value
    ? (entries.find(
        (entry) =>
          entry.numberPair === value.numberPair &&
          entry.subject === value.removedSubject,
      )?.id ?? "")
    : ""

  const placeholder = !groupId
    ? "Сначала выберите группу"
    : loading
      ? "Загрузка расписания..."
      : options.length === 0
        ? "Нет занятий в этот день/неделю"
        : "Выберите пару из расписания"

  return (
    <NativeSelect
      value={selectedKey}
      onValueChange={(key) => {
        if (disabled || !groupId) return
        if (!key) {
          onChange(null)
          return
        }
        const entry = options.find((item) => item.id === key)
        if (!entry) return
        onChange({
          numberPair: entry.numberPair,
          removedSubject: entry.subject,
          removedTeacherId: entry.teacherId,
          removedTeacherName: entry.teacherName,
        })
      }}
      placeholder={placeholder}
      className={cn(disabled || !groupId ? "opacity-60" : "")}
    >
      {options.map((entry) => (
        <NativeSelectItem key={entry.id} value={entry.id}>
          {entry.numberPair} пара — {entry.subject}
          {entry.teacherName ? ` (${entry.teacherName})` : ""}
        </NativeSelectItem>
      ))}
    </NativeSelect>
  )
}