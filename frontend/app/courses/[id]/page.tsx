"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, LectureResponse, AssignmentResponse, MaterialResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

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

const roleLabels: Record<string, string> = {
  Admin: "Админ",
  Teacher: "Преподаватель",
  Student: "Студент",
  Dispatcher: "Диспетчер",
}

const roleVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Admin: "default",
  Teacher: "secondary",
  Student: "secondary",
  Dispatcher: "outline",
}

type Tab = "lectures" | "assignments" | "materials"

export default function CourseDetailPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [lectures, setLectures] = useState<LectureResponse[]>([])
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([])
  const [materials, setMaterials] = useState<MaterialResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>("lectures")

  const isTeacher = user?.role === "Teacher" && course?.teacherId === user?.id
  const isAdmin = user?.role === "Admin"
  const canEdit = isTeacher || isAdmin

  const fetchCourse = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseResponse>>(`/api/courses/${courseId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setCourse(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки курса")
    }
  }, [courseId])

  const fetchLectures = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse[]>>(`/api/courses/${courseId}/lectures`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLectures(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchAssignments = useCallback(async () => {
    try {
      const res = await api.get<Result<AssignmentResponse[]>>(`/api/courses/${courseId}/assignments`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setAssignments(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  const fetchMaterials = useCallback(async () => {
    try {
      const res = await api.get<Result<MaterialResponse[]>>(`/api/courses/${courseId}/materials`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setMaterials(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [courseId])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      setLoading(true)
      Promise.all([fetchCourse(), fetchLectures(), fetchAssignments(), fetchMaterials()])
        .finally(() => setLoading(false))
    }
  }, [token, fetchCourse, fetchLectures, fetchAssignments, fetchMaterials])

  if (authLoading) return <Loading />
  if (!token) return null

  if (loading) return <Loading />
  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
        <Button variant="ghost" onClick={() => router.push("/courses")}>Назад к курсам</Button>
      </div>
    )
  }
  if (!course) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto min-h-screen">
      <header className="flex items-center justify-between py-2">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            CL
          </div>
          <h1 className="text-lg font-semibold">CollegeLMS</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="hidden sm:block text-sm text-muted-foreground">{user?.email}</span>
          <Badge variant={roleVariants[user?.role ?? ""] ?? "secondary"}>
            {roleLabels[user?.role ?? ""] ?? user?.role}
          </Badge>
          <Button variant="ghost" size="sm" onClick={() => { logout(); router.push("/login") }}>
            Выйти
          </Button>
        </div>
      </header>

      <div className="flex items-start justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-xl font-semibold">{course.title}</h2>
          <p className="text-sm text-muted-foreground">
            {course.teacherName} &middot; {course.groupName}
          </p>
          {course.description && (
            <p className="text-sm text-muted-foreground mt-1">{course.description}</p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={statusVariants[course.status] ?? "outline"}>
            {statusLabels[course.status] ?? course.status}
          </Badge>
          {canEdit && (
            <Button variant="outline" size="sm" onClick={() => router.push(`/courses/${courseId}/edit`)}>
              Ред.
            </Button>
          )}
        </div>
      </div>

      <div className="flex gap-4 border-b">
        {(["lectures", "assignments", "materials"] as Tab[]).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            {t === "lectures" ? "Лекции" : t === "assignments" ? "Задания" : "Материалы"}
          </button>
        ))}
      </div>

      {tab === "lectures" && (
        <div className="flex flex-col gap-3">
          {canEdit && (
            <div className="flex justify-end">
              <Button size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/new`)}>
                + Добавить лекцию
              </Button>
            </div>
          )}
          {lectures.length === 0 ? (
            <p className="text-muted-foreground">Нет лекций</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {lectures.map(l => (
                <div
                  key={l.id}
                  className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/courses/${courseId}/lectures/${l.id}`)}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground w-6">{l.order}</span>
                    <span className="font-medium">{l.title}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {tab === "assignments" && (
        <div className="flex flex-col gap-3">
          {canEdit && (
            <div className="flex justify-end">
              <Button size="sm" onClick={() => router.push(`/courses/${courseId}/assignments/new`)}>
                + Добавить задание
              </Button>
            </div>
          )}
          {assignments.length === 0 ? (
            <p className="text-muted-foreground">Нет заданий</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {assignments.map(a => (
                <div
                  key={a.id}
                  className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50"
                  onClick={() => router.push(`/courses/${courseId}/assignments/${a.id}`)}
                >
                  <div className="flex flex-col gap-1">
                    <span className="font-medium">{a.title}</span>
                    <span className="text-xs text-muted-foreground">
                      Макс. баллов: {a.maxScore}
                      {a.dueDate ? ` · Срок: ${new Date(a.dueDate).toLocaleDateString("ru-RU")}` : ""}
                    </span>
                  </div>
                  <span className="text-xs text-muted-foreground">{a.submissionCount} работ</span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {tab === "materials" && (
        <div className="flex flex-col gap-3">
          {materials.length === 0 ? (
            <p className="text-muted-foreground">Нет материалов</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {materials.map(m => (
                <div key={m.id} className="flex items-center justify-between p-4">
                  <div className="flex flex-col gap-1">
                    <span className="font-medium">{m.fileName}</span>
                    <span className="text-xs text-muted-foreground">
                      {(m.fileSize / 1024).toFixed(1)} KB
                    </span>
                  </div>
                  <Button variant="outline" size="sm" asChild>
                    <a href={`/api/courses/${courseId}/materials/${m.id}/download`} download>
                      Скачать
                    </a>
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
