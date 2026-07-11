"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, SubmissionResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function MySubmissionsPage() {
  const { token } = useAuth()

  const [submissions, setSubmissions] = useState<(SubmissionResponse & { assignmentTitle?: string; courseTitle?: string })[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchSubmissions = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<(SubmissionResponse & { assignmentTitle?: string; courseTitle?: string })[]>>(`/api/my/submissions`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setSubmissions(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки работ")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchSubmissions()
    }
  }, [token, fetchSubmissions])


  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Мои работы</h2>

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
      ) : submissions.length === 0 ? (
        <p className="text-muted-foreground">Нет отправленных работ</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Задание</TableHead>
                <TableHead>Курс</TableHead>
                <TableHead>Оценка</TableHead>
                <TableHead>Статус</TableHead>
                <TableHead>Дата отправки</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {submissions.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.assignmentTitle ?? "—"}</TableCell>
                  <TableCell>{s.courseTitle ?? "—"}</TableCell>
                  <TableCell>
                    {s.score !== null ? s.score : "—"}
                  </TableCell>
                  <TableCell>
                    {s.score !== null ? (
                      <Badge variant="default">Оценено</Badge>
                    ) : (
                      <Badge variant="outline">На проверке</Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {new Date(s.submittedAt).toLocaleString("ru-RU")}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

