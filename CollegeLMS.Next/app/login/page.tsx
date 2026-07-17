"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { Result, LoginResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Card,
  CardContent,
} from "@/components/ui/card"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { UserRoundCog, UserRoundPen, UserRound, UserCog } from "lucide-react"

const QUICK_LOGINS = [
  { role: "Admin", login: "admin", password: "admin", label: "Администратор", icon: UserRoundCog },
  { role: "Teacher", login: "teacher", password: "teacher", label: "Преподаватель", icon: UserRoundPen },
  { role: "Student", login: "student", password: "student", label: "Студент", icon: UserRound },
  { role: "Dispatcher", login: "dispatcher", password: "dispatcher", label: "Диспетчер", icon: UserCog },
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
        if (body.data.user.role === "Admin") {
          router.push("/admin")
        } else if (body.data.user.role === "Teacher") {
          router.push("/teacher/dashboard")
        } else if (body.data.user.role === "Dispatcher") {
          router.push("/schedule")
        } else {
          router.push("/my/dashboard")
        }
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
    <div className="flex min-h-screen flex-col items-center justify-center p-8">
      <Link href="/" className="mb-8">
        <img
          src="/logo.svg"
          alt="Колледж связи"
          className="h-auto"
          style={{ maxHeight: "8rem" }}
        />
      </Link>

      <Card className="w-full max-w-sm">
        <CardContent className="pt-6">
          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {error && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </div>
            )}

            <div className="flex flex-col gap-2">
              <Label>Быстрый вход</Label>
              <Select
                onValueChange={(val) => {
                  const account = QUICK_LOGINS.find(a => a.role === val)
                  if (account) {
                    setLoginInput(account.login)
                    setPassword(account.password)
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Выберите роль..." />
                </SelectTrigger>
                <SelectContent>
                  {QUICK_LOGINS.map(a => {
                    const Icon = a.icon
                    return (
                      <SelectItem key={a.role} value={a.role}>
                        <span className="flex items-center gap-2">
                          <Icon className="size-4 text-muted-foreground" />
                          {a.label}
                        </span>
                      </SelectItem>
                    )
                  })}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="login">Логин</Label>
                <Input
                  id="login"
                  type="text"
                  required
                  value={loginInput}
                  onChange={e => setLoginInput(e.target.value)}
                  placeholder="admin"
                  autoComplete="username"
                />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="password">Пароль</Label>
              <Input
                id="password"
                type="password"
                required
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="••••••"
                autoComplete="current-password"
              />
            </div>

            <Button type="submit" disabled={submitting} className="w-full">
              {submitting ? "Вход..." : "Войти"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
