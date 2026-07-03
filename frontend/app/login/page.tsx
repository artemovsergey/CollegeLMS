"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import type { Result, LoginResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"

export default function LoginPage() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const { login } = useAuth()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const res = await api.post<Result<LoginResponse>>("/api/auth/login", { email, password })
      const body = res.data
      if (body.isSuccess && body.data) {
        login(body.data.token, body.data.user)
        router.push("/")
      } else {
        setError(body.errorMessage ?? "Ошибка входа")
      }
    } catch {
      setError("Неверный email или пароль")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex items-center justify-center min-h-screen">
      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 w-full max-w-sm p-6 rounded border border-gray-200 bg-white shadow-sm"
      >
        <h1 className="text-xl font-bold text-center">Вход в систему</h1>

        {error && (
          <p className="text-sm text-red-600 bg-red-50 rounded p-2">{error}</p>
        )}

        <div className="flex flex-col gap-1">
          <label htmlFor="email" className="text-sm font-medium">Email</label>
          <input
            id="email"
            type="email"
            required
            value={email}
            onChange={e => setEmail(e.target.value)}
            className="rounded border border-gray-300 p-2 text-sm"
            placeholder="admin@collegelms.ru"
          />
        </div>

        <div className="flex flex-col gap-1">
          <label htmlFor="password" className="text-sm font-medium">Пароль</label>
          <input
            id="password"
            type="password"
            required
            value={password}
            onChange={e => setPassword(e.target.value)}
            className="rounded border border-gray-300 p-2 text-sm"
            placeholder="••••••"
          />
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="rounded bg-blue-600 px-4 py-2 text-white font-medium hover:bg-blue-700 disabled:opacity-50 transition"
        >
          {submitting ? "Вход..." : "Войти"}
        </button>
      </form>
    </div>
  )
}
