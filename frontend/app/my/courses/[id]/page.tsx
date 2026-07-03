"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, LectureResponse, AssignmentResponse, MaterialResponse, SubmissionResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"

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

export default function MyCourseDetailPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [lectures, setLectures] = useState<LectureResponse[]>([])
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([])
  const [materials, setMaterials] = useState<MaterialResponse[]>([])
  const [submissions, setSubmissions] = useState<Record<string, SubmissionResponse | null>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>("lectures")

  const fetchData = useCallback(async () => {
    try {
      const [courseRes, lecturesRes, assignmentsRes, materialsRes] = await Promise.all([
        api.get<Result<CourseResponse>>(`/api/courses/${courseId}`),
        api.get<Result<LectureResponse[]>>(`/api/courses/${courseId}/lectures`),
        api.get<Result<AssignmentResponse[]>>(`/api/courses/${courseId}/assignments`),
        api.get<Result<MaterialResponse[]>>(`/api/courses/${courseId}/materials`),
      ])

      if (courseRes.data.isSuccess && courseRes.data.data) {
        setCourse(courseRes.data.data)
      }
      if (lecturesRes.data.isSuccess && lecturesRes.data.data) {
        setLectures(lecturesRes.data.data)
      }
      if (assignmentsRes.data.isSuccess && assignmentsRes.data.data) {
        setAssignments(assignmentsRes.data.data)
        const subMap: Record<string, SubmissionResponse | null> = {}
        if (user) {
          await Promise.all(assignmentsRes.data.data.map(async a => {
            try {
              const subRes = await api.get<Result<SubmissionResponse[]>>(`/api/assignments/${a.id}/submissions?studentId=${user.id}`)
              if (subRes.data.isSuccess && subRes.data.data && subRes.data.data.length > 0) {
                subMap[a.id] = subRes.data.data[0]
              }
            } catch {
              // ignore
            }
          }))
        }
        setSubmissions(subMap)
      }
      if (materialsRes.data.isSuccess && materialsRes.data.data) {
        setMaterials(materialsRes.data.data)
      }
    } catch {
      setError("Ошибка загрузки курса")
    } finally {
      setLoading(false)
    }
  }, [courseId, user])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchData()
    }
  }, [token, fetchData])

  if (authLoading) return <Loading />
  if (!token) return null
  if (loading) return <Loading />

  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-5xl mx-auto">
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
        <Button variant="ghost" onClick={() => router.push("/my/courses")}>Назад к курсам</Button>
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

      <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push("/my/courses")}>
        &larr; Назад к курсам
      </Button>

      <div className="flex flex-col gap-1">
        <h2 className="text-xl font-semibold">{course.title}</h2>
        <p className="text-sm text-muted-foreground">{course.teacherName} &middot; {course.groupName}</p>
        {course.description && (
          <p className="text-sm text-muted-foreground mt-1">{course.description}</p>
        )}
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
        <div>
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
        <div>
          {assignments.length === 0 ? (
            <p className="text-muted-foreground">Нет заданий</p>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {assignments.map(a => {
                const sub = submissions[a.id]
                return (
                  <div key={a.id} className="p-4">
                    <div className="flex items-center justify-between mb-2">
                      <div className="flex flex-col gap-1">
                        <span className="font-medium">{a.title}</span>
                        <span className="text-xs text-muted-foreground">
                          Макс. баллов: {a.maxScore}
                          {a.dueDate ? ` · Срок: ${new Date(a.dueDate).toLocaleDateString("ru-RU")}` : ""}
                        </span>
                      </div>
                      {sub?.score !== null && sub?.score !== undefined ? (
                        <Badge variant="default">{sub.score} / {a.maxScore}</Badge>
                      ) : sub ? (
                        <Badge variant="secondary">На проверке</Badge>
                      ) : (
                        <Badge variant="outline">Не сдано</Badge>
                      )}
                    </div>
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => router.push(`/courses/${courseId}/assignments/${a.id}`)}
                      >
                        Подробнее
                      </Button>
                      {(!sub) && (
                        <Button
                          size="sm"
                          onClick={() => router.push(`/courses/${courseId}/assignments/${a.id}`)}
                        >
                          Сдать
                        </Button>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </div>
      )}

      {tab === "materials" && (
        <div>
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
