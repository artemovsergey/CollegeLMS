"use client"

import { useEffect, useState, useCallback } from "react"
import type { User, Result, CreateUserRequest, UpdateUserRequest, ChangeRoleRequest } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { Pencil, Ban, ChevronLeft, ChevronRight } from "lucide-react"
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
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

export default function UsersPage() {
  const { user } = useAuth()

  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formLogin, setFormLogin] = useState("")
  const [formEmail, setFormEmail] = useState("")
  const [formPassword, setFormPassword] = useState("")
  const [formFullName, setFormFullName] = useState("")
  const [formRole, setFormRole] = useState("Student")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const pageSize = 20
  const totalPages = Math.ceil(users.length / pageSize)
  const paginatedUsers = users.slice((page - 1) * pageSize, page * pageSize)

  const isAdmin = user?.role === "Admin"

  const fetchUsers = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<User[]>>("/api/users")
      const body = res.data
      if (body.isSuccess && body.data) {
        setUsers(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки пользователей")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  useEffect(() => { setPage(1) }, [users.length])

  const resetForm = () => {
    setFormLogin("")
    setFormEmail("")
    setFormPassword("")
    setFormFullName("")
    setFormRole("Student")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateUserRequest = { login: formLogin, email: formEmail, password: formPassword, fullName: formFullName, role: formRole }
      const res = await api.post<Result<User>>("/api/users", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchUsers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания пользователя")
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (u: User) => {
    setEditingId(u.id)
    setFormLogin(u.login)
    setFormEmail(u.email)
    setFormFullName(u.fullName)
    setFormRole(u.role)
    setFormPassword("")
    setFormError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateUserRequest = { login: formLogin, email: formEmail, fullName: formFullName, role: formRole }
      const res = await api.put<Result<User>>(`/api/users/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchUsers()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления пользователя")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    try {
      await api.delete(`/api/users/${deleteConfirmId}`)
      await fetchUsers()
    } catch {
      setError("Ошибка деактивации")
    } finally {
      setDeleteConfirmId(null)
    }
  }

  const handleChangeRole = async (id: string, role: string) => {
    try {
      const body: ChangeRoleRequest = { role }
      await api.patch(`/api/users/${id}/role`, body)
      await fetchUsers()
    } catch {
      setError("Ошибка смены роли")
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Пользователи</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={setShowCreate}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent className="bg-card">
              <DialogHeader>
                <DialogTitle>Создать пользователя</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                {formError && <ErrorBanner message={formError} />}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-login">Логин</Label>
                  <Input id="create-login" type="text" required value={formLogin} onChange={e => setFormLogin(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-email">Email</Label>
                  <Input id="create-email" type="email" value={formEmail} onChange={e => setFormEmail(e.target.value)} />
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
                  <Label htmlFor="create-role">Роль</Label>
                  <Select value={formRole} onValueChange={setFormRole}>
                    <SelectTrigger id="create-role"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(roleLabels).map(([key, label]) => (
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
      ) : users.length === 0 ? (
        <p className="text-muted-foreground">Нет пользователей</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Логин</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>ФИО</TableHead>
                <TableHead>Роль</TableHead>
                <TableHead>Статус</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedUsers.map(u => (
                <TableRow key={u.id} className={!u.isActive ? "opacity-50" : ""}>
                  <TableCell className="font-medium">{u.login}</TableCell>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>{u.fullName}</TableCell>
                  <TableCell>
                    {isAdmin ? (
                      <Select value={u.role} onValueChange={v => handleChangeRole(u.id, v)}>
                        <SelectTrigger className="w-32 h-8"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {Object.entries(roleLabels).map(([key, label]) => (
                            <SelectItem key={key} value={key}>{label}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    ) : (
                      <Badge variant={roleVariants[u.role] ?? "secondary"}>
                        {roleLabels[u.role] ?? u.role}
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    {u.isActive ? (
                      <span className="text-sm text-green-600">Активен</span>
                    ) : (
                      <span className="text-sm text-muted-foreground">Неактивен</span>
                    )}
                  </TableCell>
                  {isAdmin && (
                    <TableCell>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button variant="ghost" size="sm" onClick={() => startEdit(u)}>
                              <Pencil size={16} />
                            </Button>
                          </DialogTrigger>
            <DialogContent className="bg-card">
                            <DialogHeader>
                              <DialogTitle>Редактировать пользователя</DialogTitle>
                            </DialogHeader>
                            <form onSubmit={handleUpdate} className="flex flex-col gap-4">
                              {formError && <ErrorBanner message={formError} />}
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-login">Логин</Label>
                                <Input id="edit-login" type="text" required value={formLogin} onChange={e => setFormLogin(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-email">Email</Label>
                                <Input id="edit-email" type="email" value={formEmail} onChange={e => setFormEmail(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-name">ФИО</Label>
                                <Input id="edit-name" required value={formFullName} onChange={e => setFormFullName(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-role">Роль</Label>
                                <Select value={formRole} onValueChange={setFormRole}>
                                  <SelectTrigger id="edit-role"><SelectValue /></SelectTrigger>
                                  <SelectContent>
                                    {Object.entries(roleLabels).map(([key, label]) => (
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
                        {u.isActive && (
                          <Button variant="ghost" size="sm" onClick={() => setDeleteConfirmId(u.id)} className="text-destructive hover:text-destructive" aria-label="Деактивировать">
                            <Ban size={16} />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
            <ChevronLeft size={16} />
          </Button>
          <span className="text-sm text-muted-foreground px-2">{page} / {totalPages}</span>
          <Button variant="outline" size="sm" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
            <ChevronRight size={16} />
          </Button>
        </div>
      )}

      <AlertDialog open={!!deleteConfirmId} onOpenChange={(o) => !o && setDeleteConfirmId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Деактивировать пользователя?</AlertDialogTitle>
            <AlertDialogDescription>
              Пользователь потеряет доступ к системе. Это действие можно отменить через редактирование.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Деактивировать
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
