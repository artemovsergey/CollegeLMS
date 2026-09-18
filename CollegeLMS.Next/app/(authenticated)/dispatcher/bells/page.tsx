"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { BellRing, Info, RefreshCw, Save } from "lucide-react"
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
import ErrorBanner from "@/components/ErrorBanner"
import LoadingSpinner from "@/components/LoadingSpinner"
import {
  fetchBells,
  updateBells,
  type BellSchedule,
  type BellSlotInput,
  type BigBreakInput,
} from "@/api/bells"
import { extractErrorMessage } from "@/lib/utils"
import { PAIR_NUMBERS, toApiTime, toTimeInput } from "@/lib/reference"

interface SlotRow {
  numberPair: number
  startTime: string
  endTime: string
}

function emptyRows(): SlotRow[] {
  return PAIR_NUMBERS.map((numberPair) => ({
    numberPair,
    startTime: "",
    endTime: "",
  }))
}

function rowsFromSchedule(schedule: BellSchedule): SlotRow[] {
  const rows = emptyRows()
  for (const slot of schedule.slots) {
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

  const [rows, setRows] = useState<SlotRow[]>(emptyRows)
  const [breakEnabled, setBreakEnabled] = useState(true)
  const [breakAfterPair, setBreakAfterPair] = useState("4")
  const [breakStart, setBreakStart] = useState("")
  const [breakEnd, setBreakEnd] = useState("")

  const load = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      const schedule = await fetchBells()
      setRows(rowsFromSchedule(schedule))
      const bigBreak = schedule.bigBreak
      setBreakEnabled(bigBreak !== null)
      setBreakAfterPair(bigBreak ? String(bigBreak.afterPair) : "4")
      setBreakStart(bigBreak ? toTimeInput(bigBreak.startTime) : "")
      setBreakEnd(bigBreak ? toTimeInput(bigBreak.endTime) : "")
    } catch (err) {
      setLoadError(
        extractErrorMessage(err) ?? "Не удалось загрузить справочник звонков",
      )
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

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

    setSaving(true)
    try {
      const schedule = await updateBells({ slots, bigBreak })
      setRows(rowsFromSchedule(schedule))
      const savedBreak = schedule.bigBreak
      setBreakEnabled(savedBreak !== null)
      setBreakAfterPair(savedBreak ? String(savedBreak.afterPair) : "4")
      setBreakStart(savedBreak ? toTimeInput(savedBreak.startTime) : "")
      setBreakEnd(savedBreak ? toTimeInput(savedBreak.endTime) : "")
      toast.success("Справочник звонков сохранён")
    } catch (err) {
      const message =
        extractErrorMessage(err) ?? "Не удалось сохранить справочник звонков"
      setFormError(message)
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
        <h1 className="text-2xl font-semibold">Звонки</h1>
        <div role="status" aria-label="Загрузка справочника звонков">
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
          Единое время пар для расписания, экспорта и бота. Сохранение заменяет
          справочник целиком.
        </p>
      </header>

      {loadError && (
        <ErrorBanner
          message={loadError}
          className="justify-between"
        />
      )}
      {loadError && (
        <Button variant="outline" onClick={() => void load()} className="w-fit">
          <RefreshCw className="size-4" aria-hidden="true" />
          Повторить загрузку
        </Button>
      )}

      {!loadError && (
        <>
          {shortened && (
            <div className="flex items-start gap-3 rounded-lg border border-border bg-muted p-4">
              <Info className="mt-0.5 size-5 shrink-0 text-muted-fg" aria-hidden="true" />
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
                      <tr key={row.numberPair} className="border-b last:border-0">
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
                              updateRow(row.numberPair, "startTime", e.target.value)
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
                              updateRow(row.numberPair, "endTime", e.target.value)
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
                  <Label htmlFor="big-break-enabled" className="cursor-pointer">
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
              Изменения применяются ко всему расписанию, корректировкам и
              экспорту.
            </p>
            <Button onClick={() => void handleSave()} disabled={saving} className="min-w-44">
              {saving ? (
                <>
                  <LoadingSpinner size="sm" />
                  Сохранение…
                </>
              ) : (
                <>
                  <Save className="size-4" aria-hidden="true" />
                  Сохранить
                </>
              )}
            </Button>
          </div>
        </>
      )}
    </div>
  )
}
