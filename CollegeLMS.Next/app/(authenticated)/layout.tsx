"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import LoadingSpinner from "@/components/LoadingSpinner"
import { LayoutDashboard, BookOpen, FileCheck, CalendarDays, Settings, GraduationCap } from "lucide-react"

const studentMenu = [
  { label: "Обучение", items: [
    { href: "/my/dashboard", label: "Моя панель", icon: LayoutDashboard },
    { href: "/my/courses", label: "Мои курсы", icon: BookOpen },
    { href: "/my/submissions", label: "Мои работы", icon: FileCheck },
    { href: "/schedule", label: "Расписание", icon: CalendarDays },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки", icon: Settings },
  ]},
]

const teacherMenu = [
  { label: "Обучение", items: [
    { href: "/teacher/dashboard", label: "Панель преподавателя", icon: GraduationCap },
    { href: "/courses", label: "Курсы", icon: BookOpen },
    { href: "/schedule", label: "Расписание", icon: CalendarDays },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки", icon: Settings },
  ]},
]

const dispatcherMenu = [
  { label: "Расписание", items: [
    { href: "/schedule", label: "Расписание", icon: CalendarDays },
  ]},
  { label: "Профиль", items: [
    { href: "/my/profile", label: "Настройки", icon: Settings },
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
