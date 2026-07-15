"use client"

import { useEffect, useState, useCallback } from "react"
import type { Result, StudentResponse, GroupResponse } from "@/types"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

export default function StudentsPage() {
  const { user, token, isLoading: authLoading } = useAuth()

  const [students, setStudents] = useState<StudentResponse[]>([])
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [filterGroupId, setFilterGroupId] = useState("all")

  const [showCreate, setShowCreate] = useState(false)

  const [formEmail, setFormEmail] = useState("")
  const [formPassword, setFormPassword] = useState("")
  const [formFullName, setFormFullName] = useState("")
  const [formGroupId, setFormGroupId] = useState("")
  const [formRecordBook, setFormRecordBook] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const isAdmin = user?.role === "Admin"

  const fetchStudents = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const url = filterGroupId && filterGroupId !== "all"
        ? `/api/students?groupId=${filterGroupId}`
        : "/api/students"
      const res = await api.get<Result<StudentResponse[]>>(url)
      const body = res.data
      if (body.isSuccess && body.data) {
        setStudents(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки студентов")
    } finally {
      setLoading(false)
    }
  }, [filterGroupId])

  const fetchGroups = useCallback(async () => {
    try {
      const res = await api.get<Result<GroupResponse[]>>("/api/groups")
      const body = res.data
      if (body.isSuccess && body.data) {
        setGroups(body.data)
      }
    } catch {
      // silently ignore
    }
  }, [])

  useEffect(() => {
    if (token) {
      fetchStudents()
      fetchGroups()
    }
  }, [token, fetchStudents, fetchGroups])

  const resetForm = () => {
    setFormEmail("")
    setFormPassword("")
    setFormFullName("")
    setFormGroupId("")
    setFormRecordBook("")
    setFormError(null)
    setShowCreate(false)
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
        groupId: formGroupId,
        recordBookNumber: formRecordBook,
      }
      const res = await api.post<Result<StudentResponse>>("/api/students", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchStudents()
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания студента")
    } finally {
      setFormSubmitting(false)
    }
  }

  const filteredStudents = filterGroupId && filterGroupId !== "all"
    ? students.filter(s => s.groupId === filterGroupId)
    : students

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">

      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Студенты</h2>
        <div className="flex items-center gap-3">
          <div className="flex flex-col gap-1">
            <Label htmlFor="filter-group" className="text-xs">Фильтр по группе</Label>
            <Select value={filterGroupId} onValueChange={setFilterGroupId}>
              <SelectTrigger id="filter-group" className="w-48 h-8"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Все группы</SelectItem>
                {groups.map(g => (
                  <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {isAdmin && (
            <Dialog open={showCreate} onOpenChange={setShowCreate}>
              <DialogTrigger asChild>
                <Button size="sm">+ Создать</Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Создать студента</DialogTitle>
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
                    <Label htmlFor="create-group">Группа</Label>
                    <Select value={formGroupId} onValueChange={setFormGroupId}>
                      <SelectTrigger id="create-group"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {groups.map(g => (
                          <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="create-record">Номер зачётки</Label>
                    <Input id="create-record" required value={formRecordBook} onChange={e => setFormRecordBook(e.target.value)} />
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
      </div>

      {error && <ErrorBanner message={error} />}

      {loading ? (
        <LoadingSpinner className="py-16" />
      ) : filteredStudents.length === 0 ? (
        <p className="text-muted-foreground">Нет студентов</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ФИО</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Группа</TableHead>
                <TableHead>Номер зачётки</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredStudents.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.fullName}</TableCell>
                  <TableCell>{s.email}</TableCell>
                  <TableCell>{s.groupName}</TableCell>
                  <TableCell>{s.recordBookNumber}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

