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
import Pagination from "@/components/ui/pagination"
import ErrorBanner from "@/components/ErrorBanner"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  createNonWorkingDay,
  deleteNonWorkingDay,
  fetchNonWorkingDays,
  updateNonWorkingDay,
  type NonWorkingDay,
} from "@/api/nonWorkingDays"
import { extractErrorMessage } from "@/lib/utils"
import { formatDate, formatDateRange, toDateInput } from "@/lib/reference"

const PAGE_SIZE = 20

export default function DispatcherHolidaysPage() {
  const [items, setItems] = useState<NonWorkingDay[]>([])
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
  const [formTitle, setFormTitle] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<NonWorkingDay | null>(null)
  const [deleting, setDeleting] = useState(false)

  const load = useCallback(
    async (targetPage: number) => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchNonWorkingDays({
          from: appliedFrom || undefined,
          to: appliedTo || undefined,
          page: targetPage,
          pageSize: PAGE_SIZE,
        })
        setItems(result.items)
        setTotalPages(Math.max(result.totalPages, 1))
      } catch (err) {
        setError(
          extractErrorMessage(err) ??
            "Не удалось загрузить нерабочие дни",
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
    setFormTitle("")
    setFormError(null)
    setDialogOpen(true)
  }

  const openEdit = (item: NonWorkingDay) => {
    setEditingId(item.id)
    setFormDateFrom(toDateInput(item.dateFrom))
    setFormDateTo(toDateInput(item.dateTo))
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
      setFormError("Укажите название периода.")
      return
    }

    setSubmitting(true)
    setFormError(null)
    const body = {
      dateFrom: formDateFrom,
      dateTo: formDateTo,
      title: formTitle.trim(),
    }
    try {
      if (editingId) {
        await updateNonWorkingDay(editingId, body)
        toast.success("Нерабочий период обновлён")
      } else {
        await createNonWorkingDay(body)
        toast.success("Нерабочий период добавлен")
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
        extractErrorMessage(err) ?? "Не удалось сохранить нерабочий период"
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
      await deleteNonWorkingDay(deleteTarget.id)
      toast.success("Нерабочий период удалён")
      setDeleteTarget(null)
      if (items.length === 1 && page > 1) {
        setPage(page - 1)
      } else {
        await load(page)
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить период")
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold">Нерабочие дни</h1>
          <p className="text-sm text-muted-foreground">
            Праздники и выходные: расписание и рассылка на эти даты не
            формируются.
          </p>
        </div>
        <Button onClick={openCreate} className="min-h-11 sm:min-h-9">
          <Plus className="size-4" aria-hidden="true" />
          Добавить период
        </Button>
      </header>

      <div className="flex flex-wrap items-end gap-3 rounded-xl border bg-card p-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="holidays-from">Период с</Label>
          <Input
            id="holidays-from"
            type="date"
            value={filterFrom}
            onChange={(e) => setFilterFrom(e.target.value)}
            className="h-11 w-44 bg-card sm:h-9"
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="holidays-to">по</Label>
          <Input
            id="holidays-to"
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
              {appliedFrom || appliedTo ? "Найденные периоды" : "Все периоды"}
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {loading ? (
              <div role="status" aria-label="Загрузка нерабочих дней">
                <LoadingSpinner size="lg" className="py-20" />
              </div>
            ) : items.length === 0 ? (
              <div className="flex flex-col items-center gap-3 px-6 py-16 text-center text-muted-foreground">
                <Inbox className="size-12 opacity-40" aria-hidden="true" />
                <p className="text-base font-medium text-fg">
                  {appliedFrom || appliedTo
                    ? "В выбранном периоде записей нет"
                    : "Нерабочих дней пока нет"}
                </p>
                <EmptyState
                  message={
                    appliedFrom || appliedTo
                      ? "Измените период или сбросьте фильтр."
                      : "Добавьте праздники и выходные, чтобы они не попадали в расписание."
                  }
                />
                {!appliedFrom && !appliedTo && (
                  <Button variant="outline" onClick={openCreate}>
                    <Plus className="size-4" aria-hidden="true" />
                    Добавить период
                  </Button>
                )}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[560px] text-sm">
                  <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-fg">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium">Период</th>
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
                        <td className="px-4 py-3">{item.title}</td>
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11"
                              onClick={() => openEdit(item)}
                              aria-label={`Редактировать период «${item.title}»`}
                            >
                              <Pencil className="size-4" aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                              onClick={() => setDeleteTarget(item)}
                              aria-label={`Удалить период «${item.title}»`}
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
                ? "Изменить нерабочий период"
                : "Добавить нерабочий период"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {formError && <ErrorBanner message={formError} />}
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="holiday-date-from">Дата начала *</Label>
                <Input
                  id="holiday-date-from"
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
                <Label htmlFor="holiday-date-to">Дата окончания *</Label>
                <Input
                  id="holiday-date-to"
                  type="date"
                  required
                  value={formDateTo}
                  onChange={(e) => setFormDateTo(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="holiday-title">Название *</Label>
              <Input
                id="holiday-title"
                value={formTitle}
                onChange={(e) => setFormTitle(e.target.value)}
                maxLength={200}
                required
                placeholder="Например, осенние каникулы"
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
            <AlertDialogTitle>Удалить нерабочий период?</AlertDialogTitle>
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
    </div>
  )
}
