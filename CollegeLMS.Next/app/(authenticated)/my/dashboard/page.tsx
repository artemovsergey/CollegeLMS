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
          <div className="grid grid-cols-1 sm:grid-cols-1 gap-4">
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
          </div>
        </>
      )}
    </div>
  )
}

