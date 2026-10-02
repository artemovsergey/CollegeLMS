"use client"

import { PageTitle } from "@/components/ui/heading"
import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, UpdateCourseRequest, TeacherResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import { Textarea } from "@/components/ui/textarea"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

const statusOptions = [
  { value: "Draft", label: "Черновик" },
  { value: "Active", label: "Активен" },
  { value: "Archived", label: "Архив" },
]

export default function EditCoursePage() {
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [status, setStatus] = useState("Draft")
  const [authorIds, setAuthorIds] = useState<string[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [teacherId, setTeacherId] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchData = useCallback(async () => {
    try {
      const [courseRes, teachersRes] = await Promise.all([
        api.get<Result<CourseResponse>>(`/api/courses/${courseId}`),
        api.get<Result<TeacherResponse[]>>("/api/teachers"),
      ])
      const courseBody = courseRes.data
      if (courseBody.isSuccess && courseBody.data) {
        setTitle(courseBody.data.title)
        setDescription(courseBody.data.description)
        setStatus(courseBody.data.status || "Draft")
        setAuthorIds(courseBody.data.authorIds ?? [])
        setTeacherId(courseBody.data.teacherId)
      } else {
        setError(courseBody.errorMessage ?? "Ошибка загрузки")
      }
      if (teachersRes.data.isSuccess && teachersRes.data.data)
        setTeachers(teachersRes.data.data)
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const body: UpdateCourseRequest = { title, description, status, authorIds }
      const res = await api.put<Result<CourseResponse>>(`/api/courses/${courseId}`, body)
      if (res.data.isSuccess) {
        router.push(`/courses/${courseId}`)
      } else {
        setError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setError("Ошибка обновления курса")
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <LoadingSpinner className="py-16" />

  const teacherOptions = teachers.filter(t => t.id !== teacherId)

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <PageTitle>Редактировать курс</PageTitle>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}
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
          <Label htmlFor="status">Статус</Label>
          <NativeSelect
            id="status"
            value={status}
            onValueChange={setStatus}
            className="w-full"
          >
            {statusOptions.map((s) => (
              <NativeSelectItem key={s.value} value={s.value}>
                {s.label}
              </NativeSelectItem>
            ))}
          </NativeSelect>
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
          <Button type="button" variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>
            Отмена
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? "Сохранение..." : "Сохранить"}
          </Button>
        </div>
      </form>
    </div>
  )
}