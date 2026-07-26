"use client"

import { LogIn, User, Calendar, Clock, FileText } from "lucide-react"
import Link from "next/link"
import { useAuth } from "@/lib/auth"

export default function EmployeePage() {
  const { user, login, logout } = useAuth()

  if (!user) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center py-16">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mb-6 flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
              <User size={32} className="text-primary" />
            </div>
          </div>
          <h1 className="mb-2 text-2xl font-bold text-fg">Сотруднику</h1>
          <p className="mb-8 text-sm text-muted-foreground">
            Войдите в личный кабинет для доступа к расписанию, журналам и другим сервисам
          </p>
          <Link
            href="/login"
            className="inline-flex items-center gap-2 rounded-lg bg-accent px-6 py-3 text-sm font-medium text-white transition-colors hover:bg-accent-hover"
          >
            <LogIn size={18} />
            Войти в систему
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-fg">Сотруднику</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {user.fullName}, {user.role === "Admin" ? "администратор" : user.role === "Teacher" ? "преподаватель" : "сотрудник"}
          </p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Link
            href="/lms"
            className="flex items-center gap-4 rounded-lg border border-border bg-card p-5 transition-colors hover:border-accent/30 hover:shadow-sm"
          >
            <span className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <User size={24} />
            </span>
            <div>
              <h3 className="text-sm font-semibold text-fg">Личный кабинет</h3>
              <p className="text-xs text-muted-foreground">Управление профилем и настройками</p>
            </div>
          </Link>

          <Link
            href="/schedule"
            className="flex items-center gap-4 rounded-lg border border-border bg-card p-5 transition-colors hover:border-accent/30 hover:shadow-sm"
          >
            <span className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Calendar size={24} />
            </span>
            <div>
              <h3 className="text-sm font-semibold text-fg">Расписание</h3>
              <p className="text-xs text-muted-foreground">Занятия, экзамены, консультации</p>
            </div>
          </Link>

          <Link
            href="/my/profile"
            className="flex items-center gap-4 rounded-lg border border-border bg-card p-5 transition-colors hover:border-accent/30 hover:shadow-sm"
          >
            <span className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <FileText size={24} />
            </span>
            <div>
              <h3 className="text-sm font-semibold text-fg">Журналы</h3>
              <p className="text-xs text-muted-foreground">Успеваемость и посещаемость</p>
            </div>
          </Link>
        </div>
      </div>
    </div>
  )
}
