"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams } from "next/navigation"
import type { Result, LectureResponse } from "@/types"
import api from "@/lib/api"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import LectureForm from "@/components/LectureForm"

export default function EditLecturePage() {
  const params = useParams()
  const courseId = params.id as string
  const lectureId = params.lectureId as string

  const [lecture, setLecture] = useState<LectureResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchLecture = useCallback(async () => {
    try {
      const res = await api.get<Result<LectureResponse>>(`/api/courses/${courseId}/lectures/${lectureId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setLecture(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки занятия")
    } finally {
      setLoading(false)
    }
  }, [courseId, lectureId])

  useEffect(() => {
    fetchLecture()
  }, [fetchLecture])

  if (loading) return <LoadingSpinner size="lg" className="py-20" />
  if (error || !lecture) return <ErrorBanner message={error ?? "Занятие не найдено"} className="m-6 max-w-2xl mx-auto" />

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">
      <h2 className="text-xl font-semibold">Редактировать занятие</h2>
      <LectureForm courseId={courseId} lecture={lecture} />
    </div>
  )
}
