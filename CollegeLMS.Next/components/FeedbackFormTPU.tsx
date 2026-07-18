"use client"

import { useState } from "react"
import type { Result } from "@/types"
import api from "@/lib/api"

interface FeedbackResponse {
  message: string
}

export default function FeedbackFormTPU() {
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
      <div>
        <label htmlFor="tpu-feedback-name" className="mb-1.5 block text-sm font-medium text-[var(--color-tpu-text-primary)]">
          Имя
        </label>
        <input
          id="tpu-feedback-name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Ваше имя"
          className="w-full rounded-lg border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] px-4 py-2.5 text-sm text-[var(--color-tpu-text-primary)] placeholder:text-[var(--color-tpu-text-secondary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-tpu-accent)]/30 focus:border-[var(--color-tpu-accent)] transition-colors"
        />
      </div>

      <div>
        <label htmlFor="tpu-feedback-email" className="mb-1.5 block text-sm font-medium text-[var(--color-tpu-text-primary)]">
          Email
        </label>
        <input
          id="tpu-feedback-email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="your@email.ru"
          className="w-full rounded-lg border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] px-4 py-2.5 text-sm text-[var(--color-tpu-text-primary)] placeholder:text-[var(--color-tpu-text-secondary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-tpu-accent)]/30 focus:border-[var(--color-tpu-accent)] transition-colors"
        />
      </div>

      <div>
        <label htmlFor="tpu-feedback-message" className="mb-1.5 block text-sm font-medium text-[var(--color-tpu-text-primary)]">
          Сообщение
        </label>
        <textarea
          id="tpu-feedback-message"
          required
          rows={4}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Напишите ваш вопрос или предложение..."
          className="w-full rounded-lg border border-[var(--color-tpu-border)] bg-[var(--color-tpu-card-bg)] px-4 py-2.5 text-sm text-[var(--color-tpu-text-primary)] placeholder:text-[var(--color-tpu-text-secondary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-tpu-accent)]/30 focus:border-[var(--color-tpu-accent)] transition-colors resize-y"
        />
      </div>

      {status && (
        <div
          className={`rounded-lg p-3 text-sm ${
            status.type === "success" ? "bg-green-50 text-green-700" : "bg-red-50 text-red-600"
          }`}
        >
          {status.text}
        </div>
      )}

      <button
        type="submit"
        disabled={submitting}
        className="w-full rounded-lg bg-[var(--color-tpu-accent)] px-6 py-3 text-sm font-medium text-white hover:bg-[var(--color-tpu-accent-hover)] transition-colors disabled:opacity-50"
      >
        {submitting ? "Отправка..." : "Отправить"}
      </button>
    </form>
  )
}
