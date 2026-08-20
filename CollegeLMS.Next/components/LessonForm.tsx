"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import type { Result, LessonResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import FormField from "@/components/FormField"
import FormErrorBanner from "@/components/FormErrorBanner"
import { parseErrors } from "@/lib/errors"
import { LESSON_KIND_LABELS } from "@/lib/lessonTypes"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

const LESSON_KIND_OPTIONS: { value: string; label: string }[] = Object.entries(
  LESSON_KIND_LABELS,
).map(([value, label]) => ({ value, label }))

interface LessonFormProps {
  courseId: string
  lesson?: LessonResponse
}

export default function LessonForm({ courseId, lesson }: LessonFormProps) {
  const router = useRouter()
  const isEdit = !!lesson

  const [title, setTitle] = useState(lesson?.title ?? "")
  const [content, setContent] = useState(lesson?.content ?? "")
  const [kind, setKind] = useState<string>(lesson?.kind ?? "Lecture")
  const [mode, setMode] = useState<"markup" | "preview">(isEdit ? "preview" : "markup")
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    const clientErrors: Record<string, string[]> = {}
    if (!title.trim()) clientErrors.title = ["Название занятия обязательно"]
    else if (title.trim().length < 3) clientErrors.title = ["Название должно содержать минимум 3 символа"]
    if (!content.trim()) clientErrors.content = ["Содержание обязательно"]
    setFieldErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return
    setSubmitting(true)
    try {
      const body = { title, content, kind }
      const res = isEdit
        ? await api.put<Result<LessonResponse>>(`/api/courses/${courseId}/lessons/${lesson!.id}`, body)
        : await api.post<Result<LessonResponse>>(`/api/courses/${courseId}/lessons`, body)
      if (res.data.isSuccess) {
        const id = res.data.data?.id
        router.push(id ? `/courses/${courseId}/lessons/${id}` : `/courses/${courseId}`)
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка сохранения")
      }
    } catch (err) {
      const parsed = parseErrors(err)
      setFieldErrors(parsed.fieldErrors)
      setFormError(parsed.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {formError && <FormErrorBanner message={formError} />}

      <FormField id="lesson-title" label="Название занятия" required error={fieldErrors.title?.[0]}>
        <Input
          id="lesson-title"
          required
          value={title}
          onChange={e => setTitle(e.target.value)}
        />
      </FormField>

      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <div className="flex rounded-md border bg-muted/40 p-0.5">
            <button
              type="button"
              onClick={() => setMode("markup")}
              className={`rounded-sm px-3 py-1 text-sm ${mode === "markup" ? "bg-card font-medium shadow-sm" : "text-muted-foreground"}`}
            >
              Разметка
            </button>
            <button
              type="button"
              onClick={() => setMode("preview")}
              className={`rounded-sm px-3 py-1 text-sm ${mode === "preview" ? "bg-card font-medium shadow-sm" : "text-muted-foreground"}`}
            >
              Предпросмотр
            </button>
          </div>
          <span className="text-xs text-muted-foreground">Markdown</span>
        </div>
        {mode === "preview" ? (
          <div className="min-h-40 max-h-96 overflow-y-auto rounded-md border bg-muted/40 p-4">
            <div className="prose max-w-none">
              <ReactMarkdown>{content || "Введите текст для предпросмотра"}</ReactMarkdown>
            </div>
          </div>
        ) : (
          <FormField id="lesson-content" label="Содержание" required error={fieldErrors.content?.[0]}>
            <Textarea
              id="lesson-content"
              required
              className="min-h-[200px] font-mono text-sm"
              value={content}
              onChange={e => setContent(e.target.value)}
            />
          </FormField>
        )}
      </div>

      <FormField id="lesson-kind" label="Тип занятия">
        <Select value={kind} onValueChange={setKind}>
          <SelectTrigger id="lesson-kind">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {LESSON_KIND_OPTIONS.map(opt => (
              <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </FormField>

      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={() => router.push(`/courses/${courseId}`)}>
          Отмена
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Сохранение..." : isEdit ? "Сохранить" : "Создать"}
        </Button>
      </div>
    </form>
  )
}
