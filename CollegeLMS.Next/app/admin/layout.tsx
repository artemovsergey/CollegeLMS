"use client"

import { useEffect, useState, type ReactNode } from "react"
import { useRouter, usePathname } from "next/navigation"
import Link from "next/link"
import { useAuth } from "@/lib/auth"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Users,
  Newspaper,
  MessageSquare,
  Upload,
  BookOpen,
  UsersRound,
  GraduationCap,
  ClipboardCheck,
  CalendarDays,
  BookType,
  BadgeInfo,
  Banknote,
  ChevronDown,
  ChevronRight,
  Menu,
  X,
  Lock,
} from "lucide-react"
import api from "@/lib/api"
import type { Result } from "@/types"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"

const sidebarNav: {
  label: string
  items: { href: string; label: string; icon: typeof Users; roles: string[] }[]
}[] = [
  {
    label: "Система",
    items: [
      { href: "/admin", label: "Пользователи", icon: Users, roles: ["Admin"] },
      { href: "/admin/news", label: "Новости", icon: Newspaper, roles: ["Admin", "Dispatcher"] },
      { href: "/admin/feedback", label: "Обратная связь", icon: MessageSquare, roles: ["Admin"] },
      { href: "/admin/import", label: "Импорт", icon: Upload, roles: ["Admin"] },
    ],
  },
  {
    label: "Обучение",
    items: [
      { href: "/courses", label: "Курсы", icon: BookOpen, roles: ["Admin", "Teacher"] },
      { href: "/groups", label: "Группы", icon: UsersRound, roles: ["Admin"] },
      { href: "/teachers", label: "Преподаватели", icon: GraduationCap, roles: ["Admin"] },
      { href: "/students", label: "Студенты", icon: Users, roles: ["Admin"] },
      { href: "/admin/semesters", label: "Семестры", icon: CalendarDays, roles: ["Admin"] },
      { href: "/admin/specialties", label: "Специальности", icon: BadgeInfo, roles: ["Admin"] },
      { href: "/admin/exams", label: "Экзамены", icon: ClipboardCheck, roles: ["Admin"] },
      { href: "/admin/testing", label: "Тесты", icon: BookType, roles: ["Admin"] },
    ],
  },
  {
    label: "Финансы",
    items: [
      { href: "/admin/stipends", label: "Стипендии", icon: Banknote, roles: ["Admin"] },
    ],
  },
  {
    label: "Расписание",
    items: [
      { href: "/schedule", label: "Расписание", icon: CalendarDays, roles: ["Admin", "Dispatcher", "Teacher"] },
    ],
  },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading, logout } = useAuth()
  const router = useRouter()
  const pathname = usePathname()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(false)
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [cpOldPassword, setCpOldPassword] = useState("")
  const [cpNewPassword, setCpNewPassword] = useState("")
  const [cpError, setCpError] = useState<string | null>(null)
  const [cpSubmitting, setCpSubmitting] = useState(false)

  useEffect(() => {
    if (!isLoading && !token) {
      router.push("/login")
    }
  }, [isLoading, token, router])

  useEffect(() => {
    setSidebarOpen(false)
  }, [pathname])

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setCpError(null)
    setCpSubmitting(true)
    try {
      const res = await api.post<Result<null>>("/api/auth/change-password", {
        oldPassword: cpOldPassword,
        newPassword: cpNewPassword,
      })
      if (res.data.isSuccess) {
        setShowChangePassword(false)
        setCpOldPassword("")
        setCpNewPassword("")
        const { toast } = await import("sonner")
        toast.success("Пароль изменён")
      } else {
        setCpError(res.data.errorMessage ?? "Ошибка смены пароля")
      }
    } catch {
      setCpError("Ошибка смены пароля")
    } finally {
      setCpSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <LoadingSpinner />
      </div>
    )
  }

  if (!token || !user) return null

  const isActive = (href: string) => {
    if (href === "/admin") return pathname === "/admin"
    return pathname.startsWith(href)
  }

  const sidebarWidth = collapsed ? "w-16" : "w-56"

  return (
    <div className="flex min-h-screen">
      <aside
        className={`fixed inset-y-0 left-0 z-30 flex flex-col border-r bg-card transition-all duration-200 ${sidebarWidth} ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        } lg:translate-x-0`}
      >
        <div className="flex h-14 items-center gap-3 border-b px-4">
          <img src="/logo.svg" alt="ГБПОУ СКС" className="h-8 w-8 shrink-0 object-contain" />
          {!collapsed && (
            <Link href="/admin" className="text-sm font-semibold whitespace-nowrap">
              CollegeLMS
            </Link>
          )}
        </div>

        <nav className="flex-1 overflow-y-auto p-2 space-y-4">
          {sidebarNav.map(section => {
            const items = section.items.filter(item => item.roles.includes(user.role))
            if (items.length === 0) return null

            return (
              <div key={section.label}>
                {!collapsed && (
                  <p className="px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                    {section.label}
                  </p>
                )}
                <div className="space-y-0.5">
                  {items.map(item => (
                    <Link
                      key={item.href}
                      href={item.href}
                      className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                        isActive(item.href)
                          ? "bg-primary/10 text-primary"
                          : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                      }`}
                      title={collapsed ? item.label : undefined}
                    >
                      <item.icon className="size-5 shrink-0" />
                      {!collapsed && <span>{item.label}</span>}
                    </Link>
                  ))}
                </div>
              </div>
            )
          })}
        </nav>

        <div className="border-t p-2">
          {!collapsed && (
            <div className="px-3 pb-2">
              <p className="text-sm font-medium truncate">{user.login}</p>
              <Badge variant={roleVariants[user.role] ?? "secondary"} className="mt-1">
                {roleLabels[user.role] ?? user.role}
              </Badge>
            </div>
          )}
          <Button
            variant="ghost"
            size="sm"
            className="w-full justify-start gap-3"
            onClick={() => setShowChangePassword(true)}
          >
            <Lock className="size-4 shrink-0" />
            {!collapsed && <span>Пароль</span>}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="w-full justify-start gap-3"
            onClick={() => {
              logout()
              router.push("/login")
            }}
          >
            <X className="size-4 shrink-0" />
            {!collapsed && <span>Выйти</span>}
          </Button>
        </div>

        <Dialog open={showChangePassword} onOpenChange={setShowChangePassword}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Сменить пароль</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleChangePassword} className="flex flex-col gap-4">
              {cpError && <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{cpError}</p>}
              <div className="flex flex-col gap-2">
                <Label htmlFor="cp-old">Текущий пароль</Label>
                <Input id="cp-old" type="password" required value={cpOldPassword} onChange={e => setCpOldPassword(e.target.value)} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="cp-new">Новый пароль</Label>
                <Input id="cp-new" type="password" required value={cpNewPassword} onChange={e => setCpNewPassword(e.target.value)} />
              </div>
              <div className="flex gap-2 justify-end pt-2">
                <Button type="button" variant="ghost" onClick={() => setShowChangePassword(false)}>Отмена</Button>
                <Button type="submit" disabled={cpSubmitting}>
                  {cpSubmitting ? "Сохранение..." : "Сохранить"}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </aside>

      {sidebarOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/30 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <div className={`flex flex-1 flex-col transition-all duration-200 lg:${collapsed ? "ml-16" : "ml-56"}`}>
        <header className="sticky top-0 z-10 flex h-14 items-center gap-3 border-b bg-background px-4">
          <Button
            variant="ghost"
            size="sm"
            className="lg:hidden"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu className="size-5" />
          </Button>
          <div className="flex-1" />
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setCollapsed(!collapsed)}
            className="hidden lg:flex"
          >
            {collapsed ? <ChevronRight className="size-4" /> : <ChevronDown className="size-4 rotate-90" />}
          </Button>
        </header>
        <main className="flex-1">{children}</main>
      </div>
    </div>
  )
}
