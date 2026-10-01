"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import {
  BookOpen,
  CalendarDays,
  CircleAlert,
  GraduationCap,
  Layers,
  LoaderCircle,
  Lock,
  Minus,
  Plus,
  UserRound,
} from "lucide-react"
import { getCorrectionReferences } from "@/api/correction"
import type {
  CorrectionDayEntry,
  CorrectionGroupTeacher,
  CorrectionPosition,
  CorrectionReferences,
  CreateCorrectionPosition,
} from "@/types/correction"
import type { GroupResponse } from "@/types"
import { cn, extractErrorMessage } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import GroupDayCard, {
  entryKey,
  physicalEntriesForPair,
} from "@/components/GroupDayCard"
import { isPhysicalSelfStudyNote, isSelfStudyNote } from "@/lib/change-tags"
import { toCorrectionRow } from "@/lib/correction-row"
import {
  SearchableMultiSelect,
  SearchableSelect,
} from "@/components/SearchableSelect"

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

/**
 * Операций всего две: добавить и снять. Всё остальное — решение внутри
 * добавления: что делать с занятием в выбранной паре («в параллель» или
 * «заменить») и служебные отметки примечания («сам.р», «сам.р+», «вм.X»).
 */
const OPERATION_CARDS: {
  value: CorrectionOperation
  label: string
  hint: string
  className: string
}[] = [
  {
    value: "add",
    label: "Добавить",
    hint: "Новое занятие в выбранную пару",
    className:
      "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-200",
  },
  {
    value: "remove",
    label: "Снять",
    hint: "Занятие убирается из пары",
    className:
      "border-red-300 bg-red-50 text-red-800 dark:border-red-900 dark:bg-red-950/40 dark:text-red-200",
  },
]

/** Операция, которую выбирает диспетчер. */
type CorrectionOperation = "add" | "remove"

/** Что происходит с занятием, которое уже стоит в выбранной паре. */
type SlotMode = "parallel" | "replace"

/** Отметка самостоятельной работы в примечании позиции. */
type SelfStudyMode = "none" | "info" | "physical"

/** Занятие, которое позиция снимает. */
interface RemovedLesson {
  numberPair: number
  subject: string
  teacherId: string | null
  teacherName: string | null
}

interface Draft {
  operation: CorrectionOperation
  groupId: string
  /** Пара, в которую вводится или из которой снимается занятие. */
  targetPair: number | null
  /** Занятие, которое снимают: снятие и замена. */
  source: RemovedLesson | null
  /** Слот занят: новое занятие встаёт вторым или заменяет прежнее. */
  slotMode: SlotMode
  /**
   * «вм.X» — пара, откуда переносится вводимое занятие. С ней добавление
   * освобождает пар�� X. null — переноса нет.
   */
  moveFrom: number | null
  /** «сам.р.» — только информирование, «сам.р+» — добавить и пометить. */
  selfStudy: SelfStudyMode
  /** Служебное слово «снять» в примечании позиции снятия. */
  removeMark: boolean
  teacherIds: string[]
  /**
   * Имена преподавателей позиции как есть (через слеш). Нужны, чтобы при
   * редактировании восстановить всех преподавателей, а не только первого —
   * в позиции хранится лишь один `teacherId`.
   */
  teacherNameHint: string
  subject: string
  note: string
}

function emptyDraft(): Draft {
  return {
    operation: "add",
    groupId: "",
    targetPair: null,
    source: null,
    slotMode: "parallel",
    moveFrom: null,
    selfStudy: "none",
    removeMark: false,
    teacherIds: [],
    teacherNameHint: "",
    subject: "",
    note: "",
  }
}

/** «Рахимова А.Л.» и «рахимова а.л.» — один и тот же преподаватель. */
function sameTeacherName(a: string, b: string): boolean {
  const normalize = (value: string) =>
    value.replace(/\s+/g, " ").trim().toLowerCase().replace(/ё/g, "е")
  return normalize(a) === normalize(b)
}

/** «вм.4 п.» в примечании позиции — какая пара в ней названа. */
const NOTE_PAIR_RE = /вм\.?\s*(\d{1,2})/i

/** Служебное слово «снять» — метка позиции снятия. */
const NOTE_REMOVE_RE = /(?:^|\s)снять(?=\s|$)/iu

/** Служебные слова, которые не должны оставаться в свободном тексте. */
/**
 * Служебные слова, которые не должны оставаться в свободном тексте. Вместе
 * с «вм.X» уходит и точка после него («вм.2 п.») — иначе в поле остаётся
 * одинокая точка от служебной надписи.
 */
const NOTE_SERVICE_RE =
  /сам[\s./\-]*р[\s.]*\+?|вм\.?\s*\d{1,2}\s*п?\.?|(?:^|\s)снять(?=\s|$)/giu

/** Убирает служебные слова из набранного текста — их задают переключатели. */
function stripServiceWords(text: string): string {
  return text
    .replace(NOTE_SERVICE_RE, " ")
    .replace(/\s+/g, " ")
    .trim()
}

/** «Предмет Преподаватель» одной строкой. */
function lessonLine(subject?: string | null, teacher?: string | null): string {
  return [subject, teacher].filter(Boolean).join(" ") || "—"
}

/**
 * Восстанавливает форму из сохранённой позиции. Служебные слова читаются из
 * примечания, поэтому форма показывает то же, что лежит в базе, независимо от
 * того, каким кодом позиция была создана.
 */
function draftFromPosition(position: CorrectionPosition): Draft {
  const groupId =
    position.groupId && position.groupId !== EMPTY_GUID ? position.groupId : ""
  const isRemove = position.changeType === "Remove"
  const note = position.note ?? ""
  const noteMatch = NOTE_PAIR_RE.exec(note)
  const notePair = noteMatch ? Number(noteMatch[1]) : null

  // Пара «откуда». У переноса это RemovedNumberPair, в новой модели — тоже он.
  const moveFrom =
    position.removedNumberPair != null &&
    position.removedNumberPair !== position.numberPair
      ? position.removedNumberPair
      : notePair

  const selfStudy: SelfStudyMode = isPhysicalSelfStudyNote(note)
    ? "physical"
    : isSelfStudyNote(note)
      ? "info"
      : "none"

  // Замена — это добавление, в котором снимается занятие выбранной пары. При
  // переносе снимается само вводимое занятие из пары «вм.X», поэтому слот
  // остаётся нетронутым.
  const replacesSlot =
    !isRemove
    && position.removedSubject != null
    && (position.removedNumberPair == null ||
      position.removedNumberPair === position.numberPair)

  return {
    operation: isRemove ? "remove" : "add",
    groupId,
    targetPair: position.numberPair,
    source:
      position.removedSubject && (isRemove || replacesSlot)
        ? {
            numberPair: position.numberPair,
            subject: position.removedSubject,
            teacherId: position.removedTeacherId ?? null,
            teacherName: position.removedTeacherName ?? null,
          }
        : null,
    slotMode: replacesSlot ? "replace" : "parallel",
    moveFrom,
    selfStudy,
    removeMark: NOTE_REMOVE_RE.test(note),
    teacherIds: position.teacherId ? [position.teacherId] : [],
    teacherNameHint: position.teacherName ?? "",
    subject: position.subject ?? "",
    note: stripServiceWords(note),
  }
}

interface CorrectionPositionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  batchId: string
  /** Дата корректировки в формате yyyy-MM-dd. */
  batchDate: string
  groups: GroupResponse[]
  /** Позиция для редактирования; null — создание новой. */
  position?: CorrectionPosition | null
  onSubmit: (payload: CreateCorrectionPosition) => Promise<void>
}

/**
 * Пошаговая форма позиции корректировки. Каждый шаг ограничивает следующий
 * справочником реальных данных (расписание группы, её преподаватели и их
 * предметы), поэтому ввести заведомо неверную позицию нельзя.
 */
export function CorrectionPositionDialog({
  open,
  onOpenChange,
  batchId,
  batchDate,
  groups,
  position,
  onSubmit,
}: CorrectionPositionDialogProps) {
  const editing = Boolean(position)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [refs, setRefs] = useState<CorrectionReferences | null>(null)
  const [loading, setLoading] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!open) return
    setDraft(position ? draftFromPosition(position) : emptyDraft())
    setRefs(null)
    setLoadError(null)
    setReloadKey((key) => key + 1)
  }, [open, position])

  const patch = (part: Partial<Draft>) =>
    setDraft((current) => ({ ...current, ...part }))

  const loadReferences = useCallback(() => {
    if (!draft.groupId) {
      setRefs(null)
      setLoadError(null)
      return
    }
    let cancelled = false
    setLoading(true)
    setLoadError(null)
    getCorrectionReferences({
      groupId: draft.groupId,
      date: batchDate,
      batchId,
      // Редактируемая позиция исключается: иначе её собственный слот
      // возвращается занятым и выбрать другой уже нельзя.
      excludePositionId: position?.id,
    })
      .then((data) => {
        if (cancelled) return
        setRefs(data)
        // Преподаватель и предмет берём из справочника группы, а не из всего
        // расписания: в позиции хранится один teacherId, а преподавателей может
        // быть несколько — восстанавливаем их по именам из teacherNameHint.
        setDraft((current) => {
          const byName = current.teacherNameHint
            .split("/")
            .map((name) => name.trim())
            .filter(Boolean)
            .flatMap((name) =>
              data.teachers
                .filter((teacher) => sameTeacherName(teacher.fullName, name))
                .map((teacher) => teacher.id),
            )
          const known = new Set(data.teachers.map((teacher) => teacher.id))
          const teacherIds = [
            ...new Set([...current.teacherIds, ...byName]),
          ].filter((id) => known.has(id))
          const subjects = subjectOptions(data.teachers, teacherIds)
          return {
            ...current,
            teacherIds,
            subject:
              current.subject && subjects.includes(current.subject)
                ? current.subject
                : "",
          }
        })
      })
      .catch((error) => {
        if (cancelled) return
        setRefs(null)
        setLoadError(
          extractErrorMessage(error) ??
            "Не удалось загрузить расписание группы",
        )
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [batchDate, batchId, draft.groupId, position?.id])

  useEffect(() => {
    if (!open) return
    const cleanup = loadReferences()
    return () => cleanup?.()
  }, [open, loadReferences, reloadKey])

  const group = useMemo(
    () => groups.find((item) => item.id === draft.groupId) ?? null,
    [groups, draft.groupId],
  )
  const teachers = useMemo(() => refs?.teachers ?? [], [refs])
  const entries = useMemo(() => refs?.entries ?? [], [refs])
  const subjectList = useMemo(
    () => subjectOptions(teachers, draft.teacherIds),
    [teachers, draft.teacherIds],
  )

  // В позиции хранится один преподаватель, им считается первый выбранный.
  const incomingTeacherName =
    teachers.find((teacher) => teacher.id === draft.teacherIds[0])?.fullName ??
    ""

  const isRemove = draft.operation === "remove"
  const informational = !isRemove && draft.selfStudy === "info"

  // Занятия выбранной пары, которые действительно стоят в расписании: пометки
  // «только информация» слот не занимают и заменяться не могут.
  const slotPhysical = useMemo(
    () =>
      draft.targetPair == null
        ? []
        : physicalEntriesForPair(entries, draft.targetPair),
    [entries, draft.targetPair],
  )
  // Занят ли слот: есть занятие, которого не трогает ни одна позиция пакета.
  // От выбранного занятия не зависит — иначе шаг «что делаем с парой» пропадал бы
  // сразу после выбора занятия для замены.
  const slotBusy = useMemo(
    () => slotPhysical.some((entry) => entry.pendingChangeType == null),
    [slotPhysical],
  )

  // «вм.X»: у вводимого преподавателя уже есть это же занятие в другой паре
  // этого дня. Предлагаем только такие пары — иначе перенос унёс бы чужое
  // занятие.
  const movePairs = useMemo(() => {
    const teacherId = draft.teacherIds[0]
    if (!teacherId || !draft.subject) return []
    return entries.filter(
      (entry) =>
        !entry.informational &&
        entry.pendingChangeType == null &&
        entry.teacherId === teacherId &&
        entry.numberPair !== draft.targetPair &&
        sameSubject(entry.subject, draft.subject),
    )
  }, [entries, draft.teacherIds, draft.subject, draft.targetPair])

  const fromEntry = useMemo(
    () =>
      draft.moveFrom == null
        ? null
        : (movePairs.find((entry) => entry.numberPair === draft.moveFrom) ??
          null),
    [movePairs, draft.moveFrom],
  )

  const sourceBusy = useMemo(
    () =>
      draft.source
        ? entries.find(
            (entry) =>
              entry.numberPair === draft.source?.numberPair &&
              entry.subject === draft.source?.subject,
          )?.pendingChangeType != null
        : false,
    [draft.source, entries],
  )

  const replacesSameLesson = useMemo(() => {
    if (!draft.source || isRemove) return false
    return (
      sameTeacherName(draft.source.teacherName ?? "", incomingTeacherName) &&
      sameSubject(draft.source.subject, draft.subject)
    )
  }, [draft.source, draft.subject, incomingTeacherName, isRemove])

  const blockedReason = useMemo(() => {
    if (!draft.groupId) return "Выберите группу"
    if (draft.targetPair == null) return "Выберите пару"

    if (isRemove) {
      if (slotPhysical.length === 0)
        return `В паре ${draft.targetPair} нет занятий`
      if (!draft.source)
        return "Выберите в карточке занятие, которое нужно снять"
      if (sourceBusy) return "На это занятие уже есть позиция в пакете"
      return null
    }

    if (draft.teacherIds.length === 0) return "Выберите преподавателя"
    if (!draft.subject) return "Выберите предмет"

    if (slotBusy && draft.slotMode === "replace") {
      if (!draft.source)
        return "Выберите занятие, которое заменяете — в шаге «Что делаем с парой»"
      if (sourceBusy) return "На это занятие уже есть позиция в пакете"
    }

    // Замена на то же самое занятие ничего не меняет: пара была бы удалена и
    // создана заново, потеряв аудиторию. Поэтому запрещаем в любом случае.
    if (replacesSameLesson)
      return "Нельзя заменить занятие на такое же — выберите другой предмет или преподавателя"

    // «вм.X» без занятия в паре X превратился бы в пустую пару.
    if (draft.moveFrom != null) {
      if (draft.moveFrom === draft.targetPair)
        return "Пара «откуда» должна отличаться от пары, в которую вводите"
      if (!fromEntry) return `В паре ${draft.moveFrom} нет занятия`
    }

    return null
  }, [
    draft.groupId,
    draft.targetPair,
    draft.source,
    draft.slotMode,
    draft.subject,
    draft.teacherIds.length,
    draft.moveFrom,
    isRemove,
    slotBusy,
    slotPhysical.length,
    sourceBusy,
    replacesSameLesson,
    fromEntry,
  ])

  /**
   * Примечание в том виде, в каком его понимает система: служебные слова из
   * отдельных полей плюс свободный текст.
   */
  const buildNote = useCallback(() => {
    const parts: string[] = []
    if (draft.note.trim()) parts.push(draft.note.trim())
    if (draft.removeMark) parts.push("снять")
    if (draft.selfStudy === "info") parts.push("сам.р.")
    if (draft.selfStudy === "physical") parts.push("сам.р+")
    if (draft.moveFrom != null) parts.push(`вм.${draft.moveFrom} п.`)
    return parts.length > 0 ? parts.join(" ") : null
  }, [draft.note, draft.removeMark, draft.selfStudy, draft.moveFrom])

  // Обе операции — это Add и Remove. Замена и перенос получаются из полей
  // позиции: снимаемое занятие в выбранной паре и пара «откуда».
  const payload = useMemo<CreateCorrectionPosition>(() => {
    const teacherNames = draft.teacherIds
      .map((id) => teachers.find((teacher) => teacher.id === id)?.fullName)
      .filter((name): name is string => Boolean(name))
    const isMove = draft.moveFrom != null && fromEntry != null
    const note = buildNote()

    return {
      changeType: isRemove ? "Remove" : "Add",
      groupId: draft.groupId,
      groupName: group?.name ?? "",
      numberPair: draft.targetPair ?? 1,
      subject: isRemove ? null : draft.subject,
      teacherId: isRemove ? null : (draft.teacherIds[0] ?? null),
      teacherName:
        isRemove || teacherNames.length === 0 ? null : teacherNames.join("/"),
      // Снятие — выбранное занятие. Перенос — вводимое занятие из пары «вм.X».
      // Замена — прежнее занятие из выбранной пары.
      removedSubject: isRemove
        ? (draft.source?.subject ?? null)
        : isMove
          ? (fromEntry?.subject ?? null)
          : draft.slotMode === "replace"
            ? (draft.source?.subject ?? null)
            : null,
      removedTeacherId: isRemove
        ? (draft.source?.teacherId ?? null)
        : isMove
          ? (fromEntry?.teacherId ?? null)
          : draft.slotMode === "replace"
            ? (draft.source?.teacherId ?? null)
            : null,
      removedTeacherName: isRemove
        ? (draft.source?.teacherName ?? null)
        : isMove
          ? (fromEntry?.teacherName ?? null)
          : draft.slotMode === "replace"
            ? (draft.source?.teacherName ?? null)
            : null,
      removedNumberPair: isRemove
        ? (draft.source?.numberPair ?? null)
        : isMove
          ? draft.moveFrom
          : null,
      note,
    }
  }, [buildNote, draft, fromEntry, group?.name, isRemove, teachers])

  /** Строка позиции в том виде, в каком она уйдёт в файл и в уведомление. */
  const preview = useMemo(() => toCorrectionRow(payload), [payload])

  const handleSubmit = async () => {
    if (blockedReason) {
      toast.error(blockedReason)
      return
    }

    setSaving(true)
    try {
      await onSubmit(payload)
      onOpenChange(false)
    } catch (error) {
      toast.error(
        extractErrorMessage(error) ??
          (editing
            ? "Не удалось сохранить позицию"
            : "Не удалось добавить позицию"),
      )
    } finally {
      setSaving(false)
    }
  }

  /** Итог позиции одной фразой — видно до сохранения. */
  const summary = useMemo(() => {
    if (blockedReason) return null
    const pair = draft.targetPair
    const newLesson = lessonLine(draft.subject, incomingTeacherName)

    if (isRemove)
      return `Пара ${pair}: снять «${lessonLine(draft.source?.subject, draft.source?.teacherName)}»`

    const parts = [
      informational
        ? `Пара ${pair}: «${newLesson}» — только информация, в расписание не встаёт`
        : `Пара ${pair}: добавить «${newLesson}»`,
    ]
    // Замена и перенос работают вместе с любой отметкой: они снимают занятия,
    // даже когда сама пара в расписание не встаёт.
    if (draft.slotMode === "replace" && draft.source)
      parts.push(
        informational
          ? `снимется «${lessonLine(draft.source.subject, draft.source.teacherName)}»`
          : `вместо «${lessonLine(draft.source.subject, draft.source.teacherName)}»`,
      )
    else if (slotBusy && !informational)
      parts.push("вторым занятием в паре")
    if (draft.moveFrom != null) parts.push(`пара ${draft.moveFrom} освободится`)
    return parts.join(" · ")
  }, [
    blockedReason,
    draft.targetPair,
    draft.subject,
    draft.slotMode,
    draft.source,
    draft.moveFrom,
    incomingTeacherName,
    isRemove,
    informational,
    slotBusy,
  ])

  const teacherStep = 4
  const subjectStep = 5
  const slotStep = 6
  // Шаг про занятие в паре появляется только когда слот занят, поэтому номер
  // примечания считаем так, чтобы нумерация не прерывалась.
  const showSlotStep = !isRemove && slotBusy
  const noteStep = isRemove ? 4 : showSlotStep ? 7 : 6

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      {/* Высота задана явно: иначе при переключении операции окно то выше,
          то ниже — содержимое прыгает под курсором. */}
      <DialogContent className="flex h-[85vh] flex-col sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {editing ? "Редактирование позиции" : "Новая позиция корректировки"}
          </DialogTitle>
          <DialogDescription>
            Позиция добавляется в пакет за {formatDate(batchDate)}. Операций две
            — добавить и снять; замена и перенос получаются сами: первая — из
            выбора занятия в паре, второй — из отметки «вм.X» в примечании.
          </DialogDescription>
        </DialogHeader>

        <div className="grid min-h-0 flex-1 gap-5 overflow-y-auto pr-1">
          <Step index={1} title="Операция">
            <div
              className="grid gap-2 sm:grid-cols-2"
              role="radiogroup"
              aria-label="Операция"
            >
              {OPERATION_CARDS.map((card) => (
                <button
                  key={card.value}
                  type="button"
                  role="radio"
                  aria-checked={draft.operation === card.value}
                  onClick={() =>
                    patch({
                      operation: card.value,
                      // Пара нужна обеим операциям — её выбор не сбрасываем.
                      source: card.value === "add" ? null : draft.source,
                      // Служебные отметки примечания относятся к добавлению.
                      selfStudy:
                        card.value === "add" ? draft.selfStudy : "none",
                      moveFrom: card.value === "add" ? draft.moveFrom : null,
                    })
                  }
                  className={cn(
                    "rounded-md border px-3 py-2 text-left transition-colors",
                    "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                    draft.operation === card.value
                      ? card.className
                      : "border-input bg-background hover:bg-muted",
                  )}
                >
                  <span className="block text-sm font-semibold">
                    {card.label}
                  </span>
                  <span className="block text-xs opacity-80">{card.hint}</span>
                </button>
              ))}
            </div>
          </Step>

          <Step index={2} title="Группа">
            <SearchableSelect
              id="position-group"
              aria-label="Группа"
              options={groups.map((item) => ({
                value: item.id,
                label: item.name,
              }))}
              value={draft.groupId}
              onValueChange={(groupId) =>
                patch({
                  groupId,
                  targetPair: null,
                  source: null,
                  moveFrom: null,
                  teacherIds: [],
                  teacherNameHint: "",
                  subject: "",
                })
              }
              placeholder="Выберите группу"
              searchPlaceholder="Поиск группы по названию"
              emptyLabel="Групп не найдено"
            />
          </Step>

          {draft.groupId && (
            <Step
              index={3}
              title="Пара"
              icon={<CalendarDays className="size-4" aria-hidden />}
            >
              {loading && (
                <p className="flex items-center gap-2 text-sm text-muted-foreground">
                  <LoaderCircle className="size-4 animate-spin" aria-hidden />
                  Загрузка расписания группы на {formatDate(batchDate)}…
                </p>
              )}
              {loadError && (
                <div
                  role="alert"
                  className="flex flex-wrap items-center gap-2 rounded-md border border-destructive/40 bg-destructive/5 px-3 py-2 text-sm text-destructive"
                >
                  <CircleAlert className="size-4 shrink-0" aria-hidden />
                  <span>{loadError}</span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setReloadKey((key) => key + 1)}
                  >
                    Повторить
                  </Button>
                </div>
              )}

              {!loading && !loadError && showSlotStep && (
                <p className="flex items-start gap-1.5 text-xs text-muted-foreground">
                  <CircleAlert
                    className="mt-0.5 size-3.5 shrink-0"
                    aria-hidden
                  />
                  Пара {draft.targetPair} занята. Что с ней будет — решается
                  ниже, отдельным шагом.
                </p>
              )}

              {!loading && !loadError && (
                <GroupDayCard
                  groupName={group?.name ?? ""}
                  dateLabel={formatDate(batchDate)}
                  entries={entries}
                  selectedPair={draft.targetPair}
                  onSelectPair={(numberPair) =>
                    patch({
                      targetPair: numberPair,
                      // Снимаемое занятие принадлежало прежней паре — в новой
                      // его может не быть.
                      source:
                        draft.source?.numberPair === numberPair
                          ? draft.source
                          : null,
                      moveFrom:
                        draft.moveFrom === numberPair ? null : draft.moveFrom,
                    })
                  }
                  // При снятии нужен не слот, а занятие: выбираем его прямо в
                  // карточке, чтобы не делать лишний шаг.
                  onSelectEntry={
                    isRemove
                      ? (entry) =>
                          patch({
                            targetPair: entry.numberPair,
                            source: {
                              numberPair: entry.numberPair,
                              subject: entry.subject,
                              teacherId: entry.teacherId,
                              teacherName: entry.teacherName,
                            },
                          })
                      : undefined
                  }
                  selectedEntryKey={
                    isRemove && draft.source
                      ? entryKey({
                          numberPair: draft.source.numberPair,
                          subject: draft.source.subject,
                          teacherName: draft.source.teacherName,
                        } as CorrectionDayEntry)
                      : null
                  }
                />
              )}
            </Step>
          )}

          {draft.groupId && !isRemove && (
            <Step
              index={teacherStep}
              title="Преподаватель"
              icon={<UserRound className="size-4" aria-hidden />}
            >
              {teachers.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  В системе нет преподавателей, ведущих занятия у этой группы.
                  Сначала загрузите расписание.
                </p>
              ) : (
                <>
                  <SearchableMultiSelect
                    id="position-teachers"
                    aria-label="Преподаватели"
                    options={teachers.map((teacher) => ({
                      value: teacher.id,
                      label: teacher.fullName,
                      description: teacher.subjects.join(", "),
                    }))}
                    values={draft.teacherIds}
                    onValuesChange={(teacherIds) =>
                      patch({
                        teacherIds,
                        // Подсказка нужна только для восстановления при открытии.
                        teacherNameHint: "",
                        subject: subjectOptions(teachers, teacherIds).includes(
                          draft.subject,
                        )
                          ? draft.subject
                          : "",
                        moveFrom: null,
                      })
                    }
                    placeholder="Преподаватели этой группы"
                    searchPlaceholder="Поиск по имени или предмету"
                    emptyLabel="Преподавателей не найдено"
                  />
                  <p className="text-xs text-muted-foreground">
                    Показаны только преподаватели, которые ведут занятия у
                    группы. Можно выбрать нескольких — они сохранятся через
                    слеш.
                  </p>
                </>
              )}
            </Step>
          )}

          {draft.groupId && !isRemove && (
            <Step
              index={subjectStep}
              title="Предмет"
              icon={<GraduationCap className="size-4" aria-hidden />}
            >
              {draft.teacherIds.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  Сначала выберите преподавателя — предметы берутся из его
                  нагрузки в этой группе.
                </p>
              ) : subjectList.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  У выбранных преподавателей нет предметов в этой группе.
                </p>
              ) : (
                <SearchableSelect
                  id="position-subject"
                  aria-label="Предмет"
                  options={subjectList.map((subject) => ({
                    value: subject,
                    label: subject,
                  }))}
                  value={draft.subject}
                  onValueChange={(subject) =>
                    patch({ subject, moveFrom: null })
                  }
                  placeholder="Предмет"
                  searchPlaceholder="Поиск предмета"
                  emptyLabel="Предметов не найдено"
                />
              )}
            </Step>
          )}

          {draft.groupId && showSlotStep && (
            <Step
              index={slotStep}
              title="Что делаем с занятием в паре"
              icon={<Layers className="size-4" aria-hidden />}
            >
              <div
                className="grid gap-2"
                role="radiogroup"
                aria-label="Действие"
              >
                <Choice
                  selected={draft.slotMode === "parallel"}
                  onSelect={() => patch({ slotMode: "parallel", source: null })}
                  label="Поставить в параллель"
                  hint="Прежнее занятие остаётся: в паре их станет два"
                />
                <Choice
                  selected={draft.slotMode === "replace"}
                  onSelect={() => patch({ slotMode: "replace" })}
                  label="Заменить"
                  hint="Прежнее занятие снимется, новое встанет на его место"
                />
              </div>

              {draft.slotMode === "replace" && (
                <div className="grid gap-2">
                  <p className="text-sm font-medium">
                    Какое занятие заменяем в паре {draft.targetPair}
                  </p>
                  <div
                    className="grid gap-1"
                    role="radiogroup"
                    aria-label="Занятие"
                  >
                    {slotPhysical.map((entry) => {
                      const key = entryKey(entry)
                      const selected =
                        draft.source != null &&
                        removedLessonKey(draft.source) === key
                      return (
                        <button
                          key={key}
                          type="button"
                          role="radio"
                          aria-checked={selected}
                          disabled={entry.pendingChangeType != null}
                          onClick={() =>
                            patch({
                              source: {
                                numberPair: entry.numberPair,
                                subject: entry.subject,
                                teacherId: entry.teacherId,
                                teacherName: entry.teacherName,
                              },
                            })
                          }
                          className={cn(
                            "rounded-md border px-3 py-2 text-left text-sm transition-colors",
                            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                            "disabled:cursor-not-allowed disabled:opacity-50",
                            selected
                              ? "border-primary bg-primary/10"
                              : "border-input bg-background hover:bg-muted",
                          )}
                        >
                          <span className="font-medium">{entry.subject}</span>
                          {entry.teacherName && (
                            <span className="ml-2 text-xs text-muted-foreground">
                              {entry.teacherName}
                            </span>
                          )}
                          {entry.pendingChangeType != null && (
                            <span className="ml-2 text-xs text-muted-foreground">
                              уже есть позиция в пакете
                            </span>
                          )}
                        </button>
                      )
                    })}
                  </div>
                </div>
              )}
            </Step>
          )}

          <Step index={noteStep} title="Примечание">
            <div className="grid gap-3">
              {isRemove ? (
                // У снятия своя служебная отметка: слово «снять» само пишется
                // в примечание, руками набирать не нужно.
                <div className="grid gap-1.5">
                  <span className="text-sm font-medium">Бейдж позиции</span>
                  <div className="flex flex-wrap items-center gap-2">
                    <ToggleChip
                      active={draft.removeMark}
                      onToggle={() => patch({ removeMark: !draft.removeMark })}
                      icon={<Minus className="size-3.5" aria-hidden />}
                      label="снять"
                    />
                    <span className="text-xs text-muted-foreground">
                      Слово само попадёт в примечание — руками писать не нужно
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Пару нужно снять полностью. Если пара должна остаться, но с
                    пометкой для студентов, добавьте занятие заново через
                    «Добавить» с отметкой «сам.р».
                  </p>
                </div>
              ) : (
                <>
                  {informational && slotBusy && (
                    <p className="flex items-start gap-1.5 rounded-md border border-violet-200 bg-violet-50 px-3 py-2 text-xs text-violet-800 dark:border-violet-900 dark:bg-violet-950/40 dark:text-violet-200">
                      <CircleAlert
                        className="mt-0.5 size-3.5 shrink-0"
                        aria-hidden
                      />
                      Пара {draft.targetPair} в расписание не встанет, а{" "}
                      {draft.slotMode === "replace" && draft.source
                        ? "прежнее занятие снимется"
                        : "перенос освободит указанную пару"}{" "}
                      — отметки работают вместе.
                    </p>
                  )}
                  <SelfStudyPicker
                    value={draft.selfStudy}
                    onChange={(value) => patch({ selfStudy: value })}
                  />
                </>
              )}

              {!isRemove && (
                <div className="grid gap-1.5">
                  <Label htmlFor="position-from-pair">
                    Перенос занятия из другой пары
                  </Label>
                  <NativeSelect
                    value={
                      draft.moveFrom == null ? "none" : String(draft.moveFrom)
                    }
                    onValueChange={(value) =>
                      patch({
                        moveFrom: value === "none" ? null : Number(value),
                      })
                    }
                    aria-label="Перенос из другой пары"
                  >
                    <NativeSelectItem value="none">
                      Без переноса — пара в выбранной просто появится
                    </NativeSelectItem>
                    {movePairs.map((entry) => (
                      <NativeSelectItem
                        key={entry.numberPair}
                        value={String(entry.numberPair)}
                      >
                        {`Из пары ${entry.numberPair} — освободится`}
                      </NativeSelectItem>
                    ))}
                  </NativeSelect>
                  <p className="text-xs text-muted-foreground">
                    {movePairs.length === 0
                      ? "Перенос возможен, если у этого преподавателя есть такое же занятие в другой паре этого дня — сейчас таких пар нет."
                      : "Перенос освободит указанную пару: преподаватель перестанет вести её и проведёт занятие в выбранной. Записывается в примечание как «вм.X»."}
                  </p>
                </div>
              )}

              <div className="grid gap-1.5">
                <Label htmlFor="position-note">Свободный текст</Label>
                <Input
                  id="position-note"
                  value={draft.note}
                  onChange={(event) => patch({ note: event.target.value })}
                  onBlur={() => patch({ note: stripServiceWords(draft.note) })}
                  placeholder="Необязательно"
                  disabled={saving}
                />
              </div>

              {buildNote() && (
                <Badge
                  variant="outline"
                  className="w-fit gap-1 bg-muted text-muted-foreground"
                >
                  <Lock className="size-3" aria-hidden />
                  {buildNote()}
                </Badge>
              )}
            </div>
          </Step>
        </div>

        {/* Предпросмотр строки: диспетчер видит позицию ровно в том виде, в
            каком она уйдёт в файл корректировки и в уведомление бота. */}
        <div className="shrink-0 rounded-md border bg-muted/30 px-3 py-2">
          <p className="mb-1.5 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
            Позиция в файле корректировки
          </p>
          <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-xs sm:grid-cols-4">
            <PreviewCell label="Группа" value={preview.groupName} />
            <PreviewCell
              label="Снимается"
              value={
                // При переносе в файле колонка пустая (занятие само переезжает),
                // поэтому показываем освобождаемую пару здесь.
                preview.removedSubject
                  ? [preview.removedSubject, preview.removedTeacher]
                      .filter(Boolean)
                      .join(" ")
                  : preview.movedFrom
                    ? `пара ${preview.movedFrom.pair} — ${preview.movedFrom.lesson}`
                    : ""
              }
            />
            <PreviewCell
              label="Вводится"
              value={[preview.addedSubject, preview.addedTeacher]
                .filter(Boolean)
                .join(" ")}
            />
            <PreviewCell
              label="Пара"
              value={preview.numberPair ? String(preview.numberPair) : ""}
            />
            <div className="col-span-2 sm:col-span-4">
              <PreviewCell label="Примечание" value={preview.note} />
            </div>
          </dl>
        </div>

        <DialogFooter>
          <span className="mr-auto self-center text-xs text-muted-foreground">
            {blockedReason ?? summary}
          </span>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={saving}
          >
            Отмена
          </Button>
          <Button
            onClick={() => void handleSubmit()}
            disabled={saving || blockedReason != null}
          >
            {saving ? (
              <>
                <LoaderCircle className="size-4 animate-spin" aria-hidden />
                Сохранение…
              </>
            ) : editing ? (
              "Сохранить изменения"
            ) : (
              "Добавить позицию"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

/** Ключ занятия: пара, предмет и преподаватель. */
function removedLessonKey(lesson: RemovedLesson): string {
  return `${lesson.numberPair}-${lesson.subject}-${lesson.teacherName ?? ""}`
}

function sameSubject(left: string, right: string): boolean {
  const normalize = (value: string) =>
    value.replace(/\s+/g, " ").trim().toLowerCase()
  return normalize(left) === normalize(right)
}

function subjectOptions(
  teachers: CorrectionGroupTeacher[],
  teacherIds: string[],
): string[] {
  const subjects = new Set<string>()
  for (const teacher of teachers) {
    if (!teacherIds.includes(teacher.id)) continue
    for (const subject of teacher.subjects) subjects.add(subject)
  }
  return [...subjects].sort((a, b) => a.localeCompare(b, "ru"))
}

function formatDate(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString("ru-RU")
}

function Step({
  index,
  title,
  icon,
  children,
}: {
  index: number
  title: string
  icon?: React.ReactNode
  children: React.ReactNode
}) {
  return (
    <section className="grid gap-2">
      <h3 className="flex items-center gap-2 text-sm font-semibold">
        <span
          className="flex size-5 shrink-0 items-center justify-center rounded-full bg-primary text-[11px] font-bold text-primary-foreground"
          aria-hidden
        >
          {index}
        </span>
        {icon}
        {title}
      </h3>
      {children}
    </section>
  )
}

/** Ячейка предпросмотра: подпись сверху, значение снизу. */
function PreviewCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid gap-0.5">
      <dt className="text-muted-foreground">{label}</dt>
      <dd
        // Значения короткие, но «пара N — Предмет Преподаватель» в тесной
        // колонке обрезался многоточием и читался как «преподаватель не указан» —
        // поэтому переносим, а не режем.
        className={cn("break-words font-medium", !value && "text-muted-foreground")}
        title={value || undefined}
      >
        {value || "—"}
      </dd>
    </div>
  )
}

/** Переключатель-бейдж служебного слова примечания. */
function ToggleChip({
  active,
  onToggle,
  icon,
  label,
}: {
  active: boolean
  onToggle: () => void
  icon: React.ReactNode
  label: string
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onToggle}
      className={cn(
        "inline-flex h-9 items-center gap-1.5 rounded-full border px-3 text-sm font-medium transition-colors",
        "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
        active
          ? "border-primary bg-primary text-primary-foreground"
          : "border-input bg-background text-muted-foreground hover:bg-muted",
      )}
    >
      {icon}
      {label}
    </button>
  )
}

/** Вариант ответа на вопрос «что делаем с занятием в паре». */
function Choice({
  selected,
  onSelect,
  label,
  hint,
}: {
  selected: boolean
  onSelect: () => void
  label: string
  hint: string
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      onClick={onSelect}
      className={cn(
        "rounded-md border px-3 py-2 text-left transition-colors",
        "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
        selected
          ? "border-primary bg-primary/10"
          : "border-input bg-background hover:bg-muted",
      )}
    >
      <span className="block text-sm font-semibold">{label}</span>
      <span className="block text-xs text-muted-foreground">{hint}</span>
    </button>
  )
}

/**
 * Самостоятельная работа двумя тумблерами: первый — «это сам.работа?», второй —
 * «ввести пару в расписание?». Оба выключены — обычная пара. Второй появляется
 * только когда первый включён.
 */
function SelfStudyPicker({
  value,
  onChange,
}: {
  value: SelfStudyMode
  onChange: (value: SelfStudyMode) => void
}) {
  const isSelfStudy = value !== "none"
  const isInSchedule = value === "physical"

  return (
    <div className="grid gap-2 rounded-md border px-3 py-2.5">
      <SelfStudyToggle
        id="self-study"
        label="Это самостоятельная работа?"
        hint="Пометить пару для студентов: они занимаются сами"
        checked={isSelfStudy}
        onCheckedChange={(checked) => onChange(checked ? "info" : "none")}
      />

      {isSelfStudy && (
        <SelfStudyToggle
          id="self-study-schedule"
          label="Ввести пару в расписание?"
          hint="Нет — пара останется только пометкой, в базу расписания не встанет"
          checked={isInSchedule}
          onCheckedChange={(checked) => onChange(checked ? "physical" : "info")}
        />
      )}

      <p className="text-xs text-muted-foreground">
        {isSelfStudy
          ? isInSchedule
            ? "В примечание попадёт «сам.р+» — пара встанет в расписание с пометкой."
            : "В примечание попадёт «сам.р» — пара не встаёт в расписание, нужна только пометка."
          : "Обычная пара: в примечание не попадёт ничего."}
      </p>
    </div>
  )
}

function SelfStudyToggle({
  id,
  label,
  hint,
  checked,
  onCheckedChange,
}: {
  id: string
  label: string
  hint: string
  checked: boolean
  onCheckedChange: (checked: boolean) => void
}) {
  return (
    <div className="flex items-start gap-3">
      <Switch
        id={id}
        checked={checked}
        onCheckedChange={onCheckedChange}
        aria-label={label}
      />
      <Label htmlFor={id} className="grid cursor-pointer gap-0.5">
        <span className="text-sm font-medium">{label}</span>
        <span className="text-xs font-normal text-muted-foreground">{hint}</span>
      </Label>
    </div>
  )
}
