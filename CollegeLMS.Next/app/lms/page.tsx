"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import LoadingSpinner from "@/components/LoadingSpinner"

const roleRedirect: Record<string, string> = {
  Admin: "/admin",
  Teacher: "/teacher/dashboard",
  Student: "/my/dashboard",
  Dispatcher: "/schedule",
}

export default function LMSPage() {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) {
      router.push("/login")
      return
    }
    if (token && user) {
      router.replace(roleRedirect[user.role] ?? "/schedule")
    }
  }, [isLoading, token, user, router])

  return <div className="flex min-h-screen items-center justify-center"><LoadingSpinner /></div>
}
