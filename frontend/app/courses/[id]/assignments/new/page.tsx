"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

export default function CreateAssignmentPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [dueDate, setDueDate] = useState("")
  const [maxScore, setMaxScore] = useState("100")
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const body = {
        title,
        description,
        dueDate: dueDate || null,
        maxScore: Number(maxScore),
      }
      const res = await api.post<Result<unknown>>(`/api/courses/${courseId}/assignments`, body)
      if (res.data.isSuccess) {
        router.push(`/courses/${courseId}`)
      } else {
        setError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setError("Ошибка создания задания")
    } finally {
      setSubmitting(false)
    }
  }

  if (authLoading) return <Loading />
  if (!token) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto min-h-screen">
      <header className="flex items-center justify-between py-2">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            CL
          </div>
          <h1 className="text-lg font-semibold">CollegeLMS</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="hidden sm:block text-sm text-muted-foreground">{user?.email}</span>
          <Button variant="ghost" size="sm" onClick={() => router.push(`/courses/${courseId}`)}>
            Назад
          </Button>
        </div>
      </header>

      <h2 className="text-xl font-semibold">Новое задание</h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && (
          <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
            {error}
          </div>
        )}
        <div className="flex flex-col gap-2">
          <Label htmlFor="title">Название задания</Label>
          <Input id="title" required value={title} onChange={e => setTitle(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="description">Описание</Label>
          <Textarea id="description" className="min-h-[120px]" value={description} onChange={e => setDescription(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="dueDate">Срок сдачи</Label>
          <Input id="dueDate" type="datetime-local" value={dueDate} onChange={e => setDueDate(e.target.value)} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="maxScore">Максимальный балл</Label>
          <Input id="maxScore" type="number" min="1" required value={maxScore} onChange={e => setMaxScore(e.target.value)} />
        </div>
        <div className="flex gap-2 justify-end pt-2">
          <Button type="button" variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>
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

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
