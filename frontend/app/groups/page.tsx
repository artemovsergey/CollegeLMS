"use client"

import { useEffect, useState, useCallback } from "react"
import { useRouter } from "next/navigation"
import type { Result, GroupResponse, CreateGroupRequest } from "@/types"
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

export default function GroupsPage() {
  const { user, token, logout, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formName, setFormName] = useState("")
  const [formCourse, setFormCourse] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
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
    if (!authLoading && !token) {
      router.push("/login")
    }
  }, [authLoading, token, router])

  useEffect(() => {
    if (token) {
      fetchGroups()
    }
  }, [token, fetchGroups])

  const resetForm = () => {
    setFormName("")
    setFormCourse("")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateGroupRequest = { name: formName, course: Number(formCourse) }
      const res = await api.post<Result<GroupResponse>>("/api/groups", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchGroups()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания группы")
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (g: GroupResponse) => {
    setEditingId(g.id)
    setFormName(g.name)
    setFormCourse(String(g.course))
    setFormError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateGroupRequest = { name: formName, course: Number(formCourse) }
      const res = await api.put<Result<GroupResponse>>(`/api/groups/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchGroups()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления группы")
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
          <span className="text-sm text-muted-foreground">Группы</span>
          <Button variant="ghost" size="sm" onClick={() => { logout(); router.push("/login") }}>
            Выйти
          </Button>
        </div>
      </header>

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
                {formError && (
                  <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                    {formError}
                  </div>
                )}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-name">Название группы</Label>
                  <Input id="create-name" required value={formName} onChange={e => setFormName(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-course">Курс</Label>
                  <Input id="create-course" type="number" min="1" max="6" required value={formCourse} onChange={e => setFormCourse(e.target.value)} />
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
                              {formError && (
                                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                                  {formError}
                                </div>
                              )}
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-name">Название группы</Label>
                                <Input id="edit-name" required value={formName} onChange={e => setFormName(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-course">Курс</Label>
                                <Input id="edit-course" type="number" min="1" max="6" required value={formCourse} onChange={e => setFormCourse(e.target.value)} />
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

function Loading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
