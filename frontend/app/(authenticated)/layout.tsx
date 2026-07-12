"use client"

import { useEffect, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  const { user, token, isLoading, logout } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!isLoading && !token) {
      router.push("/login")
    }
  }, [isLoading, token, router])

  if (isLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token || !user) return null

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b">
        <div className="flex h-14 items-center justify-between px-6">
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
              CL
            </div>
            <span className="text-sm font-semibold">CollegeLMS</span>
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden text-sm text-muted-foreground sm:block">
              {user.login}
            </span>
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
