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
  CardDescription,
  CardHeader,
  CardTitle,
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
  { role: "Admin", email: "admin@collegelms.ru", password: "admin", label: "Администратор", icon: UserRoundCog },
  { role: "Teacher", email: "teacher@collegelms.ru", password: "teacher", label: "Преподаватель", icon: UserRoundPen },
  { role: "Student", email: "student@collegelms.ru", password: "student", label: "Студент", icon: UserRound },
  { role: "Dispatcher", email: "dispatcher@collegelms.ru", password: "dispatcher", label: "Диспетчер", icon: UserCog },
]

export default function LoginPage() {
  const [email, setEmail] = useState("admin@collegelms.ru")
  const [password, setPassword] = useState("admin")
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [quickRole, setQuickRole] = useState("")
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
      setError("Неверный email или пароль")
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen flex-col md:flex-row">
      {/* Left: Logo */}
      <div className="flex w-full items-center justify-center p-8 md:w-1/2">
        <Link href="/">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src="http://stvcc.ru/wp-content/uploads/2017/02/logo.jpg"
            alt="ГБПОУ СКС"
            className="max-w-xs h-auto"
          />
        </Link>
      </div>

      {/* Right: Form */}
      <div className="flex w-full items-center justify-center bg-muted p-8 md:w-1/2">
        <Card className="w-full max-w-sm">
          <CardHeader className="pb-2 text-center">
            <CardTitle className="text-xl">Вход в систему</CardTitle>
            <CardDescription>CollegeLMS</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="flex flex-col gap-5">
              {error && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                  {error}
                </div>
              )}

              <div className="flex flex-col gap-2">
                <Label>Быстрый вход</Label>
                <Select
                  value={quickRole}
                  onValueChange={(val) => {
                    const account = QUICK_LOGINS.find(a => a.role === val)
                    if (account) {
                      setEmail(account.email)
                      setPassword(account.password)
                    }
                    setQuickRole("")
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
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="admin@collegelms.ru"
                  autoComplete="email"
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
    </div>
  )
}
