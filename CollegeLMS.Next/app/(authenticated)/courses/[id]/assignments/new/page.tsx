"use client"

import { useState } from "react"
import { useParams, useRouter } from "next/navigation"
import type { Result } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function CreateAssignmentPage() {
  const router = useRouter()
  const params = useParams()
  const courseId = params.id as string

  const [title, setTitle] = useState("")
  const [description, setDescription] = useState("")
  const [dueDate, setDueDate] = useState("")
  const [maxScore, setMaxScore] = useState("100")
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)


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

  return (
    <div className="flex flex-col gap-6 p-6 max-w-2xl mx-auto">

      <h2 className="text-xl font-semibold">Новое задание</h2>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}
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

