"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import {
  BellRing,
  Info,
  Plus,
  RefreshCw,
  Save,
  Trash2,
  CalendarPlus,
} from "lucide-react"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
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
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  createBellProfile,
  deleteBellProfile,
  fetchBellProfiles,
  updateBellProfile,
  updateBells,
  type BellProfile,
  type BellProfileRequest,
  type BellSlotInput,
  type BigBreakInput,
} from "@/api/bells"
import { extractErrorMessage, cn } from "@/lib/utils"
import {
  PAIR_NUMBERS,
  WEEK_DAYS,
  toApiTime,
  toDateInput,
  toTimeInput,
} from "@/lib/reference"

interface SlotRow {
  numberPair: number
  startTime: string
  endTime: string
}

interface DateRow {
  dateFrom: string
  dateTo: string
}

type ActiveKey = string | "new"

function emptyRows(): SlotRow[] {
  return PAIR_NUMBERS.map((numberPair) => ({
    numberPair,
    startTime: "",
    endTime: "",
  }))
}

function rowsFromSlots(slots: BellProfile["slots"]): SlotRow[] {
  const rows = emptyRows()
  for (const slot of slots) {
    const row = rows.find((r) => r.numberPair === slot.numberPair)
    if (row) {
      row.startTime = toTimeInput(slot.startTime)
      row.endTime = toTimeInput(slot.endTime)
    }
  }
  return rows
}

function durationLabel(start: string, end: string): string {
  if (!start || !end) return "—"
  const [sh, sm] = start.split(":").map(Number)
  const [eh, em] = end.split(":").map(Number)
  const minutes = eh * 60 + em - (sh * 60 + sm)
  if (!Number.isFinite(minutes) || minutes <= 0) return "—"
  return `${minutes} мин`
}

export default function DispatcherBellsPage() {
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const [profiles, setProfiles] = useState<BellProfile[]>([])
  const [activeKey, setActiveKey] = useState<ActiveKey>("new")

  const [name, setName] = useState("")
  const [rows, setRows] = useState<SlotRow[]>(emptyRows)
  const [selectedDays, setSelectedDays] = useState<number[]>([])
  const [dates, setDates] = useState<DateRow[]>([])
  const [breakEnabled, setBreakEnabled] = useState(true)
  const [breakAfterPair, setBreakAfterPair] = useState("4")
  const [breakStart, setBreakStart] = useState("")
  const [breakEnd, setBreakEnd] = useState("")

  const [deleteTarget, setDeleteTarget] = useState<BellProfile | null>(null)
  const [deleting, setDeleting] = useState(false)

  const applyProfile = useCallback((profile: BellProfile) => {
    setActiveKey(profile.id)
    setName(profile.name)
    setRows(rowsFromSlots(profile.slots))
    const days = profile.daysOfWeek ?? []
    const profileDates = profile.dates ?? []
    setSelectedDays([...days].sort((a, b) => a - b))
    setDates(
      profileDates.map((d) => ({
        dateFrom: toDateInput(d.dateFrom),
        dateTo: toDateInput(d.dateTo),
      })),
    )
    const bigBreak = profile.bigBreak
    setBreakEnabled(bigBreak !== null)
    setBreakAfterPair(bigBreak ? String(bigBreak.afterPair) : "4")
    setBreakStart(bigBreak ? toTimeInput(bigBreak.startTime) : "")
    setBreakEnd(bigBreak ? toTimeInput(bigBreak.endTime) : "")
    setFormError(null)
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      const list = await fetchBellProfiles()
      setProfiles(list)
      const preferred = list.find((p) => p.isDefault) ?? list[0]
      if (preferred) applyProfile(preferred)
    } catch (err) {
      setLoadError(
        extractErrorMessage(err) ?? "Не удалось загрузить профили звонков",
      )
    } finally {
      setLoading(false)
    }
  }, [applyProfile])

  useEffect(() => {
    void load()
  }, [load])

  const activeProfile = useMemo(
    () => profiles.find((p) => p.id === activeKey) ?? null,
    [profiles, activeKey],
  )
  const isCreating = activeKey === "new"
  const isDefaultProfile = activeProfile?.isDefault ?? false

  const filledCount = useMemo(
    () => rows.filter((r) => r.startTime && r.endTime).length,
    [rows],
  )
  const shortened = filledCount > 0 && filledCount < PAIR_NUMBERS.length

  const updateRow = (
    numberPair: number,
    field: "startTime" | "endTime",
    value: string,
  ) => {
    setRows((prev) =>
      prev.map((row) =>
        row.numberPair === numberPair ? { ...row, [field]: value } : row,
      ),
    )
  }

  const startCreate = () => {
    setActiveKey("new")
    setName("")
    setRows(emptyRows())
    setSelectedDays([])
    setDates([])
    setBreakEnabled(false)
    setBreakAfterPair("4")
    setBreakStart("")
    setBreakEnd("")
    setFormError(null)
  }

  const toggleDay = (value: number) => {
    setSelectedDays((prev) =>
      prev.includes(value)
        ? prev.filter((v) => v !== value)
        : [...prev, value].sort((a, b) => a - b),
    )
  }

  const addDateRow = () => {
    setDates((prev) => [...prev, { dateFrom: "", dateTo: "" }])
  }

  const updateDateRow = (index: number, field: keyof DateRow, value: string) => {
    setDates((prev) =>
      prev.map((row, i) => {
        if (i !== index) return row
        if (field === "dateFrom") {
          const dateTo =
            !row.dateTo || value > row.dateTo ? value : row.dateTo
          return { dateFrom: value, dateTo }
        }
        return { ...row, [field]: value }
      }),
    )
  }

  const removeDateRow = (index: number) => {
    setDates((prev) => prev.filter((_, i) => i !== index))
  }

  const validate = (
    slots: BellSlotInput[],
    bigBreak: BigBreakInput | null,
  ): string | null => {
    if (slots.length === 0) return "Заполните время хотя бы одной пары."
    const ordered = [...slots].sort((a, b) => a.numberPair - b.numberPair)
    for (const slot of ordered) {
      if (slot.startTime < "07:00" || slot.endTime > "21:00") {
        return `Время пары ${slot.numberPair} должно быть в диапазоне 07:00–21:00.`
      }
      if (slot.startTime >= slot.endTime) {
        return `В паре ${slot.numberPair} начало должно быть раньше окончания.`
      }
    }
    for (let i = 1; i < ordered.length; i++) {
      if (ordered[i].startTime < ordered[i - 1].startTime) {
        return "Время пар должно идти по возрастанию номера."
      }
      if (ordered[i].startTime < ordered[i - 1].endTime) {
        return `Пары ${ordered[i - 1].numberPair} и ${ordered[i].numberPair} пересекаются по времени.`
      }
    }
    if (bigBreak) {
      if (
        bigBreak.startTime >= bigBreak.endTime ||
        bigBreak.startTime < "07:00" ||
        bigBreak.endTime > "21:00"
      ) {
        return "Проверьте время большой перемены: оно должно быть в диапазоне 07:00–21:00, начало раньше окончания."
      }
    }
    return null
  }

  const handleSave = async () => {
    setFormError(null)

    const partiallyFilled = rows.find(
      (r) => (r.startTime && !r.endTime) || (!r.startTime && r.endTime),
    )
    if (partiallyFilled) {
      setFormError(
        `В паре ${partiallyFilled.numberPair} заполните и начало, и окончание.`,
      )
      return
    }

    const slots: BellSlotInput[] = rows
      .filter((r) => r.startTime && r.endTime)
      .map((r) => ({
        numberPair: r.numberPair,
        startTime: toApiTime(r.startTime),
        endTime: toApiTime(r.endTime),
      }))

    let bigBreak: BigBreakInput | null = null
    if (breakEnabled) {
      if (!breakStart || !breakEnd) {
        setFormError("Заполните время большой перемены или отключите её.")
        return
      }
      bigBreak = {
        afterPair: Number(breakAfterPair),
        startTime: toApiTime(breakStart),
        endTime: toApiTime(breakEnd),
      }
    }

    const validationError = validate(slots, bigBreak)
    if (validationError) {
      setFormError(validationError)
      return
    }

    const trimmedName = name.trim()
    let profileDates: BellProfileRequest["dates"] = []
    if (!isDefaultProfile) {
      if (!trimmedName) {
        setFormError("Укажите название профиля.")
        return
      }
      if (trimmedName.length > 100) {
        setFormError("Название профиля не должно превышать 100 символов.")
        return
      }
      const invalidDate = dates.find(
        (d) => !d.dateFrom || !d.dateTo || d.dateFrom > d.dateTo,
      )
      if (invalidDate) {
        setFormError(
          "Проверьте диапазоны дат: укажите начало и окончание, начало не позже окончания.",
        )
        return
      }
      profileDates = dates.map((d) => ({
        dateFrom: d.dateFrom,
        dateTo: d.dateTo,
      }))
    }

    setSaving(true)
    try {
      let saved: BellProfile
      if (isDefaultProfile) {
        const res = await updateBells({ slots, bigBreak })
        saved = activeProfile
          ? { ...activeProfile, slots: res.slots, bigBreak: res.bigBreak }
          : res
        toast.success("Профиль звонков сохранён")
      } else if (isCreating) {
        saved = await createBellProfile({
          name: trimmedName,
          daysOfWeek: selectedDays,
          slots,
          bigBreak,
          dates: profileDates,
        })
        toast.success("Профиль создан")
      } else {
        saved = await updateBellProfile(activeKey, {
          name: trimmedName,
          daysOfWeek: selectedDays,
          slots,
          bigBreak,
          dates: profileDates,
        })
        toast.success("Профиль сохранён")
      }
      setProfiles((prev) =>
        prev.some((p) => p.id === saved.id)
          ? prev.map((p) => (p.id === saved.id ? saved : p))
          : [...prev, saved],
      )
      applyProfile(saved)
    } catch (err) {
      const message = extractErrorMessage(err) ?? "Не удалось сохранить профиль"
      setFormError(message)
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteBellProfile(deleteTarget.id)
      toast.success("Профиль удалён")
      const rest = profiles.filter((p) => p.id !== deleteTarget.id)
      setProfiles(rest)
      setDeleteTarget(null)
      const fallback = rest.find((p) => p.isDefault) ?? rest[0]
      if (fallback) {
        applyProfile(fallback)
      } else {
        startCreate()
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить профиль")
    } finally {
      setDeleting(false)
    }
  }

  if (loading) {
    return (
      <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
        <h1 className="text-2xl font-semibold">Звонки</h1>
        <div role="status" aria-label="Загрузка профилей звонков">
          <LoadingSpinner size="lg" className="py-24" />
        </div>
      </div>
    )
  }

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold">Звонки</h1>
        <p className="text-sm text-muted-foreground">
          Профили звонков для расписания, экспорта и бота. Профиль выбирается по
          дате, затем по дню недели, иначе применяется профиль по умолчанию.
        </p>
      </header>

      {loadError && (
        <>
          <ErrorBanner message={loadError} className="justify-between" />
          <Button
            variant="outline"
            onClick={() => void load()}
            className="w-fit"
          >
            <RefreshCw className="size-4" aria-hidden="true" />
            Повторить загрузку
          </Button>
        </>
      )}

      {!loadError && (
        <>
          <div
            role="tablist"
            aria-label="Профили звонков"
            className="flex flex-wrap items-center gap-2"
          >
            {profiles.map((profile) => {
              const active = activeKey === profile.id
              return (
                <Button
                  key={profile.id}
                  role="tab"
                  aria-selected={active}
                  variant={active ? "default" : "outline"}
                  onClick={() => applyProfile(profile)}
                  className="min-h-11 sm:min-h-9"
                >
                  {profile.name}
                  {profile.isDefault && (
                    <span
                      className={cn(
                        "rounded-full px-1.5 py-0.5 text-[10px] font-medium",
                        active
                          ? "bg-primary-foreground/20 text-primary-foreground"
                          : "bg-muted text-muted-foreground",
                      )}
                    >
                      по умолчанию
                    </span>
                  )}
                </Button>
              )
            })}
            <Button
              role="tab"
              aria-selected={isCreating}
              variant={isCreating ? "default" : "outline"}
              onClick={startCreate}
              className="min-h-11 sm:min-h-9"
            >
              <Plus className="size-4" aria-hidden="true" />
              Новый профиль
            </Button>
          </div>

          <section
            role="tabpanel"
            aria-label={
              isCreating ? "Новый профиль" : (activeProfile?.name ?? "Профиль")
            }
            className="flex flex-col gap-6"
          >
            {isDefaultProfile ? (
              <div className="flex items-start gap-3 rounded-lg border border-border bg-muted p-4">
                <Info
                  className="mt-0.5 size-5 shrink-0 text-muted-fg"
                  aria-hidden="true"
                />
                <p className="text-sm text-muted-foreground">
                  Профиль по умолчанию применяется, когда для даты и дня недели
                  не найден другой профиль. Название, дни недели и диапазоны
                  дат у него изменить нельзя.
                </p>
              </div>
            ) : (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Параметры профиля</CardTitle>
                  <CardDescription>
                    Имя профиля и условия, когда его применять: дни недели,
                    диапазоны дат или и то, и другое.
                  </CardDescription>
                </CardHeader>
                <CardContent className="flex flex-col gap-5">
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="profile-name">Название *</Label>
                    <Input
                      id="profile-name"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      maxLength={100}
                      placeholder="Например, понедельник"
                      className="h-11 bg-card sm:h-9"
                    />
                  </div>

                  <fieldset className="flex flex-col gap-2">
                    <legend className="mb-1 text-sm font-medium">
                      Дни недели
                    </legend>
                    <div className="flex flex-wrap gap-2">
                      {WEEK_DAYS.map((day) => {
                        const checked = selectedDays.includes(day.value)
                        return (
                          <label
                            key={day.value}
                            className={cn(
                              "flex min-h-11 cursor-pointer items-center gap-2 rounded-md border px-3 text-sm transition-colors focus-within:ring-[3px] focus-within:ring-ring/50",
                              checked
                                ? "border-primary bg-primary/[0.06]"
                                : "border-border hover:bg-muted",
                            )}
                          >
                            <input
                              type="checkbox"
                              checked={checked}
                              onChange={() => toggleDay(day.value)}
                              className="size-4 accent-primary"
                            />
                            {day.full}
                          </label>
                        )
                      })}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Можно не выбирать дни, если профиль привязан только к
                      диапазонам дат.
                    </p>
                  </fieldset>

                  <fieldset className="flex flex-col gap-3">
                    <legend className="text-sm font-medium">
                      Диапазоны дат
                    </legend>
                    {dates.length === 0 && (
                      <p className="text-xs text-muted-foreground">
                        Профиль действует на выбранные дни недели, пока не
                        добавлены диапазоны дат.
                      </p>
                    )}
                    {dates.map((row, index) => (
                      <div
                        key={index}
                        className="flex flex-wrap items-end gap-2"
                      >
                        <div className="flex flex-col gap-1.5">
                          <Label htmlFor={`profile-date-from-${index}`}>
                            С
                          </Label>
                          <Input
                            id={`profile-date-from-${index}`}
                            type="date"
                            value={row.dateFrom}
                            onChange={(e) =>
                              updateDateRow(index, "dateFrom", e.target.value)
                            }
                            className="h-11 w-40 bg-card sm:h-9"
                          />
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <Label htmlFor={`profile-date-to-${index}`}>
                            По
                          </Label>
                          <Input
                            id={`profile-date-to-${index}`}
                            type="date"
                            value={row.dateTo}
                            onChange={(e) =>
                              updateDateRow(index, "dateTo", e.target.value)
                            }
                            className="h-11 w-40 bg-card sm:h-9"
                          />
                        </div>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="size-11 text-destructive hover:bg-destructive/10 hover:text-destructive"
                          onClick={() => removeDateRow(index)}
                          aria-label={`Удалить диапазон дат ${index + 1}`}
                        >
                          <Trash2 className="size-4" aria-hidden="true" />
                        </Button>
                      </div>
                    ))}
                    <Button
                      type="button"
                      variant="outline"
                      onClick={addDateRow}
                      className="w-fit min-h-11 sm:min-h-9"
                    >
                      <CalendarPlus className="size-4" aria-hidden="true" />
                      Добавить диапазон
                    </Button>
                  </fieldset>
                </CardContent>
              </Card>
            )}

            {shortened && (
              <div className="flex items-start gap-3 rounded-lg border border-border bg-muted p-4">
                <Info
                  className="mt-0.5 size-5 shrink-0 text-muted-fg"
                  aria-hidden="true"
                />
                <div className="text-sm">
                  <p className="font-medium">
                    Сокращённый режим: заполнено {filledCount} из{" "}
                    {PAIR_NUMBERS.length} пар
                  </p>
                  <p className="text-muted-foreground">
                    Для пар без времени звонки выводиться не будут. Это
                    допустимо, но проверьте, что так и задумано.
                  </p>
                </div>
              </div>
            )}

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-base">
                  <BellRing className="size-4" aria-hidden="true" />
                  Расписание звонков
                </CardTitle>
                <CardDescription>
                  Время в диапазоне 07:00–21:00, пары идут по возрастанию и не
                  пересекаются.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[520px] text-sm">
                    <caption className="sr-only">
                      Время начала и окончания для восьми пар
                    </caption>
                    <thead>
                      <tr className="border-b text-left text-xs uppercase tracking-wide text-muted-fg">
                        <th scope="col" className="w-16 py-3 pr-3 font-medium">
                          Пара
                        </th>
                        <th scope="col" className="py-3 pr-3 font-medium">
                          Начало
                        </th>
                        <th scope="col" className="py-3 pr-3 font-medium">
                          Окончание
                        </th>
                        <th scope="col" className="py-3 font-medium">
                          Длительность
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((row) => (
                        <tr
                          key={row.numberPair}
                          className="border-b last:border-0"
                        >
                          <th
                            scope="row"
                            className="py-2 pr-3 text-left font-mono text-base tabular-nums"
                          >
                            {row.numberPair}
                          </th>
                          <td className="py-2 pr-3">
                            <Input
                              type="time"
                              value={row.startTime}
                              onChange={(e) =>
                                updateRow(
                                  row.numberPair,
                                  "startTime",
                                  e.target.value,
                                )
                              }
                              aria-label={`Начало ${row.numberPair} пары`}
                              className="h-11 w-32 bg-card sm:h-9"
                            />
                          </td>
                          <td className="py-2 pr-3">
                            <Input
                              type="time"
                              value={row.endTime}
                              onChange={(e) =>
                                updateRow(
                                  row.numberPair,
                                  "endTime",
                                  e.target.value,
                                )
                              }
                              aria-label={`Окончание ${row.numberPair} пары`}
                              className="h-11 w-32 bg-card sm:h-9"
                            />
                          </td>
                          <td className="py-2 font-mono text-xs tabular-nums text-muted-fg">
                            {durationLabel(row.startTime, row.endTime)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-col gap-1">
                    <CardTitle className="text-base">Большая перемена</CardTitle>
                    <CardDescription>
                      Необязательна. Укажите, после какой пары она начинается.
                    </CardDescription>
                  </div>
                  <div className="flex items-center gap-2">
                    <Switch
                      id="big-break-enabled"
                      checked={breakEnabled}
                      onCheckedChange={setBreakEnabled}
                      aria-label="Включить большую перемену"
                    />
                    <Label
                      htmlFor="big-break-enabled"
                      className="cursor-pointer"
                    >
                      {breakEnabled ? "Включена" : "Выключена"}
                    </Label>
                  </div>
                </div>
              </CardHeader>
              {breakEnabled && (
                <CardContent className="grid gap-4 sm:grid-cols-3">
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="big-break-after">После пары</Label>
                    <NativeSelect
                      value={breakAfterPair}
                      onValueChange={setBreakAfterPair}
                      className="w-full"
                    >
                      {PAIR_NUMBERS.map((n) => (
                        <NativeSelectItem key={n} value={String(n)}>
                          {n}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="big-break-start">Начало</Label>
                    <Input
                      id="big-break-start"
                      type="time"
                      value={breakStart}
                      onChange={(e) => setBreakStart(e.target.value)}
                      className="h-11 bg-card sm:h-9"
                    />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="big-break-end">Окончание</Label>
                    <Input
                      id="big-break-end"
                      type="time"
                      value={breakEnd}
                      onChange={(e) => setBreakEnd(e.target.value)}
                      className="h-11 bg-card sm:h-9"
                    />
                  </div>
                </CardContent>
              )}
            </Card>

            {formError && <ErrorBanner message={formError} />}

            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs text-muted-foreground">
                {isDefaultProfile
                  ? "Изменения применяются ко всему расписанию, корректировкам и экспорту."
                  : "Профиль применяется к датам и дням недели, указанным выше."}
              </p>
              <div className="flex flex-wrap items-center gap-2">
                {!isDefaultProfile && !isCreating && activeProfile && (
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setDeleteTarget(activeProfile)}
                    className="min-h-11 text-destructive hover:bg-destructive/10 hover:text-destructive sm:min-h-9"
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                    Удалить профиль
                  </Button>
                )}
                <Button
                  onClick={() => void handleSave()}
                  disabled={saving}
                  className="min-w-44 min-h-11 sm:min-h-9"
                >
                  {saving ? (
                    <>
                      <LoadingSpinner size="sm" />
                      Сохранение…
                    </>
                  ) : (
                    <>
                      <Save className="size-4" aria-hidden="true" />
                      {isCreating ? "Создать профиль" : "Сохранить"}
                    </>
                  )}
                </Button>
              </div>
            </div>
          </section>
        </>
      )}

      <AlertDialog
        open={deleteTarget !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Удалить профиль?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `«${deleteTarget.name}» будет удалён вместе со слотами, большой переменой и диапазонами дат. Действие необратимо.`
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
