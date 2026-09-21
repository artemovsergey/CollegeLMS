"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, StudentDashboardResponse, ProfileResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"
import { DAYS } from "@/types/schedule"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import {
  fetchSchedule,
  fetchScheduleMeta,
  normalizeDateOnly,
  parseIsoDate,
  type ScheduleMeta,
} from "@/api/schedule"
import { fetchProfile } from "@/api/profile"
import ErrorBanner from "@/components/ErrorBanner"
import CourseCard from "@/components/CourseCard"
import LoadingSpinner from "@/components/LoadingSpinner"
import WeekNavigation from "@/components/WeekNavigation"
import ScheduleTable from "@/components/ScheduleTable"
import { CalendarDays, BookOpen } from "lucide-react"
import { cn } from "@/lib/utils"

export default function StudentDashboardPage() {
  const { token, user } = useAuth()

  const [dashboard, setDashboard] = useState<StudentDashboardResponse | null>(
    null,
  )
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [studentGroupId, setStudentGroupId] = useState<string | null>(null)
  const [studentGroupName, setStudentGroupName] = useState<string | null>(null)
  const [scheduleEntries, setScheduleEntries] = useState<ScheduleResponse[]>([])
  const [scheduleLoading, setScheduleLoading] = useState(false)

  const [meta, setMeta] = useState<ScheduleMeta | null>(null)
  const [selectedWeek, setSelectedWeek] = useState(1)
  const [selectedDay, setSelectedDay] = useState<number | null>(
    (() => {
      const day = new Date().getDay()
      return day >= 1 && day <= 5 ? day : 1
    })(),
  )

  const fetchDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<StudentDashboardResponse>>(
        "/api/my/dashboard",
      )
      const body = res.data
      if (body.isSuccess && body.data) {
        setDashboard(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [])

  const fetchProfileData = useCallback(async () => {
    try {
      const result = await fetchProfile()
      if (result.isSuccess && result.data?.studentData) {
        setStudentGroupId(result.data.studentData.groupId)
        setStudentGroupName(result.data.studentData.groupName)
      }
    } catch {
      /* ignore */
    }
  }, [])

  const loadSchedule = useCallback(async () => {
    if (!studentGroupId) return
    setScheduleLoading(true)
    try {
      const body = await fetchSchedule({
        groupId: studentGroupId,
        week: selectedWeek,
        pageSize: 200,
      })
      if (body.isSuccess && body.data) {
        setScheduleEntries(body.data.items)
      }
    } catch {
      /* ignore */
    } finally {
      setScheduleLoading(false)
    }
  }, [studentGroupId, selectedWeek])

  useEffect(() => {
    if (token) {
      fetchDashboard()
      fetchProfileData()
    }
  }, [token, fetchDashboard, fetchProfileData])

  useEffect(() => {
    if (!token) return
    let cancelled = false
    fetchScheduleMeta()
      .then((body) => {
        if (cancelled || !body.isSuccess || !body.data) return
        setMeta(body.data)
        setSelectedWeek(body.data.currentWeek)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [token])

  useEffect(() => {
    if (token && studentGroupId) {
      loadSchedule()
    }
  }, [token, studentGroupId, loadSchedule])

  if (loading)
    return (
      <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
        <div className="flex min-h-[60vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      </div>
    )

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      {user && (
        <h2 className="text-xl font-semibold">
          Здравствуйте, {user.fullName}
        </h2>
      )}

      {error && <ErrorBanner message={error} />}

      <div className="flex flex-col gap-3">
        <div className="flex items-center gap-2">
          <CalendarDays className="size-5 text-primary" />
          <h3 className="text-lg font-semibold">
            Расписание
            {studentGroupName && (
              <span className="ml-2 text-sm font-normal text-muted-foreground">
                — {studentGroupName}
              </span>
            )}
          </h3>
        </div>

        {!studentGroupId ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground py-4">
            <LoadingSpinner size="sm" />
            Загрузка данных группы...
          </div>
        ) : (
          <>
            {meta && (
              <WeekNavigation
                currentWeek={selectedWeek}
                onChange={setSelectedWeek}
                totalWeeks={meta.totalWeeks}
                semesterStart={parseIsoDate(
                  normalizeDateOnly(meta.semesterStart),
                )}
                todayWeek={meta.currentWeek}
              />
            )}

            <div
              className="flex gap-1 overflow-x-auto"
              role="group"
              aria-label="День недели"
            >
              {DAYS.filter((d) => d.value >= 1 && d.value <= 5).map((day) => (
                <button
                  key={day.value}
                  type="button"
                  aria-pressed={day.value === selectedDay}
                  onClick={() =>
                    setSelectedDay(day.value === selectedDay ? null : day.value)
                  }
                  className={cn(
                    "min-h-11 min-w-[48px] rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                    day.value === selectedDay
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground",
                  )}
                >
                  {day.label}
                </button>
              ))}
            </div>

            <div className="relative">
              {scheduleLoading && (
                <div className="absolute inset-0 z-10 flex items-center justify-center bg-background/60">
                  <LoadingSpinner size="lg" />
                </div>
              )}
              <ScheduleTable
                entries={scheduleEntries}
                inserts={[]}
                selectedDay={selectedDay}
                currentWeek={meta?.currentWeek}
              />
            </div>
          </>
        )}
      </div>

      <div className="flex items-center gap-2 mt-2">
        <BookOpen className="size-5 text-primary" />
        <h3 className="text-lg font-semibold">Мои курсы</h3>
      </div>

      {dashboard && dashboard.courses.length === 0 && (
        <p className="text-muted-foreground">У вас нет активных курсов</p>
      )}

      {dashboard && dashboard.courses.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {dashboard.courses.map((c) => (
            <CourseCard
              key={c.id}
              id={c.id}
              title={c.title}
              subtitle={c.teacherName}
              href={`/my/courses/${c.id}`}
              progress={{
                percent: c.completionPercent,
                completed: c.completedItems,
                total: c.totalItems,
              }}
            />
          ))}
        </div>
      )}
    </div>
  )
}
