"use client"

import { useEffect, useState, useCallback } from "react"
import Link from "next/link"
import {
  Users,
  GraduationCap,
  BookOpen,
  UsersRound,
  Newspaper,
  MessageSquare,
  UserRound,
  type LucideIcon,
} from "lucide-react"
import type { Result, AdminDashboardResponse } from "@/types"
import api from "@/lib/api"
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"

interface StatCard {
  label: string
  value: number
  href: string
  icon: LucideIcon
}

export default function AdminDashboardPage() {
  const [stats, setStats] = useState<AdminDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchStats = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<AdminDashboardResponse>>("/api/admin/dashboard")
      const body = res.data
      if (body.isSuccess && body.data) {
        setStats(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки данных")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchStats()
  }, [fetchStats])

  if (loading) return <LoadingSpinner size="lg" className="py-20" />
  if (error || !stats) return <ErrorBanner message={error ?? "Данные не найдены"} className="m-6" />

  const cards: StatCard[] = [
    { label: "Пользователи", value: stats.userCount, href: "/admin", icon: Users },
    { label: "Преподаватели", value: stats.teacherCount, href: "/teachers", icon: GraduationCap },
    { label: "Студенты", value: stats.studentCount, href: "/students", icon: UserRound },
    { label: "Курсы", value: stats.courseCount, href: "/courses", icon: BookOpen },
    { label: "Группы", value: stats.groupCount, href: "/groups", icon: UsersRound },
    { label: "Новости", value: stats.newsCount, href: "/admin/news", icon: Newspaper },
    { label: "Обратная связь", value: stats.feedbackCount, href: "/admin/feedback", icon: MessageSquare },
  ]

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto max-w-5xl">
      <h2 className="text-xl font-semibold">Панель администратора</h2>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map(card => (
          <Link key={card.label} href={card.href} className="transition-opacity hover:opacity-90">
            <Card>
              <CardHeader className="pb-2">
                <CardDescription className="flex items-center gap-2 text-sm">
                  <card.icon size={16} className="text-primary" />
                  {card.label}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <CardTitle className="text-3xl">{card.value}</CardTitle>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
