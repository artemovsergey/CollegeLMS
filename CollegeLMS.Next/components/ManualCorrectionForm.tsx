"use client"

import { useEffect, useState } from "react"
import { Download, Plus, Trash2, WandSparkles } from "lucide-react"
import { toast } from "sonner"
import api, { unwrap } from "@/lib/api"
import { exportManualCorrection, previewCorrection, confirmCorrection } from "@/api/correction"
import type { GroupResponse, Result, TeacherResponse } from "@/types"
import type { ManualCorrectionRow } from "@/api/correction"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"

const emptyRow = (): ManualCorrectionRow => ({
  groupName: "",
  removedSubject: "",
  removedTeacherName: "",
  addedSubject: "",
  addedTeacherName: "",
  numberPair: 1,
  note: "",
})

function downloadBlob(blob: Blob) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = "Корректировка.xlsx"
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export default function ManualCorrectionForm() {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [rows, setRows] = useState<ManualCorrectionRow[]>([emptyRow()])
  const [groups, setGroups] = useState<GroupResponse[]>([])
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    Promise.all([
      api.get<Result<GroupResponse[]>>("/api/groups").then(unwrap),
      api.get<Result<TeacherResponse[]>>("/api/teachers").then(unwrap),
    ])
      .then(([loadedGroups, loadedTeachers]) => {
        setGroups(loadedGroups)
        setTeachers(loadedTeachers)
      })
      .catch(() => toast.error("Не удалось загрузить группы и преподавателей"))
  }, [])

  const updateRow = (index: number, patch: Partial<ManualCorrectionRow>) => {
    setRows((current) =>
      current.map((row, rowIndex) => (rowIndex === index ? { ...row, ...patch } : row)),
    )
  }

  const generate = async (apply: boolean) => {
    if (!date || rows.some((row) => !row.groupName || row.numberPair < 1 || row.numberPair > 8)) {
      toast.error("Заполните дату, группу и номер пары для каждой строки")
      return
    }

    setBusy(true)
    try {
      const blob = await exportManualCorrection(date, rows)
      downloadBlob(blob)
      if (apply) {
        const file = new File([blob], "Корректировка.xlsx", {
          type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        })
        const preview = await previewCorrection(file)
        if (preview.errors.length > 0) {
          toast.error(preview.errors[0].message)
          return
        }
        const result = await confirmCorrection(preview.entries)
        toast.success(`Файл сформирован, применено изменений: ${result.applied}`)
      } else {
        toast.success("Файл корректировки сформирован")
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Не удалось сформировать файл")
    } finally {
      setBusy(false)
    }
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
                  <td className="p-2"><Input value={row.removedSubject} onChange={(e) => updateRow(index, { removedSubject: e.target.value })} /></td>
                  <td className="p-2">
                    <NativeSelect value={row.removedTeacherName} onValueChange={(value) => updateRow(index, { removedTeacherName: value })} placeholder="Преподаватель">
                      {teachers.map((teacher) => <NativeSelectItem key={teacher.id} value={teacher.fullName}>{teacher.fullName}</NativeSelectItem>)}
                    </NativeSelect>
                  </td>
                  <td className="p-2"><Input value={row.addedSubject} onChange={(e) => updateRow(index, { addedSubject: e.target.value })} /></td>
                  <td className="p-2">
                    <NativeSelect value={row.addedTeacherName} onValueChange={(value) => updateRow(index, { addedTeacherName: value })} placeholder="Преподаватель">
                      {teachers.map((teacher) => <NativeSelectItem key={teacher.id} value={teacher.fullName}>{teacher.fullName}</NativeSelectItem>)}
                    </NativeSelect>
                  </td>
                  <td className="p-2 w-20"><Input type="number" min={1} max={8} value={row.numberPair} onChange={(e) => updateRow(index, { numberPair: Number(e.target.value) })} /></td>
                  <td className="p-2"><Input value={row.note} placeholder="сам.р." onChange={(e) => updateRow(index, { note: e.target.value })} /></td>
                  <td className="p-2">
                    <Button variant="ghost" size="icon" aria-label="Удалить строку" onClick={() => setRows((current) => current.filter((_, rowIndex) => rowIndex !== index))}>
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
          <Button variant="outline" disabled={busy} onClick={() => generate(false)}>
            <Download className="mr-2 size-4" /> Скачать XLSX
          </Button>
          <Button disabled={busy} onClick={() => generate(true)}>
            {busy ? "Формирование..." : "Скачать и применить"}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
