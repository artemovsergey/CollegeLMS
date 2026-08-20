"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams } from "next/navigation"
import type { Result, LessonResponse } from "@/types"
import api from "@/lib/api"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import LessonForm from "@/components/LessonForm"

export default function EditLessonPage() {
  const params = useParams()
  const courseId = params.id as string
  const lessonId = params.lessonId as string

  const [lesson, setLesson] = useState<LessonResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchLesson = useCallback(async () => {
    try {
      const res = await api.get<Result<LessonResponse>>(`/api/courses/${courseId}/lessons/${lessonId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLesson(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки занятия")
    } finally {
      setLoading(false)
    }
  }, [courseId, lessonId])

  useEffect(() => {
    fetchLesson()
  }, [fetchLesson])

  if (loading) return <LoadingSpinner size="lg" className="py-20" />
  if (error || !lesson) return <ErrorBanner message={error ?? "Занятие не найдено"} className="m-6 max-w-2xl mx-auto" />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <h2 className="text-xl font-semibold">Редактировать занятие</h2>
      <LessonForm courseId={courseId} lesson={lesson} />
    </div>
  )
}
