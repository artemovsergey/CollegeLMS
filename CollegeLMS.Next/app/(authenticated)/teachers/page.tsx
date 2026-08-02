"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, TeacherResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { parseErrors } from "@/lib/errors"
import ErrorBanner from "@/components/ErrorBanner"
import FormField from "@/components/FormField"
import LoadingSpinner from "@/components/LoadingSpinner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string>>({})
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
    setFormFieldErrors({})
    setShowCreate(false)
    setEditingId(null)
  }

  const validate = (): Record<string, string> => {
    const errors: Record<string, string> = {}
    if (!formEmail.trim()) errors.email = "Email обязателен"
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formEmail.trim())) errors.email = "Некорректный email"
    if (!formPassword) errors.password = "Пароль обязателен"
    else if (formPassword.length < 6) errors.password = "Пароль должен содержать минимум 6 символов"
    if (!formFullName.trim()) errors.fullName = "ФИО обязательно"
    else if (formFullName.trim().length > 200) errors.fullName = "ФИО не должно превышать 200 символов"
    if (!formDepartment.trim()) errors.department = "Цикловая комиссия обязательна"
    else if (formDepartment.trim().length > 200) errors.department = "Цикловая комиссия не должна превышать 200 символов"
    if (!formPosition.trim()) errors.position = "Должность обязательна"
    else if (formPosition.trim().length > 200) errors.position = "Должность не должна превышать 200 символов"
    return errors
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    const fieldErrors = validate()
    if (Object.keys(fieldErrors).length > 0) {
      setFormFieldErrors(fieldErrors)
      return
    }
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body = {
        email: formEmail.trim(),
        password: formPassword,
        fullName: formFullName.trim(),
        department: formDepartment.trim(),
        position: formPosition.trim(),
      }
      const res = await api.post<Result<TeacherResponse>>("/api/teachers", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchTeachers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch (err) {
      const parsed = parseErrors(err)
      setFormError(parsed.message)
      setFormFieldErrors(
        Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0] ?? ""])),
      )
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
                <FormField id="create-email" label="Email" required error={formFieldErrors.email}>
                  <Input id="create-email" type="email" value={formEmail} onChange={e => setFormEmail(e.target.value)} />
                </FormField>
                <FormField id="create-password" label="Пароль" required error={formFieldErrors.password} hint="Минимум 6 символов">
                  <Input id="create-password" type="password" value={formPassword} onChange={e => setFormPassword(e.target.value)} />
                </FormField>
                <FormField id="create-name" label="ФИО" required error={formFieldErrors.fullName}>
                  <Input id="create-name" value={formFullName} onChange={e => setFormFullName(e.target.value)} />
                </FormField>
                <FormField id="create-department" label="Цикловая комиссия" required error={formFieldErrors.department}>
                  <Input id="create-department" value={formDepartment} onChange={e => setFormDepartment(e.target.value)} />
                </FormField>
                <FormField id="create-position" label="Должность" required error={formFieldErrors.position}>
                  <Input id="create-position" value={formPosition} onChange={e => setFormPosition(e.target.value)} />
                </FormField>
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
        <LoadingSpinner size="lg" className="py-20" />
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
                  <TableCell>{t.cyclicalCommission}</TableCell>
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

