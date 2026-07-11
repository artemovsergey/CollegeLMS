"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, StudentDashboardResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function StudentDashboardPage() {
  const { token } = useAuth()

  const [dashboard, setDashboard] = useState<StudentDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<StudentDashboardResponse>>("/api/my/dashboard")
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
      <h2 className="text-xl font-semibold">Моя панель</h2>

      {error && <ErrorBanner message={error} />}

      {dashboard && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Курсов
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-3xl font-bold">{dashboard.coursesCount}</p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Дедлайнов
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-3xl font-bold">{dashboard.upcomingDeadlines.length}</p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Оценок
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-3xl font-bold">{dashboard.recentGrades.length}</p>
              </CardContent>
            </Card>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">Ближайшие дедлайны</h3>
            {dashboard.upcomingDeadlines.length === 0 ? (
              <p className="text-muted-foreground">Нет дедлайнов</p>
            ) : (
              <div className="rounded-lg border bg-card divide-y">
                {dashboard.upcomingDeadlines.map((d, i) => (
                  <div key={i} className="flex items-center justify-between p-4">
                    <span className="font-medium">{d.assignmentTitle}</span>
                    <span className="text-sm text-muted-foreground">
                      {d.dueDate ? new Date(d.dueDate).toLocaleDateString("ru-RU") : "—"}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">Последние оценки</h3>
            {dashboard.recentGrades.length === 0 ? (
              <p className="text-muted-foreground">Нет оценок</p>
            ) : (
              <div className="rounded-lg border bg-card divide-y">
                {dashboard.recentGrades.map((g, i) => (
                  <div key={i} className="flex items-center justify-between p-4">
                    <span className="font-medium">{g.courseName}</span>
                    <span className="text-sm">{g.score !== null ? g.score : "—"}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}

