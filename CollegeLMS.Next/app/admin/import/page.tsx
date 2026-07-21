"use client"

import { useState } from "react"
import type { Result, ImportProgressDto } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function AdminImportPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [progress, setProgress] = useState<ImportProgressDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [polling, setPolling] = useState(false)

  const startImport = async () => {
    setError(null)
    setProgress(null)
    setLoading(true)

    try {
      const res = await api.post<Result<string>>("/api/import/wordpress/rest")
      const body = res.data

      if (!body.isSuccess || !body.data) {
        setError(body.errorMessage ?? "Ошибка запуска импорта")
        setLoading(false)
        return
      }

      const importId = body.data
      setLoading(false)
      setPolling(true)
      pollStatus(importId)
    } catch {
      setError("Ошибка запуска импорта")
      setLoading(false)
    }
  }

  const pollStatus = async (importId: string) => {
    const interval = setInterval(async () => {
      try {
        const res = await api.get<Result<ImportProgressDto>>(
          `/api/import/wordpress/status/${importId}`
        )
        const body = res.data

        if (body.isSuccess && body.data) {
          setProgress(body.data)
          if (body.data.status === "completed" || body.data.status === "failed") {
            clearInterval(interval)
            setPolling(false)
          }
        } else {
          clearInterval(interval)
          setPolling(false)
          setError(body.errorMessage ?? "Ошибка получения статуса")
        }
      } catch {
        clearInterval(interval)
        setPolling(false)
        setError("Ошибка получения статуса импорта")
      }
    }, 2000)
  }

  const progressPercent = progress
    ? Math.round((progress.processed / (progress.total || 1)) * 100)
    : 0

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-3xl">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Импорт данных</h2>
      </div>

      {error && <ErrorBanner message={error} />}

      <div className="flex gap-4">
        <Button
          onClick={startImport}
          disabled={loading || polling || !isAdmin}
        >
          {loading ? "Запуск..." : "Импортировать"}
        </Button>
      </div>

      {loading && <LoadingSpinner className="py-10" />}

      {polling && !progress && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <div className="h-4 w-4 animate-spin rounded-full border-2 border-muted border-t-primary" />
          Запуск импорта...
        </div>
      )}

      {progress && (
        <div className="flex flex-col gap-4 rounded-lg border bg-card p-6">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">
              Статус:{" "}
              {progress.status === "running"
                ? "Выполняется..."
                : progress.status === "completed"
                  ? "Завершён"
                  : "Ошибка"}
            </span>
            {progress.status === "running" && (
              <div className="h-5 w-5 animate-spin rounded-full border-2 border-muted border-t-primary" />
            )}
          </div>

          {progress.total > 0 && (
            <div className="flex flex-col gap-2">
              <div className="flex justify-between text-sm text-muted-foreground">
                <span>
                  Обработано: {progress.processed} из {progress.total}
                </span>
                <span>{progressPercent}%</span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary transition-all duration-500"
                  style={{ width: `${progressPercent}%` }}
                />
              </div>
            </div>
          )}

          {progress.errors > 0 && (
            <div className="text-sm text-destructive">
              Ошибок: {progress.errors}
            </div>
          )}

          {progress.result && (
            <div className="mt-2 flex flex-col gap-1 text-sm">
              <p>Категорий создано: {progress.result.categoriesCreated}</p>
              <p>Новостей импортировано: {progress.result.postsImported}</p>
              <p>Новостей пропущено: {progress.result.postsSkipped}</p>
              {progress.result.errors.length > 0 && (
                <div className="mt-2 flex flex-col gap-1">
                  <p className="font-medium text-destructive">Ошибки:</p>
                  <ul className="list-inside list-disc text-destructive/80">
                    {progress.result.errors.map((err, i) => (
                      <li key={i}>{err}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
