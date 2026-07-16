"use client"

import { useEffect, useState, useCallback } from "react"
import type {
  Result,
  StipendListResponse,
  StipendListItemResponse,
  GenerateStipendRequest,
  SemesterResponse,
} from "@/types"
import api from "@/lib/api"
import { useAuth } from "@/lib/auth"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
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
import EmptyState from "@/components/EmptyState"
import { toast } from "sonner"

const STIPEND_TYPE_LABELS: Record<string, string> = {
  Academic: "Академическая",
  Social: "Социальная",
}

export default function StipendsPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === "Admin"

  const [stipends, setStipends] = useState<StipendListResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [showGenerate, setShowGenerate] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const [semesters, setSemesters] = useState<SemesterResponse[]>([])
  const [genSemesterId, setGenSemesterId] = useState("")
  const [genType, setGenType] = useState("Academic")
  const [genMinScore, setGenMinScore] = useState("")
  const [genError, setGenError] = useState<string | null>(null)
  const [genSubmitting, setGenSubmitting] = useState(false)

  const [detailsId, setDetailsId] = useState<string | null>(null)
  const [details, setDetails] = useState<StipendListItemResponse[]>([])
  const [detailsLoading, setDetailsLoading] = useState(false)

  const fetchStipends = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.get<Result<StipendListResponse[]>>("/api/stipends")
      const body = res.data
      if (body.isSuccess && body.data) {
        setStipends(body.data)
      } else {
        setError(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      setError("Ошибка загрузки стипендий")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchStipends()
  }, [fetchStipends])

  useEffect(() => {
    api
      .get<Result<SemesterResponse[]>>("/api/semesters")
      .then(res => {
        if (res.data.isSuccess && res.data.data) setSemesters(res.data.data)
      })
      .catch(() => {})
  }, [])

  const resetGenerateForm = () => {
    setGenSemesterId("")
    setGenType("Academic")
    setGenMinScore("")
    setGenError(null)
    setShowGenerate(false)
  }

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault()
    setGenError(null)
    setGenSubmitting(true)
    try {
      const body: GenerateStipendRequest = {
        semesterId: genSemesterId,
        type: genType,
        minScore: Number(genMinScore),
      }
      const res = await api.post<Result<StipendListResponse>>("/api/stipends", body)
      if (res.data.isSuccess) {
        resetGenerateForm()
        await fetchStipends()
        toast("Стипендия сформирована")
      } else {
        setGenError(res.data.errorMessage ?? "Ошибка формирования")
      }
    } catch {
      setGenError("Ошибка формирования стипендии")
    } finally {
      setGenSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteConfirmId) return
    try {
      await api.delete(`/api/stipends/${deleteConfirmId}`)
      setDeleteConfirmId(null)
      await fetchStipends()
      toast("Стипендия удалена")
    } catch {
      setError("Ошибка удаления")
    }
  }

  const handleViewDetails = async (id: string) => {
    setDetailsId(id)
    setDetailsLoading(true)
    setDetails([])
    try {
      const res = await api.get<Result<StipendListItemResponse[]>>(`/api/stipends/${id}`)
      const body = res.data
      if (body.isSuccess && body.data) {
        setDetails(body.data)
      } else {
        toast.error(body.errorMessage ?? "Ошибка загрузки")
      }
    } catch {
      toast.error("Ошибка загрузки деталей")
    } finally {
      setDetailsLoading(false)
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Стипендии</h2>
        {isAdmin && (
          <Dialog open={showGenerate} onOpenChange={open => { if (open) resetGenerateForm(); setShowGenerate(open) }}>
            <DialogTrigger asChild>
              <Button size="sm">+ Сформировать</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Сформировать стипендию</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleGenerate} className="flex flex-col gap-4">
                {genError && <ErrorBanner message={genError} />}
                <div className="flex flex-col gap-2">
                  <Label htmlFor="gen-semester">Семестр</Label>
                  <Select value={genSemesterId} onValueChange={setGenSemesterId}>
                    <SelectTrigger id="gen-semester">
                      <SelectValue placeholder="Выберите семестр" />
                    </SelectTrigger>
                    <SelectContent>
                      {semesters.map(s => (
                        <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="gen-type">Тип</Label>
                  <Select value={genType} onValueChange={setGenType}>
                    <SelectTrigger id="gen-type"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {Object.entries(STIPEND_TYPE_LABELS).map(([key, label]) => (
                        <SelectItem key={key} value={key}>{label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="gen-minScore">Минимальный балл</Label>
                  <Input
                    id="gen-minScore"
                    type="number"
                    min={0}
                    max={100}
                    required
                    value={genMinScore}
                    onChange={e => setGenMinScore(e.target.value)}
                  />
                </div>
                <div className="flex gap-2 justify-end pt-2">
                  <Button type="button" variant="ghost" onClick={resetGenerateForm}>Отмена</Button>
                  <Button type="submit" disabled={genSubmitting || !genSemesterId}>
                    {genSubmitting ? "Формирование..." : "Сформировать"}
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
      ) : stipends.length === 0 ? (
        <EmptyState message="Стипендии не найдены" />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Название</TableHead>
                <TableHead>Семестр</TableHead>
                <TableHead>Студентов</TableHead>
                <TableHead>Создан</TableHead>
                {isAdmin && <TableHead>Действия</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {stipends.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{s.title}</TableCell>
                  <TableCell>{s.semesterName}</TableCell>
                  <TableCell>{s.studentCount}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {new Date(s.createdAt).toLocaleDateString("ru-RU")}
                  </TableCell>
                  {isAdmin && (
                    <TableCell>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button variant="ghost" size="sm" onClick={() => handleViewDetails(s.id)}>
                              Детали
                            </Button>
                          </DialogTrigger>
                          <DialogContent className="max-w-3xl">
                            <DialogHeader>
                              <DialogTitle>{s.title} — детали</DialogTitle>
                            </DialogHeader>
                            {detailsLoading ? (
                              <LoadingSpinner className="py-8" />
                            ) : details.length === 0 ? (
                              <EmptyState message="Нет данных" />
                            ) : (
                              <Table>
                                <TableHeader>
                                  <TableRow>
                                    <TableHead>Студент</TableHead>
                                    <TableHead>Группа</TableHead>
                                    <TableHead>Сумма</TableHead>
                                    <TableHead>Тип</TableHead>
                                    <TableHead>Комментарий</TableHead>
                                  </TableRow>
                                </TableHeader>
                                <TableBody>
                                  {details.map(d => (
                                    <TableRow key={d.id}>
                                      <TableCell className="font-medium">{d.studentName}</TableCell>
                                      <TableCell>{d.groupName}</TableCell>
                                      <TableCell>{d.amount.toLocaleString("ru-RU")} ₽</TableCell>
                                      <TableCell>{STIPEND_TYPE_LABELS[d.type] ?? d.type}</TableCell>
                                      <TableCell className="text-sm text-muted-foreground max-w-xs truncate">
                                        {d.comment ?? "—"}
                                      </TableCell>
                                    </TableRow>
                                  ))}
                                </TableBody>
                              </Table>
                            )}
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
            <AlertDialogTitle>Удалить стипендию?</AlertDialogTitle>
            <AlertDialogDescription>
              Это действие нельзя отменить.
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
