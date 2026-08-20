"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, LessonResponse, MaterialResponse, MyTestResultDto } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import LessonList from "@/components/lesson/LessonList"
import DocumentsTab from "@/components/course/DocumentsTab"

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

type Tab = "lessons" | "materials" | "documents"

export default function MyCourseDetailPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [lessons, setLessons] = useState<LessonResponse[]>([])
  const [materials, setMaterials] = useState<MaterialResponse[]>([])
  const [myTestResults, setMyTestResults] = useState<Map<string, MyTestResultDto>>(new Map())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>("lessons")

  const fetchData = useCallback(async () => {
    try {
      const [courseRes, lessonsRes, materialsRes, testRes] = await Promise.all([
        api.get<Result<CourseResponse>>(`/api/courses/${courseId}`),
        api.get<Result<LessonResponse[]>>(`/api/courses/${courseId}/lessons`),
        api.get<Result<MaterialResponse[]>>(`/api/courses/${courseId}/materials`),
        api.get<Result<MyTestResultDto[]>>("/api/my/test-results"),
      ])
      if (courseRes.data.isSuccess && courseRes.data.data) setCourse(courseRes.data.data)
      if (lessonsRes.data.isSuccess && lessonsRes.data.data) setLessons(lessonsRes.data.data)
      if (materialsRes.data.isSuccess && materialsRes.data.data) setMaterials(materialsRes.data.data)
      if (testRes.data.isSuccess && testRes.data.data) {
        setMyTestResults(new Map(testRes.data.data.map(r => [r.testId, r])))
      }
    } catch {
      setError("Ошибка загрузки курса")
    } finally {
      setLoading(false)
    }
  }, [courseId])

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
        <p className="text-sm text-muted-foreground">{course.teacherName} &middot; {course.groupNames}</p>
        {course.description && (
          <p className="text-sm text-muted-foreground mt-1">{course.description}</p>
        )}
      </div>

      <div className="flex gap-4 border-b">
        {(["lessons", "materials", "documents"] as Tab[]).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            {t === "lessons" ? "Занятия" : t === "materials" ? "Материалы" : "Документы"}
          </button>
        ))}
      </div>

      {tab === "lessons" && (
        <LessonList
          courseId={courseId}
          lessons={lessons}
          canManage={false}
          onChanged={() => {}}
        />
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

      {tab === "documents" && (
        <DocumentsTab courseId={courseId} canManage={false} />
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
