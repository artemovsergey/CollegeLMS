"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseProgressResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function CourseProgressPage() {
  const { token } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [progress, setProgress] = useState<CourseProgressResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)

  const fetchProgress = useCallback(async () => {
    setLoading(true)
    setError(null)
    setNotFound(false)
    try {
      const res = await api.get<Result<CourseProgressResponse>>(
        `/api/my/courses/${courseId}/progress`
      )
      const body = res.data
      if (body.isSuccess && body.data) {
        setProgress(body.data)
      } else if (body.statusCode === 404) {
        setNotFound(true)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки прогресса")
      }
    } catch {
      setError("Ошибка загрузки прогресса")
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    if (token) {
      fetchProgress()
    }
  }, [token, fetchProgress])

  if (!token) return null
  if (loading) return <LoadingSpinner className="py-16" />

  if (notFound) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <ErrorBanner message="Курс не найден" />
        <Button variant="ghost" className="self-start" onClick={() => router.push("/my/courses")}>
          &larr; Назад к курсам
        </Button>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <ErrorBanner message={error} />
        <div className="flex gap-2">
          <Button variant="outline" onClick={fetchProgress}>
            Повторить
          </Button>
          <Button variant="ghost" onClick={() => router.push("/my/courses")}>
            Назад к курсам
          </Button>
        </div>
      </div>
    )
  }

  if (!progress) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/my/courses/${courseId}`)}>
        &larr; Назад к курсу
      </Button>

      <h1 className="text-xl font-semibold">{progress.courseTitle}</h1>

      <div className="grid grid-cols-2 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Всего заданий
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{progress.totalAssignments}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Выполнено
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{progress.completedAssignments}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Всего тестов
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{progress.totalTests}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Пройдено
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{progress.completedTests}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">
            Общий прогресс
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          <div className="h-2.5 w-full overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full bg-primary transition-all duration-300"
              style={{ width: `${Math.min(progress.completionPercent, 100)}%` }}
            />
          </div>
          <span className="text-sm text-muted-foreground">
            {progress.completionPercent}%
          </span>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">
            Средний балл
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-3xl font-bold">{progress.averageScore}</p>
        </CardContent>
      </Card>
    </div>
  )
}
