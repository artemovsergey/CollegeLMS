"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, TeacherDashboardResponse, SubmissionResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
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

export default function TeacherDashboardPage() {
  const { token } = useAuth()
  const router = useRouter()

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
      <h2 className="text-xl font-semibold">Панель преподавателя</h2>

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
                  Студентов
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-3xl font-bold">{dashboard.studentsCount}</p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Недавних работ
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-3xl font-bold">{dashboard.recentSubmissions.length}</p>
              </CardContent>
            </Card>
          </div>

          <div>
            <h3 className="text-lg font-semibold mb-3">Последние работы</h3>
            {dashboard.recentSubmissions.length === 0 ? (
              <p className="text-muted-foreground">Нет работ</p>
            ) : (
              <div className="rounded-lg border bg-card">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Студент</TableHead>
                      <TableHead>Файл</TableHead>
                      <TableHead>Оценка</TableHead>
                      <TableHead>Дата</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {dashboard.recentSubmissions.map(s => (
                      <TableRow key={s.id}>
                        <TableCell className="font-medium">{s.studentName}</TableCell>
                        <TableCell className="max-w-[200px] truncate">{s.filePath}</TableCell>
                        <TableCell>{s.score !== null ? s.score : "—"}</TableCell>
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

          <div>
            <h3 className="text-lg font-semibold mb-3">Мои курсы</h3>
            {dashboard.courses.length === 0 ? (
              <p className="text-muted-foreground">Нет курсов</p>
            ) : (
              <div className="rounded-lg border bg-card divide-y">
                {dashboard.courses.map(c => (
                  <div
                    key={c.id}
                    className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50"
                    onClick={() => router.push(`/courses/${c.id}`)}
                  >
                    <span className="font-medium">{c.title}</span>
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

