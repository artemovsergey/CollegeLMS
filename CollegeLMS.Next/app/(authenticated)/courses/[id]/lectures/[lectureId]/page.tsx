"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import type { Result, LectureResponse, CourseResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import { toast } from "sonner"
import { LECTURE_TYPE_LABELS, LECTURE_TYPE_VARIANTS } from "@/lib/lectureTypes"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

export default function LectureViewPage() {
  const { user } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const lectureId = params.lectureId as string

  const [lecture, setLecture] = useState<LectureResponse | null>(null)
  const [course, setCourse] = useState<CourseResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)

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

  const fetchCourse = useCallback(async () => {
    try {
      const res = await api.get<Result<CourseResponse>>(`/api/courses/${courseId}`)
      const body = res.data
      if (body.isSuccess && body.data) setCourse(body.data)
    } catch {
      // silently ignore
    }
  }, [courseId])

  useEffect(() => {
    Promise.all([fetchLecture(), fetchCourse()])
  }, [fetchLecture, fetchCourse])

  const canManage = user?.role === "Admin" || (user?.role === "Teacher" && course?.teacherId === user?.id)

  const handleDelete = async () => {
    if (!lecture) return
    setDeleting(true)
    try {
      await api.delete(`/api/courses/${courseId}/lectures/${lecture.id}`)
      toast.success("Занятие удалено")
      router.push(`/courses/${courseId}`)
    } catch {
      toast.error("Ошибка удаления занятия")
      setDeleteOpen(false)
    } finally {
      setDeleting(false)
    }
  }

  if (loading) return <LoadingSpinner size="lg" className="py-20" />

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

      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h2 className="text-xl font-semibold">
            {lecture.order}. {lecture.title}
          </h2>
          <Badge variant={LECTURE_TYPE_VARIANTS[lecture.lectureType] ?? "outline"} className="w-fit">
            {LECTURE_TYPE_LABELS[lecture.lectureType] ?? lecture.lectureType}
          </Badge>
        </div>
        {canManage && (
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => router.push(`/courses/${courseId}/lectures/${lecture.id}/edit`)}>
              Редактировать
            </Button>
            <Button variant="outline" size="sm" className="text-muted-foreground hover:text-fg" onClick={() => setDeleteOpen(true)}>
              Удалить
            </Button>
          </div>
        )}
      </div>

      <div className="rounded-lg border bg-card p-6">
        <div className="prose max-w-none">
          <ReactMarkdown>{lecture.content}</ReactMarkdown>
        </div>
      </div>

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить занятие?</AlertDialogTitle>
            <AlertDialogDescription>Действие нельзя отменить.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={deleting} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              {deleting ? "Удаление..." : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
