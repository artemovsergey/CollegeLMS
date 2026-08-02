"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { CalendarDays, UsersRound } from "lucide-react"
import type { Result, CalendarResponse, GroupResponse } from "@/types"
import api from "@/lib/api"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

const DAY_LABELS = ["", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"]

export default function DispatcherDashboardPage() {
  const [calendar, setCalendar] = useState<CalendarResponse | null>(null)
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      api.get<Result<CalendarResponse>>("/api/schedule", { params: { view: "calendar" } }),
      api.get<Result<GroupResponse[]>>("/api/groups"),
    ])
      .then(([calRes, groupsRes]) => {
        if (calRes.data.isSuccess && calRes.data.data) setCalendar(calRes.data.data)
        if (groupsRes.data.isSuccess && groupsRes.data.data) setGroups(groupsRes.data.data)
      })
      .catch(() => setError("Ошибка загрузки данных"))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <LoadingSpinner size="lg" className="py-20" />
      </div>
    )
  }

  const today = new Date().getDay()

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Панель диспетчера</h2>

      {error && <ErrorBanner message={error} />}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <CalendarDays size={16} /> Расписание на неделю
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!calendar || calendar.days.length === 0 ? (
            <p className="text-sm text-muted-foreground">Расписание пусто</p>
          ) : (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {calendar.days.map(day => (
                <div
                  key={day.dayOfWeek}
                  className={`rounded-md border p-3 ${day.dayOfWeek === today ? "border-primary bg-primary/5" : "border-border"}`}
                >
                  <p className="mb-2 text-sm font-medium">{day.day}</p>
                  {day.entries.length === 0 ? (
                    <p className="text-xs text-muted-foreground">Нет занятий</p>
                  ) : (
                    <ul className="flex flex-col gap-1.5">
                      {day.entries.map(e => (
                        <li key={e.id} className="text-xs">
                          <span className="font-medium">{e.numberPair} пара</span> — {e.subject}
                          <span className="text-muted-foreground"> · {e.groupName}</span>
                          <span className="text-muted-foreground"> · {e.room}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <UsersRound size={16} /> Группы
          </CardTitle>
        </CardHeader>
        <CardContent>
          {groups.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет групп</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {groups.map(g => (
                <Link
                  key={g.id}
                  href={`/schedule?groupId=${g.id}`}
                  className="rounded-md border border-border px-3 py-1.5 text-sm hover:border-primary hover:text-primary"
                >
                  {g.name} ({g.studentCount})
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
