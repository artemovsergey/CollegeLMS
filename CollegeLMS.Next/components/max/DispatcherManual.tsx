"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Plus, Search, Trash2, Users, GraduationCap } from "lucide-react"
import { Button, Input, Typography } from "@maxhub/max-ui"
import { fetchSchedule, fetchSubjects, searchSchedule } from "@/api/schedule"
import type { ScheduleResponse } from "@/types/schedule"
import type {
  CorrectionChangeType,
  CorrectionPreviewEntry,
  ConfirmResult,
} from "@/types/correction"
import { WEEKDAYS } from "@/lib/max-lesson"
import { NoteChips } from "@/components/NoteChips"
import ConfirmOpsSheet from "@/components/max/ConfirmOpsSheet"

type DraftOp = {
  key: number
  changeType: CorrectionChangeType
  groupId: string
  groupName: string
  dayOfWeek: number
  week: number
  numberPair: number
  subject: string
  teacherId: string | null
  teacherName: string | null
  removedSubject: string
  removedTeacherId: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string
}

const changeTypeOptions: {
  value: CorrectionChangeType
  label: string
}[] = [
  { value: "Add", label: "Добавить" },
  { value: "Remove", label: "Снять" },
  { value: "Replace", label: "Заменить" },
  { value: "Move", label: "Перенести" },
]

const PAIR_OPTIONS = Array.from({ length: 8 }, (_, i) => i + 1)
const WEEK_OPTIONS = Array.from({ length: 16 }, (_, i) => i + 1)

function dayLabel(dayOfWeek: number): string {
  return WEEKDAYS.find((d) => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
}

export default function DispatcherManual({
  onApplied,
}: {
  onApplied: (result: ConfirmResult) => void
}) {
  const [changeType, setChangeType] =
    useState<CorrectionChangeType>("Add")
  const [groupId, setGroupId] = useState("")
  const [groupName, setGroupName] = useState("")
  const [dayOfWeek, setDayOfWeek] = useState<number>(1)
  const [week, setWeek] = useState<number>(1)
  const [numberPair, setNumberPair] = useState<number>(1)
  const [subject, setSubject] = useState("")
  const [teacherId, setTeacherId] = useState<string | null>(null)
  const [teacherName, setTeacherName] = useState("")
  const [removedPair, setRemovedPair] = useState<number | null>(null)
  const [note, setNote] = useState("")

  const [ops, setOps] = useState<DraftOp[]>([])
  const [formError, setFormError] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)

  const [schedule, setSchedule] = useState<ScheduleResponse[]>([])
  const [scheduleLoading, setScheduleLoading] = useState(false)

  const fetchCurrent = useCallback(async () => {
    if (!groupId) {
      setSchedule([])
      return
    }
    setScheduleLoading(true)
    try {
      const res = await fetchSchedule({
        groupId,
        dayOfWeek,
        week,
        pageSize: 100,
      })
      if (!res.isSuccess) throw new Error(res.errorMessage ?? "Ошибка загрузки")
      setSchedule(res.data?.items ?? [])
    } catch {
      setSchedule([])
    } finally {
      setScheduleLoading(false)
    }
  }, [groupId, dayOfWeek, week])

  useEffect(() => {
    if (changeType === "Remove" || changeType === "Replace" || changeType === "Move") {
      void fetchCurrent()
    } else {
      setSchedule([])
    }
  }, [changeType, fetchCurrent])

  const pickedRemoved = useMemo(() => {
    if (!removedPair) return null
    return schedule.find((e) => e.numberPair === removedPair) ?? null
  }, [schedule, removedPair])

  const add = () => {
    setFormError(null)
    if (!groupId) {
      setFormError("Укажите группу")
      return
    }
    if (changeType === "Add" && subject.trim().length === 0) {
      setFormError("Укажите предмет")
      return
    }
    if (
      (changeType === "Replace" || changeType === "Move") &&
      !pickedRemoved
    ) {
      setFormError("Выберите занятие, которое меняем")
      return
    }

    const base = {
      key: Date.now(),
      changeType,
      groupId,
      groupName,
      dayOfWeek,
      week,
      numberPair,
      subject: subject.trim(),
      teacherId,
      teacherName: teacherName.trim() || null,
      removedSubject: pickedRemoved?.subject ?? "",
      removedTeacherId: pickedRemoved?.teacherId ?? null,
      removedTeacherName: pickedRemoved?.teacherName ?? null,
      removedNumberPair:
        changeType === "Move"
          ? pickedRemoved?.numberPair ?? null
          : changeType === "Replace"
            ? pickedRemoved?.numberPair ?? numberPair
            : null,
      note: note.trim(),
    }

    setOps((prev) => [...prev, base])
    setNote("")
  }

  const toEntry = (op: DraftOp, row: number): CorrectionPreviewEntry => {
    const common = {
      row,
      groupId: op.groupId,
      groupName: op.groupName,
      changeType: op.changeType,
      dayOfWeek: op.dayOfWeek,
      week: op.week,
      note: op.note || null,
    }
    if (op.changeType === "Remove") {
      return {
        ...common,
        numberPair: op.numberPair,
        subject: null,
        teacherId: null,
        teacherName: null,
        removedSubject: op.removedSubject || null,
        removedTeacherId: op.removedTeacherId,
        removedTeacherName: op.removedTeacherName || null,
        removedNumberPair: null,
      }
    }
    if (op.changeType === "Add") {
      return {
        ...common,
        numberPair: op.numberPair,
        subject: op.subject,
        teacherId: op.teacherId,
        teacherName: op.teacherName,
        removedSubject: null,
        removedTeacherId: null,
        removedTeacherName: null,
        removedNumberPair: null,
      }
    }
    return {
      ...common,
      numberPair: op.numberPair,
      subject: op.subject,
      teacherId: op.teacherId,
      teacherName: op.teacherName,
      removedSubject: op.removedSubject || null,
      removedTeacherId: op.removedTeacherId,
      removedTeacherName: op.removedTeacherName || null,
      removedNumberPair: op.changeType === "Move" ? op.removedNumberPair : op.numberPair,
    }
  }

  const entries = useMemo(
    () => ops.map((op, index) => toEntry(op, index)),
    [ops],
  )

  const openConfirm = () => {
    if (ops.length === 0) {
      setFormError("Добавьте хотя бы одну операцию")
      return
    }
    setFormError(null)
    setConfirmOpen(true)
  }

  const changeLabel = (t: CorrectionChangeType): string =>
    changeTypeOptions.find((o) => o.value === t)?.label ?? t

  return (
    <div className="max-app__dispatcher-manual">
      <div className="max-app__form-row">
        <span className="max-app__form-label">Тип операции</span>
        <div className="max-schedule__view-switch" role="tablist" aria-label="Тип корректировки">
          {changeTypeOptions.map((opt) => (
            <button
              key={opt.value}
              type="button"
              role="tab"
              aria-selected={changeType === opt.value}
              className={`max-schedule__view-tab ${
                changeType === opt.value ? "max-schedule__view-tab--active" : ""
              }`}
              onClick={() => setChangeType(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      <GroupPicker
        value={{ id: groupId, name: groupName }}
        onChange={(id, name) => {
          setGroupId(id)
          setGroupName(name)
        }}
      />

      <div className="max-app__form-grid">
        <label className="max-app__field">
          <span className="max-app__form-label">День</span>
          <select
            className="max-app__select"
            value={dayOfWeek}
            onChange={(e) => setDayOfWeek(Number(e.target.value))}
          >
            {WEEKDAYS.map((d) => (
              <option key={d.value} value={d.value}>
                {d.full}
              </option>
            ))}
          </select>
        </label>
        <label className="max-app__field">
          <span className="max-app__form-label">Неделя</span>
          <select
            className="max-app__select"
            value={week}
            onChange={(e) => setWeek(Number(e.target.value))}
          >
            {WEEK_OPTIONS.map((w) => (
              <option key={w} value={w}>
                {w}
              </option>
            ))}
          </select>
        </label>
      </div>

      {changeType !== "Move" ? (
        <label className="max-app__field">
          <span className="max-app__form-label">
            Пара {changeType === "Replace" ? "(куда вводим)" : ""}
          </span>
          <select
            className="max-app__select"
            value={numberPair}
            onChange={(e) => setNumberPair(Number(e.target.value))}
          >
            {PAIR_OPTIONS.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </label>
      ) : null}

      {changeType === "Remove" || changeType === "Replace" || changeType === "Move" ? (
        <div>
          <span className="max-app__form-label">Что снимаем</span>
          {scheduleLoading ? (
            <Typography.Body className="max-app__note">
              Загружаем расписание…{" "}
            </Typography.Body>
          ) : schedule.length === 0 ? (
            <Typography.Body className="max-app__note">
              На {dayLabel(dayOfWeek)} {week}-й неделе занятий нет
            </Typography.Body>
          ) : (
            <div className="max-app__schedule-options">
              {schedule
                .slice()
                .sort((a, b) => a.numberPair - b.numberPair)
                .map((entry) => (
                  <label
                    key={entry.id}
                    className={`max-app__option${
                      removedPair === entry.numberPair
                        ? " max-app__option--on"
                        : ""
                    }`}
                  >
                    <input
                      type="radio"
                      name="removed"
                      value={entry.numberPair}
                      checked={removedPair === entry.numberPair}
                      onChange={() => setRemovedPair(entry.numberPair)}
                    />
                    <span>
                      <strong>{entry.numberPair} пара</strong> · {entry.subject}
                      {entry.teacherName ? ` · ${entry.teacherName}` : ""}
                      {entry.room ? ` · ${entry.room}` : ""}
                    </span>
                  </label>
                ))}
            </div>
          )}
        </div>
      ) : null}

      {changeType === "Add" || changeType === "Replace" ? (
        <>
          <SubjectPicker value={subject} onChange={setSubject} />
          <TeacherPicker
            value={{ id: teacherId, name: teacherName }}
            placeholder="Преподаватель (необязательно)"
            onChange={(id, name) => {
              setTeacherId(id)
              setTeacherName(name)
            }}
          />
        </>
      ) : changeType === "Move" ? (
        <>
          {pickedRemoved ? (
            <Typography.Body className="max-app__note">
              Переносим «{pickedRemoved.subject}
              {pickedRemoved.teacherName ? ` · ${pickedRemoved.teacherName}` : ""}
              » с пары {pickedRemoved.numberPair}
            </Typography.Body>
          ) : null}
          <label className="max-app__field">
            <span className="max-app__form-label">На пару</span>
            <select
              className="max-app__select"
              value={numberPair}
              onChange={(e) => setNumberPair(Number(e.target.value))}
            >
              {PAIR_OPTIONS.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>
        </>
      ) : null}

      <label className="max-app__field">
        <span className="max-app__form-label">Примечание</span>
        <NoteChips value={note} onChange={setNote} />
        <Input
          value={note}
          placeholder="Необязательно"
          onChange={(e) => setNote(e.target.value)}
        />
      </label>

      <Button stretched variant="secondary" onClick={add} iconBefore={<Plus size={18} aria-hidden />}>
        Добавить операцию
      </Button>

      {formError ? (
        <Typography.Body className="max-app__error">{formError}</Typography.Body>
      ) : null}

      {ops.length > 0 ? (
        <div className="max-app__ops">
          <Typography.Title>Операции ({ops.length})</Typography.Title>
          {ops.map((op, i) => (
            <div className="max-app__op" key={op.key}>
              <span
                className={`max-app__badge max-app__badge--${op.changeType.toLowerCase()}`}
              >
                {changeLabel(op.changeType)}
              </span>
              <span className="max-app__op-text">
                {op.groupName} · {dayLabel(op.dayOfWeek)} {op.week}-я нед. ·{" "}
                {op.changeType === "Move"
                  ? `${op.removedNumberPair ?? "—"} → ${op.numberPair}`
                  : `${op.numberPair} пара`}
                {op.subject ? ` · ${op.subject}` : ""}
                {op.teacherName ? ` · ${op.teacherName}` : ""}
              </span>
              <Button
                size="xsmall"
                variant="ghost"
                aria-label="Удалить операцию"
                onClick={() => setOps((prev) => prev.filter((x) => x.key !== op.key))}
                iconBefore={<Trash2 size={16} aria-hidden />}
              />
            </div>
          ))}
          <Button
            stretched
            onClick={openConfirm}
          >
            Применить изменения
          </Button>
        </div>
      ) : null}

      <ConfirmOpsSheet
        open={confirmOpen}
        ops={entries}
        onCancel={() => setConfirmOpen(false)}
        onApplied={(result) => {
          onApplied(result)
          setOps([])
          setConfirmOpen(false)
        }}
      />
    </div>
  )
}

function GroupPicker({
  value,
  onChange,
}: {
  value: { id: string; name: string }
  onChange: (id: string, name: string) => void
}) {
  const [query, setQuery] = useState("")
  const [options, setOptions] = useState<{ id: string; name: string }[]>([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    const q = query.trim()
    if (q.length === 0) {
      setOptions([])
      setOpen(false)
      return
    }
    const timer = window.setTimeout(async () => {
      setLoading(true)
      try {
        const res = await searchSchedule(q)
        const groups = res.isSuccess
          ? (res.data?.groups ?? [])
              .slice(0, 6)
              .map((g) => ({ id: g.id, name: g.name }))
          : []
        setOptions(groups)
        setOpen(groups.length > 0)
      } catch {
        setOptions([])
        setOpen(false)
      } finally {
        setLoading(false)
      }
    }, 250)
    return () => window.clearTimeout(timer)
  }, [query])

  return (
    <div className="max-app__field">
      <span className="max-app__form-label">Группа</span>
      {value.id ? (
        <div className="max-app__picked">
          <span>
            <Users size={14} aria-hidden /> {value.name}
          </span>
          <Button
            size="xsmall"
            variant="ghost"
            aria-label="Выбрать другую группу"
            onClick={() => {
              onChange("", "")
              setQuery("")
            }}
          >
            Изменить
          </Button>
        </div>
      ) : (
        <>
          <Input
            value={query}
            placeholder="Начните вводить название группы"
            onChange={(e) => setQuery(e.target.value)}
            iconBefore={<Search size={18} aria-hidden />}
          />
          {loading ? (
            <Typography.Body className="max-app__note">Поиск…</Typography.Body>
          ) : open && options.length > 0 ? (
            <div className="max-app__search-item">
              {options.map((g) => (
                <Button
                  key={g.id}
                  size="small"
                  variant="secondary"
                  onClick={() => {
                    onChange(g.id, g.name)
                    setOpen(false)
                  }}
                >
                  {g.name}
                </Button>
              ))}
            </div>
          ) : open ? (
            <Typography.Body className="max-app__note">
              Группа не найдена
            </Typography.Body>
          ) : null}
        </>
      )}
    </div>
  )
}

function SubjectPicker({
  value,
  onChange,
}: {
  value: string
  onChange: (subject: string) => void
}) {
  const [query, setQuery] = useState("")
  const [options, setOptions] = useState<string[]>([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    setQuery(value)
  }, [value])

  useEffect(() => {
    const q = query.trim()
    if (q.length === 0) {
      setOptions([])
      setOpen(false)
      return
    }
    const timer = window.setTimeout(async () => {
      setLoading(true)
      try {
        const res = await fetchSubjects(q)
        const subjects = res.isSuccess
          ? (res.data?.subjects ?? []).slice(0, 6)
          : []
        setOptions(subjects)
        setOpen(subjects.length > 0)
      } catch {
        setOptions([])
        setOpen(false)
      } finally {
        setLoading(false)
      }
    }, 250)
    return () => window.clearTimeout(timer)
  }, [query])

  return (
    <div className="max-app__field">
      <span className="max-app__form-label">Предмет</span>
      <Input
        value={query}
        placeholder="Начните вводить предмет"
        onChange={(e) => {
          setQuery(e.target.value)
          onChange(e.target.value)
        }}
        iconBefore={<Search size={18} aria-hidden />}
      />
      {loading ? (
        <Typography.Body className="max-app__note">Поиск…</Typography.Body>
      ) : null}
      {open && options.length > 0 ? (
        <div className="max-app__search-item">
          {options.map((subject) => (
            <Button
              key={subject}
              size="small"
              variant="secondary"
              onClick={() => {
                onChange(subject)
                setQuery(subject)
                setOpen(false)
              }}
            >
              {subject}
            </Button>
          ))}
        </div>
      ) : null}
    </div>
  )
}

function TeacherPicker({
  value,
  placeholder,
  onChange,
}: {
  value: { id: string | null; name: string }
  placeholder: string
  onChange: (id: string | null, name: string) => void
}) {
  const [query, setQuery] = useState("")
  const [options, setOptions] = useState<{ id: string; name: string }[]>([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    const q = query.trim()
    if (q.length === 0) {
      setOptions([])
      setOpen(false)
      return
    }
    const timer = window.setTimeout(async () => {
      setLoading(true)
      try {
        const res = await searchSchedule(q)
        const teachers = res.isSuccess
          ? (res.data?.teachers ?? [])
              .slice(0, 6)
              .map((t) => ({ id: t.id, name: t.fullName }))
          : []
        setOptions(teachers)
        setOpen(teachers.length > 0)
      } catch {
        setOptions([])
        setOpen(false)
      } finally {
        setLoading(false)
      }
    }, 250)
    return () => window.clearTimeout(timer)
  }, [query])

  return (
    <div className="max-app__field">
      <span className="max-app__form-label">{placeholder}</span>
      {value.id ? (
        <div className="max-app__picked">
          <span>
            <GraduationCap size={14} aria-hidden /> {value.name}
          </span>
          <Button
            size="xsmall"
            variant="ghost"
            aria-label="Сбросить преподавателя"
            onClick={() => {
              onChange(null, "")
              setQuery("")
            }}
          >
            Сбросить
          </Button>
        </div>
      ) : (
        <>
          <Input
            value={query}
            placeholder="Начните вводить ФИО"
            onChange={(e) => setQuery(e.target.value)}
            iconBefore={<Search size={18} aria-hidden />}
          />
          {loading ? (
            <Typography.Body className="max-app__note">Поиск…</Typography.Body>
          ) : open && options.length > 0 ? (
            <div className="max-app__search-item">
              {options.map((t) => (
                <Button
                  key={t.id}
                  size="small"
                  variant="secondary"
                  onClick={() => {
                    onChange(t.id, t.name)
                    setOpen(false)
                  }}
                >
                  {t.name}
                </Button>
              ))}
            </div>
          ) : open ? (
            <Typography.Body className="max-app__note">
              Преподаватель не найден
            </Typography.Body>
          ) : null}
        </>
      )}
    </div>
  )
}