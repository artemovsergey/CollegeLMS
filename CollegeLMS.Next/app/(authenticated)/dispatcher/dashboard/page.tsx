"use client"

import { useEffect, useState, useMemo } from "react"
import Link from "next/link"
import { Loader2, AlertTriangle } from "lucide-react"
import type { Result } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"
import api from "@/lib/api"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip"
import { LESSON_TYPE_LABELS, type LessonType } from "@/types/schedule"

interface DispatcherEntry {
  groupId: string
  groupName: string
  subject: string
  room: string
  startTime: string
  endTime: string
  lessonType: LessonType
  changeType: string | null
}

interface DispatcherPairSlot {
  numberPair: number
  startTime: string
  endTime: string
  entries: DispatcherEntry[]
}

interface DispatcherTeacherStatus {
  teacherId: string
  teacherName: string
  totalPairs: number
  entries: DispatcherEntry[]
}

interface DispatcherDashboardResponse {
  date: string
  week: number
  dayOfWeek: number
  slots: DispatcherPairSlot[]
  teachers: DispatcherTeacherStatus[]
}

const LESSON_COLORS: Record<LessonType, string> = {
  Lecture: "#3b82f6",
  Practice: "#10b981",
  Lab: "#f59e0b",
  Exam: "#ef4444",
}

const SEMESTER_START = new Date(2026, 8, 1) // Sep 1 2026

function getMondayOfWeek(date: Date): Date {
  const d = new Date(date)
  const day = d.getDay()
  const diff = (day === 0 ? -6 : 1) - day
  d.setDate(d.getDate() + diff)
  d.setHours(0, 0, 0, 0)
  return d
}

function weekNumber(date: Date): number {
  const monday = getMondayOfWeek(date)
  const semesterMonday = getMondayOfWeek(SEMESTER_START)
  return Math.floor((monday.getTime() - semesterMonday.getTime()) / 604800000) + 1
}

interface GanttBar {
  id: string
  subject: string
  teacherName: string | null
  room: string
  weeks: number[]
  lessonType: LessonType
  groupName: string
}

export default function DispatcherDashboardPage() {
  const [entries, setEntries] = useState<ScheduleResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dashboard, setDashboard] = useState<DispatcherDashboardResponse | null>(null)

  useEffect(() => {
    api
      .get<Result<ScheduleResponse[]>>("/api/schedule", {
        params: { pageSize: 2000 },
      })
      .then((res) => {
        if (res.data.isSuccess && res.data.data)
          setEntries(res.data.data as ScheduleResponse[])
      })
      .catch(() => setError("Ошибка загрузки расписания"))
      .finally(() => setLoading(false))

    const todayStr = new Date().toLocaleDateString("en-CA")
    api
      .get<Result<DispatcherDashboardResponse>>("/api/dispatcher/dashboard", {
        params: { date: todayStr },
      })
      .then((res) => {
        if (res.data.isSuccess && res.data.data) setDashboard(res.data.data)
      })
      .catch(() => setError((prev) => prev ?? "Ошибка загрузки дашборда"))
  }, [])

  const bars = useMemo(() => {
    const map = new Map<string, GanttBar>()
    for (const e of entries) {
      const key = `${e.groupId}|${e.subject}|${e.teacherId}`
      if (map.has(key)) {
        const existing = map.get(key)!
        const merged = [...new Set([...existing.weeks, ...e.weeks])].sort(
          (a, b) => a - b,
        )
        existing.weeks = merged
      } else {
        map.set(key, {
          id: e.id,
          subject: e.subject,
          teacherName: e.teacherName,
          room: e.room,
          weeks: [...e.weeks].sort((a, b) => a - b),
          lessonType: e.lessonType,
          groupName: e.groupName,
        })
      }
    }
    return Array.from(map.values())
  }, [entries])

  const groups = useMemo(() => {
    const nameMap = new Map<string, string>()
    for (const e of entries) nameMap.set(e.groupId, e.groupName)
    return Array.from(nameMap.entries())
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name))
  }, [entries])

  const allWeeks = useMemo(() => {
    const w = new Set<number>()
    for (const b of bars) for (const wk of b.weeks) w.add(wk)
    return w.size > 0 ? Array.from(w).sort((a, b) => a - b) : Array.from({ length: 52 }, (_, i) => i + 1)
  }, [bars])

  const weeksRange = useMemo(() => {
    if (allWeeks.length === 0) return { min: 1, max: 52 }
    return { min: allWeeks[0], max: allWeeks[allWeeks.length - 1] }
  }, [allWeeks])

  const totalWeeks = weeksRange.max - weeksRange.min + 1

  if (loading) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-7xl mx-auto">
        <Loader2 className="size-6 animate-spin text-muted-foreground mx-auto py-20" />
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-7xl mx-auto">
      <h2 className="text-xl font-semibold">Панель диспетчера</h2>

      {error && (
        <div className="flex items-center gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          <AlertTriangle className="size-4 shrink-0" />
          {error}
        </div>
      )}

      {dashboard && (
        <>
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center justify-between text-base">
                <span>Преподаватели на {new Date(dashboard.date).toLocaleDateString("ru-RU")}</span>
                <span className="text-xs text-muted-foreground font-normal">
                  Нед. {dashboard.week} · {dashboard.teachers.length} преподавателей
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {dashboard.teachers.length === 0 ? (
                <p className="text-sm text-muted-foreground py-10 text-center">Нет занятий на этот день</p>
              ) : (
                <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                  {dashboard.teachers.map((t) => (
                    <Link
                      key={t.teacherId}
                      href={`/schedule?teacherId=${t.teacherId}`}
                      className="rounded-lg border p-3 transition-colors hover:bg-accent/50"
                    >
                      <div className="flex items-center justify-between">
                        <p className="text-sm font-medium truncate">{t.teacherName}</p>
                        <span className="text-xs text-muted-foreground">{t.totalPairs} пар</span>
                      </div>
                      <div className="mt-2 space-y-1">
                        {t.entries.slice(0, 3).map((e, i) => (
                          <p key={i} className="text-xs text-muted-foreground truncate">
                            {e.startTime.slice(0, 5)} · {e.groupName} · {e.subject} · {e.room}
                          </p>
                        ))}
                        {t.entries.length > 3 && (
                          <p className="text-xs text-muted-foreground">+{t.entries.length - 3} ещё</p>
                        )}
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Слоты пар</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-xs text-muted-foreground">
                      <th className="text-left py-2">Пара</th>
                      <th className="text-left py-2">Время</th>
                      <th className="text-left py-2">Занятия</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dashboard.slots.map((slot) => (
                      <tr key={slot.numberPair} className="border-b border-border/50">
                        <td className="py-2 align-top">{slot.numberPair}</td>
                        <td className="py-2 align-top whitespace-nowrap">
                          {slot.startTime.slice(0, 5)}–{slot.endTime.slice(0, 5)}
                        </td>
                        <td className="py-2">
                          {slot.entries.length === 0 ? (
                            <span className="text-xs text-muted-foreground">—</span>
                          ) : (
                            <ul className="space-y-1">
                              {slot.entries.map((e, i) => (
                                <li key={i} className="text-xs">
                                  <span className="font-medium">{e.groupName}</span>
                                  <span className="text-muted-foreground">
                                    {" "}· {e.subject} · {e.room}
                                  </span>
                                </li>
                              ))}
                            </ul>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </>
      )}

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center justify-between text-base">
            <span>Расписание по группам (Gantt)</span>
            <span className="text-xs text-muted-foreground font-normal">
              {weeksRange.min}–{weeksRange.max} нед. · {groups.length} групп · {bars.length} записей
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {bars.length === 0 ? (
            <p className="text-sm text-muted-foreground py-10 text-center">Расписание пусто</p>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[800px]">
                {/* Header: week numbers */}
                <div className="flex items-end border-b pb-1 mb-1">
                  <div className="w-36 shrink-0 text-xs font-medium text-muted-foreground">Группа</div>
                  <div className="flex-1 flex">
                    {Array.from({ length: totalWeeks }, (_, i) => weeksRange.min + i).map((wk) => (
                      <div
                        key={wk}
                        className="flex-1 text-center text-[10px] text-muted-foreground leading-none"
                      >
                        {wk}
                      </div>
                    ))}
                  </div>
                </div>

                {/* Rows: one per group */}
                <TooltipProvider delayDuration={200}>
                  {groups.map((g) => {
                    const groupBars = bars.filter((b) => b.groupName === g.name)
                    return (
                      <div
                        key={g.id}
                        className="flex items-center border-b border-border/50 last:border-0 min-h-[36px]"
                      >
                        <div className="w-36 shrink-0 text-xs font-medium truncate pr-2" title={g.name}>
                          {g.name}
                        </div>
                        <div className="flex-1 relative h-7">
                          {groupBars.map((bar) => (
                            <Tooltip key={bar.id}>
                              <TooltipTrigger asChild>
                                <div
                                  className="absolute top-1 h-5 rounded-sm opacity-90 cursor-pointer hover:opacity-100 hover:ring-1 hover:ring-foreground/20 transition-opacity"
                                  style={{
                                    left: `${((bar.weeks[0] - weeksRange.min) / totalWeeks) * 100}%`,
                                    width: `${((bar.weeks[bar.weeks.length - 1] - bar.weeks[0] + 1) / totalWeeks) * 100}%`,
                                    minWidth: "6px",
                                    backgroundColor: LESSON_COLORS[bar.lessonType],
                                  }}
                                />
                              </TooltipTrigger>
                              <TooltipContent side="top" className="max-w-xs">
                                <div className="text-xs space-y-1">
                                  <p className="font-medium">{bar.subject}</p>
                                  <p className="text-muted-foreground">
                                    {bar.teacherName ?? "—"} · {bar.room}
                                  </p>
                                  <p className="text-muted-foreground">
                                    {LESSON_TYPE_LABELS[bar.lessonType]} · нед. {bar.weeks[0]}–{bar.weeks[bar.weeks.length - 1]}
                                  </p>
                                </div>
                              </TooltipContent>
                            </Tooltip>
                          ))}
                        </div>
                      </div>
                    )
                  })}
                </TooltipProvider>

                {/* Legend */}
                <div className="flex items-center gap-4 mt-3 pt-2">
                  {Object.entries(LESSON_COLORS).map(([type, color]) => (
                    <div key={type} className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <div className="w-3 h-3 rounded-sm" style={{ backgroundColor: color }} />
                      {LESSON_TYPE_LABELS[type as LessonType]}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
