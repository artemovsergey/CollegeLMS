"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, TeacherDashboardResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import CourseCard from "@/components/CourseCard"

export default function TeacherDashboardPage() {
  const { token, user } = useAuth()

  const [dashboard, setDashboard] = useState<TeacherDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<TeacherDashboardResponse>>("/api/teacher/dashboard")
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

  useEffect(() => {
    if (token) {
      fetchDashboard()
    }
  }, [token, fetchDashboard])

  if (loading) return <LoadingSpinner className="py-16" />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      {user && (
        <h2 className="text-xl font-semibold">Здравствуйте, {user.fullName}</h2>
      )}

      {error && <ErrorBanner message={error} />}

      {dashboard && dashboard.courses.length === 0 && (
        <p className="text-muted-foreground">У вас нет курсов</p>
      )}

      {dashboard && dashboard.courses.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {dashboard.courses.map(c => (
            <CourseCard
              key={c.id}
              id={c.id}
              title={c.title}
              subtitle={c.groupNames}
              href={`/courses/${c.id}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}
