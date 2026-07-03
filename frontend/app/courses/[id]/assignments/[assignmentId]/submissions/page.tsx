"use client"

import { useEffect, useState, useCallback } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result, SubmissionResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

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

export default function SubmissionsPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string
  const assignmentId = params.assignmentId as string

  const [submissions, setSubmissions] = useState<SubmissionResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [scores, setScores] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState<string | null>(null)

  const fetchSubmissions = useCallback(async () => {
    try {
      const res = await api.get<Result<SubmissionResponse[]>>(`/api/assignments/${assignmentId}/submissions`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setSubmissions(body.data)
        const initialScores: Record<string, string> = {}
        body.data.forEach(s => {
          initialScores[s.id] = s.score !== null ? String(s.score) : ""
        })
        setScores(initialScores)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки работ")
    } finally {
      setLoading(false)
    }
  }, [assignmentId])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchSubmissions()
    }
  }, [token, fetchSubmissions])

  const handleSaveScore = async (submissionId: string) => {
    const score = scores[submissionId]
    if (score === "") return
    setSaving(submissionId)
    try {
      await api.put(`/api/submissions/${submissionId}/grade`, { score: Number(score) })
      await fetchSubmissions()
    } catch {
      setError("Ошибка сохранения оценки")
    } finally {
      setSaving(null)
    }
  }

  if (authLoading) return <Loading />
  if (!token) return null

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

      <Button variant="ghost" size="sm" className="self-start" onClick={() => router.push(`/courses/${courseId}`)}>
        &larr; Назад к курсу
      </Button>

      <h2 className="text-xl font-semibold">Работы студентов</h2>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <Loading />
      ) : submissions.length === 0 ? (
        <p className="text-muted-foreground">Нет работ</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Студент</TableHead>
                <TableHead>Дата отправки</TableHead>
                <TableHead>Файл</TableHead>
                <TableHead>Комментарий</TableHead>
                <TableHead>Оценка</TableHead>
                <TableHead>Действия</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {submissions.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.studentName}</TableCell>
                  <TableCell>
                    {new Date(s.submittedAt).toLocaleString("ru-RU")}
                  </TableCell>
                  <TableCell className="max-w-[200px] truncate">{s.filePath}</TableCell>
                  <TableCell className="max-w-[200px] truncate">{s.comment ?? "—"}</TableCell>
                  <TableCell>
                    <Input
                      type="number"
                      min="0"
                      className="w-20 h-8"
                      value={scores[s.id] ?? ""}
                      onChange={e => setScores(prev => ({ ...prev, [s.id]: e.target.value }))}
                    />
                  </TableCell>
                  <TableCell>
                    <Button
                      size="sm"
                      disabled={saving === s.id || !scores[s.id]}
                      onClick={() => handleSaveScore(s.id)}
                    >
                      {saving === s.id ? "..." : "Сохранить"}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
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
