"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import LoadingSpinner from "@/components/LoadingSpinner"
import { adminMenuSections, type MenuSection } from "@/lib/menus"
import { LayoutDashboard, BookOpen, CalendarDays, GraduationCap, Settings, FileText, Briefcase, Bell, CalendarX2, CalendarClock, History, ClipboardList } from "lucide-react"

const studentMenu = [
  {
    label: "Обучение", items: [
      { href: "/my/dashboard", label: "Моя панель", icon: LayoutDashboard },
      { href: "/my/courses", label: "Мои курсы", icon: BookOpen },
      { href: "/schedule", label: "Расписание", icon: CalendarDays },
    ]
  },
]

const teacherMenu = [
  {
    label: "Обучение", items: [
      { href: "/teacher/dashboard", label: "Панель преподавателя", icon: GraduationCap },
      { href: "/courses", label: "Мои курсы", icon: BookOpen },
      { href: "/schedule", label: "Расписание", icon: CalendarDays },
      { href: "/teacher/journal", label: "Журнал", icon: ClipboardList },
    ]
  },
  {
    label: "Изменения", items: [
      { href: "/changes", label: "Изменения расписания", icon: History },
    ]
  },
]

const dispatcherMenu = [
  {
    label: "Расписание", items: [
      { href: "/dispatcher/dashboard", label: "Дашборд", icon: LayoutDashboard },
      { href: "/schedule", label: "Расписание", icon: CalendarDays },
      { href: "/changes", label: "Изменения", icon: History },
      { href: "/dispatcher/correction", label: "Корректировка", icon: Settings },
    ]
  },
  {
    label: "Справочники", items: [
      { href: "/dispatcher/bells", label: "Звонки", icon: Bell },
      { href: "/dispatcher/holidays", label: "Нерабочие дни", icon: CalendarX2 },
      { href: "/dispatcher/inserts", label: "Вставки", icon: CalendarClock },
    ]
  },
  {
    label: "Учебный процесс", items: [
      { href: "/dispatcher/documents", label: "Документы", icon: FileText },
      { href: "/dispatcher/practices", label: "Учебные практики", icon: Briefcase },
      { href: "/teacher/journal", label: "Журнал преподавателя", icon: ClipboardList },
    ]
  },
]

const menuByRole: Record<string, MenuSection[]> = {
  Student: studentMenu,
  Teacher: teacherMenu,
  Dispatcher: dispatcherMenu,
  Admin: adminMenuSections,
}

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) router.push("/login")
  }, [isLoading, token, router])

  if (isLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token || !user) return null

  const userRoles = user?.roles ?? []
  const menuSections = userRoles.includes("Admin")
    ? menuByRole.Admin
    : userRoles.includes("Dispatcher")
      ? menuByRole.Dispatcher
      : userRoles.includes("Teacher")
        ? menuByRole.Teacher
        : studentMenu

  return <AuthenticatedShell menuSections={menuSections}>{children}</AuthenticatedShell>
}
