"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, CourseResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

const statusLabels: Record<string, string> = {
  Active: "Активен",
  Completed: "Завершён",
  Draft: "Черновик",
}

const statusVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Active: "default",
  Completed: "secondary",
  Draft: "outline",
}

export default function CoursesPage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [courses, setCourses] = useState<CourseResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const isTeacher = user?.role === "Teacher"
  const isAdmin = user?.role === "Admin"
  const canCreate = isTeacher || isAdmin

  const fetchCourses = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const params = isTeacher && user ? `?teacherId=${user.id}` : ""
      const res = await api.get<Result<CourseResponse[]>>(`/api/courses${params}`)
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
  }, [isTeacher, user])

  useEffect(() => {
    if (token) {
      fetchCourses()
    }
  }, [token, fetchCourses])

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Курсы</h2>
        {canCreate && (
          <Button size="sm" onClick={() => router.push("/courses/new")}>+ Создать</Button>
        )}
      </div>

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
      ) : courses.length === 0 ? (
        <p className="text-muted-foreground">Нет курсов</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Преподаватель</TableHead>
                <TableHead>Группа</TableHead>
                <TableHead>Статус</TableHead>
                <TableHead>Лекции</TableHead>
                <TableHead>Задания</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {courses.map(c => (
                <TableRow
                  key={c.id}
                  className="cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/courses/${c.id}`)}
                >
                  <TableCell className="font-medium">{c.title}</TableCell>
                  <TableCell>{c.teacherName}</TableCell>
                  <TableCell>{c.groupNames}</TableCell>
                  <TableCell>
                    <Badge variant={statusVariants[c.status] ?? "outline"}>
                      {statusLabels[c.status] ?? c.status}
                    </Badge>
                  </TableCell>
                  <TableCell>{c.lectureCount}</TableCell>
                  <TableCell>{c.assignmentCount}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}


