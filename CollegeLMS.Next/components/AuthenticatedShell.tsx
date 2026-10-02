"use client"

import { useEffect, useState, type ReactNode } from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { useAuth } from "@/lib/auth"
import { Menu, X, LogOut, User, Lock, type LucideIcon } from "lucide-react"
import { roleLabels, roleVariants } from "@/lib/constants"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { NativeDialog, NativeDialogHeader, NativeDialogTitle, NativeDialogFooter, NativeDialogClose } from "@/components/ui/native-dialog"
import api from "@/lib/api"
import type { Result } from "@/types"

type MenuItem = { href: string; label: string; icon?: LucideIcon }
type MenuSection = { label: string; items: MenuItem[] }

interface AuthenticatedShellProps {
  children: ReactNode
  menuSections: MenuSection[]
}

export default function AuthenticatedShell({ children, menuSections }: AuthenticatedShellProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const [showChangePassword, setShowChangePassword] = useState(false)
  const [cpOldPassword, setCpOldPassword] = useState("")
  const [cpNewPassword, setCpNewPassword] = useState("")
  const [cpError, setCpError] = useState<string | null>(null)
  const [cpSubmitting, setCpSubmitting] = useState(false)
  const pathname = usePathname()
  const { user, logout } = useAuth()

  // Escape закрывает шторку. Без этого единственный способ выйти из меню —
  // мышь: ни фокус не уходил внутрь, ни с клавиатуры закрыть было нечем.
  useEffect(() => {
    if (!menuOpen && !profileOpen) return
    function onKey(event: KeyboardEvent) {
      if (event.key !== "Escape") return
      setMenuOpen(false)
      setProfileOpen(false)
    }
    document.addEventListener("keydown", onKey)
    return () => document.removeEventListener("keydown", onKey)
  }, [menuOpen, profileOpen])
  // Embed-режим (?embed=1): прячем шапку CRM, чтобы страницу можно было
  // показать в телефонной рамке превью мини-приложения.
  const [embed, setEmbed] = useState(false)
  useEffect(() => {
    if (typeof window === "undefined") return
    setEmbed(new URLSearchParams(window.location.search).get("embed") === "1")
  }, [])

  const isActive = (href: string) => pathname === href || (href !== "/admin" && pathname.startsWith(href + "/"))

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

  const initials = user
    ? user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)
    : "?"

  const homeByRole: Record<string, string> = {
    Admin: "/admin",
    Dispatcher: "/schedule",
    Teacher: "/teacher/dashboard",
    Student: "/my/dashboard",
  }

  return (
    <div className="flex min-h-screen flex-col">
      {/* Header */}
      {!embed && (
        <header className="sticky top-0 z-30 border-b border-border bg-bg">
        <div className="flex h-14 items-center justify-between px-4">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setMenuOpen(true)}
              className="text-muted-fg"
              aria-label="Меню"
            >
              <Menu size={20} />
            </Button>
            <Link href={homeByRole[user?.roles?.[0] ?? ""] ?? "/my/dashboard"} className="ml-2 flex items-center">
              <span className="text-sm font-semibold text-fg leading-tight">Колледж связи</span>
            </Link>
          </div>

          <Button
            variant="ghost"
            size="icon-lg"
            onClick={() => setProfileOpen(true)}
            className="text-sm font-medium text-muted-fg"
            aria-label="Профиль"
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent overflow-hidden">
              {user?.avatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={user.avatarUrl} alt="Аватар" className="h-8 w-8 rounded-full object-cover" />
              ) : (
                initials
              )}
            </span>
          </Button>
        </div>
      </header>
      )}

      {/* Left drawer (menu) */}
      {menuOpen && (
        <div className="fixed inset-0 z-40 flex">
          <div className="absolute inset-0 bg-black/30" onClick={() => setMenuOpen(false)} />
          <aside
            role="dialog"
            aria-modal="true"
            aria-label="Навигация"
            className="relative z-50 flex max-w-[85vw] flex-col border-r border-border bg-card shadow-lg"
          >
            <div className="flex h-14 items-center justify-between border-b border-border px-4">
              <span className="text-sm font-semibold text-fg">Навигация</span>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setMenuOpen(false)}
                className="text-muted-fg"
                aria-label="Закрыть меню"
              >
                <X size={18} />
              </Button>
            </div>
            <nav className="flex-1 overflow-y-auto p-3 space-y-4">
              {menuSections.map((section) => (
                <div key={section.label}>
                  <p className="px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-fg mb-1">{section.label}</p>
                  <div className="space-y-0.5">
                    {section.items.map((item) => (
                      <Link
                        key={item.href}
                        href={item.href}
                        onClick={() => setMenuOpen(false)}
                        className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${isActive(item.href)
                            ? "bg-accent/10 text-accent"
                            : "text-muted-fg hover:bg-muted hover:text-fg"
                          }`}
                      >
                        {item.icon && <item.icon size={16} className="shrink-0" />}
                        <span>{item.label}</span>
                      </Link>
                    ))}
                  </div>
                </div>
              ))}
            </nav>
          </aside>
        </div>
      )}

      {/* Right drawer (profile) */}
      {profileOpen && (
        <div className="fixed inset-0 z-40 flex justify-end">
          <div className="absolute inset-0 bg-black/30" onClick={() => setProfileOpen(false)} />
          <aside
            role="dialog"
            aria-modal="true"
            aria-label="Профиль"
            className="relative z-50 flex max-w-[85vw] flex-col border-l border-border bg-card shadow-lg"
          >
            <div className="flex h-14 items-center justify-between border-b border-border px-4">
              <span className="text-sm font-semibold text-fg">Профиль</span>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setProfileOpen(false)}
                className="text-muted-fg"
                aria-label="Закрыть профиль"
              >
                <X size={18} />
              </Button>
            </div>
            <div className="p-4">
              <div className="flex flex-col items-center text-center mb-6">
                <span className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/20 text-xl font-bold text-accent mb-3 overflow-hidden">
                  {user?.avatarUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={user.avatarUrl} alt="Аватар" className="h-16 w-16 rounded-full object-cover" />
                  ) : (
                    initials
                  )}
                </span>
                <h3 className="text-sm font-semibold text-fg">{user?.fullName}</h3>
                <p className="text-xs text-muted-fg mt-0.5">{user?.email}</p>
                {user?.roles && user.roles.length > 0 && (
                  <Badge variant={roleVariants[user.roles[0]] ?? "secondary"} className="mt-2">
                    {roleLabels[user.roles[0]] ?? user.roles[0]}
                  </Badge>
                )}
              </div>
              <div className="space-y-1">
                <Link
                  href="/my/profile"
                  onClick={() => setProfileOpen(false)}
                  className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-fg hover:bg-muted hover:text-fg transition-colors"
                >
                  <User size={16} />
                  Профиль
                </Link>
                <Button
                  variant="ghost"
                  onClick={() => { setShowChangePassword(true) }}
                  className="h-11 w-full justify-start gap-3 px-3 text-sm font-medium text-muted-fg hover:text-fg sm:h-9"
                >
                  <Lock size={16} aria-hidden />
                  Сменить пароль
                </Button>
                <hr className="border-border" />
                <Button
                  variant="ghost"
                  onClick={() => { logout() }}
                  className="h-11 w-full justify-start gap-3 px-3 text-sm font-medium text-muted-fg hover:text-fg sm:h-9"
                >
                  <LogOut size={16} aria-hidden />
                  Выйти
                </Button>
              </div>
            </div>
          </aside>
        </div>
      )}

      {/* Change Password Dialog */}
      <NativeDialog open={showChangePassword} onOpenChange={setShowChangePassword} className="sm:max-w-sm w-full">
        <NativeDialogClose onClick={() => setShowChangePassword(false)} />
        <NativeDialogHeader><NativeDialogTitle>Сменить пароль</NativeDialogTitle></NativeDialogHeader>
        <form onSubmit={handleChangePassword} className="flex flex-col gap-4 p-6">
          {cpError && <p className="rounded-md bg-destructive/10 p-3 text-sm text-destructive-text">{cpError}</p>}
          <div className="flex flex-col gap-2">
            <Label htmlFor="cp-old">Текущий пароль</Label>
            <Input id="cp-old" type="password" required value={cpOldPassword} onChange={e => setCpOldPassword(e.target.value)} className="bg-muted" />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="cp-new">Новый пароль</Label>
            <Input id="cp-new" type="password" required value={cpNewPassword} onChange={e => setCpNewPassword(e.target.value)} className="bg-muted" />
          </div>
          <NativeDialogFooter>
            <Button type="button" variant="ghost" onClick={() => setShowChangePassword(false)}>Отмена</Button>
            <Button type="submit" disabled={cpSubmitting}>{cpSubmitting ? "Сохранение..." : "Сохранить"}</Button>
          </NativeDialogFooter>
        </form>
      </NativeDialog>

      {/* Main content */}
      <main className="flex-1">{children}</main>
    </div>
  )
}
