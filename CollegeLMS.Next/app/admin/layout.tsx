"use client"

import { useEffect, useState, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import api from "@/lib/api"
import type { Result } from "@/types"
import { Lock } from "lucide-react"
import LoadingSpinner from "@/components/LoadingSpinner"

const menuSections = [
  {
    label: "Система",
    items: [
      { href: "/admin", label: "Пользователи" },
      { href: "/admin/news", label: "Новости" },
      { href: "/admin/feedback", label: "Обратная связь" },
      { href: "/admin/import", label: "Импорт" },
    ],
  },
  {
    label: "Обучение",
    items: [
      { href: "/courses", label: "Курсы" },
      { href: "/groups", label: "Группы" },
      { href: "/teachers", label: "Преподаватели" },
      { href: "/students", label: "Студенты" },
      { href: "/admin/semesters", label: "Семестры" },
      { href: "/admin/specialties", label: "Специальности" },
      { href: "/admin/exams", label: "Экзамены" },
      { href: "/admin/testing", label: "Тесты" },
    ],
  },
  {
    label: "Финансы",
    items: [
      { href: "/admin/stipends", label: "Стипендии" },
    ],
  },
  {
    label: "Расписание",
    items: [
      { href: "/schedule", label: "Расписание" },
    ],
  },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading, logout } = useAuth()
  const router = useRouter()
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [cpOldPassword, setCpOldPassword] = useState("")
  const [cpNewPassword, setCpNewPassword] = useState("")
  const [cpError, setCpError] = useState<string | null>(null)
  const [cpSubmitting, setCpSubmitting] = useState(false)

  useEffect(() => {
    if (!isLoading && !token) router.push("/login")
  }, [isLoading, token, router])

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
    <>
      <AuthenticatedShell menuSections={filtered}>
        {children}
      </AuthenticatedShell>

      <div className="fixed bottom-4 right-4 z-50">
        <Button variant="outline" size="sm" onClick={() => setShowChangePassword(true)} className="flex items-center gap-2 shadow-sm">
          <Lock size={14} />
          Сменить пароль
        </Button>
      </div>

      <Dialog open={showChangePassword} onOpenChange={setShowChangePassword}>
        <DialogContent>
          <DialogHeader><DialogTitle>Сменить пароль</DialogTitle></DialogHeader>
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
              <Button type="submit" disabled={cpSubmitting}>{cpSubmitting ? "Сохранение..." : "Сохранить"}</Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}
