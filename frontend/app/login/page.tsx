"use client"

import { useState } from "react"
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
      <Card className="w-full max-w-sm mx-4">
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Вход в систему</CardTitle>
          <CardDescription>CollegeLMS</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {error && (
              <div className="rounded bg-destructive/15 p-3 text-sm text-destructive">
                {error}
              </div>
            )}

            <div className="flex flex-col gap-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="admin@collegelms.ru"
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
