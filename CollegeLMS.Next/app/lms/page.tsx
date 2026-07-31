"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"

export default function LmsRedirectPage() {
  const { token, user } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!token) {
      router.replace("/login")
      return
    }
    switch (user?.role) {
      case "Admin":
        router.replace("/admin")
        break
      case "Teacher":
        router.replace("/teacher/dashboard")
        break
      case "Dispatcher":
        router.replace("/schedule")
        break
      default:
        router.replace("/my/dashboard")
    }
  }, [token, user, router])

  return null
}
