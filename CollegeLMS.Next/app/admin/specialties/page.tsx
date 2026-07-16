"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, SpecialtyResponse, CreateSpecialtyRequest, UpdateSpecialtyRequest } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
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
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
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
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateSpecialtyRequest = { code: formCode, name: formName, description: formDescription }
      const res = await api.post<Result<SpecialtyResponse>>("/api/specialties", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSpecialties(search)
        toast("Специальность создана")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания специальности")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateSpecialtyRequest = { code: formCode, name: formName, description: formDescription, isActive: formIsActive }
      const res = await api.put<Result<SpecialtyResponse>>(`/api/specialties/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSpecialties(search)
        toast("Специальность обновлена")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления специальности")
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
      <div className="flex flex-col gap-2">
        <Label htmlFor="code">Код</Label>
        <Input id="code" required value={formCode} onChange={e => setFormCode(e.target.value)} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="name">Название</Label>
        <Input id="name" required value={formName} onChange={e => setFormName(e.target.value)} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="description">Описание</Label>
        <Input id="description" value={formDescription} onChange={e => setFormDescription(e.target.value)} />
      </div>
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
        <LoadingSpinner className="py-16" />
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
                          className="text-destructive hover:text-destructive"
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
