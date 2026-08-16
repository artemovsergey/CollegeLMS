"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"
import api from "@/lib/api"
import type {
  Result,
  ProfileResponse,
  UpdateProfileRequest,
  TeacherCategory,
} from "@/types"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"
import { Save, ArrowLeft } from "lucide-react"

export default function ProfilePage() {
  const { user, token, isLoading: authLoading, updateUser } = useAuth()
  const router = useRouter()
  const [profile, setProfile] = useState<ProfileResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [fullName, setFullName] = useState("")
  const [email, setEmail] = useState("")
  const [cyclicalCommission, setCyclicalCommission] = useState("")
  const [category, setCategory] = useState<TeacherCategory | "">("")

  const fetchProfile = useCallback(async () => {
    try {
      const res = await api.get<Result<ProfileResponse>>("/api/auth/profile")
      const body = res.data
      if (body.isSuccess && body.data) {
        setProfile(body.data)
        setFullName(body.data.fullName)
        setEmail(body.data.email)
        setCyclicalCommission(body.data.teacherData?.cyclicalCommission ?? "")
        setCategory(body.data.teacherData?.category ? (body.data.teacherData.category as TeacherCategory) : "")
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
      const request: UpdateProfileRequest = {
        fullName,
        email,
        cyclicalCommission: profile?.teacherData ? cyclicalCommission : undefined,
        category: profile?.teacherData && category ? category : undefined,
      }
      const res = await api.put<Result<ProfileResponse>>(
        "/api/auth/profile",
        request
      )
      const body = res.data
      if (body.isSuccess) {
        toast.success("Профиль сохранён")
        if (body.data) {
          setProfile(body.data)
          setCyclicalCommission(body.data.teacherData?.cyclicalCommission ?? "")
          setCategory(body.data.teacherData?.category ? (body.data.teacherData.category as TeacherCategory) : "")
        }
      } else {
        toast.error(body.errorMessage || "Ошибка сохранения")
      }
    } catch {
      toast.error("Ошибка сети")
    } finally {
      setSaving(false)
    }
  }

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const formData = new FormData()
    formData.append("file", file)
    setSaving(true)
    try {
      const res = await api.post<Result<ProfileResponse>>("/api/auth/avatar", formData)
      const body = res.data
      if (body.isSuccess && body.data) {
        setProfile(body.data)
        updateUser({ avatarUrl: body.data.avatarUrl })
        toast.success("Аватар обновлён")
      } else {
        toast.error(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      toast.error("Ошибка сети")
    } finally {
      setSaving(false)
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
          {user?.role !== "Student" && (
            <div className="flex items-center gap-4">
              {profile.avatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={profile.avatarUrl} alt="Аватар" className="h-16 w-16 rounded-full object-cover" />
              ) : (
                <span className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/20 text-xl font-bold text-accent">
                  {profile.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)}
                </span>
              )}
              <div className="flex flex-col gap-1">
                <input
                  type="file"
                  accept="image/jpeg,image/png"
                  onChange={handleAvatarChange}
                  className="text-sm file:mr-3 file:rounded-md file:border-0 file:bg-primary file:px-3 file:py-1.5 file:text-sm file:text-white"
                />
                <p className="text-xs text-muted-foreground">JPEG или PNG, до 5 МБ</p>
              </div>
            </div>
          )}
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
          <div className="space-y-4">
            <div>
              <Label htmlFor="cyclicalCommission">Цикловая комиссия</Label>
              <Input id="cyclicalCommission" value={cyclicalCommission} onChange={e => setCyclicalCommission(e.target.value)} />
            </div>
            <div>
              <Label htmlFor="category">Категория</Label>
              <select
                id="category"
                value={category}
                onChange={e => setCategory(e.target.value as TeacherCategory | "")}
                className="w-full rounded-md border border-input bg-card px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
              >
                <option value="">Не выбрана</option>
                <option value="None">Без категории</option>
                <option value="First">Первая</option>
                <option value="Higher">Высшая</option>
              </select>
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

      <Button
        onClick={handleSave}
        disabled={saving}
        variant="outline"
        className="mt-4 w-full"
      >
        <Save size={16} className="mr-2" />
        {saving ? "Сохранение..." : "Сохранить изменения"}
      </Button>
    </div>
  )
}
