"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter, usePathname } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import { adminMenuSections, adminRoleMap } from "@/lib/menus"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  const allowedRoles = (() => {
    const exact = adminRoleMap[pathname]
    if (exact) return exact
    const byPrefix = Object.entries(adminRoleMap).find(([href]) => pathname.startsWith(href))
    return byPrefix ? byPrefix[1] : []
  })()
  const denied = !!user && allowedRoles.length > 0 && !allowedRoles.includes(user.role)

  useEffect(() => {
    if (isLoading) return
    if (!token) {
      router.push("/login")
      return
    }
    if (denied) router.push("/")
  }, [isLoading, token, denied, router])

  if (isLoading || denied) return <div className="flex min-h-screen items-center justify-center"><LoadingSpinner /></div>
  if (!token || !user) return null

  // Filter by user role
  const filtered = adminMenuSections
    .map(s => ({
      ...s,
      items: s.items.filter(item => (adminRoleMap[item.href] ?? []).includes(user.role)),
    }))
    .filter(s => s.items.length > 0)

  return (
    <AuthenticatedShell menuSections={filtered}>
      {children}
    </AuthenticatedShell>
  )
}
