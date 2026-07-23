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
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="hidden lg:flex flex-col items-center justify-center bg-[#24386a] p-12">
        <div className="max-w-md">
          <Image
            src="/logo.svg"
            alt="Ставропольский колледж связи"
            width={300}
            height={200}
            className="w-full h-auto brightness-0 invert opacity-90"
            unoptimized
          />
          <h2 className="mt-8 text-center text-xl font-semibold text-white/90">
            ГБПОУ «Ставропольский колледж связи<br />
            имени Героя Советского Союза В.А. Петрова»
          </h2>
        </div>
      </div>

      <div className="flex items-center justify-center p-4 bg-white">
        <div className="w-full max-w-sm">
          <div className="mb-8 lg:hidden">
            <Link href="/">
              <Image
                src="/logo.svg"
                alt="Колледж связи"
                width={0}
                height={0}
                sizes="100vw"
                className="mx-auto h-auto"
                style={{ maxHeight: "4rem", width: 'auto', height: '100%' }}
                unoptimized
              />
            </Link>
          </div>

          <h1 className="mb-8 text-2xl font-semibold text-[#24386a] text-center">Личный кабинет</h1>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {error && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <label htmlFor="login" className="text-sm text-[#1a1a2e]">
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
                className="w-full rounded-md border border-[#c9ceda] bg-white px-3 py-2 text-sm text-[#1a1a2e] placeholder:text-[#929cb5] focus:outline-none focus:ring-2 focus:ring-[#24386a]/30 focus:border-[#24386a]"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label htmlFor="password" className="text-sm text-[#1a1a2e]">
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
                className="w-full rounded-md border border-[#c9ceda] bg-white px-3 py-2 text-sm text-[#1a1a2e] placeholder:text-[#929cb5] focus:outline-none focus:ring-2 focus:ring-[#24386a]/30 focus:border-[#24386a]"
              />
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="w-full rounded-md bg-[#24386a] px-4 py-2 text-sm font-medium text-white hover:bg-[#1c2c54] disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {submitting ? "Вход..." : "Войти"}
            </button>
          </form>

          <div className="mt-6 pt-6 border-t border-[#c9ceda]">
            <p className="text-xs text-[#929cb5] mb-2">Быстрый вход (разработка)</p>
            <select
              onChange={(e) => {
                const account = QUICK_LOGINS.find(a => a.role === e.target.value)
                if (account) {
                  setLoginInput(account.login)
                  setPassword(account.password)
                }
              }}
              defaultValue=""
              className="w-full rounded-md border border-[#c9ceda] bg-white px-3 py-1.5 text-xs text-[#1a1a2e] focus:outline-none focus:ring-2 focus:ring-[#24386a]/30 focus:border-[#24386a]"
            >
              <option value="" disabled>Выберите роль...</option>
              {QUICK_LOGINS.map(a => (
                <option key={a.role} value={a.role}>{a.label}</option>
              ))}
            </select>
          </div>
        </div>
      </div>
    </div>
  )
}
