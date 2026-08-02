"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { useParams, useRouter } from "next/navigation"
import { ChevronLeft, BookOpen } from "lucide-react"
import type { Result, UserProfileResponse } from "@/types"
import api from "@/lib/api"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import ErrorBanner from "@/components/ErrorBanner"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"

export default function UserProfilePage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const [profile, setProfile] = useState<UserProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<Result<UserProfileResponse>>(`/api/users/${id}/profile`)
      .then(res => {
        const body = res.data
        if (body.isSuccess && body.data) setProfile(body.data)
        else setError(body.errorMessage ?? "Ошибка загрузки")
      })
      .catch(() => setError("Ошибка загрузки профиля"))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) {
    return (
      <div className="flex flex-col gap-4 p-6 max-w-3xl mx-auto">
        <LoadingSpinner size="lg" className="py-20" />
      </div>
    )
  }

  if (error || !profile) {
    return (
      <div className="p-6 max-w-3xl mx-auto">
        <ErrorBanner message={error ?? "Профиль не найден"} />
      </div>
    )
  }

  const { user, courses } = profile

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl mx-auto">
      <button
        onClick={() => router.push("/admin")}
        className="flex items-center gap-1 text-sm text-muted-foreground hover:text-fg w-fit"
      >
        <ChevronLeft size={16} /> К списку пользователей
      </button>

      <Card>
        <CardHeader>
          <CardTitle>Пользователь</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 text-sm">
          <div className="flex items-center justify-between">
            <span className="font-medium">{user.fullName}</span>
            <Badge variant={roleVariants[user.role] ?? "secondary"}>
              {roleLabels[user.role] ?? user.role}
            </Badge>
          </div>
          <p>Логин: {user.login}</p>
          <p>Email: {user.email}</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <BookOpen size={16} /> Курсы преподавателя
          </CardTitle>
        </CardHeader>
        <CardContent>
          {courses.length === 0 ? (
            <p className="text-sm text-muted-foreground">Нет курсов</p>
          ) : (
            <ul className="flex flex-col divide-y divide-border">
              {courses.map(c => (
                <li key={c.id}>
                  <Link href={`/courses/${c.id}`} className="block py-2 text-sm hover:text-primary">
                    {c.title}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
