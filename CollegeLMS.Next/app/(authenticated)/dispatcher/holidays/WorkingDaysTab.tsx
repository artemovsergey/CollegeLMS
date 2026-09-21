"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import { Filter, Inbox, Pencil, Plus, SearchX, Trash2 } from "lucide-react"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
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
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import Pagination from "@/components/ui/pagination"
import ErrorBanner from "@/components/ErrorBanner"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  createWorkingDay,
  deleteWorkingDay,
  fetchWorkingDays,
  updateWorkingDay,
  type WorkingDay,
} from "@/api/workingDays"
import { extractErrorMessage } from "@/lib/utils"
import {
  DAY_OF_WEEK_LABELS,
  formatDate,
  formatDateRange,
  substituteDayLabel,
  toDateInput,
} from "@/lib/reference"

const PAGE_SIZE = 20
const SUBSTITUTE_DAYS = [1, 2, 3, 4, 5] as const
const NO_SUBSTITUTE = "none"

export default function WorkingDaysTab() {
  const [items, setItems] = useState<WorkingDay[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)

  const [filterFrom, setFilterFrom] = useState("")
  const [filterTo, setFilterTo] = useState("")
  const [appliedFrom, setAppliedFrom] = useState("")
  const [appliedTo, setAppliedTo] = useState("")

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formDateFrom, setFormDateFrom] = useState("")
  const [formDateTo, setFormDateTo] = useState("")
  const [formSubstitute, setFormSubstitute] = useState(NO_SUBSTITUTE)
  const [formTitle, setFormTitle] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<WorkingDay | null>(null)
  const [deleting, setDeleting] = useState(false)

  const load = useCallback(
    async (targetPage: number) => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchWorkingDays({
          from: appliedFrom || undefined,
          to: appliedTo || undefined,
          page: targetPage,
          pageSize: PAGE_SIZE,
        })
        setItems(result.items)
        setTotalPages(Math.max(result.totalPages, 1))
      } catch (err) {
        setError(
          extractErrorMessage(err) ?? "Не удалось загрузить рабочие дни",
        )
      } finally {
        setLoading(false)
      }
    },
    [appliedFrom, appliedTo],
  )

  useEffect(() => {
    void load(page)
  }, [page, load])

  const applyFilters = () => {
    setPage(1)
    setAppliedFrom(filterFrom)
    setAppliedTo(filterTo)
  }

  const resetFilters = () => {
    setFilterFrom("")
    setFilterTo("")
    setAppliedFrom("")
    setAppliedTo("")
    setPage(1)
  }

  const openCreate = () => {
    setEditingId(null)
    setFormDateFrom("")
    setFormDateTo("")
    setFormSubstitute(NO_SUBSTITUTE)
    setFormTitle("Работа в субботу")
    setFormError(null)
    setDialogOpen(true)
  }

  const openEdit = (item: WorkingDay) => {
    setEditingId(item.id)
    setFormDateFrom(toDateInput(item.dateFrom))
    setFormDateTo(toDateInput(item.dateTo))
    setFormSubstitute(
      item.substituteDayOfWeek ? String(item.substituteDayOfWeek) : NO_SUBSTITUTE,
    )
    setFormTitle(item.title)
    setFormError(null)
    setDialogOpen(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formDateFrom || !formDateTo) {
      setFormError("Укажите даты начала и окончания.")
      return
    }
    if (formDateFrom > formDateTo) {
      setFormError("Дата начала не может быть позже даты окончания.")
      return
    }
    if (!formTitle.trim()) {
      setFormError("Укажите название рабочего дня.")
      return
    }

    setSubmitting(true)
    setFormError(null)
    const body = {
      dateFrom: formDateFrom,
      dateTo: formDateTo,
      substituteDayOfWeek:
        formSubstitute === NO_SUBSTITUTE ? null : Number(formSubstitute),
      title: formTitle.trim(),
    }
    try {
      if (editingId) {
        await updateWorkingDay(editingId, body)
        toast.success("Рабочий день обновлён")
      } else {
        await createWorkingDay(body)
        toast.success("Рабочий день добавлен")
      }
      setDialogOpen(false)
      if (editingId) {
        await load(page)
      } else if (page !== 1) {
        setPage(1)
      } else {
        await load(1)
      }
    } catch (err) {
      const message =
        extractErrorMessage(err) ?? "Не удалось сохранить рабочий день"
      setFormError(message)
      toast.error(message)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteWorkingDay(deleteTarget.id)
      toast.success("Рабочий день удалён")
      setDeleteTarget(null)
      if (items.length === 1 && page > 1) {
        setPage(page - 1)
      } else {
        await load(page)
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить запись")
    } finally {
      setDeleting(false)
    }
  }

  const filtering = Boolean(appliedFrom || appliedTo)

  return (
    <section
      role="tabpanel"
      aria-label="Рабочие дни"
      className="flex flex-col gap-6"
    >
      <div className="flex flex-wrap items-end gap-3 rounded-xl border bg-card p-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="working-from">Период с</Label>
          <Input
            id="working-from"
            type="date"
            value={filterFrom}
            onChange={(e) => setFilterFrom(e.target.value)}
            className="h-11 w-44 bg-card sm:h-9"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="working-to">по</Label>
          <Input
            id="working-to"
            type="date"
            value={filterTo}
            onChange={(e) => setFilterTo(e.target.value)}
            className="h-11 w-44 bg-card sm:h-9"
          />
        </div>
        <Button
          variant="outline"
          onClick={applyFilters}
          className="min-h-11 sm:min-h-9"
        >
          <Filter className="size-4" aria-hidden="true" />
          Применить
        </Button>
        {(appliedFrom || appliedTo || filterFrom || filterTo) && (
          <Button
            variant="ghost"
            onClick={resetFilters}
            className="min-h-11 sm:min-h-9"
          >
            <SearchX className="size-4" aria-hidden="true" />
            Сбросить
          </Button>
        )}
        <Button
          onClick={openCreate}
          className="min-h-11 sm:ml-auto sm:min-h-9"
        >
          <Plus className="size-4" aria-hidden="true" />
          Добавить рабочий день
        </Button>
      </div>

      {error && (
        <>
          <ErrorBanner message={error} />
          <Button
            variant="outline"
            onClick={() => void load(page)}
            className="w-fit min-h-11 sm:min-h-9"
          >
            Повторить загрузку
          </Button>
        </>
      )}

      {!error && (
        <Card className="gap-0 py-0">
          <CardHeader className="border-b py-4">
            <CardTitle className="text-base">
              {filtering ? "Найденные периоды" : "Все периоды"}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {loading ? (
              <div role="status" aria-label="Загрузка рабочих дней">
                <LoadingSpinner size="lg" className="py-20" />
              </div>
            ) : items.length === 0 ? (
              <div className="flex flex-col items-center gap-3 px-6 py-16 text-center text-muted-foreground">
                <Inbox className="size-12 opacity-40" aria-hidden="true" />
                <p className="text-base font-medium text-fg">
                  {filtering
                    ? "В выбранном периоде записей нет"
                    : "Рабочих дней пока нет"}
                </p>
                <EmptyState
                  message={
                    filtering
                      ? "Измените период или сбросьте фильтр."
                      : "Отметьте субботы и воскресенья, которые сделаны учебными."
                  }
                />
                {!filtering && (
                  <Button variant="outline" onClick={openCreate}>
                    <Plus className="size-4" aria-hidden="true" />
                    Добавить рабочий день
                  </Button>
                )}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] text-sm">
                  <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-fg">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium">Период</th>
                      <th className="px-4 py-3 text-left font-medium">
                        День недели
                      </th>
                      <th className="px-4 py-3 text-left font-medium">
                        Название
                      </th>
                      <th className="px-4 py-3 text-right font-medium">
                        Действия
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((item) => (
                      <tr key={item.id} className="border-b last:border-0">
                        <td className="px-4 py-3 font-mono text-xs tabular-nums whitespace-nowrap">
                          {formatDateRange(item.dateFrom, item.dateTo)}
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap">
                          {substituteDayLabel(item.substituteDayOfWeek)}
                        </td>
                        <td className="px-4 py-3">{item.title}</td>
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11"
                              onClick={() => openEdit(item)}
                              aria-label={`Редактировать рабочий день «${item.title}»`}
                            >
                              <Pencil className="size-4" aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                              onClick={() => setDeleteTarget(item)}
                              aria-label={`Удалить рабочий день «${item.title}»`}
                            >
                              <Trash2 className="size-4" aria-hidden="true" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {!loading && !error && items.length > 0 && (
        <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
      )}

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingId
                ? "Изменить рабочий день"
                : "Добавить рабочий день"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {formError && <ErrorBanner message={formError} />}
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="working-date-from">Дата начала *</Label>
                <Input
                  id="working-date-from"
                  type="date"
                  required
                  value={formDateFrom}
                  onChange={(e) => {
                    setFormDateFrom(e.target.value)
                    if (!formDateTo || e.target.value > formDateTo) {
                      setFormDateTo(e.target.value)
                    }
                  }}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="working-date-to">Дата окончания *</Label>
                <Input
                  id="working-date-to"
                  type="date"
                  required
                  value={formDateTo}
                  onChange={(e) => setFormDateTo(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="working-substitute">День недели</Label>
              <NativeSelect
                value={formSubstitute}
                onValueChange={setFormSubstitute}
                className="w-full"
              >
                <NativeSelectItem value={NO_SUBSTITUTE}>
                  Без переноса
                </NativeSelectItem>
                {SUBSTITUTE_DAYS.map((day) => (
                  <NativeSelectItem key={day} value={String(day)}>
                    За {DAY_OF_WEEK_LABELS[day].toLowerCase()}
                  </NativeSelectItem>
                ))}
              </NativeSelect>
              <p className="text-xs text-muted-foreground">
                Занятия этого дня недели пройдут в выбранные даты.
              </p>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="working-title">Название *</Label>
              <Input
                id="working-title"
                value={formTitle}
                onChange={(e) => setFormTitle(e.target.value)}
                maxLength={200}
                required
                placeholder="Например, работа в субботу"
                className="h-11 bg-card sm:h-9"
              />
              <p className="text-xs text-muted-foreground">
                Отображается в расписании и уведомлениях.
              </p>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setDialogOpen(false)}
              >
                Отмена
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting ? "Сохранение…" : "Сохранить"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <AlertDialog
        open={deleteTarget !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить рабочий день?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `«${deleteTarget.title}» (${formatDate(deleteTarget.dateFrom)}). Действие необратимо.`
                : ""}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleting}>Отмена</AlertDialogCancel>
            <AlertDialogAction
              disabled={deleting}
              onClick={(e) => {
                e.preventDefault()
                void handleDelete()
              }}
              className="bg-destructive text-white hover:bg-destructive/90"
            >
              {deleting ? "Удаление…" : "Удалить"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  )
}
