"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import type { Result, LectureResponse } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import FormField from "@/components/FormField"
import FormErrorBanner from "@/components/FormErrorBanner"
import ErrorBanner from "@/components/ErrorBanner"
import { parseErrors } from "@/lib/errors"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

const LECTURE_TYPE_OPTIONS = [
  { value: "Lecture", label: "Лекция" },
  { value: "Practice", label: "Практика" },
  { value: "SelfStudy", label: "Самостоятельная" },
]

interface LectureFormProps {
  courseId: string
  lecture?: LectureResponse
}

export default function LectureForm({ courseId, lecture }: LectureFormProps) {
  const router = useRouter()
  const isEdit = !!lecture

  const [title, setTitle] = useState(lecture?.title ?? "")
  const [content, setContent] = useState(lecture?.content ?? "")
  const [lectureType, setLectureType] = useState<string>(lecture?.lectureType ?? "Lecture")
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
      const body = { title, content, lectureType }
      const res = isEdit
        ? await api.put<Result<LectureResponse>>(`/api/courses/${courseId}/lectures/${lecture.id}`, body)
        : await api.post<Result<LectureResponse>>(`/api/courses/${courseId}/lectures`, body)
      if (res.data.isSuccess) {
        router.push(`/courses/${courseId}`)
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

      <FormField id="lecture-title" label="Название занятия" required error={fieldErrors.title?.[0]}>
        <Input
          id="lecture-title"
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
          <FormField id="lecture-content" label="Содержание" required error={fieldErrors.content?.[0]}>
            <Textarea
              id="lecture-content"
              required
              className="min-h-[200px] font-mono text-sm"
              value={content}
              onChange={e => setContent(e.target.value)}
            />
          </FormField>
        )}
      </div>

      <FormField id="lecture-type" label="Тип занятия">
        <Select value={lectureType} onValueChange={setLectureType}>
          <SelectTrigger id="lecture-type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {LECTURE_TYPE_OPTIONS.map(opt => (
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
