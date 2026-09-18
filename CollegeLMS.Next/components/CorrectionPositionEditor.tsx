"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import {
  ArrowLeft,
  CheckCircle,
  CircleAlert,
  Download,
  Pencil,
  Play,
  Plus,
  Trash2,
  WandSparkles,
} from "lucide-react"
import api, { unwrap } from "@/lib/api"
import { fetchSubjects, normalizeDateOnly } from "@/api/schedule"
import {
  addPosition,
  applyBatch,
  deletePosition,
  exportBatch,
  getBatch,
  updatePosition,
} from "@/api/correction"
import type {
  CorrectionBatch,
  CorrectionChangeType,
  CorrectionPosition,
  CreateCorrectionPosition,
  ScheduleValidationError,
} from "@/types/correction"
import type { GroupResponse, Result, TeacherResponse } from "@/types"
import { DAYS } from "@/types/schedule"
import { extractErrorMessage } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  NativeSelect,
  NativeSelectItem,
} from "@/components/ui/native-select"
import { NoteChips } from "@/components/NoteChips"
import RemovePairPicker, {
  type RemovedPairSelection,
} from "@/components/RemovePairPicker"
import EmptyState from "@/components/EmptyState"

const CHANGE_TYPE_META: Record<
  CorrectionChangeType,
  { label: string; className: string }
> = {
  Add: { label: "Добавлено", className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300" },
  Remove: { label: "Снято", className: "bg-red-100 text-red-700 dark:bg-red-950/60 dark:text-red-300" },
  Replace: { label: "Замена", className: "bg-blue-100 text-blue-700 dark:bg-blue-950/60 dark:text-blue-300" },
  Move: { label: "Перенос", className: "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300" },
}

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

function PositionTypeBadge({ type }: { type: CorrectionChangeType }) {
  const meta = CHANGE_TYPE_META[type]
  return (
    <Badge variant="outline" className={meta.className}>
      {meta.label}
    </Badge>
  )
}

function formatValidationError(error: ScheduleValidationError): string {
  const message = error.message ?? ""
  if (/^Строка\s+\d+/i.test(message)) return message
  const row = error.row ? `Строка ${error.row}` : ""
  return row ? `${row}: ${message}` : message
}

interface PositionForm {
  changeType: CorrectionChangeType
  groupId: string
  groupName: string
  numberPair: string
  subject: string
  teacherId: string
  teacherName: string
  removedSubject: string
  removedTeacherId: string
  removedTeacherName: string
  removedNumberPair: string
  note: string
}

const emptyForm = (): PositionForm => ({
  changeType: "Add",
  groupId: "",
  groupName: "",
  numberPair: "1",
  subject: "",
  teacherId: "",
  teacherName: "",
  removedSubject: "",
  removedTeacherId: "",
  removedTeacherName: "",
  removedNumberPair: "",
  note: "",
})

function formFromPosition(position: CorrectionPosition): PositionForm {
  return {
    changeType: position.changeType,
    groupId:
      position.groupId && position.groupId !== EMPTY_GUID
        ? position.groupId
        : "",
    groupName: position.groupName ?? "",
    numberPair: String(position.numberPair),
    subject: position.subject ?? "",
    teacherId: position.teacherId ?? "",
    teacherName: position.teacherName ?? "",
    removedSubject: position.removedSubject ?? "",
    removedTeacherId: position.removedTeacherId ?? "",
    removedTeacherName: position.removedTeacherName ?? "",
    removedNumberPair:
      position.removedNumberPair != null
        ? String(position.removedNumberPair)
        : "",
    note: position.note ?? "",
  }
}

function toRequest(form: PositionForm): CreateCorrectionPosition {
  return {
    changeType: form.changeType,
    groupId: form.groupId,
    groupName: form.groupName,
    numberPair: Number(form.numberPair) || 1,
    subject: form.subject.trim() || null,
    teacherId: form.teacherId || null,
    teacherName: form.teacherName || null,
    removedSubject: form.removedSubject.trim() || null,
    removedTeacherId: form.removedTeacherId || null,
    removedTeacherName: form.removedTeacherName || null,
    removedNumberPair: form.removedNumberPair
      ? Number(form.removedNumberPair)
      : null,
    note: form.note.trim() || null,
  }
}

function renderTitle(position: CorrectionPosition) {
  if (position.changeType === "Replace" || position.changeType === "Move") {
    return (
      <span className="flex items-center gap-1">
        <span className="line-through text-muted-foreground">
          {position.removedSubject ?? "—"}
        </span>
        <span className="font-medium">{position.subject ?? "—"}</span>
      </span>
    )
  }
  if (position.changeType === "Remove") {
    return (
      <span className="line-through text-muted-foreground">
        {position.removedSubject ?? "—"}
      </span>
    )
  }
  return <span className="font-medium">{position.subject ?? "—"}</span>
}

function renderTeacher(position: CorrectionPosition) {
  if (position.changeType === "Replace" || position.changeType === "Move") {
    const from = position.removedTeacherName
    const to = position.teacherName
    if (from && to) return `${from} → ${to}`
    return to ?? from ?? "—"
  }
  return (
    position.teacherName ??
    (position.changeType === "Remove" ? position.removedTeacherName : null) ??
    "—"
  )
}

interface CorrectionPositionEditorProps {
  batchId: string
  onBack: () => void
  onApplied: () => void
}

export default function CorrectionPositionEditor({
  batchId,
  onBack,
  onApplied,
}: CorrectionPositionEditorProps) {
  const [batch, setBatch] = useState<CorrectionBatch | null>(null)
  const [loading, setLoading] = useState(true)
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [subjects, setSubjects] = useState<string[]>([])
  const [form, setForm] = useState<PositionForm>(emptyForm())
  const [editingId, setEditingId] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [applying, setApplying] = useState(false)
  const [applyErrors, setApplyErrors] = useState<string[]>([])
  const [busyRowId, setBusyRowId] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setBatch(await getBatch(batchId))
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Ошибка загрузки пакета")
    } finally {
      setLoading(false)
    }
  }, [batchId])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    Promise.all([
      api.get<Result<GroupResponse[]>>("/api/groups").then(unwrap),
      api.get<Result<TeacherResponse[]>>("/api/teachers").then(unwrap),
    ])
      .then(([g, t]) => {
        setGroups(g)
        setTeachers(t)
      })
      .catch(() => toast.error("Не удалось загрузить справочники"))
  }, [])

  // Предметы зависят от выбранного преподавателя: бэкенд отдаёт только его предметы.
  useEffect(() => {
    let cancelled = false
    fetchSubjects(undefined, form.teacherId || undefined)
      .then((res) => {
        const list = res.data?.subjects ?? []
        if (cancelled) return
        setSubjects(list)
        setForm((current) =>
          current.subject && !list.includes(current.subject)
            ? { ...current, subject: "" }
            : current,
        )
      })
      .catch(() => {
        if (!cancelled) setSubjects([])
      })
    return () => {
      cancelled = true
    }
  }, [form.teacherId])

  const patchForm = (patch: Partial<PositionForm>) => {
    setForm((current) => ({ ...current, ...patch }))
  }

  const resetForm = () => {
    setForm(emptyForm())
    setEditingId(null)
  }

  const handleSubmit = async () => {
    if (!form.groupId || !form.groupName) {
      toast.error("Выберите группу")
      return
    }
    if (form.changeType !== "Remove") {
      const pair = Number(form.numberPair)
      if (!pair || pair < 1 || pair > 8) {
        toast.error("Номер пары должен быть от 1 до 8")
        return
      }
    }
    if (form.changeType !== "Add" && !form.removedSubject) {
      toast.error("Выберите снимаемую пару")
      return
    }
    setSubmitting(true)
    try {
      const payload = toRequest(form)
      if (editingId) {
        await updatePosition(batchId, editingId, payload)
        toast.success("Позиция обновлена")
      } else {
        await addPosition(batchId, payload)
        toast.success("Позиция добавлена")
      }
      resetForm()
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сохранить позицию")
    } finally {
      setSubmitting(false)
    }
  }

  const handleEdit = (position: CorrectionPosition) => {
    setForm(formFromPosition(position))
    setEditingId(position.id)
    window.scrollTo({ top: 0, behavior: "smooth" })
  }

  const handleDelete = async (position: CorrectionPosition) => {
    if (!window.confirm(`Удалить позицию ${position.row} (${position.groupName})?`))
      return
    setBusyRowId(position.id)
    try {
      await deletePosition(batchId, position.id)
      toast.success("Позиция удалена")
      await load()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось удалить позицию")
    } finally {
      setBusyRowId(null)
    }
  }

  const handleExport = async () => {
    if (!batch) return
    try {
      const { blob, fileName } = await exportBatch(batch.id)
      downloadBlob(blob, fileName)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сформировать файл")
    }
  }

  const handleApply = async () => {
    if (!batch) return
    if (!window.confirm("Применить все позиции пакета?")) return
    setApplying(true)
    setApplyErrors([])
    try {
      const result = await applyBatch(batch.id, crypto.randomUUID())
      toast.success(`Применено изменений: ${result.applied}`)
      try {
        const { blob, fileName } = await exportBatch(batch.id)
        downloadBlob(blob, fileName)
      } catch {
        toast.message("Пакет применён, но файл не удалось сформировать")
      }
      onApplied()
    } catch (err) {
      const message = extractErrorMessage(err)
      const lines = message
        ? message
            .split(/\r?\n/)
            .map((line) => line.trim())
            .filter(Boolean)
        : []
      if (lines.length > 0) {
        setApplyErrors(lines)
        toast.error("Не удалось применить пакет", {
          description: lines[0],
          duration: 8000,
        })
      } else {
        toast.error("Не удалось применить пакет")
      }
    } finally {
      setApplying(false)
    }
  }

  const handleRemovedPair = (selection: RemovedPairSelection | null) => {
    if (!selection) return
    if (form.changeType === "Remove") {
      patchForm({
        numberPair: String(selection.numberPair),
        removedNumberPair: String(selection.numberPair),
        removedSubject: selection.removedSubject,
        removedTeacherId: selection.removedTeacherId ?? "",
        removedTeacherName: selection.removedTeacherName ?? "",
      })
      return
    }
    const autoNote = /^вм\.\d+$/.test(form.note.trim())
    patchForm({
      removedNumberPair: String(selection.numberPair),
      removedSubject: selection.removedSubject,
      removedTeacherId: selection.removedTeacherId ?? "",
      removedTeacherName: selection.removedTeacherName ?? "",
      ...(form.changeType === "Move" && (!form.note.trim() || autoNote)
        ? { note: `вм.${selection.numberPair}` }
        : {}),
    })
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="py-8 text-center text-sm text-muted-foreground">
          Загрузка пакета...
        </CardContent>
      </Card>
    )
  }

  if (!batch) {
    return (
      <Card>
        <CardContent className="py-8">
          <EmptyState message="Пакет не найден." />
          <Button variant="outline" className="mt-4" onClick={onBack}>
            <ArrowLeft className="size-4 mr-2" aria-hidden /> К пакетам
          </Button>
        </CardContent>
      </Card>
    )
  }

  const batchIsDraft = batch.status === "Draft"
  const batchErrors = batch.errors ?? []
  const dateOnly = normalizeDateOnly(batch.correctionDate)
  const applyDisabled =
    !batchIsDraft ||
    applying ||
    batch.positionCount === 0 ||
    batchErrors.length > 0
  const applyHint = !batchIsDraft
    ? "Пакет уже применён или отменён"
    : batchErrors.length > 0
      ? "Сначала исправьте ошибки пакета"
      : batch.positionCount === 0
        ? "В пакете нет позиций"
        : "Проверьте позиции и примените пакет"
  const needsTarget = form.changeType !== "Remove"
  const removedSelection: RemovedPairSelection | null = form.removedSubject
    ? {
        numberPair:
          Number(form.removedNumberPair) || Number(form.numberPair) || 1,
        removedSubject: form.removedSubject,
        removedTeacherId: form.removedTeacherId || null,
        removedTeacherName: form.removedTeacherName || null,
      }
    : null

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <WandSparkles className="size-4" aria-hidden />
            Редактор пакета
            <Badge
              variant="outline"
              className={
                batchIsDraft
                  ? "bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300"
                  : "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-300"
              }
            >
              {batchIsDraft ? "Подготовлен" : "Применён"}
            </Badge>
          </span>
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={onBack}>
              <ArrowLeft className="size-4 mr-2" aria-hidden /> Назад
            </Button>
            <Button variant="outline" onClick={() => void handleExport()}>
              <Download className="size-4 mr-2" aria-hidden /> XLSX
            </Button>
            <Button
              disabled={applyDisabled}
              title={applyDisabled ? applyHint : undefined}
              onClick={() => void handleApply()}
            >
              {applying ? "Применение..." : "Применить"}
              {!applying && <Play className="size-4 ml-2" aria-hidden />}
            </Button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <div className="flex flex-wrap items-center gap-x-6 gap-y-1 rounded-md border bg-muted/30 px-4 py-3 text-sm">
          <span>
            Дата:{" "}
            <span className="font-medium">
              {new Date(batch.correctionDate).toLocaleDateString("ru-RU")}
            </span>
          </span>
          <span>
            Неделя: <span className="font-medium">{batch.week}</span>
          </span>
          <span>
            День:{" "}
            <span className="font-medium">
              {DAYS.find((d) => d.value === batch.dayOfWeek)?.full ??
                batch.dayOfWeek}
            </span>
          </span>
          <span>
            Позиций: <span className="font-medium">{batch.positionCount}</span>
          </span>
          <span className="text-muted-foreground">{applyHint}</span>
        </div>

        {batchErrors.length > 0 && (
          <div
            role="alert"
            className="grid gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-4"
          >
            <p className="flex items-center gap-2 text-sm font-semibold text-destructive">
              <CircleAlert className="size-4 shrink-0" aria-hidden />
              Ошибки пакета ({batchErrors.length}) — применить нельзя
            </p>
            <ul className="grid max-h-48 gap-1 overflow-y-auto text-xs text-muted-foreground">
              {batchErrors.map((error, index) => (
                <li key={index}>{formatValidationError(error)}</li>
              ))}
            </ul>
          </div>
        )}

        {applyErrors.length > 0 && (
          <div
            role="alert"
            className="grid gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-4"
          >
            <p className="flex items-center gap-2 text-sm font-semibold text-destructive">
              <CircleAlert className="size-4 shrink-0" aria-hidden />
              Пакет не применён — бэкенд вернул ошибки
            </p>
            <ul className="grid max-h-48 gap-1 overflow-y-auto text-xs text-muted-foreground">
              {applyErrors.map((line, index) => (
                <li key={index}>{line}</li>
              ))}
            </ul>
          </div>
        )}

        {batch.positions.length === 0 ? (
          <EmptyState message="Позиций пока нет — добавьте первую ниже." />
        ) : (
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full min-w-[900px] text-sm">
              <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 text-left">№</th>
                  <th className="px-3 py-2 text-left">Тип</th>
                  <th className="px-3 py-2 text-left">Группа</th>
                  <th className="px-3 py-2 text-left">Пара</th>
                  <th className="px-3 py-2 text-left">Предмет</th>
                  <th className="px-3 py-2 text-left">Преподаватель</th>
                  <th className="px-3 py-2 text-left">Примечание</th>
                  <th className="px-3 py-2" />
                </tr>
              </thead>
              <tbody className="divide-y">
                {batch.positions.map((position) => {
                  const positionErrors = position.errors ?? []
                  return (
                    <PositionRow
                      key={position.id}
                      position={position}
                      errors={positionErrors}
                      batchIsDraft={batchIsDraft}
                      busy={busyRowId === position.id}
                      onEdit={handleEdit}
                      onDelete={handleDelete}
                    />
                  )
                })}
              </tbody>
            </table>
          </div>
        )}

        {batchIsDraft && (
          <div className="rounded-md border p-4 grid gap-4">
            <p className="flex items-center gap-2 text-sm font-medium">
              <Plus className="size-4" aria-hidden />
              {editingId ? "Редактирование позиции" : "Новая позиция"}
            </p>

            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              <label className="grid gap-1 text-sm font-medium">
                Тип операции
                <NativeSelect
                  value={form.changeType}
                  onValueChange={(value) => {
                    const changeType = value as CorrectionChangeType
                    patchForm({
                      changeType,
                      removedSubject: "",
                      removedTeacherId: "",
                      removedTeacherName: "",
                      removedNumberPair: "",
                    })
                  }}
                >
                  {(Object.keys(CHANGE_TYPE_META) as CorrectionChangeType[]).map(
                    (type) => (
                      <NativeSelectItem key={type} value={type}>
                        {CHANGE_TYPE_META[type].label}
                      </NativeSelectItem>
                    ),
                  )}
                </NativeSelect>
              </label>

              <label className="grid gap-1 text-sm font-medium">
                Группа
                <NativeSelect
                  value={form.groupId}
                  onValueChange={(groupId) => {
                    const group = groups.find((item) => item.id === groupId)
                    patchForm({
                      groupId,
                      groupName: group?.name ?? form.groupName,
                      removedSubject: "",
                      removedTeacherId: "",
                      removedTeacherName: "",
                      removedNumberPair: "",
                    })
                  }}
                  placeholder="Выберите группу"
                >
                  {groups.map((group) => (
                    <NativeSelectItem key={group.id} value={group.id}>
                      {group.name}
                    </NativeSelectItem>
                  ))}
                </NativeSelect>
              </label>

              {needsTarget && (
                <label className="grid gap-1 text-sm font-medium">
                  {form.changeType === "Move" ? "Новая № пары" : "№ пары"}
                  <Input
                    type="number"
                    min={1}
                    max={8}
                    value={form.numberPair}
                    onChange={(e) => patchForm({ numberPair: e.target.value })}
                  />
                </label>
              )}

              {needsTarget && (
                <label className="grid gap-1 text-sm font-medium">
                  Предмет (вводится)
                  <NativeSelect
                    value={form.subject}
                    onValueChange={(value) => patchForm({ subject: value })}
                    placeholder="Предмет"
                  >
                    {subjects.map((subject) => (
                      <NativeSelectItem key={subject} value={subject}>
                        {subject}
                      </NativeSelectItem>
                    ))}
                  </NativeSelect>
                </label>
              )}

              {needsTarget && (
                <label className="grid gap-1 text-sm font-medium">
                  Преподаватель (вводится)
                  <NativeSelect
                    value={form.teacherId}
                    onValueChange={(teacherId) => {
                      const teacher = teachers.find(
                        (item) => item.id === teacherId,
                      )
                      patchForm({
                        teacherId,
                        teacherName: teacher?.fullName ?? "",
                      })
                    }}
                    placeholder="Преподаватель"
                  >
                    <NativeSelectItem value="">Не указан</NativeSelectItem>
                    {teachers.map((teacher) => (
                      <NativeSelectItem key={teacher.id} value={teacher.id}>
                        {teacher.fullName}
                      </NativeSelectItem>
                    ))}
                  </NativeSelect>
                </label>
              )}

              {form.changeType !== "Add" && (
                <div className="grid gap-1 text-sm font-medium sm:col-span-2 lg:col-span-3">
                  <span>
                    {form.changeType === "Remove"
                      ? "Снимаемая пара"
                      : "Снимаемая пара (заменяется/переносится)"}
                  </span>
                  <RemovePairPicker
                    groupId={form.groupId || null}
                    date={dateOnly}
                    batchId={batch.id}
                    value={removedSelection}
                    onChange={handleRemovedPair}
                  />
                </div>
              )}

              {(form.changeType === "Replace" ||
                form.changeType === "Move") && (
                <label className="grid gap-1 text-sm font-medium">
                  Старый № пары
                  <Input
                    type="number"
                    min={1}
                    max={8}
                    value={form.removedNumberPair}
                    placeholder="Из выбранной пары"
                    onChange={(e) =>
                      patchForm({ removedNumberPair: e.target.value })
                    }
                  />
                </label>
              )}

              <label className="grid gap-1 text-sm font-medium">
                Примечание
                <Input
                  value={form.note}
                  placeholder="Своё примечание"
                  onChange={(e) => patchForm({ note: e.target.value })}
                />
              </label>
            </div>

            <NoteChips
              value={form.note}
              onChange={(note) => patchForm({ note })}
              hints={
                form.changeType === "Remove"
                  ? ["сам.р."]
                  : form.changeType === "Move"
                    ? ["перенос"]
                    : ["замена"]
              }
            />

            <div className="flex flex-wrap gap-2">
              <Button
                onClick={() => void handleSubmit()}
                disabled={submitting}
              >
                {submitting
                  ? "Сохранение..."
                  : editingId
                    ? "Сохранить изменения"
                    : "Добавить позицию"}
              </Button>
              {editingId && (
                <Button variant="outline" onClick={resetForm}>
                  Отменить
                </Button>
              )}
            </div>
          </div>
        )}

        {batch.status === "Applied" && (
          <div className="flex items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300">
            <CheckCircle className="size-4 shrink-0" aria-hidden />
            <span>
              Применён
              {batch.appliedByName ? `: ${batch.appliedByName}` : ""}
              {batch.appliedAt
                ? `, ${new Date(batch.appliedAt).toLocaleString("ru-RU")}`
                : ""}
              . Позиции зафиксированы в журнале изменений.
            </span>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

interface PositionRowProps {
  position: CorrectionPosition
  errors: ScheduleValidationError[]
  batchIsDraft: boolean
  busy: boolean
  onEdit: (position: CorrectionPosition) => void
  onDelete: (position: CorrectionPosition) => void
}

function PositionRow({
  position,
  errors,
  batchIsDraft,
  busy,
  onEdit,
  onDelete,
}: PositionRowProps) {
  const hasErrors = errors.length > 0
  return (
    <>
      <tr
        className={
          hasErrors
            ? "bg-destructive/5"
            : position.status === "Applied"
              ? "opacity-60"
              : ""
        }
      >
        <td className="px-3 py-2 text-muted-foreground">{position.row}</td>
        <td className="px-3 py-2">
          <PositionTypeBadge type={position.changeType} />
        </td>
        <td className="px-3 py-2 whitespace-nowrap">
          {position.groupName || (
            <span className="text-destructive">Группа не указана</span>
          )}
        </td>
        <td className="px-3 py-2 whitespace-nowrap">
          {position.removedNumberPair != null &&
          position.changeType !== "Remove" &&
          position.removedNumberPair !== position.numberPair
            ? `${position.removedNumberPair} → ${position.numberPair}`
            : position.numberPair || <span className="text-destructive">—</span>}
        </td>
        <td className="px-3 py-2">{renderTitle(position)}</td>
        <td className="px-3 py-2 max-w-[220px] truncate">
          {renderTeacher(position)}
        </td>
        <td className="px-3 py-2 max-w-[160px] truncate text-muted-foreground">
          {position.note ?? "—"}
        </td>
        <td className="px-3 py-2">
          {batchIsDraft && (
            <div className="flex justify-end gap-1">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onEdit(position)}
                aria-label={`Редактировать позицию ${position.row}`}
              >
                <Pencil className="size-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                disabled={busy}
                onClick={() => onDelete(position)}
                aria-label={`Удалить позицию ${position.row}`}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </div>
          )}
        </td>
      </tr>
      {hasErrors && (
        <tr className="bg-destructive/5">
          <td colSpan={8} className="px-3 pb-2">
            <ul className="grid gap-1 text-xs text-destructive">
              {errors.map((error, index) => (
                <li key={index} className="flex items-start gap-1.5">
                  <CircleAlert className="mt-0.5 size-3.5 shrink-0" aria-hidden />
                  {formatValidationError(error)}
                </li>
              ))}
            </ul>
          </td>
        </tr>
      )}
    </>
  )
}
