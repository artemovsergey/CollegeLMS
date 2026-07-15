"use client"

import { useState, useEffect } from "react"
import { toast } from "sonner"
import type { GroupResponse, TeacherResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"
import { DAYS, LESSON_TYPE_LABELS } from "@/types/schedule"
import { extractErrorMessage } from "@/lib/utils"
import {
  createSchedule,
  updateSchedule,
  type CreateScheduleRequest,
  type UpdateScheduleRequest,
} from "@/api/schedule"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

interface ScheduleEntryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved: () => void
  entry: ScheduleResponse | null
  groups: GroupResponse[]
  teachers: TeacherResponse[]
}

const LESSON_TYPES = ["Lecture", "Practice", "Lab", "Exam"] as const

export default function ScheduleEntryDialog({
  open,
  onOpenChange,
  onSaved,
  entry,
  groups,
  teachers,
}: ScheduleEntryDialogProps) {
  const isEdit = !!entry

  const [groupId, setGroupId] = useState("")
  const [teacherId, setTeacherId] = useState("")
  const [subject, setSubject] = useState("")
  const [room, setRoom] = useState("")
  const [dayOfWeek, setDayOfWeek] = useState("")
  const [startTime, setStartTime] = useState("")
  const [endTime, setEndTime] = useState("")
  const [lessonType, setLessonType] = useState("")
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (open) {
      if (entry) {
        setGroupId(entry.groupId)
        setTeacherId(entry.teacherId ?? "")
        setSubject(entry.subject)
        setRoom(entry.room)
        setDayOfWeek(String(entry.dayOfWeek))
        setStartTime(entry.startTime.slice(0, 5))
        setEndTime(entry.endTime.slice(0, 5))
        setLessonType(entry.lessonType)
      } else {
        setGroupId("")
        setTeacherId("")
        setSubject("")
        setRoom("")
        setDayOfWeek("")
        setStartTime("")
        setEndTime("")
        setLessonType("")
      }
    }
  }, [open, entry])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!groupId || !subject || !room || !dayOfWeek || !startTime || !endTime || !lessonType) {
      toast.error("Заполните все обязательные поля")
      return
    }

    if (startTime >= endTime) {
      toast.error("Время начала должно быть раньше времени окончания")
      return
    }

    setSaving(true)
    try {
      const base = {
        groupId,
        teacherId: teacherId || null,
        subject,
        room,
        dayOfWeek: Number(dayOfWeek),
        startTime: `${startTime}:00`,
        endTime: `${endTime}:00`,
        lessonType,
      }

      if (isEdit) {
        const result = await updateSchedule(entry!.id, base as UpdateScheduleRequest)
        if (result.isSuccess) {
          toast.success("Запись обновлена")
          onOpenChange(false)
          onSaved()
        } else {
          toast.error(result.errorMessage ?? "Ошибка обновления")
        }
      } else {
        const result = await createSchedule(base as CreateScheduleRequest)
        if (result.isSuccess) {
          toast.success("Запись создана")
          onOpenChange(false)
          onSaved()
        } else {
          toast.error(result.errorMessage ?? "Ошибка создания")
        }
      }
    } catch (err: unknown) {
      toast.error(extractErrorMessage(err) ?? "Ошибка сохранения")
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEdit ? "Редактировать запись" : "Новая запись"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Измените данные записи расписания"
              : "Заполните данные для новой записи расписания"}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="grid gap-4">
          <div className="grid gap-2">
            <Label>Группа *</Label>
            <Select value={groupId} onValueChange={setGroupId}>
              <SelectTrigger>
                <SelectValue placeholder="Выберите группу" />
              </SelectTrigger>
              <SelectContent>
                {groups.map((g) => (
                  <SelectItem key={g.id} value={g.id}>
                    {g.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-2">
            <Label>Преподаватель</Label>
            <Select value={teacherId} onValueChange={setTeacherId}>
              <SelectTrigger>
                <SelectValue placeholder="Выберите преподавателя" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Не выбрано</SelectItem>
                {teachers.map((t) => (
                  <SelectItem key={t.id} value={t.id}>
                    {t.fullName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-2">
            <Label>Предмет *</Label>
            <Input value={subject} onChange={(e) => setSubject(e.target.value)} placeholder="Математика" />
          </div>

          <div className="grid gap-2">
            <Label>Аудитория *</Label>
            <Input value={room} onChange={(e) => setRoom(e.target.value)} placeholder="301" />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>День недели *</Label>
              <Select value={dayOfWeek} onValueChange={setDayOfWeek}>
                <SelectTrigger>
                  <SelectValue placeholder="День" />
                </SelectTrigger>
                <SelectContent>
                  {DAYS.filter((d) => d.value >= 1 && d.value <= 6).map((d) => (
                    <SelectItem key={d.value} value={String(d.value)}>
                      {d.full}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label>Тип занятия *</Label>
              <Select value={lessonType} onValueChange={setLessonType}>
                <SelectTrigger>
                  <SelectValue placeholder="Тип" />
                </SelectTrigger>
                <SelectContent>
                  {LESSON_TYPES.map((lt) => (
                    <SelectItem key={lt} value={lt}>
                      {LESSON_TYPE_LABELS[lt]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Начало *</Label>
              <Input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
            </div>
            <div className="grid gap-2">
              <Label>Конец *</Label>
              <Input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Отмена
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? "Сохранение..." : isEdit ? "Сохранить" : "Создать"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
