"use client"

import { useEffect, useState, useCallback, useRef } from "react"
import type { Result, StudentResponse, GroupResponse, TransferRecordResponse } from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { roleLabels, roleVariants } from "@/lib/constants"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"
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
  const { user, token } = useAuth()

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

  const [showTransfer, setShowTransfer] = useState(false)
  const [transferStudentId, setTransferStudentId] = useState<string | null>(null)
  const [transferGroupId, setTransferGroupId] = useState("")
  const [transferReason, setTransferReason] = useState("")
  const [transferSubmitting, setTransferSubmitting] = useState(false)
  const [transferError, setTransferError] = useState<string | null>(null)

  const [showHistory, setShowHistory] = useState(false)
  const [historyStudentName, setHistoryStudentName] = useState("")
  const [historyRecords, setHistoryRecords] = useState<TransferRecordResponse[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState<string | null>(null)

  const [showImport, setShowImport] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [importGroupId, setImportGroupId] = useState("")
  const [importSubmitting, setImportSubmitting] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

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

  const openTransfer = (studentId: string) => {
    setTransferStudentId(studentId)
    setTransferGroupId("")
    setTransferReason("")
    setTransferError(null)
    setShowTransfer(true)
  }

  const handleTransfer = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!transferStudentId) return
    setTransferError(null)
    setTransferSubmitting(true)
    try {
      const res = await api.patch<Result<void>>(`/api/students/${transferStudentId}/transfer`, {
        newGroupId: transferGroupId,
        reason: transferReason,
      })
      if (res.data.isSuccess) {
        toast.success("Студент переведён")
        setShowTransfer(false)
        await fetchStudents()
      } else {
        setTransferError(res.data.errorMessage ?? "Ошибка перевода")
      }
    } catch {
      setTransferError("Ошибка перевода студента")
    } finally {
      setTransferSubmitting(false)
    }
  }

  const openHistory = async (studentId: string, studentName: string) => {
    setHistoryStudentName(studentName)
    setHistoryRecords([])
    setHistoryError(null)
    setShowHistory(true)
    setHistoryLoading(true)
    try {
      const res = await api.get<Result<TransferRecordResponse[]>>(`/api/students/${studentId}/transfers`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setHistoryRecords(body.data)
      } else {
        setHistoryError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setHistoryError("Ошибка загрузки истории переводов")
    } finally {
      setHistoryLoading(false)
    }
  }

  const handleImportFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setImportFile(e.target.files?.[0] ?? null)
  }

  const resetImport = () => {
    setImportFile(null)
    setImportGroupId("")
    setImportError(null)
    if (fileInputRef.current) fileInputRef.current.value = ""
    setShowImport(false)
  }

  const handleImport = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!importFile) return
    setImportError(null)
    setImportSubmitting(true)
    try {
      const formData = new FormData()
      formData.append("file", importFile)
      formData.append("groupId", importGroupId)
      const res = await api.post<Result<void>>("/api/students/import", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      })
      if (res.data.isSuccess) {
        toast.success("Студенты импортированы")
        resetImport()
        await fetchStudents()
      } else {
        setImportError(res.data.errorMessage ?? "Ошибка импорта")
      }
    } catch {
      setImportError("Ошибка импорта CSV")
    } finally {
      setImportSubmitting(false)
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
            <>
              <Dialog open={showImport} onOpenChange={setShowImport}>
                <DialogTrigger asChild>
                  <Button size="sm" variant="outline">Импорт CSV</Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Импорт студентов из CSV</DialogTitle>
                  </DialogHeader>
                  <form onSubmit={handleImport} className="flex flex-col gap-4">
                    {importError && <ErrorBanner message={importError} />}
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="import-file">CSV файл</Label>
                      <Input
                        id="import-file"
                        ref={fileInputRef}
                        type="file"
                        accept=".csv"
                        onChange={handleImportFileChange}
                      />
                      {importFile && (
                        <p className="text-xs text-muted-foreground">{importFile.name}</p>
                      )}
                    </div>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="import-group">Группа</Label>
                      <Select value={importGroupId} onValueChange={setImportGroupId}>
                        <SelectTrigger id="import-group"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {groups.map(g => (
                            <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="flex gap-2 justify-end pt-2">
                      <Button type="button" variant="ghost" onClick={resetImport}>Отмена</Button>
                      <Button type="submit" disabled={importSubmitting || !importFile}>
                        {importSubmitting ? "Загрузка..." : "Импортировать"}
                      </Button>
                    </div>
                  </form>
                </DialogContent>
              </Dialog>
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
            </>
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
                <TableHead className="w-40">Действия</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredStudents.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.fullName}</TableCell>
                  <TableCell>{s.email}</TableCell>
                  <TableCell>{s.groupName}</TableCell>
                  <TableCell>{s.recordBookNumber}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button variant="outline" size="sm" onClick={() => openTransfer(s.id)}>
                        Перевести
                      </Button>
                      <Button variant="outline" size="sm" onClick={() => openHistory(s.id, s.fullName)}>
                        История
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={showTransfer} onOpenChange={setShowTransfer}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Перевод студента</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleTransfer} className="flex flex-col gap-4">
            {transferError && <ErrorBanner message={transferError} />}
            <div className="flex flex-col gap-2">
              <Label htmlFor="transfer-group">Новая группа</Label>
              <Select value={transferGroupId} onValueChange={setTransferGroupId} required>
                <SelectTrigger id="transfer-group"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {groups.map(g => (
                    <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="transfer-reason">Причина перевода</Label>
              <Input id="transfer-reason" value={transferReason} onChange={e => setTransferReason(e.target.value)} />
            </div>
            <div className="flex gap-2 justify-end pt-2">
              <Button type="button" variant="ghost" onClick={() => setShowTransfer(false)}>Отмена</Button>
              <Button type="submit" disabled={transferSubmitting || !transferGroupId}>
                {transferSubmitting ? "Сохранение..." : "Перевести"}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={showHistory} onOpenChange={setShowHistory}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>История переводов — {historyStudentName}</DialogTitle>
          </DialogHeader>
          {historyLoading ? (
            <LoadingSpinner className="py-8" />
          ) : historyError ? (
            <ErrorBanner message={historyError} />
          ) : historyRecords.length === 0 ? (
            <p className="text-muted-foreground text-sm">Нет записей о переводах</p>
          ) : (
            <div className="rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Откуда</TableHead>
                    <TableHead>Куда</TableHead>
                    <TableHead>Причина</TableHead>
                    <TableHead>Дата</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {historyRecords.map(r => (
                    <TableRow key={r.id}>
                      <TableCell>{r.fromGroupName}</TableCell>
                      <TableCell>{r.toGroupName}</TableCell>
                      <TableCell>{r.reason}</TableCell>
                      <TableCell>{new Date(r.transferredAt).toLocaleDateString("ru-RU")}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
