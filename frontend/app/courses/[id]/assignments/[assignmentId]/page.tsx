"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, AssignmentResponse, SubmissionResponse } from "@/types"
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

export default function AssignmentViewPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const assignmentId = params.assignmentId as string

  const [assignment, setAssignment] = useState<AssignmentResponse | null>(null)
  const [mySubmission, setMySubmission] = useState<SubmissionResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [filePath, setFilePath] = useState("")
  const [comment, setComment] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const isTeacher = user?.role === "Teacher"
  const isStudent = user?.role === "Student"

  const fetchData = useCallback(async () => {
    try {
      const res = await api.get<Result<AssignmentResponse>>(`/api/courses/${courseId}/assignments/${assignmentId}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setAssignment(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
      if (isStudent && user) {
        const subRes = await api.get<Result<SubmissionResponse[]>>(`/api/assignments/${assignmentId}/submissions?studentId=${user.id}`)
        const subBody = subRes.data
        if (subBody.isSuccess && subBody.data && subBody.data.length > 0) {
          setMySubmission(subBody.data[0])
        }
      }
    } catch {
      setError("Ошибка загрузки задания")
    } finally {
      setLoading(false)
    }
  }, [courseId, assignmentId, isStudent, user])

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

  const handleSubmitAssignment = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitError(null)
    setSubmitting(true)
    try {
      const body = { filePath, comment: comment || null }
      const res = await api.post<Result<SubmissionResponse>>(`/api/assignments/${assignmentId}/submissions`, body)
      if (res.data.isSuccess) {
        await fetchData()
        setFilePath("")
        setComment("")
      } else {
        setSubmitError(res.data.errorMessage ?? "Ошибка отправки")
      }
    } catch {
      setSubmitError("Ошибка отправки работы")
    } finally {
      setSubmitting(false)
    }
  }

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
  if (!assignment) return null

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

      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">{assignment.title}</h2>
        <div className="flex gap-3 text-sm text-muted-foreground">
          <span>Макс. баллов: {assignment.maxScore}</span>
          {assignment.dueDate && (
            <span>Срок: {new Date(assignment.dueDate).toLocaleDateString("ru-RU")}</span>
          )}
        </div>
      </div>

      {assignment.description && (
        <div className="rounded-lg border bg-card p-4 whitespace-pre-wrap">
          {assignment.description}
        </div>
      )}

      {isTeacher && (
        <div className="flex justify-end">
          <Button onClick={() => router.push(`/courses/${courseId}/assignments/${assignmentId}/submissions`)}>
            Просмотр работ ({assignment.submissionCount})
          </Button>
        </div>
      )}

      {isStudent && (
        <div className="flex flex-col gap-4">
          {mySubmission ? (
            <div className="rounded-lg border bg-card p-4">
              <h3 className="font-semibold mb-2">Ваша работа</h3>
              <p className="text-sm">Файл: {mySubmission.filePath}</p>
              {mySubmission.comment && <p className="text-sm">Комментарий: {mySubmission.comment}</p>}
              <p className="text-sm text-muted-foreground">
                Отправлено: {new Date(mySubmission.submittedAt).toLocaleString("ru-RU")}
              </p>
              {mySubmission.score !== null && (
                <p className="text-sm font-medium mt-1">
                  Оценка: {mySubmission.score} / {assignment.maxScore}
                </p>
              )}
            </div>
          ) : (
            <form onSubmit={handleSubmitAssignment} className="flex flex-col gap-4">
              <h3 className="font-semibold">Отправить работу</h3>
              {submitError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{submitError}</div>
              )}
              <div className="flex flex-col gap-2">
                <Label htmlFor="filePath">Путь к файлу</Label>
                <Input id="filePath" required value={filePath} onChange={e => setFilePath(e.target.value)} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="comment">Комментарий</Label>
                <Textarea id="comment" value={comment} onChange={e => setComment(e.target.value)} />
              </div>
              <div className="flex justify-end">
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Отправка..." : "Отправить"}
                </Button>
              </div>
            </form>
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
