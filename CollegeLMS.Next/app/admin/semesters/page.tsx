"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, SemesterResponse, CreateSemesterRequest, UpdateSemesterRequest } from "@/types"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { toast } from "sonner"

const typeLabels: Record<string, string> = {
  Autumn: "Осенний",
  Spring: "Весенний",
}

const typeVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Autumn: "default",
  Spring: "secondary",
}

export default function SemestersPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [semesters, setSemesters] = useState<SemesterResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formName, setFormName] = useState("")
  const [formStartDate, setFormStartDate] = useState("")
  const [formEndDate, setFormEndDate] = useState("")
  const [formType, setFormType] = useState("Autumn")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const fetchSemesters = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<SemesterResponse[]>>("/api/semesters")
      const body = res.data
      if (body.isSuccess && body.data) {
        setSemesters(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки семестров")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchSemesters()
  }, [fetchSemesters])

  const resetForm = () => {
    setFormName("")
    setFormStartDate("")
    setFormEndDate("")
    setFormType("Autumn")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateSemesterRequest = {
        name: formName,
        startDate: formStartDate,
        endDate: formEndDate,
        type: formType,
      }
      const res = await api.post<Result<SemesterResponse>>("/api/semesters", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSemesters()
        toast.success("Семестр создан")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания семестра")
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (s: SemesterResponse) => {
    setEditingId(s.id)
    setFormName(s.name)
    setFormStartDate(s.startDate.slice(0, 10))
    setFormEndDate(s.endDate.slice(0, 10))
    setFormType(s.type)
    setFormError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateSemesterRequest = {
        name: formName,
        startDate: formStartDate,
        endDate: formEndDate,
        type: formType,
      }
      const res = await api.put<Result<SemesterResponse>>(`/api/semesters/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchSemesters()
        toast.success("Семестр обновлён")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления семестра")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    try {
      const res = await api.delete(`/api/semesters/${deleteConfirmId}`)
      if (res.data?.isSuccess !== false) {
        await fetchSemesters()
        toast.success("Семестр удалён")
      }
    } catch {
      toast.error("Ошибка удаления семестра")
    } finally {
      setDeleteConfirmId(null)
    }
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString("ru-RU")
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Семестры</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={setShowCreate}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Создать семестр</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                {formError && <ErrorBanner message={formError} />}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-name">Название</Label>
                  <Input id="create-name" required value={formName} onChange={e => setFormName(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-start">Начало</Label>
                  <Input id="create-start" type="date" required value={formStartDate} onChange={e => setFormStartDate(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-end">Конец</Label>
                  <Input id="create-end" type="date" required value={formEndDate} onChange={e => setFormEndDate(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-type">Тип</Label>
                  <Select value={formType} onValueChange={setFormType}>
                    <SelectTrigger id="create-type"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(typeLabels).map(([key, label]) => (
                        <SelectItem key={key} value={key}>{label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
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
      ) : semesters.length === 0 ? (
        <p className="text-muted-foreground">Нет семестров</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Начало</TableHead>
                <TableHead>Конец</TableHead>
                <TableHead>Тип</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {semesters.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.name}</TableCell>
                  <TableCell>{formatDate(s.startDate)}</TableCell>
                  <TableCell>{formatDate(s.endDate)}</TableCell>
                  <TableCell>
                    <Badge variant={typeVariants[s.type] ?? "secondary"}>
                      {typeLabels[s.type] ?? s.type}
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
                              <DialogTitle>Редактировать семестр</DialogTitle>
                            </DialogHeader>
                            <form onSubmit={handleUpdate} className="flex flex-col gap-4">
                              {formError && <ErrorBanner message={formError} />}
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-name">Название</Label>
                                <Input id="edit-name" required value={formName} onChange={e => setFormName(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-start">Начало</Label>
                                <Input id="edit-start" type="date" required value={formStartDate} onChange={e => setFormStartDate(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-end">Конец</Label>
                                <Input id="edit-end" type="date" required value={formEndDate} onChange={e => setFormEndDate(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-type">Тип</Label>
                                <Select value={formType} onValueChange={setFormType}>
                                  <SelectTrigger id="edit-type"><SelectValue /></SelectTrigger>
                                  <SelectContent>
                                    {Object.entries(typeLabels).map(([key, label]) => (
                                      <SelectItem key={key} value={key}>{label}</SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
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
                        <Button variant="ghost" size="sm" onClick={() => setDeleteConfirmId(s.id)} className="text-destructive hover:text-destructive">
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

      <AlertDialog open={!!deleteConfirmId} onOpenChange={(o) => !o && setDeleteConfirmId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить семестр?</AlertDialogTitle>
            <AlertDialogDescription>
              Семестр будет удалён. Это действие необратимо.
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
