"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import type { Result, CourseResponse, CreateCourseRequest, TeacherResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

export default function CreateCoursePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [courses, setCourses] = useState<CourseResponse[]>([])
  const [baseCourseId, setBaseCourseId] = useState("")
  const [authorIds, setAuthorIds] = useState<string[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [loadingOptions, setLoadingOptions] = useState(true)

  useEffect(() => {
    if (!token) return
    Promise.all([
      api.get<Result<CourseResponse[]>>("/api/courses"),
      api.get<Result<TeacherResponse[]>>("/api/teachers"),
    ])
      .then(([coursesRes, teachersRes]) => {
        if (coursesRes.data.isSuccess && coursesRes.data.data)
          setCourses(coursesRes.data.data)
        if (teachersRes.data.isSuccess && teachersRes.data.data)
          setTeachers(teachersRes.data.data)
      })
      .catch(() => {
        setError("Ошибка загрузки данных")
      })
      .finally(() => setLoadingOptions(false))
  }, [token])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (baseCourseId) {
        const dup = await api.post<Result<CourseResponse>>(
          `/api/courses/${baseCourseId}/duplicate`
        )
        if (!dup.data.isSuccess || !dup.data.data) {
          setError(dup.data.errorMessage ?? "Ошибка создания копии")
          return
        }
        const copyId = dup.data.data.id
        const copyTitle = title.trim() || dup.data.data.title
        await api.put<Result<CourseResponse>>(`/api/courses/${copyId}`, {
          title: copyTitle,
          description,
          status: "Draft",
          authorIds,
        })
        router.push(`/courses/${copyId}`)
      } else {
        const body: CreateCourseRequest = { title, description, authorIds }
        const res = await api.post<Result<{ id: string }>>("/api/courses", body)
        if (res.data.isSuccess && res.data.data) {
          router.push(`/courses/${res.data.data.id}`)
        } else {
          setError(res.data.errorMessage ?? "Ошибка создания")
        }
      }
    } catch {
      setError("Ошибка создания курса")
    } finally {
      setSubmitting(false)
    }
  }

  if (authLoading || loadingOptions) return <LoadingSpinner className="py-16" />

  const teacherOptions = teachers.filter(t => t.id !== (user?.teacherId ?? ""))

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <h2 className="text-xl font-semibold">Создать курс</h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}

        <div className="flex flex-col gap-2">
          <Label htmlFor="baseCourse">Создать на основе курса</Label>
          <select
            id="baseCourse"
            value={baseCourseId}
            onChange={e => setBaseCourseId(e.target.value)}
            className="w-full rounded-md border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
          >
            <option value="">— Пустой курс —</option>
            {courses.map(c => (
              <option key={c.id} value={c.id}>
                {c.title}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="title">Название курса</Label>
          <Input id="title" required value={title} onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="description">Описание</Label>
          <Textarea
            id="description"
            value={description}
            onChange={e => setDescription(e.target.value)}
          />
        </div>

        <div className="flex flex-col gap-2">
          <Label>Соавторы</Label>
          {teacherOptions.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет других преподавателей</p>
          ) : (
            <div className="grid gap-1.5 sm:grid-cols-2">
              {teacherOptions.map(t => (
                <label
                  key={t.id}
                  className="flex items-center gap-2 rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
                >
                  <input
                    type="checkbox"
                    checked={authorIds.includes(t.id)}
                    onChange={e => {
                      setAuthorIds(prev =>
                        e.target.checked ? [...prev, t.id] : prev.filter(id => id !== t.id)
                      )
                    }}
                  />
                  {t.fullName}
                </label>
              ))}
            </div>
          )}
        </div>

        <div className="flex gap-2 justify-end pt-2">
          <Button type="button" variant="ghost" onClick={() => router.push("/courses")}>
            Отмена
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? "Создание..." : "Создать"}
          </Button>
        </div>
      </form>
    </div>
  )
}