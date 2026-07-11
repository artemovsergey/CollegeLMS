"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, TeacherResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
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

export default function TeachersPage() {
  const { user, token, isLoading: authLoading } = useAuth()

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

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

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
                {formError && <ErrorBanner message={formError} />}
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

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
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

