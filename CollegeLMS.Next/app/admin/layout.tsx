"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import AuthenticatedShell from "@/components/AuthenticatedShell"
import { adminMenuSections, adminRoleMap } from "@/lib/menus"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) router.push("/login")
  }, [isLoading, token, router])

  if (isLoading) return <div className="flex min-h-screen items-center justify-center"><LoadingSpinner /></div>
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
