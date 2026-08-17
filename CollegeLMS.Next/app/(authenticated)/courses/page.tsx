"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import { Copy, Trash2 } from "lucide-react"
import type { Result, CourseResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { toast } from "sonner"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

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
  const [courseToDelete, setCourseToDelete] = useState<CourseResponse | null>(null)

  const isTeacher = user?.role === "Teacher"
  const isAdmin = user?.role === "Admin"
  const canCreate = isTeacher || isAdmin

  const fetchCourses = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const params = ""
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

  const handleToggleActive = async (course: CourseResponse) => {
    const res = await api.patch<Result<null>>(`/api/courses/${course.id}/active`, {
      isActive: !course.isActive,
    })
    if (res.data.isSuccess) {
      setCourses(prev =>
        prev.map(c => (c.id === course.id ? { ...c, isActive: !c.isActive } : c))
      )
    } else {
      toast.error(res.data.errorMessage ?? "Ошибка изменения активности")
    }
  }

  const handleDuplicate = async (course: CourseResponse) => {
    const res = await api.post<Result<CourseResponse>>(`/api/courses/${course.id}/duplicate`)
    if (res.data.isSuccess && res.data.data) {
      setCourses(prev => [...prev, res.data!.data!])
      toast.success("Курс продублирован")
    } else {
      toast.error(res.data.errorMessage ?? "Ошибка дублирования")
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">{isTeacher ? "Мои курсы" : "Курсы"}</h2>
        {canCreate && (
          <Button size="sm" onClick={() => router.push("/courses/new")}>+ Создать</Button>
        )}
      </div>

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <div className="flex min-h-[60vh] items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
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
                <TableHead>Активность</TableHead>
                {isTeacher && <TableHead>Действия</TableHead>}
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
                  <TableCell>
                    {user?.teacherId === c.teacherId ? (
                      <Switch
                        checked={c.isActive}
                        onCheckedChange={() => handleToggleActive(c)}
                        aria-label={`Активность курса ${c.title}`}
                      />
                    ) : (
                      <Badge variant={c.isActive ? "default" : "outline"}>
                        {c.isActive ? "Активен" : "Неактивен"}
                      </Badge>
                    )}
                  </TableCell>
                  {isTeacher && (
                    <TableCell>
                      <div className="flex items-center gap-1.5">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={e => {
                            e.stopPropagation()
                            handleDuplicate(c)
                          }}
                        >
                          <Copy size={14} />
                          <span className="ml-1">Дублировать</span>
                        </Button>
                        {user?.teacherId === c.teacherId && (
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={e => {
                              e.stopPropagation()
                              setCourseToDelete(c)
                            }}
                          >
                            <Trash2 size={14} />
                            <span className="ml-1">Удалить</span>
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  )}
                  <TableCell>{c.lectureCount}</TableCell>
                  <TableCell>{c.assignmentCount}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <AlertDialog open={!!courseToDelete} onOpenChange={open => !open && setCourseToDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить курс?</AlertDialogTitle>
            <AlertDialogDescription>
              Курс «{courseToDelete?.title}» будет удалён безвозвратно вместе с занятиями и
              материалами.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={async () => {
                if (!courseToDelete) return
                const res = await api.delete<Result<null>>(`/api/courses/${courseToDelete.id}`)
                if (res.data.isSuccess) {
                  setCourses(prev => prev.filter(c => c.id !== courseToDelete.id))
                  toast.success("Курс удалён")
                } else {
                  toast.error(res.data.errorMessage ?? "Ошибка удаления")
                }
                setCourseToDelete(null)
              }}
            >
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}


