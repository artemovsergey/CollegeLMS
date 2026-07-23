"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { Result, LoginResponse } from "@/types"
import api from "@/lib/api"
import Image from "next/image"
import { useAuth } from "@/lib/auth"

const QUICK_LOGINS = [
  { role: "Admin", login: "admin", password: "admin", label: "Администратор" },
  { role: "Teacher", login: "teacher", password: "teacher", label: "Преподаватель" },
  { role: "Student", login: "student", password: "student", label: "Студент" },
  { role: "Dispatcher", login: "dispatcher", password: "dispatcher", label: "Диспетчер" },
]

export default function LoginPage() {
  const [loginInput, setLoginInput] = useState("admin")
  const [password, setPassword] = useState("admin")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const { login } = useAuth()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      const res = await api.post<Result<LoginResponse>>("/api/auth/login", { login: loginInput, password })
      const body = res.data
      if (body.isSuccess && body.data) {
        login(body.data.token, body.data.user)
        router.push("/lms")
      } else {
        setError(body.errorMessage ?? "Ошибка входа")
      }
    } catch {
      setError("Неверный логин или пароль")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-4 bg-muted">
      <div className="w-full max-w-sm bg-card rounded-lg border border-border p-8">
        <div className="flex flex-col items-center gap-3 mb-8">
          <Link href="/">
            <Image
              src="/logo.svg"
              alt="Колледж связи"
              width={0}
              height={0}
              sizes="100vw"
              className="h-auto"
              style={{ maxHeight: "4rem", width: 'auto', height: '100%' }}
              unoptimized
            />
          </Link>
          <h1 className="text-xl font-semibold text-fg">Личный кабинет</h1>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          {error && (
            <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
              {error}
            </div>
          )}

          <div className="flex flex-col gap-1.5">
            <label htmlFor="login" className="text-sm text-fg">
              Логин
            </label>
            <input
              id="login"
              type="text"
              required
              value={loginInput}
              onChange={e => setLoginInput(e.target.value)}
              placeholder="admin"
              autoComplete="username"
              className="w-full rounded-md border border-border bg-bg px-3 py-2 text-sm text-fg placeholder:text-muted-fg focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="password" className="text-sm text-fg">
              Пароль
            </label>
            <input
              id="password"
              type="password"
              required
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="••••••"
              autoComplete="current-password"
              className="w-full rounded-md border border-border bg-bg px-3 py-2 text-sm text-fg placeholder:text-muted-fg focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent"
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {submitting ? "Вход..." : "Войти"}
          </button>
        </form>

        <div className="mt-6 pt-6 border-t border-border">
          <p className="text-xs text-muted-fg mb-2">Быстрый вход (разработка)</p>
          <select
            onChange={(e) => {
              const account = QUICK_LOGINS.find(a => a.role === e.target.value)
              if (account) {
                setLoginInput(account.login)
                setPassword(account.password)
              }
            }}
            defaultValue=""
            className="w-full rounded-md border border-border bg-bg px-3 py-1.5 text-xs text-fg focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent"
          >
            <option value="" disabled>Выберите роль...</option>
            {QUICK_LOGINS.map(a => (
              <option key={a.role} value={a.role}>{a.label}</option>
            ))}
          </select>
        </div>
      </div>
    </div>
  )
}
