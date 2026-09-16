"use client"

import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"
import {
  ArrowLeft,
  CheckCircle,
  Download,
  Pencil,
  Play,
  Plus,
  Trash2,
  WandSparkles,
} from "lucide-react"
import api, { unwrap } from "@/lib/api"
import { fetchSubjects } from "@/api/schedule"
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
    groupId: position.groupId,
    groupName: position.groupName,
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
      fetchSubjects().then((res) => unwrap({ data: res })),
    ])
      .then(([g, t, s]) => {
        setGroups(g)
        setTeachers(t)
        setSubjects(s.subjects)
      })
      .catch(() => toast.error("Не удалось загрузить справочники"))
  }, [])

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
    const pair = Number(form.numberPair)
    if (!pair || pair < 1 || pair > 8) {
      toast.error("Номер пары должен быть от 1 до 8")
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
      const blob = await exportBatch(batch.id)
      const stamp = new Date().toISOString().slice(0, 19).replace(/[T:]/g, "-")
      downloadBlob(blob, `Корректировка_${stamp}.xlsx`)
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось сформировать файл")
    }
  }

  const handleApply = async () => {
    if (!batch) return
    if (!window.confirm("Применить все позиции пакета?")) return
    setApplying(true)
    try {
      const result = await applyBatch(batch.id, crypto.randomUUID())
      toast.success(`Применено изменений: ${result.applied}`)
      let blob: Blob | null = null
      try {
        blob = await exportBatch(batch.id)
      } catch {
        blob = null
      }
      if (blob) {
        const stamp = new Date().toISOString().slice(0, 19).replace(/[T:]/g, "-")
        downloadBlob(blob, `Корректировка_${stamp}.xlsx`)
      }
      onApplied()
    } catch (err) {
      toast.error(extractErrorMessage(err) ?? "Не удалось применить пакет")
    } finally {
      setApplying(false)
    }
  }

  const handleRemovedPair = (selection: RemovedPairSelection | null) => {
    if (!selection) return
    patchForm({
      numberPair: String(selection.numberPair),
      removedNumberPair: String(selection.numberPair),
      removedSubject: selection.removedSubject,
      removedTeacherId: selection.removedTeacherId ?? "",
      removedTeacherName: selection.removedTeacherName ?? "",
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
            <ArrowLeft className="size-4 mr-2" /> К пакетам
          </Button>
        </CardContent>
      </Card>
    )
  }

  const batchIsDraft = batch.status === "Draft"
  const hint = batchIsDraft ? batch.positionCount === 0
    ? "Позиций пока нет"
    : "Проверьте позиции и примените пакет"
    : "Пакет уже применён"

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <WandSparkles className="size-4" />
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
              <ArrowLeft className="size-4 mr-2" /> Назад
            </Button>
            <Button variant="outline" onClick={() => void handleExport()}>
              <Download className="size-4 mr-2" /> XLSX
            </Button>
            <Button
              disabled={!batchIsDraft || applying || batch.positionCount === 0}
              onClick={() => void handleApply()}
            >
              {applying ? "Применение..." : "Применить"}
              {!applying && <Play className="size-4 ml-2" />}
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
          <span className="text-muted-foreground">
            {hint}
          </span>
        </div>

        {batch.positions.length === 0 ? (
          <EmptyState message="Позиций пока нет — добавьте первую ниже." />
        ) : (
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full min-w-[860px] text-sm">
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
                {batch.positions.map((position) => (
                  <tr
                    key={position.id}
                    className={position.status === "Applied" ? "opacity-60" : ""}
                  >
                    <td className="px-3 py-2 text-muted-foreground">
                      {position.row}
                    </td>
                    <td className="px-3 py-2">
                      <PositionTypeBadge type={position.changeType} />
                    </td>
                    <td className="px-3 py-2 whitespace-nowrap">
                      {position.groupName}
                    </td>
                    <td className="px-3 py-2 whitespace-nowrap">
                      {position.removedNumberPair != null &&
                        position.changeType !== "Remove" &&
                        position.removedNumberPair !== position.numberPair
                        ? `${position.removedNumberPair} → ${position.numberPair}`
                        : position.numberPair}
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
                            onClick={() => handleEdit(position)}
                            aria-label="Редактировать позицию"
                          >
                            <Pencil className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            disabled={busyRowId === position.id}
                            onClick={() => void handleDelete(position)}
                            aria-label="Удалить позицию"
                          >
                            <Trash2 className="size-4 text-destructive" />
                          </Button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {batchIsDraft && (
          <div className="rounded-md border p-4 grid gap-4">
            <p className="flex items-center gap-2 text-sm font-medium">
              <Plus className="size-4" />
              {editingId ? "Редактирование позиции" : "Новая позиция"}
            </p>

            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              <label className="grid gap-1 text-sm font-medium">
                Тип операции
                <NativeSelect
                  value={form.changeType}
                  onValueChange={(value) =>
                    patchForm({ changeType: value as CorrectionChangeType })
                  }
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

              <label className="grid gap-1 text-sm font-medium">
                № пары
                <Input
                  type="number"
                  min={1}
                  max={8}
                  value={form.numberPair}
                  disabled={form.changeType === "Remove" && Boolean(form.removedSubject)}
                  onChange={(e) => patchForm({ numberPair: e.target.value })}
                />
              </label>

              {form.changeType === "Remove" ? (
                <div className="grid gap-1 text-sm font-medium">
                  <span>Снимаемая пара</span>
                  <RemovePairPicker
                    groupId={form.groupId || null}
                    week={batch.week}
                    dayOfWeek={batch.dayOfWeek}
                    value={
                      form.removedSubject
                        ? {
                            numberPair: Number(form.numberPair) || 1,
                            removedSubject: form.removedSubject,
                            removedTeacherId: form.removedTeacherId || null,
                            removedTeacherName: form.removedTeacherName || null,
                          }
                        : null
                    }
                    onChange={handleRemovedPair}
                  />
                </div>
              ) : (
                <>
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
                </>
              )}

              {(form.changeType === "Replace" ||
                form.changeType === "Move") && (
                <>
                  <label className="grid gap-1 text-sm font-medium">
                    Снимаемый предмет
                    <NativeSelect
                      value={form.removedSubject}
                      onValueChange={(value) =>
                        patchForm({ removedSubject: value })
                      }
                      placeholder="Предмет"
                    >
                      {subjects.map((subject) => (
                        <NativeSelectItem key={subject} value={subject}>
                          {subject}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </label>

                  <label className="grid gap-1 text-sm font-medium">
                    Снимаемый преподаватель
                    <NativeSelect
                      value={form.removedTeacherId}
                      onValueChange={(value) => {
                        const teacher = teachers.find(
                          (item) => item.id === value,
                        )
                        patchForm({
                          removedTeacherId: value,
                          removedTeacherName: teacher?.fullName ?? "",
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

                  <label className="grid gap-1 text-sm font-medium">
                    Старый № пары (для переноса)
                    <Input
                      type="number"
                      min={1}
                      max={8}
                      value={form.removedNumberPair}
                      placeholder="Оставьте пустым"
                      onChange={(e) =>
                        patchForm({ removedNumberPair: e.target.value })
                      }
                    />
                  </label>
                </>
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
                  : ["замена", "перенос"]
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
            <CheckCircle className="size-4 shrink-0" />
            Пакет применён. Позиции зафиксированы в журнале изменений.
          </div>
        )}
      </CardContent>
    </Card>
  )
}