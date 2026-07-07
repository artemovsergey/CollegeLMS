"use client"

import { useState } from "react"
import type { Result } from "@/types"
import api from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

interface FeedbackResponse {
  message: string
}

export default function FeedbackForm() {
  const [name, setName] = useState("")
  const [email, setEmail] = useState("")
  const [message, setMessage] = useState("")
  const [status, setStatus] = useState<{ type: "success" | "error"; text: string } | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setStatus(null)
    setSubmitting(true)

    try {
      const res = await api.post<Result<FeedbackResponse>>("/api/feedback", { name, email, message })
      const body = res.data
      if (body.isSuccess) {
        setStatus({ type: "success", text: "Сообщение отправлено! Мы свяжемся с вами в ближайшее время." })
        setName("")
        setEmail("")
        setMessage("")
      } else {
        setStatus({ type: "error", text: body.errorMessage ?? "Ошибка отправки" })
      }
    } catch {
      setStatus({ type: "error", text: "Не удалось отправить сообщение. Попробуйте позже." })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto max-w-lg space-y-5">
      <div className="flex flex-col gap-2">
        <Label htmlFor="feedback-name">Имя</Label>
        <Input
          id="feedback-name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Ваше имя"
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="feedback-email">Email</Label>
        <Input
          id="feedback-email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="your@email.ru"
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="feedback-message">Сообщение</Label>
        <Textarea
          id="feedback-message"
          required
          rows={4}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Напишите ваш вопрос или предложение..."
        />
      </div>

      {status && (
        <div
          className={`rounded-md p-3 text-sm ${
            status.type === "success" ? "bg-green-100 text-green-800" : "bg-destructive/10 text-destructive"
          }`}
        >
          {status.text}
        </div>
      )}

      <Button type="submit" disabled={submitting} className="w-full">
        {submitting ? "Отправка..." : "Отправить"}
      </Button>
    </form>
  )
}
