"use client"

import { useState, type ReactNode } from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { useAuth } from "@/lib/auth"
import { Menu, X, LogOut, User } from "lucide-react"
import { roleLabels, roleVariants } from "@/lib/constants"
import { Badge } from "@/components/ui/badge"

type MenuItem = { href: string; label: string }
type MenuSection = { label: string; items: MenuItem[] }

interface AuthenticatedShellProps {
  children: ReactNode
  menuSections: MenuSection[]
}

export default function AuthenticatedShell({ children, menuSections }: AuthenticatedShellProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const pathname = usePathname()
  const { user, logout } = useAuth()

  const isActive = (href: string) => pathname === href || pathname.startsWith(href + "/")

  const initials = user
    ? user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)
    : "?"

  return (
    <div className="flex min-h-screen flex-col">
      {/* Header */}
      <header className="sticky top-0 z-30 border-b border-border bg-bg">
        <div className="flex h-14 items-center justify-between px-4">
          <div className="flex items-center gap-2">
            <button
              onClick={() => setMenuOpen(true)}
              className="rounded-md p-2 text-muted-fg hover:bg-muted transition-colors"
              aria-label="Меню"
            >
              <Menu size={20} />
            </button>
            <Link href="/" className="flex items-center gap-2 ml-2">
              <img src="/logo.svg" alt="" className="object-contain" style={{ height: "28px" }} />
              <div className="flex flex-col leading-tight">
                <span className="text-xs font-semibold text-fg leading-tight">Ставропольский колледж связи</span>
                <span className="text-[10px] text-muted-fg leading-tight">имени В.А. Петрова</span>
              </div>
            </Link>
          </div>

          <button
            onClick={() => setProfileOpen(true)}
            className="flex items-center gap-2 rounded-md p-1.5 text-sm font-medium text-muted-fg hover:bg-muted transition-colors"
            aria-label="Профиль"
          >
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent">
              {initials}
            </span>
          </button>
        </div>
      </header>

      {/* Left drawer (menu) */}
      {menuOpen && (
        <div className="fixed inset-0 z-40 flex">
          <div className="absolute inset-0 bg-black/30" onClick={() => setMenuOpen(false)} />
          <aside className="relative z-50 flex w-64 flex-col bg-card border-r border-border shadow-lg">
            <div className="flex h-14 items-center justify-between border-b border-border px-4">
              <span className="text-sm font-semibold text-fg">Навигация</span>
              <button onClick={() => setMenuOpen(false)} className="rounded-md p-1.5 text-muted-fg hover:bg-muted transition-colors" aria-label="Закрыть">
                <X size={18} />
              </button>
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
                        className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                          isActive(item.href)
                            ? "bg-accent/10 text-accent"
                            : "text-muted-fg hover:bg-muted hover:text-fg"
                        }`}
                      >
                        <span>{item.label}</span>
                      </Link>
                    ))}
                  </div>
                </div>
              ))}
            </nav>
            <div className="border-t border-border p-3">
              <button
                onClick={() => { logout() }}
                className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-fg hover:bg-muted hover:text-fg transition-colors"
              >
                <LogOut size={16} />
                Выйти
              </button>
            </div>
          </aside>
        </div>
      )}

      {/* Right drawer (profile) */}
      {profileOpen && (
        <div className="fixed inset-0 z-40 flex justify-end">
          <div className="absolute inset-0 bg-black/30" onClick={() => setProfileOpen(false)} />
          <aside className="relative z-50 flex w-72 flex-col bg-card border-l border-border shadow-lg">
            <div className="flex h-14 items-center justify-between border-b border-border px-4">
              <span className="text-sm font-semibold text-fg">Профиль</span>
              <button onClick={() => setProfileOpen(false)} className="rounded-md p-1.5 text-muted-fg hover:bg-muted transition-colors" aria-label="Закрыть">
                <X size={18} />
              </button>
            </div>
            <div className="p-4">
              <div className="flex flex-col items-center text-center mb-6">
                <span className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/20 text-xl font-bold text-accent mb-3">
                  {initials}
                </span>
                <h3 className="text-sm font-semibold text-fg">{user?.fullName}</h3>
                <p className="text-xs text-muted-fg mt-0.5">{user?.login}</p>
                {user?.role && (
                  <Badge variant={roleVariants[user.role] ?? "secondary"} className="mt-2">
                    {roleLabels[user.role] ?? user.role}
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
                  Личный кабинет
                </Link>
                <button
                  onClick={() => { logout() }}
                  className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-fg hover:bg-muted hover:text-fg transition-colors"
                >
                  <LogOut size={16} />
                  Выйти
                </button>
              </div>
            </div>
          </aside>
        </div>
      )}

      {/* Main content */}
      <main className="flex-1">{children}</main>
    </div>
  )
}
