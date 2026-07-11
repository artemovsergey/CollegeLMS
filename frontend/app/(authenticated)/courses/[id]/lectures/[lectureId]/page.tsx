"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, LectureResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function LectureViewPage() {
  const router = useRouter()
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
      setError("Ошибка загрузки лекции")
    } finally {
      setLoading(false)
    }
  }, [courseId, lectureId])

  useEffect(() => {
    fetchLecture()
  }, [fetchLecture])

  if (loading) return <LoadingSpinner className="py-16" />

  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error} />
        <Button variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>Назад к курсу</Button>
      </div>
    )
  }
  if (!lecture) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">

      <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}`)}>
        &larr; Назад к курсу
      </Button>

      <div>
        <h2 className="text-xl font-semibold">
          Лекция {lecture.order}: {lecture.title}
        </h2>
      </div>

      <div className="rounded-lg border bg-card p-6 whitespace-pre-wrap">
        {lecture.content}
      </div>
    </div>
  )
}

