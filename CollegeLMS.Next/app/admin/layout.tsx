"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import { Users, Newspaper, MessageSquare, Upload, BookOpen, UsersRound, GraduationCap, ClipboardCheck, CalendarDays, BookType, BadgeInfo, Banknote } from "lucide-react"
import LoadingSpinner from "@/components/LoadingSpinner"

const menuSections = [
  {
    label: "Система",
    items: [
      { href: "/admin", label: "Пользователи", icon: Users },
      { href: "/admin/news", label: "Новости", icon: Newspaper },
      { href: "/admin/feedback", label: "Обратная связь", icon: MessageSquare },
      { href: "/admin/import", label: "Импорт", icon: Upload },
    ],
  },
  {
    label: "Обучение",
    items: [
      { href: "/courses", label: "Курсы", icon: BookOpen },
      { href: "/groups", label: "Группы", icon: UsersRound },
      { href: "/teachers", label: "Преподаватели", icon: GraduationCap },
      { href: "/students", label: "Студенты", icon: Users },
      { href: "/admin/semesters", label: "Семестры", icon: CalendarDays },
      { href: "/admin/specialties", label: "Специальности", icon: BadgeInfo },
      { href: "/admin/exams", label: "Экзамены", icon: ClipboardCheck },
      { href: "/admin/testing", label: "Тесты", icon: BookType },
    ],
  },
  {
    label: "Финансы",
    items: [
      { href: "/admin/stipends", label: "Стипендии", icon: Banknote },
    ],
  },
  {
    label: "Расписание",
    items: [
      { href: "/schedule", label: "Расписание", icon: CalendarDays },
    ],
  },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) router.push("/login")
  }, [isLoading, token, router])

  if (isLoading) return <div className="flex min-h-screen items-center justify-center"><LoadingSpinner /></div>
  if (!token || !user) return null

  // Filter by user role
  const filtered = menuSections
    .map(s => ({
      ...s,
      items: s.items.filter(item => {
        const roleMap: Record<string, string[]> = {
          "/admin": ["Admin"],
          "/admin/news": ["Admin", "Dispatcher"],
          "/admin/feedback": ["Admin"],
          "/admin/import": ["Admin"],
          "/courses": ["Admin", "Teacher"],
          "/groups": ["Admin"],
          "/teachers": ["Admin"],
          "/students": ["Admin"],
          "/admin/semesters": ["Admin"],
          "/admin/specialties": ["Admin"],
          "/admin/exams": ["Admin"],
          "/admin/testing": ["Admin"],
          "/admin/stipends": ["Admin"],
          "/schedule": ["Admin", "Dispatcher", "Teacher"],
        }
        return (roleMap[item.href] ?? []).includes(user.role)
      }),
    }))
    .filter(s => s.items.length > 0)

  return (
    <AuthenticatedShell menuSections={filtered}>
      {children}
    </AuthenticatedShell>
  )
}
