"use client"

import { useEffect, useState, useCallback } from "react"
import type {
  Result,
  ExamResponse,
  CreateExamRequest,
  UpdateExamRequest,
  RetakeResponse,
  CreateRetakeRequest,
  UpdateRetakeStatusRequest,
  StudentResponse,
  GroupResponse,
  TeacherResponse,
  SemesterResponse,
} from "@/types"
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
  Exam: "Экзамен",
  Credit: "Зачёт",
}

const statusLabels: Record<string, string> = {
  Scheduled: "Запланирован",
  Completed: "Проведён",
}

const statusVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Scheduled: "outline",
  Completed: "default",
}

const retakeStatusLabels: Record<string, string> = {
  Scheduled: "Запланирована",
  Passed: "Сдана",
  Failed: "Провалена",
}

const retakeStatusVariants: Record<string, "default" | "secondary" | "outline" | "destructive"> = {
  Scheduled: "outline",
  Passed: "default",
  Failed: "destructive",
}

export default function ExamsPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [exams, setExams] = useState<ExamResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formSubject, setFormSubject] = useState("")
  const [formGroupId, setFormGroupId] = useState("")
  const [formExamDate, setFormExamDate] = useState("")
  const [formType, setFormType] = useState("Exam")
  const [formTeacherId, setFormTeacherId] = useState("")
  const [formSemesterId, setFormSemesterId] = useState("")
  const [formStatus, setFormStatus] = useState("Scheduled")
  const [formError, setFormError] = useState<string | null>(null)
  const [formSubmitting, setFormSubmitting] = useState(false)

  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [semesters, setSemesters] = useState<SemesterResponse[]>([])

  const [selectedExamId, setSelectedExamId] = useState<string | null>(null)
  const [retakes, setRetakes] = useState<RetakeResponse[]>([])
  const [retakesLoading, setRetakesLoading] = useState(false)
  const [showRetakesDialog, setShowRetakesDialog] = useState(false)

  const [retakeStudentId, setRetakeStudentId] = useState("")
  const [retakeDate, setRetakeDate] = useState("")
  const [retakeFormSubmitting, setRetakeFormSubmitting] = useState(false)
  const [retakeFormError, setRetakeFormError] = useState<string | null>(null)
  const [showCreateRetake, setShowCreateRetake] = useState(false)

  const [students, setStudents] = useState<StudentResponse[]>([])

  const fetchExams = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<ExamResponse[]>>("/api/exams")
      const body = res.data
      if (body.isSuccess && body.data) {
        setExams(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки экзаменов")
    } finally {
      setLoading(false)
    }
  }, [])

  const fetchGroups = useCallback(async () => {
    try {
      const res = await api.get<Result<GroupResponse[]>>("/api/groups")
      const body = res.data
      if (body.isSuccess && body.data) {
        setGroups(body.data)
      }
    } catch {
      /* ignore */
    }
  }, [])

  const fetchTeachers = useCallback(async () => {
    try {
      const res = await api.get<Result<TeacherResponse[]>>("/api/teachers")
      const body = res.data
      if (body.isSuccess && body.data) {
        setTeachers(body.data)
      }
    } catch {
      /* ignore */
    }
  }, [])

  const fetchSemesters = useCallback(async () => {
    try {
      const res = await api.get<Result<SemesterResponse[]>>("/api/semesters")
      const body = res.data
      if (body.isSuccess && body.data) {
        setSemesters(body.data)
      }
    } catch {
      /* ignore */
    }
  }, [])

  const fetchStudents = useCallback(async () => {
    try {
      const res = await api.get<Result<StudentResponse[]>>("/api/students")
      const body = res.data
      if (body.isSuccess && body.data) {
        setStudents(body.data)
      }
    } catch {
      /* ignore */
    }
  }, [])

  useEffect(() => {
    fetchExams()
    fetchGroups()
    fetchTeachers()
    fetchSemesters()
    fetchStudents()
  }, [fetchExams, fetchGroups, fetchTeachers, fetchSemesters, fetchStudents])

  const resetForm = () => {
    setFormSubject("")
    setFormGroupId("")
    setFormExamDate("")
    setFormType("Exam")
    setFormTeacherId("")
    setFormSemesterId("")
    setFormStatus("Scheduled")
    setFormError(null)
    setShowCreate(false)
    setEditingId(null)
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: CreateExamRequest = {
        subject: formSubject,
        groupId: formGroupId,
        examDate: formExamDate,
        type: formType,
        teacherId: formTeacherId,
        semesterId: formSemesterId,
      }
      const res = await api.post<Result<ExamResponse>>("/api/exams", body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchExams()
        toast.success("Экзамен создан")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка создания")
      }
    } catch {
      setFormError("Ошибка создания экзамена")
    } finally {
      setFormSubmitting(false)
    }
  }

  const startEdit = (e: ExamResponse) => {
    setEditingId(e.id)
    setFormSubject(e.subject)
    setFormGroupId(e.groupId)
    setFormExamDate(e.examDate.slice(0, 10))
    setFormType(e.type)
    setFormTeacherId(e.teacherId)
    setFormSemesterId(e.semesterId)
    setFormStatus(e.status)
    setFormError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingId) return
    setFormError(null)
    setFormSubmitting(true)
    try {
      const body: UpdateExamRequest = {
        subject: formSubject,
        examDate: formExamDate,
        type: formType,
        teacherId: formTeacherId,
        semesterId: formSemesterId,
        status: formStatus,
      }
      const res = await api.put<Result<ExamResponse>>(`/api/exams/${editingId}`, body)
      if (res.data.isSuccess) {
        resetForm()
        await fetchExams()
        toast.success("Экзамен обновлён")
      } else {
        setFormError(res.data.errorMessage ?? "Ошибка обновления")
      }
    } catch {
      setFormError("Ошибка обновления экзамена")
    } finally {
      setFormSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    try {
      const res = await api.delete(`/api/exams/${deleteConfirmId}`)
      if (res.data?.isSuccess !== false) {
        await fetchExams()
        toast.success("Экзамен удалён")
      }
    } catch {
      toast.error("Ошибка удаления экзамена")
    } finally {
      setDeleteConfirmId(null)
    }
  }

  const openRetakes = async (examId: string) => {
    setSelectedExamId(examId)
    setShowRetakesDialog(true)
    setRetakesLoading(true)
    setShowCreateRetake(false)
    setRetakeFormError(null)
    try {
      const res = await api.get<Result<RetakeResponse[]>>(`/api/exams/${examId}/retakes`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setRetakes(body.data)
      } else {
        setRetakes([])
      }
    } catch {
      setRetakes([])
    } finally {
      setRetakesLoading(false)
    }
  }

  const handleCreateRetake = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedExamId) return
    setRetakeFormError(null)
    setRetakeFormSubmitting(true)
    try {
      const body: CreateRetakeRequest = {
        studentId: retakeStudentId,
        retakeDate: retakeDate,
      }
      const res = await api.post<Result<RetakeResponse>>(`/api/exams/${selectedExamId}/retakes`, body)
      if (res.data.isSuccess) {
        setRetakeStudentId("")
        setRetakeDate("")
        setShowCreateRetake(false)
        setRetakeFormError(null)
        toast.success("Пересдача назначена")
        const refetch = await api.get<Result<RetakeResponse[]>>(`/api/exams/${selectedExamId}/retakes`)
        if (refetch.data.isSuccess && refetch.data.data) {
          setRetakes(refetch.data.data)
        }
      } else {
        setRetakeFormError(res.data.errorMessage ?? "Ошибка создания пересдачи")
      }
    } catch {
      setRetakeFormError("Ошибка создания пересдачи")
    } finally {
      setRetakeFormSubmitting(false)
    }
  }

  const handleUpdateRetakeStatus = async (retakeId: string, status: string) => {
    if (!selectedExamId) return
    try {
      const body: UpdateRetakeStatusRequest = { status }
      const res = await api.patch(`/api/exams/${selectedExamId}/retakes/${retakeId}`, body)
      if (res.data?.isSuccess !== false) {
        toast.success("Статус пересдачи обновлён")
        const refetch = await api.get<Result<RetakeResponse[]>>(`/api/exams/${selectedExamId}/retakes`)
        if (refetch.data.isSuccess && refetch.data.data) {
          setRetakes(refetch.data.data)
        }
      } else {
        toast.error(res.data?.errorMessage ?? "Ошибка обновления статуса")
      }
    } catch {
      toast.error("Ошибка обновления статуса пересдачи")
    }
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString("ru-RU")
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Экзамены</h2>
        {isAdmin && (
          <Dialog open={showCreate} onOpenChange={setShowCreate}>
            <DialogTrigger asChild>
              <Button size="sm">+ Создать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Создать экзамен</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleCreate} className="flex flex-col gap-4">
                {formError && <ErrorBanner message={formError} />}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-subject">Предмет</Label>
                  <Input id="create-subject" required value={formSubject} onChange={e => setFormSubject(e.target.value)} />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-group">Группа</Label>
                  <Select value={formGroupId} onValueChange={setFormGroupId}>
                    <SelectTrigger id="create-group"><SelectValue placeholder="Выберите группу" /></SelectTrigger>
                    <SelectContent>
                      {groups.map(g => (
                        <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-date">Дата</Label>
                  <Input id="create-date" type="date" required value={formExamDate} onChange={e => setFormExamDate(e.target.value)} />
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
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-teacher">Преподаватель</Label>
                  <Select value={formTeacherId} onValueChange={setFormTeacherId}>
                    <SelectTrigger id="create-teacher"><SelectValue placeholder="Выберите преподавателя" /></SelectTrigger>
                    <SelectContent>
                      {teachers.map(t => (
                        <SelectItem key={t.id} value={t.id}>{t.fullName}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="create-semester">Семестр</Label>
                  <Select value={formSemesterId} onValueChange={setFormSemesterId}>
                    <SelectTrigger id="create-semester"><SelectValue placeholder="Выберите семестр" /></SelectTrigger>
                    <SelectContent>
                      {semesters.map(s => (
                        <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>
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
      ) : exams.length === 0 ? (
        <p className="text-muted-foreground">Нет экзаменов</p>
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Предмет</TableHead>
                <TableHead>Группа</TableHead>
                <TableHead>Дата</TableHead>
                <TableHead>Тип</TableHead>
                <TableHead>Преподаватель</TableHead>
                <TableHead>Статус</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {exams.map(e => (
                <TableRow
                  key={e.id}
                  className="cursor-pointer"
                  onClick={() => openRetakes(e.id)}
                >
                  <TableCell className="font-medium">{e.subject}</TableCell>
                  <TableCell>{e.groupName}</TableCell>
                  <TableCell>{formatDate(e.examDate)}</TableCell>
                  <TableCell>
                    <Badge variant="secondary">{typeLabels[e.type] ?? e.type}</Badge>
                  </TableCell>
                  <TableCell>{e.teacherName}</TableCell>
                  <TableCell>
                    <Badge variant={statusVariants[e.status] ?? "secondary"}>
                      {statusLabels[e.status] ?? e.status}
                    </Badge>
                  </TableCell>
                  {isAdmin && (
                    <TableCell onClick={e => e.stopPropagation()}>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button variant="ghost" size="sm" onClick={() => startEdit(e)}>
                              Ред.
                            </Button>
                          </DialogTrigger>
                          <DialogContent>
                            <DialogHeader>
                              <DialogTitle>Редактировать экзамен</DialogTitle>
                            </DialogHeader>
                            <form onSubmit={handleUpdate} className="flex flex-col gap-4">
                              {formError && <ErrorBanner message={formError} />}
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-subject">Предмет</Label>
                                <Input id="edit-subject" required value={formSubject} onChange={e => setFormSubject(e.target.value)} />
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-date">Дата</Label>
                                <Input id="edit-date" type="date" required value={formExamDate} onChange={e => setFormExamDate(e.target.value)} />
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
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-teacher">Преподаватель</Label>
                                <Select value={formTeacherId} onValueChange={setFormTeacherId}>
                                  <SelectTrigger id="edit-teacher"><SelectValue placeholder="Выберите преподавателя" /></SelectTrigger>
                                  <SelectContent>
                                    {teachers.map(t => (
                                      <SelectItem key={t.id} value={t.id}>{t.fullName}</SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-semester">Семестр</Label>
                                <Select value={formSemesterId} onValueChange={setFormSemesterId}>
                                  <SelectTrigger id="edit-semester"><SelectValue placeholder="Выберите семестр" /></SelectTrigger>
                                  <SelectContent>
                                    {semesters.map(s => (
                                      <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </div>
                              <div className="flex flex-col gap-2">
                                <Label htmlFor="edit-status">Статус</Label>
                                <Select value={formStatus} onValueChange={setFormStatus}>
                                  <SelectTrigger id="edit-status"><SelectValue /></SelectTrigger>
                                  <SelectContent>
                                    {Object.entries(statusLabels).map(([key, label]) => (
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
                        <Button variant="ghost" size="sm" onClick={() => setDeleteConfirmId(e.id)} className="text-destructive hover:text-destructive">
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

      <Dialog open={showRetakesDialog} onOpenChange={setShowRetakesDialog}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Пересдачи</DialogTitle>
          </DialogHeader>
          <div className="flex justify-end">
            <Button size="sm" onClick={() => { setShowCreateRetake(true); setRetakeFormError(null) }}>
              + Назначить пересдачу
            </Button>
          </div>
          {showCreateRetake && (
            <form onSubmit={handleCreateRetake} className="flex flex-col gap-4 border rounded-lg p-4 bg-muted/30">
              {retakeFormError && <ErrorBanner message={retakeFormError} />}
              <div className="flex flex-col gap-2">
                <Label htmlFor="retake-student">Студент</Label>
                <Select value={retakeStudentId} onValueChange={setRetakeStudentId}>
                  <SelectTrigger id="retake-student"><SelectValue placeholder="Выберите студента" /></SelectTrigger>
                  <SelectContent>
                    {students.map(s => (
                      <SelectItem key={s.id} value={s.id}>{s.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="retake-date">Дата пересдачи</Label>
                <Input id="retake-date" type="date" required value={retakeDate} onChange={e => setRetakeDate(e.target.value)} />
              </div>
              <div className="flex gap-2 justify-end pt-2">
                <Button type="button" variant="ghost" onClick={() => { setShowCreateRetake(false); setRetakeFormError(null) }}>Отмена</Button>
                <Button type="submit" disabled={retakeFormSubmitting}>
                  {retakeFormSubmitting ? "Сохранение..." : "Назначить"}
                </Button>
              </div>
            </form>
          )}
          {retakesLoading ? (
            <LoadingSpinner className="py-8" />
          ) : retakes.length === 0 ? (
            <p className="text-muted-foreground py-4 text-center">Нет пересдач</p>
          ) : (
            <div className="rounded-lg border bg-card">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Студент</TableHead>
                    <TableHead>Дата</TableHead>
                    <TableHead>Статус</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {retakes.map(r => (
                    <TableRow key={r.id}>
                      <TableCell>{r.studentName}</TableCell>
                      <TableCell>{formatDate(r.retakeDate)}</TableCell>
                      <TableCell>
                        <Select
                          value={r.status}
                          onValueChange={v => handleUpdateRetakeStatus(r.id, v)}
                        >
                          <SelectTrigger className="w-40 h-8"><SelectValue /></SelectTrigger>
                          <SelectContent>
                            {Object.entries(retakeStatusLabels).map(([key, label]) => (
                              <SelectItem key={key} value={key}>{label}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </DialogContent>
      </Dialog>

      <AlertDialog open={!!deleteConfirmId} onOpenChange={(o) => !o && setDeleteConfirmId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить экзамен?</AlertDialogTitle>
            <AlertDialogDescription>
              Экзамен и все связанные пересдачи будут удалены. Это действие необратимо.
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
