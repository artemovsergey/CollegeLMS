"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import LoadingSpinner from "@/components/LoadingSpinner"

const studentMenu = [
  { label: "Обучение", items: [
    { href: "/my/dashboard", label: "Моя панель" },
    { href: "/my/courses", label: "Мои курсы" },
    { href: "/my/submissions", label: "Мои работы" },
    { href: "/schedule", label: "Расписание" },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки" },
  ]},
]

const teacherMenu = [
  { label: "Обучение", items: [
    { href: "/teacher/dashboard", label: "Панель преподавателя" },
    { href: "/courses", label: "Курсы" },
    { href: "/schedule", label: "Расписание" },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки" },
  ]},
]

const dispatcherMenu = [
  { label: "Расписание", items: [
    { href: "/schedule", label: "Расписание" },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки" },
  ]},
]

const menuByRole: Record<string, typeof studentMenu> = {
  Student: studentMenu,
  Teacher: teacherMenu,
  Dispatcher: dispatcherMenu,
}

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) router.push("/login")
  }, [isLoading, token, router])

  if (isLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token || !user) return null

  const menuSections = menuByRole[user.role] ?? studentMenu

  return <AuthenticatedShell menuSections={menuSections}>{children}</AuthenticatedShell>
}
