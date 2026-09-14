"use client"

import { useEffect, useState } from "react"
import { Download, Plus, Trash2, WandSparkles } from "lucide-react"
import { toast } from "sonner"
import api, { unwrap } from "@/lib/api"
import { fetchSubjects } from "@/api/schedule"
import {
  exportManualCorrection,
  previewCorrection,
} from "@/api/correction"
import type { GroupResponse, Result, TeacherResponse } from "@/types"
import type {
  ManualCorrectionRow,
} from "@/api/correction"
import type { CorrectionPreviewEntry, ConfirmResult } from "@/types/correction"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  NativeSelect,
  NativeSelectItem,
} from "@/components/ui/native-select"
import { NoteChips } from "@/components/NoteChips"
import { CorrectionPreviewDialog } from "@/components/CorrectionPreviewDialog"

const emptyRow = (): ManualCorrectionRow => ({
  groupName: "",
  removedSubject: "",
  removedTeacherName: "",
  addedSubject: "",
  addedTeacherName: "",
  numberPair: 1,
  note: "",
})

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

interface PendingApply {
  entries: CorrectionPreviewEntry[]
  errors: string[]
}

export default function ManualCorrectionForm() {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [rows, setRows] = useState<ManualCorrectionRow[]>([emptyRow()])
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [subjects, setSubjects] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const [pending, setPending] = useState<PendingApply | null>(null)

  useEffect(() => {
    Promise.all([
      api.get<Result<GroupResponse[]>>("/api/groups").then(unwrap),
      api.get<Result<TeacherResponse[]>>("/api/teachers").then(unwrap),
      fetchSubjects().then((res) => unwrap({ data: res })),
    ])
      .then(([loadedGroups, loadedTeachers, loadedSubjects]) => {
        setGroups(loadedGroups)
        setTeachers(loadedTeachers)
        setSubjects(loadedSubjects.subjects)
      })
      .catch(() => toast.error("Не удалось загрузить справочники"))
  }, [])

  const updateRow = (index: number, patch: Partial<ManualCorrectionRow>) => {
    setRows((current) =>
      current.map((row, rowIndex) => (rowIndex === index ? { ...row, ...patch } : row)),
    )
  }

  const rowsValid = () =>
    date.length > 0 &&
    rows.every((row) => row.groupName && row.numberPair >= 1 && row.numberPair <= 8)

  const generateFile = async () => {
    const blob = await exportManualCorrection(date, rows)
    const timestamp = new Date().toISOString().slice(0, 19).replace(/[T:]/g, "-")
    downloadBlob(blob, `Корректировка_${timestamp}.xlsx`)
    return blob
  }

  const handleDownload = async () => {
    if (!rowsValid()) {
      toast.error("Заполните дату, группу и номер пары для каждой строки")
      return
    }
    setBusy(true)
    try {
      await generateFile()
      toast.success("Файл корректировки сформирован")
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Не удалось сформировать файл")
    } finally {
      setBusy(false)
    }
  }

  const handleApply = async () => {
    if (!rowsValid()) {
      toast.error("Заполните дату, группу и номер пары для каждой строки")
      return
    }
    setBusy(true)
    try {
      const blob = await generateFile()
      const file = new File([blob], "Корректировка.xlsx", {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      })
      const preview = await previewCorrection(file)
      setPending({
        entries: preview.entries,
        errors: preview.errors.map((error) => error.message),
      })
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Не удалось применить корректировку")
    } finally {
      setBusy(false)
    }
  }

  const handleConfirmed = (result: ConfirmResult) => {
    setPending(null)
    toast.success(`Применено изменений: ${result.applied}`)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <WandSparkles className="size-4" /> Ручная корректировка
        </CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <label className="grid gap-1 text-sm font-medium">
          Дата корректировки
          <Input type="date" value={date} onChange={(event) => setDate(event.target.value)} />
        </label>

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full min-w-[1100px] text-sm">
            <thead className="bg-muted/50 text-xs text-muted-foreground">
              <tr>
                <th className="p-2 text-left">Группа</th>
                <th className="p-2 text-left">Снимается</th>
                <th className="p-2 text-left">Преподаватель</th>
                <th className="p-2 text-left">Вводится</th>
                <th className="p-2 text-left">Преподаватель</th>
                <th className="p-2 text-left">№ пары</th>
                <th className="p-2 text-left">Примечание</th>
                <th className="p-2" />
              </tr>
            </thead>
            <tbody className="divide-y">
              {rows.map((row, index) => (
                <tr key={index}>
                  <td className="p-2">
                    <NativeSelect
                      value={row.groupName}
                      onValueChange={(value) => updateRow(index, { groupName: value })}
                      placeholder="Группа"
                    >
                      {groups.map((group) => (
                        <NativeSelectItem key={group.id} value={group.name}>
                          {group.name}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </td>
                  <td className="p-2">
                    <NativeSelect
                      value={row.removedSubject}
                      onValueChange={(value) => updateRow(index, { removedSubject: value })}
                      placeholder="Предмет"
                    >
                      {subjects.map((subject) => (
                        <NativeSelectItem key={subject} value={subject}>
                          {subject}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </td>
                  <td className="p-2">
                    <NativeSelect
                      value={row.removedTeacherName}
                      onValueChange={(value) => updateRow(index, { removedTeacherName: value })}
                      placeholder="Преподаватель"
                    >
                      <NativeSelectItem value="">Не указан</NativeSelectItem>
                      {teachers.map((teacher) => (
                        <NativeSelectItem key={teacher.id} value={teacher.fullName}>
                          {teacher.fullName}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </td>
                  <td className="p-2">
                    <NativeSelect
                      value={row.addedSubject}
                      onValueChange={(value) => updateRow(index, { addedSubject: value })}
                      placeholder="Предмет"
                    >
                      {subjects.map((subject) => (
                        <NativeSelectItem key={subject} value={subject}>
                          {subject}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </td>
                  <td className="p-2">
                    <NativeSelect
                      value={row.addedTeacherName}
                      onValueChange={(value) => updateRow(index, { addedTeacherName: value })}
                      placeholder="Преподаватель"
                    >
                      <NativeSelectItem value="">Не указан</NativeSelectItem>
                      {teachers.map((teacher) => (
                        <NativeSelectItem key={teacher.id} value={teacher.fullName}>
                          {teacher.fullName}
                        </NativeSelectItem>
                      ))}
                    </NativeSelect>
                  </td>
                  <td className="w-20 p-2">
                    <Input
                      type="number"
                      min={1}
                      max={8}
                      value={row.numberPair}
                      onChange={(e) => updateRow(index, { numberPair: Number(e.target.value) })}
                    />
                  </td>
                  <td className="p-2">
                    <div className="grid gap-1.5">
                      <NoteChips value={row.note} onChange={(value) => updateRow(index, { note: value })} />
                      <Input
                        value={row.note}
                        placeholder="Своё примечание"
                        onChange={(e) => updateRow(index, { note: e.target.value })}
                      />
                    </div>
                  </td>
                  <td className="p-2">
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label="Удалить строку"
                      onClick={() => setRows((current) => current.filter((_, rowIndex) => rowIndex !== index))}
                    >
                      <Trash2 className="size-4 text-destructive" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => setRows((current) => [...current, emptyRow()])}>
            <Plus className="mr-2 size-4" /> Добавить строку
          </Button>
          <Button variant="outline" disabled={busy} onClick={handleDownload}>
            <Download className="mr-2 size-4" /> Скачать XLSX
          </Button>
          <Button disabled={busy} onClick={handleApply}>
            {busy ? "Формирование..." : "Применить"}
          </Button>
        </div>
      </CardContent>

      <CorrectionPreviewDialog
        open={pending !== null}
        entries={pending?.entries ?? []}
        errors={pending?.errors ?? []}
        onConfirm={handleConfirmed}
        onCancel={() => setPending(null)}
      />
    </Card>
  )
}