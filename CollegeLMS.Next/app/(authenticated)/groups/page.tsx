"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, GroupResponse, CreateGroupRequest } from "@/types"
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

export default function GroupsPage() {
  const { user, token, isLoading: authLoading } = useAuth()

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formName, setFormName] = useState("")
  const [formCourse, setFormCourse] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string>>({})
  const [formSubmitting, setFormSubmitting] = useState(false)

  const isAdmin = user?.role === "Admin"

  const fetchGroups = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<GroupResponse[]>>("/api/groups")
      const body = res.data
      if (body.isSuccess && body.data) {
        setGroups(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки групп")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchGroups()
    }
  }, [token, fetchGroups])

  const resetForm = () => {
    setFormName("")
    setFormCourse("")
    setFormError(null)
    setFormFieldErrors({})
    setShowCreate(false)
    setEditingId(null)
  }

  const validate = (): Record<string, string> => {
    const errors: Record<string, string> = {}
    if (!formName.trim()) errors.name = "Название группы обязательно"
    else if (formName.trim().length > 100) errors.name = "Название группы не должно превышать 100 символов"
    const course = Number(formCourse)
    if (!formCourse) errors.course = "Курс обязателен"
    else if (!Number.isInteger(course) || course < 1 || course > 4) errors.course = "Курс должен быть от 1 до 4"
    return errors
  }

  const applyServerErrors = (err: unknown) => {
    const parsed = parseErrors(err)
    setFormError(parsed.message)
    setFormFieldErrors(
      Object.fromEntries(Object.entries(parsed.fieldErrors).map(([k, v]) => [k, v[0] ?? ""])),
    )
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
      const body: CreateGroupRequest = { name: formName.trim(), course: Number(formCourse) }
      const res = await api.post<Result<GroupResponse>>("/api/groups", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchGroups()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch (err) {
      applyServerErrors(err)
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (g: GroupResponse) => {
    setEditingId(g.id)
    setFormName(g.name)
    setFormCourse(String(g.course))
    setFormError(null)
    setFormFieldErrors({})
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    const fieldErrors = validate()
    if (Object.keys(fieldErrors).length > 0) {
      setFormFieldErrors(fieldErrors)
      return
    }
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateGroupRequest = { name: formName.trim(), course: Number(formCourse) }
      const res = await api.put<Result<GroupResponse>>(`/api/groups/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchGroups()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch (err) {
      applyServerErrors(err)
    } finally {
      setFormSubmitting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Группы</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={setShowCreate}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Создать группу</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                {formError && <ErrorBanner message={formError} />}
                <FormField id="create-name" label="Название группы" required error={formFieldErrors.name}>
                  <Input id="create-name" value={formName} onChange={e => setFormName(e.target.value)} />
                </FormField>
                <FormField id="create-course" label="Курс" required error={formFieldErrors.course}>
                  <Input id="create-course" type="number" min="1" max="4" value={formCourse} onChange={e => setFormCourse(e.target.value)} />
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
      ) : groups.length === 0 ? (
        <p className="text-muted-foreground">Нет групп</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Курс</TableHead>
                <TableHead>Студентов</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {groups.map(g => (
                <TableRow key={g.id}>
                  <TableCell className="font-medium">{g.name}</TableCell>
                  <TableCell>{g.course}</TableCell>
                  <TableCell>{g.studentCount}</TableCell>
                  {isAdmin && (
                    <TableCell>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button variant="ghost" size="sm" onClick={() => startEdit(g)}>
                              Ред.
                            </Button>
                          </DialogTrigger>
                          <DialogContent>
                            <DialogHeader>
                              <DialogTitle>Редактировать группу</DialogTitle>
                            </DialogHeader>
                            <form onSubmit={handleUpdate} className="flex flex-col gap-4">
                              {formError && <ErrorBanner message={formError} />}
                              <FormField id="edit-name" label="Название группы" required error={formFieldErrors.name}>
                                <Input id="edit-name" value={formName} onChange={e => setFormName(e.target.value)} />
                              </FormField>
                              <FormField id="edit-course" label="Курс" required error={formFieldErrors.course}>
                                <Input id="edit-course" type="number" min="1" max="4" value={formCourse} onChange={e => setFormCourse(e.target.value)} />
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
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

