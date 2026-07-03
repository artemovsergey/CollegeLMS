"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, TeacherResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"

const roleVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Admin: "default",
  Teacher: "secondary",
  Student: "secondary",
  Dispatcher: "outline",
}

const roleLabels: Record<string, string> = {
  Admin: "Админ",
  Teacher: "Преподаватель",
  Student: "Студент",
  Dispatcher: "Диспетчер",
}

export default function TeachersPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formEmail, setFormEmail] = useState("")
  const [formPassword, setFormPassword] = useState("")
  const [formFullName, setFormFullName] = useState("")
  const [formDepartment, setFormDepartment] = useState("")
  const [formPosition, setFormPosition] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const isAdmin = user?.role === "Admin"

  const fetchTeachers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<TeacherResponse[]>>("/api/teachers")
      const body = res.data
      if (body.isSuccess && body.data) {
        setTeachers(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки преподавателей")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchTeachers()
    }
  }, [token, fetchTeachers])

  const resetForm = () => {
    setFormEmail("")
    setFormPassword("")
    setFormFullName("")
    setFormDepartment("")
    setFormPosition("")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body = {
        email: formEmail,
        password: formPassword,
        fullName: formFullName,
        department: formDepartment,
        position: formPosition,
      }
      const res = await api.post<Result<TeacherResponse>>("/api/teachers", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchTeachers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания преподавателя")
    } finally {
      setFormSubmitting(false)
    }
  }

  if (authLoading) return <Loading />
  if (!token) return null

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto min-h-screen">
      <header className="flex items-center justify-between py-2">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            CL
          </div>
          <h1 className="text-lg font-semibold">CollegeLMS</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="hidden sm:block text-sm text-muted-foreground">{user?.email}</span>
          <span className="text-sm text-muted-foreground">Преподаватели</span>
          <Button variant="ghost" size="sm" onClick={() => { logout(); router.push("/login") }}>
            Выйти
          </Button>
        </div>
      </header>

      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Преподаватели</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={setShowCreate}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Создать преподавателя</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                {formError && (
                  <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                    {formError}
                  </div>
                )}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-email">Email</Label>
                  <Input id="create-email" type="email" required value={formEmail} onChange={e => setFormEmail(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-password">Пароль</Label>
                  <Input id="create-password" type="password" required value={formPassword} onChange={e => setFormPassword(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-name">ФИО</Label>
                  <Input id="create-name" required value={formFullName} onChange={e => setFormFullName(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-department">Кафедра</Label>
                  <Input id="create-department" required value={formDepartment} onChange={e => setFormDepartment(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-position">Должность</Label>
                  <Input id="create-position" required value={formPosition} onChange={e => setFormPosition(e.target.value)} />
                </div>
                <div className="flex gap-2 justify-end pt-2">
                  <Button type="button" variant="ghost" onClick={resetForm}>Отмена</Button>
                  <Button type="submit" disabled={formSubmitting}>
                    {formSubmitting ? "Сохранение..." : "Сохранить"}
                  </Button>
                </div>
              </form>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <Loading />
      ) : teachers.length === 0 ? (
        <p className="text-muted-foreground">Нет преподавателей</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ФИО</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Кафедра</TableHead>
                <TableHead>Должность</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {teachers.map(t => (
                <TableRow key={t.id}>
                  <TableCell className="font-medium">{t.fullName}</TableCell>
                  <TableCell>{t.email}</TableCell>
                  <TableCell>{t.department}</TableCell>
                  <TableCell>{t.position}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
