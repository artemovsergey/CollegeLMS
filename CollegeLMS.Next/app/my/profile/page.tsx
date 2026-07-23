"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import api from "@/lib/api"
import type {
  Result,
  ProfileResponse,
  UpdateProfileRequest,
  ChangePasswordRequest,
} from "@/types"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"
import { Save, Lock, ArrowLeft } from "lucide-react"

export default function ProfilePage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [changingPassword, setChangingPassword] = useState(false)
  const [fullName, setFullName] = useState("")
  const [email, setEmail] = useState("")
  const [oldPassword, setOldPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")

  const fetchProfile = useCallback(async () => {
    try {
      const res = await api.get<Result<ProfileResponse>>("/api/auth/profile")
      const body = res.data
      if (body.isSuccess && body.data) {
        setProfile(body.data)
        setFullName(body.data.fullName)
        setEmail(body.data.email)
      }
    } catch {
      toast.error("Не удалось загрузить профиль")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
      return
    }
    if (token) fetchProfile()
  }, [authLoading, token, router, fetchProfile])

  const handleSave = async () => {
    if (!fullName.trim() || !email.trim()) {
      toast.error("Заполните все поля")
      return
    }
    setSaving(true)
    try {
      const request: UpdateProfileRequest = { fullName, email }
      const res = await api.put<Result<ProfileResponse>>(
        "/api/auth/profile",
        request
      )
      const body = res.data
      if (body.isSuccess) {
        toast.success("Профиль сохранён")
        if (body.data) setProfile(body.data)
      } else {
        toast.error(body.errorMessage || "Ошибка сохранения")
      }
    } catch {
      toast.error("Ошибка сети")
    } finally {
      setSaving(false)
    }
  }

  const handleChangePassword = async () => {
    if (!oldPassword || !newPassword) {
      toast.error("Заполните все поля")
      return
    }
    if (newPassword.length < 6) {
      toast.error("Новый пароль должен содержать минимум 6 символов")
      return
    }
    setChangingPassword(true)
    try {
      const request: ChangePasswordRequest = {
        oldPassword,
        newPassword,
      }
      const res = await api.post<Result<null>>(
        "/api/auth/change-password",
        request
      )
      const body = res.data
      if (body.isSuccess) {
        toast.success("Пароль изменён")
        setOldPassword("")
        setNewPassword("")
      } else {
        toast.error(body.errorMessage || "Ошибка смены пароля")
      }
    } catch {
      toast.error("Ошибка сети")
    } finally {
      setChangingPassword(false)
    }
  }

  if (authLoading || loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
      </div>
    )
  }

  if (!profile) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-8">
        <p className="text-center text-muted-foreground">Профиль не найден</p>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 sm:px-6 lg:px-8">
      <button
        onClick={() => router.back()}
        className="mb-6 flex items-center gap-1 text-sm text-muted-foreground hover:text-accent cursor-pointer"
      >
        <ArrowLeft size={16} />
        Назад
      </button>

      <h1 className="mb-6 text-2xl font-semibold text-primary">Мой профиль</h1>

      <div className="mb-8 rounded-lg border border-border bg-card p-6">
        <h2 className="mb-4 text-lg font-medium text-foreground">
          Основные данные
        </h2>
        <div className="space-y-4">
          <div>
            <Label htmlFor="fullName">ФИО</Label>
            <Input
              id="fullName"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          <div>
            <Label>Логин</Label>
            <Input value={profile.login} disabled />
          </div>
        </div>
        <Button
          onClick={handleSave}
          disabled={saving}
          className="mt-4"
        >
          <Save size={16} className="mr-2" />
          {saving ? "Сохранение..." : "Сохранить"}
        </Button>
      </div>

      {profile.teacherData && (
        <div className="mb-8 rounded-lg border border-border bg-card p-6">
          <h2 className="mb-4 text-lg font-medium text-foreground">
            Данные преподавателя
          </h2>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Цикловая комиссия</span>
              <span className="font-medium text-foreground">
                {profile.teacherData.cyclicalCommission}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Должность</span>
              <span className="font-medium text-foreground">
                {profile.teacherData.position}
              </span>
            </div>
          </div>
        </div>
      )}

      {profile.studentData && (
        <div className="mb-8 rounded-lg border border-border bg-card p-6">
          <h2 className="mb-4 text-lg font-medium text-foreground">
            Данные студента
          </h2>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Группа</span>
              <span className="font-medium text-foreground">
                {profile.studentData.groupName}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Номер зачётной книжки</span>
              <span className="font-medium text-foreground">
                {profile.studentData.recordBookNumber}
              </span>
            </div>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-border bg-card p-6">
        <h2 className="mb-4 text-lg font-medium text-foreground">
          <Lock size={16} className="mr-2 inline" />
          Смена пароля
        </h2>
        <div className="space-y-4">
          <div>
            <Label htmlFor="oldPassword">Старый пароль</Label>
            <Input
              id="oldPassword"
              type="password"
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="newPassword">Новый пароль</Label>
            <Input
              id="newPassword"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </div>
        </div>
        <Button
          onClick={handleChangePassword}
          disabled={changingPassword}
          variant="outline"
          className="mt-4"
        >
          <Lock size={16} className="mr-2" />
          {changingPassword ? "Смена..." : "Сменить пароль"}
        </Button>
      </div>
    </div>
  )
}
