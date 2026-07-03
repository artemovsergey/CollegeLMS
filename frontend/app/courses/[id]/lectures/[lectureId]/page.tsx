"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, LectureResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
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

export default function LectureViewPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
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
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchLecture()
    }
  }, [token, fetchLecture])

  if (authLoading) return <Loading />
  if (!token) return null
  if (loading) return <Loading />

  if (error) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
        <Button variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>Назад к курсу</Button>
      </div>
    )
  }
  if (!lecture) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto min-h-screen">
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

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
