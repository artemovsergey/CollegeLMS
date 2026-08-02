"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, SpecialtyResponse, CreateSpecialtyRequest, UpdateSpecialtyRequest } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { parseErrors } from "@/lib/errors"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
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
import ErrorBanner from "@/components/ErrorBanner"
import FormField from "@/components/FormField"
import LoadingSpinner from "@/components/LoadingSpinner"
import EmptyState from "@/components/EmptyState"
import { toast } from "sonner"

export default function SpecialtiesPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [specialties, setSpecialties] = useState<SpecialtyResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState("")

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const [formCode, setFormCode] = useState("")
  const [formName, setFormName] = useState("")
  const [formDescription, setFormDescription] = useState("")
  const [formIsActive, setFormIsActive] = useState(true)
  const [formError, setFormError] = useState<string | null>(null)
  const [formFieldErrors, setFormFieldErrors] = useState<Record<string, string>>({})
  const [formSubmitting, setFormSubmitting] = useState(false)

  const fetchSpecialties = useCallback(async (searchTerm?: string) => {
    setLoading(true)
    setError(null)
    try {
      const query = searchTerm ? `?search=${encodeURIComponent(searchTerm)}` : ""
      const res = await api.get<Result<SpecialtyResponse[]>>(`/api/specialties${query}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setSpecialties(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки специальностей")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timer = setTimeout(() => {
      fetchSpecialties(search)
    }, 300)
    return () => clearTimeout(timer)
  }, [search, fetchSpecialties])

  const resetForm = () => {
    setFormCode("")
    setFormName("")
    setFormDescription("")
    setFormIsActive(true)
    setFormError(null)
    setFormFieldErrors({})
    setShowCreate(false)
    setEditingId(null)
  }

  const startEdit = (s: SpecialtyResponse) => {
    setEditingId(s.id)
    setFormCode(s.code)
    setFormName(s.name)
    setFormDescription(s.description)
    setFormIsActive(s.isActive)
    setFormError(null)
    setFormFieldErrors({})
  }

  const validate = (): Record<string, string> => {
    const errors: Record<string, string> = {}
    if (!formCode.trim()) errors.code = "Код специальности обязателен"
    else if (formCode.trim().length > 50) errors.code = "Код не должен превышать 50 символов"
    if (!formName.trim()) errors.name = "Название специальности обязательно"
    else if (formName.trim().length > 255) errors.name = "Название не должно превышать 255 символов"
    if (formDescription.trim().length > 4000) errors.description = "Описание не должно превышать 4000 символов"
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
      const body: CreateSpecialtyRequest = { code: formCode.trim(), name: formName.trim(), description: formDescription.trim() }
      const res = await api.post<Result<SpecialtyResponse>>("/api/specialties", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSpecialties(search)
        toast("Специальность создана")
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
      const body: UpdateSpecialtyRequest = { code: formCode.trim(), name: formName.trim(), description: formDescription.trim(), isActive: formIsActive }
      const res = await api.put<Result<SpecialtyResponse>>(`/api/specialties/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSpecialties(search)
        toast("Специальность обновлена")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
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

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    try {
      await api.delete(`/api/specialties/${deleteConfirmId}`)
      setDeleteConfirmId(null)
      await fetchSpecialties(search)
      toast("Специальность удалена")
    } catch (err: unknown) {
      setDeleteConfirmId(null)
      if (err && typeof err === "object" && "response" in err) {
        const axiosErr = err as { response?: { status?: number; data?: { errorMessage?: string } } }
        if (axiosErr.response?.status === 409) {
          toast.error(axiosErr.response.data?.errorMessage ?? "Специальность имеет связанные данные")
        } else {
          setError("Ошибка удаления")
        }
      } else {
        setError("Ошибка удаления")
      }
    }
  }

  const formContent = (
    <form onSubmit={editingId ? handleUpdate : handleCreate} className="flex flex-col gap-4">
      {formError && <ErrorBanner message={formError} />}
      <FormField id="code" label="Код" required error={formFieldErrors.code}>
        <Input id="code" value={formCode} onChange={e => setFormCode(e.target.value)} />
      </FormField>
      <FormField id="name" label="Название" required error={formFieldErrors.name}>
        <Input id="name" value={formName} onChange={e => setFormName(e.target.value)} />
      </FormField>
      <FormField id="description" label="Описание" error={formFieldErrors.description}>
        <Input id="description" value={formDescription} onChange={e => setFormDescription(e.target.value)} />
      </FormField>
      {editingId && (
        <div className="flex items-center gap-2">
          <input
            id="isActive"
            type="checkbox"
            checked={formIsActive}
            onChange={e => setFormIsActive(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300"
          />
          <Label htmlFor="isActive" className="text-sm">Активна</Label>
        </div>
      )}
      <div className="flex gap-2 justify-end pt-2">
        <Button type="button" variant="ghost" onClick={resetForm}>Отмена</Button>
        <Button type="submit" disabled={formSubmitting}>
          {formSubmitting ? "Сохранение..." : editingId ? "Сохранить" : "Создать"}
        </Button>
      </div>
    </form>
  )

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Специальности</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={open => { if (open) resetForm(); setShowCreate(open) }}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Создать специальность</DialogTitle>
              </DialogHeader>
              {formContent}
            </DialogContent>
          </Dialog>
        )}
      </div>

      <Input
        placeholder="Поиск по коду или названию..."
        value={search}
        onChange={e => setSearch(e.target.value)}
      />

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner size="lg" className="py-20" />
      ) : specialties.length === 0 ? (
        <EmptyState message="Специальности не найдены" />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Код</TableHead>
                <TableHead>Название</TableHead>
                <TableHead>Описание</TableHead>
                <TableHead>Статус</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {specialties.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-mono text-sm">{s.code}</TableCell>
                  <TableCell className="font-medium">{s.name}</TableCell>
                  <TableCell className="text-sm text-muted-foreground max-w-xs truncate">
                    {s.description || "—"}
                  </TableCell>
                  <TableCell>
                    <Badge variant={s.isActive ? "default" : "secondary"}>
                      {s.isActive ? "Активна" : "Неактивна"}
                    </Badge>
                  </TableCell>
                  {isAdmin && (
                    <TableCell>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button variant="ghost" size="sm" onClick={() => startEdit(s)}>
                              Ред.
                            </Button>
                          </DialogTrigger>
                          <DialogContent>
                            <DialogHeader>
                              <DialogTitle>Редактировать специальность</DialogTitle>
                            </DialogHeader>
                            {formContent}
                          </DialogContent>
                        </Dialog>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-muted-foreground hover:text-fg"
                          onClick={() => setDeleteConfirmId(s.id)}
                        >
                          Удал.
                        </Button>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <AlertDialog open={!!deleteConfirmId} onOpenChange={o => { if (!o) setDeleteConfirmId(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить специальность?</AlertDialogTitle>
            <AlertDialogDescription>
              Это действие нельзя отменить. Если у специальности есть связанные данные, удаление будет отклонено.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Удалить
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
