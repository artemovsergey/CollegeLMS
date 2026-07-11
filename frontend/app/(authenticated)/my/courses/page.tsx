"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, CourseResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
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

export default function MyCoursesPage() {
  const { token } = useAuth()
  const router = useRouter()

  const [courses, setCourses] = useState<CourseResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchCourses = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<CourseResponse[]>>(`/api/my/courses`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setCourses(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки курсов")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchCourses()
    }
  }, [token, fetchCourses])


  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Мои курсы</h2>

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
      ) : courses.length === 0 ? (
        <p className="text-muted-foreground">Вы не записаны ни на один курс</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Преподаватель</TableHead>
                <TableHead>Прогресс</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {courses.map(c => (
                <TableRow
                  key={c.id}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/my/courses/${c.id}`)}
                >
                  <TableCell className="font-medium">{c.title}</TableCell>
                  <TableCell>{c.teacherName}</TableCell>
                  <TableCell>
                    {c.lectureCount > 0 ? `${c.lectureCount} лекций, ${c.assignmentCount} заданий` : "—"}
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

