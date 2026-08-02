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
import FormField from "@/components/FormField"
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
  const [numberPair, setNumberPair] = useState("1")
  const [startTime, setStartTime] = useState("")
  const [endTime, setEndTime] = useState("")
  const [weeksInput, setWeeksInput] = useState("")
  const [lessonType, setLessonType] = useState("")
  const [saving, setSaving] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (open) {
      setFieldErrors({})
      if (entry) {
        setGroupId(entry.groupId)
        setTeacherId(entry.teacherId ?? "")
        setSubject(entry.subject)
        setRoom(entry.room)
        setDayOfWeek(String(entry.dayOfWeek))
        setNumberPair(String(entry.numberPair))
        setStartTime(entry.startTime.slice(0, 5))
        setEndTime(entry.endTime.slice(0, 5))
        setWeeksInput(entry.weeks.join(", "))
        setLessonType(entry.lessonType)
      } else {
        setGroupId("")
        setTeacherId("")
        setSubject("")
        setRoom("")
        setDayOfWeek("")
        setNumberPair("1")
        setStartTime("")
        setEndTime("")
        setWeeksInput("")
        setLessonType("")
      }
    }
  }, [open, entry])

  const parseWeeks = (input: string): number[] => {
    return input
      .split(/[,.\s]+/)
      .map((s) => parseInt(s, 10))
      .filter((n) => !isNaN(n) && n >= 1 && n <= 52)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const errors: Record<string, string> = {}
    if (!groupId) errors.groupId = "Группа обязательна"
    if (!subject.trim()) errors.subject = "Название предмета обязательно"
    else if (subject.trim().length > 200) errors.subject = "Название предмета не должно превышать 200 символов"
    if (!room.trim()) errors.room = "Номер аудитории обязателен"
    else if (room.trim().length > 50) errors.room = "Номер аудитории не должен превышать 50 символов"
    if (!dayOfWeek) errors.dayOfWeek = "День недели обязателен"
    if (!lessonType) errors.lessonType = "Тип занятия обязателен"

    if (!startTime) errors.startTime = "Время начала обязательно"
    if (!endTime) errors.endTime = "Время окончания обязательно"
    else if (startTime && startTime >= endTime) errors.endTime = "Время начала должно быть раньше времени окончания"

    const np = parseInt(numberPair, 10)
    if (isNaN(np) || np < 1 || np > 8) {
      errors.numberPair = "Номер пары должен быть от 1 до 8"
    }

    const weeks = parseWeeks(weeksInput)
    if (weeks.length === 0) {
      errors.weeks = "Укажите хотя бы одну неделю"
    }

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors)
      return
    }
    setFieldErrors({})

    setSaving(true)
    try {
      const base = {
        groupId,
        teacherId: teacherId || null,
        subject,
        room,
        dayOfWeek: Number(dayOfWeek),
        numberPair: np,
        startTime: `${startTime}:00`,
        endTime: `${endTime}:00`,
        weeks,
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
          <FormField id="schedule-group" label="Группа" required error={fieldErrors.groupId}>
            <Select value={groupId} onValueChange={setGroupId}>
              <SelectTrigger id="schedule-group">
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
          </FormField>

          <FormField id="schedule-teacher" label="Преподаватель">
            <Select value={teacherId} onValueChange={setTeacherId}>
              <SelectTrigger id="schedule-teacher">
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
          </FormField>

          <FormField id="schedule-subject" label="Предмет" required error={fieldErrors.subject}>
            <Input id="schedule-subject" value={subject} onChange={(e) => setSubject(e.target.value)} placeholder="Математика" />
          </FormField>

          <FormField id="schedule-room" label="Аудитория" required error={fieldErrors.room}>
            <Input id="schedule-room" value={room} onChange={(e) => setRoom(e.target.value)} placeholder="301" />
          </FormField>

          <FormField id="schedule-day" label="День недели" required error={fieldErrors.dayOfWeek}>
            <Select value={dayOfWeek} onValueChange={setDayOfWeek}>
              <SelectTrigger id="schedule-day">
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
          </FormField>

          <div className="grid grid-cols-2 gap-4">
            <FormField id="schedule-pair" label="Номер пары" required error={fieldErrors.numberPair}>
              <Select value={numberPair} onValueChange={setNumberPair}>
                <SelectTrigger id="schedule-pair">
                  <SelectValue placeholder="Пара" />
                </SelectTrigger>
                <SelectContent>
                  {Array.from({ length: 8 }, (_, i) => (
                    <SelectItem key={i + 1} value={String(i + 1)}>
                      {i + 1} пара
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>
            <FormField id="schedule-lesson-type" label="Тип занятия" required error={fieldErrors.lessonType}>
              <Select value={lessonType} onValueChange={setLessonType}>
                <SelectTrigger id="schedule-lesson-type">
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
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField id="schedule-start" label="Начало" required error={fieldErrors.startTime}>
              <Input id="schedule-start" type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
            </FormField>
            <FormField id="schedule-end" label="Конец" required error={fieldErrors.endTime}>
              <Input id="schedule-end" type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
            </FormField>
          </div>

          <FormField id="schedule-weeks" label="Недели" required error={fieldErrors.weeks}>
            <Input
              id="schedule-weeks"
              value={weeksInput}
              onChange={(e) => setWeeksInput(e.target.value)}
              placeholder="1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16"
            />
            <p className="text-xs text-muted-foreground">
              Номера недель через запятую. Например: 1,3,5,7,9,11,13,15 — нечётные
            </p>
          </FormField>

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
