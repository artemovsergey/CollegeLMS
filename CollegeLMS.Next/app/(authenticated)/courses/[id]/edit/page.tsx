"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, CourseResponse, CreateCourseRequest } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function EditCoursePage() {
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchData = useCallback(async () => {
    try {
      const courseRes = await api.get<Result<CourseResponse>>(`/api/courses/${courseId}`)
      const courseBody = courseRes.data
      if (courseBody.isSuccess && courseBody.data) {
        setTitle(courseBody.data.title)
        setDescription(courseBody.data.description)
      } else {
        setError(courseBody.errorMessage ?? "Ошибка загрузки")
      }
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
      const body: CreateCourseRequest = { title, description }
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

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">

      <h2 className="text-xl font-semibold">Редактировать курс</h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}
        <div className="flex flex-col gap-2">
          <Label htmlFor="title">Название курса</Label>
          <Input id="title" required value={title} onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="description">Описание</Label>
          <Textarea id="description" value={description} onChange={e => setDescription(e.target.value)} />
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
