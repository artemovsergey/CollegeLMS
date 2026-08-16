"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { Result, LoginResponse } from "@/types"
import api from "@/lib/api"
import Image from "next/image"
import { useAuth } from "@/lib/auth"
import FormField from "@/components/FormField"
import FormErrorBanner from "@/components/FormErrorBanner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { parseErrors } from "@/lib/errors"

// Тестовые входы для разработки. Убрать перед боевым запуском.
const QUICK_LOGINS = [
  { role: "Admin", login: "admin", password: "admin", label: "Администратор" },
  { role: "Teacher", login: "teacher", password: "teacher", label: "Преподаватель" },
  { role: "Student", login: "student", password: "student", label: "Студент" },
  { role: "Dispatcher", login: "dispatcher", password: "dispatcher", label: "Диспетчер" },
]

export default function LoginPage() {
  const [loginInput, setLoginInput] = useState("")
  const [password, setPassword] = useState("")
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const { login } = useAuth()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    const clientErrors: Record<string, string[]> = {}
    if (!loginInput.trim()) clientErrors.login = ["Введите логин"]
    if (!password) clientErrors.password = ["Введите пароль"]
    setFieldErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return
    setSubmitting(true)

    try {
      const res = await api.post<Result<LoginResponse>>("/api/auth/login", { login: loginInput, password })
      const body = res.data
      if (body.isSuccess && body.data) {
        login(body.data.token, body.data.user)
        const homeByRole: Record<string, string> = {
          Admin: "/admin",
          Teacher: "/teacher/dashboard",
          Student: "/my/dashboard",
          Dispatcher: "/schedule",
        }
        router.push(homeByRole[body.data.user.role] ?? "/my/dashboard")
      } else {
        setFormError(body.errorMessage ?? "Ошибка входа")
      }
    } catch (err) {
      const parsed = parseErrors(err)
      setFieldErrors(parsed.fieldErrors)
      setFormError(parsed.message ?? "Неверный логин или пароль")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="hidden lg:flex flex-col items-center justify-center bg-gradient-to-br from-primary to-primary/40 p-12">
        <div className="max-w-md">
          <Link href="/">
            <Image
              src="/logo.svg"
              alt="Ставропольский колледж связи"
              width={300}
              height={200}
              className="w-full h-auto"
              unoptimized
            />
          </Link>
          <h2 className="mt-8 text-center text-xl font-semibold text-white/90">
            ГБПОУ — Ставропольский колледж связи<br />
            имени Героя Советского Союза В.А. Петрова
          </h2>
        </div>
      </div>

      <div className="flex items-center justify-center bg-background p-4">
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
                style={{ maxHeight: "6rem", width: 'auto', height: '100%' }}
                unoptimized
              />
            </Link>
          </div>

          <h1 className="mb-6 text-2xl font-semibold text-primary text-center">Личный кабинет</h1>

          <div className="mb-6">
            <select
              onChange={(e) => {
                const account = QUICK_LOGINS.find(a => a.role === e.target.value)
                if (account) {
                  setLoginInput(account.login)
                  setPassword(account.password)
                  setFieldErrors({})
                  setFormError(null)
                }
              }}
              defaultValue=""
              aria-label="Быстрый вход (разработка)"
              className="w-full rounded-md border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
            >
              <option value="" disabled>Быстрый вход (разработка): выберите роль...</option>
              {QUICK_LOGINS.map(a => (
                <option key={a.role} value={a.role}>{a.label}</option>
              ))}
            </select>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {formError && <FormErrorBanner message={formError} />}

            <FormField
              id="login"
              label="Логин"
              showAsterisk={false}
              error={fieldErrors.login?.[0]}
            >
              <Input
                id="login"
                type="text"
                value={loginInput}
                onChange={e => setLoginInput(e.target.value)}
                placeholder="Логин"
                autoComplete="username"
                aria-invalid={!!fieldErrors.login?.[0]}
              />
            </FormField>

            <FormField
              id="password"
              label="Пароль"
              showAsterisk={false}
              error={fieldErrors.password?.[0]}
            >
              <Input
                id="password"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="Пароль"
                autoComplete="current-password"
                aria-invalid={!!fieldErrors.password?.[0]}
              />
            </FormField>

            <Button type="submit" disabled={submitting} className="w-full">
              {submitting ? "Вход..." : "Войти"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  )
}