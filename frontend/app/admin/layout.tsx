"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter, usePathname } from "next/navigation"
import Link from "next/link"
import { useAuth } from "@/lib/auth"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"

import { roleLabels, roleVariants } from "@/lib/constants"

const navItems = [
  { href: "/admin", label: "Пользователи", roles: ["Admin"] },
  { href: "/admin/news", label: "Новости", roles: ["Admin", "Dispatcher"] },
  { href: "/admin/import", label: "Импорт", roles: ["Admin"] },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading, logout } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  useEffect(() => {
    if (!isLoading && !token) {
      router.push("/login")
    }
  }, [isLoading, token, router])

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
      </div>
    )
  }

  if (!token || !user) return null

  const nav = navItems.filter(item => item.roles.includes(user.role))

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b">
        <div className="flex h-14 items-center justify-between px-6">
          <div className="flex items-center gap-4">
            <Link href="/admin" className="flex items-center gap-3">
              <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
                CL
              </div>
              <span className="text-sm font-semibold">CollegeLMS</span>
            </Link>
            {nav.length > 1 && (
              <nav className="ml-6 flex items-center gap-1">
                {nav.map(item => (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                      pathname === item.href
                        ? "bg-primary/10 text-primary"
                        : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                    }`}
                  >
                    {item.label}
                  </Link>
                ))}
              </nav>
            )}
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden text-sm text-muted-foreground sm:block">{user.email}</span>
            <Badge variant={roleVariants[user.role] ?? "secondary"}>
              {roleLabels[user.role] ?? user.role}
            </Badge>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                logout()
                router.push("/login")
              }}
            >
              Выйти
            </Button>
          </div>
        </div>
      </header>
      <main className="flex-1">{children}</main>
    </div>
  )
}
