"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function LMSPage() {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) {
      router.push("/login")
    }
  }, [isLoading, token, router])

  if (isLoading || !token || !user) {
    return <div className="flex min-h-screen items-center justify-center"><LoadingSpinner /></div>
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] p-8">
      <div className="max-w-md text-center">
        <h1 className="text-2xl font-bold text-fg mb-2">
          Добро пожаловать, {user.fullName}!
        </h1>
        <p className="text-muted-foreground mb-6">
          Вы вошли как {user.role === "Admin" ? "администратор" : user.role === "Teacher" ? "преподаватель" : user.role === "Dispatcher" ? "диспетчер" : "студент"}
        </p>
        <div className="flex flex-col gap-3">
          <a
            href="/my/profile"
            className="rounded-lg bg-accent px-6 py-3 text-sm font-medium text-white hover:bg-accent-hover transition-colors"
          >
            Перейти в профиль
          </a>
        </div>
      </div>
    </div>
  )
}
