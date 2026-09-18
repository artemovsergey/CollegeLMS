"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import {
  CalendarClock,
  Eye,
  EyeOff,
  Inbox,
  Pencil,
  Plus,
  Trash2,
} from "lucide-react"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { Switch } from "@/components/ui/switch"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
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
import ErrorBanner from "@/components/ErrorBanner"
import EmptyState from "@/components/EmptyState"
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  createInsert,
  deleteInsert,
  fetchInserts,
  updateInsert,
  type ScheduleInsert,
  type InsertDayOfWeek,
} from "@/api/inserts"
import { extractErrorMessage } from "@/lib/utils"
import {
  COURSE_NUMBERS,
  DAY_OF_WEEK_LABELS,
  INSERT_DAYS,
  toApiTime,
  toTimeInput,
} from "@/lib/reference"

export default function DispatcherInsertsPage() {
  const [items, setItems] = useState<ScheduleInsert[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showInactive, setShowInactive] = useState(false)
  const [dayFilter, setDayFilter] = useState("")

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formTitle, setFormTitle] = useState("")
  const [formDay, setFormDay] = useState<InsertDayOfWeek>("Monday")
  const [formStart, setFormStart] = useState("")
  const [formEnd, setFormEnd] = useState("")
  const [formCourse, setFormCourse] = useState("none")
  const [formActive, setFormActive] = useState(true)
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const [deleteTarget, setDeleteTarget] = useState<ScheduleInsert | null>(null)
  const [deleting, setDeleting] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await fetchInserts({
        activeOnly: !showInactive,
        dayOfWeek: (dayFilter || undefined) as InsertDayOfWeek | undefined,
      })
      setItems(data)
    } catch (err) {
      setError(extractErrorMessage(err) ?? "Не удалось загрузить вставки")
    } finally {
      setLoading(false)
    }
  }, [showInactive, dayFilter])

  useEffect(() => {
    void load()
  }, [load])

  const openCreate = () => {
    setEditingId(null)
    setFormTitle("")
    setFormDay("Monday")
    setFormStart("")
    setFormEnd("")
    setFormCourse("none")
    setFormActive(true)
    setFormError(null)
    setDialogOpen(true)
  }

  const openEdit = (item: ScheduleInsert) => {
    setEditingId(item.id)
    setFormTitle(item.title)
    setFormDay(
      (INSERT_DAYS.find((d) => d.num === item.dayOfWeek)?.value ??
        "Monday") as InsertDayOfWeek,
    )
    setFormStart(toTimeInput(item.startTime))
    setFormEnd(toTimeInput(item.endTime))
    setFormCourse(item.course ? String(item.course) : "none")
    setFormActive(item.isActive)
    setFormError(null)
    setDialogOpen(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formTitle.trim()) {
      setFormError("Укажите название вставки.")
      return
    }
    if (!formStart || !formEnd) {
      setFormError("Укажите время начала и окончания.")
      return
    }
    if (formStart >= formEnd) {
      setFormError("Начало должно быть раньше окончания.")
      return
    }

    setSubmitting(true)
    setFormError(null)
    const body = {
      title: formTitle.trim(),
      dayOfWeek: formDay,
      startTime: toApiTime(formStart),
      endTime: toApiTime(formEnd),
      course: formCourse === "none" ? null : Number(formCourse),
      isActive: formActive,
    }
    try {
      if (editingId) {
        await updateInsert(editingId, body)
        toast.success("Вставка обновлена")
      } else {
        await createInsert(body)
        toast.success("Вставка создана")
      }
      setDialogOpen(false)
      await load()
    } catch (err) {
      const message =
        extractErrorMessage(err) ?? "Не удалось сохранить вставку"
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
      await deleteInsert(deleteTarget.id)
      toast.success("Вставка удалена")
      setDeleteTarget(null)
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить вставку")
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold">Вставки</h1>
          <p className="text-sm text-muted-foreground">
            Специальные мероприятия дня («Разговор о важном», классный час) —
            отдельной строкой, без номера пары.
          </p>
        </div>
        <Button onClick={openCreate} className="min-h-11 sm:min-h-9">
          <Plus className="size-4" aria-hidden="true" />
          Добавить вставку
        </Button>
      </header>

      <div className="flex flex-wrap items-end justify-between gap-3 rounded-xl border bg-card p-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="inserts-day">День недели</Label>
          <NativeSelect
            value={dayFilter}
            onValueChange={setDayFilter}
            className="w-48"
          >
            <NativeSelectItem value="">Все дни</NativeSelectItem>
            {INSERT_DAYS.map((d) => (
              <NativeSelectItem key={d.value} value={d.value}>
                {d.label}
              </NativeSelectItem>
            ))}
          </NativeSelect>
        </div>
        <div className="flex items-center gap-2">
          {showInactive ? (
            <Eye className="size-4 text-muted-fg" aria-hidden="true" />
          ) : (
            <EyeOff className="size-4 text-muted-fg" aria-hidden="true" />
          )}
          <Switch
            id="inserts-show-inactive"
            checked={showInactive}
            onCheckedChange={setShowInactive}
            aria-label="Показывать неактивные вставки"
          />
          <Label
            htmlFor="inserts-show-inactive"
            className="cursor-pointer text-sm"
          >
            Показывать неактивные
          </Label>
        </div>
      </div>

      {error && (
        <>
          <ErrorBanner message={error} />
          <Button
            variant="outline"
            onClick={() => void load()}
            className="w-fit min-h-11 sm:min-h-9"
          >
            Повторить загрузку
          </Button>
        </>
      )}

      {!error && (
        <Card className="gap-0 py-0">
          <CardHeader className="border-b py-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <CalendarClock className="size-4" aria-hidden="true" />
              Список вставок
            </CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {loading ? (
              <div role="status" aria-label="Загрузка вставок">
                <LoadingSpinner size="lg" className="py-20" />
              </div>
            ) : items.length === 0 ? (
              <div className="flex flex-col items-center gap-3 px-6 py-16 text-center text-muted-foreground">
                <Inbox className="size-12 opacity-40" aria-hidden="true" />
                <p className="text-base font-medium text-fg">
                  {showInactive
                    ? "Вставок пока нет"
                    : "Активных вставок нет"}
                </p>
                <EmptyState
                  message={
                    showInactive
                      ? "Добавьте первую вставку — она появится в расписании отдельной строкой."
                      : "Включите показ неактивных или добавьте новую вставку."
                  }
                />
                <Button variant="outline" onClick={openCreate}>
                  <Plus className="size-4" aria-hidden="true" />
                  Добавить вставку
                </Button>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[760px] text-sm">
                  <thead className="bg-muted/50 text-xs uppercase tracking-wide text-muted-fg">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium">
                        Название
                      </th>
                      <th className="px-4 py-3 text-left font-medium">
                        День недели
                      </th>
                      <th className="px-4 py-3 text-left font-medium">Время</th>
                      <th className="px-4 py-3 text-left font-medium">Курс</th>
                      <th className="px-4 py-3 text-left font-medium">Статус</th>
                      <th className="px-4 py-3 text-right font-medium">
                        Действия
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((item) => (
                      <tr key={item.id} className="border-b last:border-0">
                        <td className="px-4 py-3 font-medium">{item.title}</td>
                        <td className="px-4 py-3">
                          {DAY_OF_WEEK_LABELS[item.dayOfWeek] ?? "—"}
                        </td>
                        <td className="px-4 py-3 font-mono text-xs tabular-nums whitespace-nowrap">
                          {toTimeInput(item.startTime)}–
                          {toTimeInput(item.endTime)}
                        </td>
                        <td className="px-4 py-3">
                          {item.course ? `${item.course} курс` : "Все курсы"}
                        </td>
                        <td className="px-4 py-3">
                          {item.isActive ? (
                            <Badge
                              variant="outline"
                              className="border-success/40 text-success"
                            >
                              Активна
                            </Badge>
                          ) : (
                            <Badge variant="secondary">Скрыта</Badge>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11"
                              onClick={() => openEdit(item)}
                              aria-label={`Редактировать вставку «${item.title}»`}
                            >
                              <Pencil className="size-4" aria-hidden="true" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                              onClick={() => setDeleteTarget(item)}
                              aria-label={`Удалить вставку «${item.title}»`}
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

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingId ? "Изменить вставку" : "Добавить вставку"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {formError && <ErrorBanner message={formError} />}
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="insert-title">Название *</Label>
              <Input
                id="insert-title"
                value={formTitle}
                onChange={(e) => setFormTitle(e.target.value)}
                maxLength={200}
                required
                placeholder="Например, Разговор о важном"
                className="h-11 bg-card sm:h-9"
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="insert-day">День недели *</Label>
                <NativeSelect
                  value={formDay}
                  onValueChange={(v) => setFormDay(v as InsertDayOfWeek)}
                  className="w-full"
                >
                  {INSERT_DAYS.map((d) => (
                    <NativeSelectItem key={d.value} value={d.value}>
                      {d.label}
                    </NativeSelectItem>
                  ))}
                </NativeSelect>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="insert-course">Курс</Label>
                <NativeSelect
                  value={formCourse}
                  onValueChange={setFormCourse}
                  className="w-full"
                >
                  <NativeSelectItem value="none">Все курсы</NativeSelectItem>
                  {COURSE_NUMBERS.map((n) => (
                    <NativeSelectItem key={n} value={String(n)}>
                      {n} курс
                    </NativeSelectItem>
                  ))}
                </NativeSelect>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="insert-start">Начало *</Label>
                <Input
                  id="insert-start"
                  type="time"
                  required
                  value={formStart}
                  onChange={(e) => setFormStart(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="insert-end">Окончание *</Label>
                <Input
                  id="insert-end"
                  type="time"
                  required
                  value={formEnd}
                  onChange={(e) => setFormEnd(e.target.value)}
                  className="h-11 bg-card sm:h-9"
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Switch
                id="insert-active"
                checked={formActive}
                onCheckedChange={setFormActive}
                aria-label="Вставка активна"
              />
              <Label htmlFor="insert-active" className="cursor-pointer">
                {formActive
                  ? "Активна — показывается в расписании"
                  : "Скрыта — не показывается в расписании"}
              </Label>
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
            <AlertDialogTitle>Удалить вставку?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `«${deleteTarget.title}» (${DAY_OF_WEEK_LABELS[deleteTarget.dayOfWeek] ?? ""}). Действие необратимо.`
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
