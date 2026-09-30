"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import {
  BookOpen,
  CalendarDays,
  CircleAlert,
  GraduationCap,
  LoaderCircle,
  Lock,
  Minus,
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
import { NativeSelect, NativeSelectItem } from "@/components/ui/native-select"
import RemovePairPicker, {
  type RemovedPairSelection,
} from "@/components/RemovePairPicker"
import GroupDayCard, {
  entryKey,
  occupiedEntryFor,
} from "@/components/GroupDayCard"
import { isSelfStudyNote } from "@/lib/change-tags"
import {
  SearchableMultiSelect,
  SearchableSelect,
} from "@/components/SearchableSelect"

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

/**
 * Три операции. Переноса среди них нет: он получается сам — «вм.X» в
 * примечании к добавлению или замене. X выбирается руками, потому что в
 * файлах корректировок он означает конкретную пару, «откуда» берётся занятие.
 */
const CHANGE_TYPE_CARDS: {
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
    value: "replace",
    label: "Заменить",
    hint: "Вместо одного занятия в паре — другое",
    className:
      "border-blue-300 bg-blue-50 text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-200",
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
type CorrectionOperation = "add" | "replace" | "remove"

const MAX_PAIR = 8

interface Draft {
  /** Операция: добавить, заменить или снять. */
  operation: CorrectionOperation
  groupId: string
  /** Пара, в которую вводится занятие (добавление и замена). */
  targetPair: number | null
  /** Занятие, которое снимают (снятие и замена). */
  source: RemovedPairSelection | null
  /**
   * «вм.X» — пара, откуда берётся вводимое занятие. С ней добавление или
   * замена превращается в перенос: пара X освобождается. null — переноса нет.
   */
  fromPair: number | null
  /** Служебное слово «сам.р.»: пара помечается, но не удаляется. */
  selfStudy: boolean
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
    fromPair: null,
    selfStudy: false,
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

/**
 * Восстанавливает форму из сохранённой позиции.
 * «вм.X» читается из примечания — так форма показывает то же, что лежит в
 * базе, независимо от того, каким кодом позиция была создана.
 */
function draftFromPosition(position: CorrectionPosition): Draft {
  const groupId =
    position.groupId && position.groupId !== EMPTY_GUID ? position.groupId : ""
  const isRemove = position.changeType === "Remove"
  const isMove = position.changeType === "Move"
  const isReplace = position.changeType === "Replace"

  const noteMatch = NOTE_PAIR_RE.exec(position.note ?? "")
  const notePair = noteMatch ? Number(noteMatch[1]) : null

  // Откуда берётся вводимое занятие. У переноса это и есть RemovedNumberPair;
  // у замены с «вм.X» — тоже, а вот снимаемое занятие лежит в паре Y.
  const fromPair = isMove
    ? (position.removedNumberPair ?? notePair)
    : isReplace && notePair != null
      ? notePair
      : null

  const source: RemovedPairSelection | null = position.removedSubject
    ? {
        numberPair: isMove
          ? (position.removedNumberPair ?? position.numberPair)
          : position.numberPair,
        removedSubject: position.removedSubject,
        removedTeacherId: position.removedTeacherId ?? null,
        removedTeacherName: position.removedTeacherName ?? null,
      }
    : null

  return {
    operation: isRemove ? "remove" : isReplace ? "replace" : "add",
    groupId,
    // Пара нужна всем операциям, в том числе снятию.
    targetPair: position.numberPair,
    source: isRemove || isReplace ? source : null,
    fromPair,
    selfStudy: isSelfStudyNote(position.note),
    teacherIds: position.teacherId ? [position.teacherId] : [],
    teacherNameHint: position.teacherName ?? "",
    subject: position.subject ?? "",
    note: (position.note ?? "").replace(NOTE_PAIR_RE, "").trim(),
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
 * справочником реальных данных (расписание группы, её преподаватели и их предметы),
 * поэтому ввести заведомо неверную позицию нельзя.
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
      // возвращается занятым и перенести пару на другой слот уже нельзя.
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
          extractErrorMessage(error) ?? "Не удалось загрузить расписание группы",
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

  const isRemove = draft.operation === "remove"
  const isReplace = draft.operation === "replace"
  // Занятие в паре выбирается и при снятии, и при замене: пара уже выбрана
  // карточкой дня, поэтому список ограничен ею. Преподаватели и предмет нужны
  // только когда вводится занятие. Номера шагов считаем явно, иначе после
  // пропущенного шага нумерация выглядит как пропавшие поля.
  const needsSource = isRemove || isReplace
  const pairEntries = useMemo(
    () =>
      draft.targetPair == null
        ? []
        : entries.filter((entry) => entry.numberPair === draft.targetPair),
    [entries, draft.targetPair],
  )
  const teacherStep = 4
  const subjectStep = 5
  const noteStep = isRemove ? 4 : 6

  // Занятие, «откуда» берётся вводимое при «вм.X».
  const fromEntry =
    draft.fromPair != null ? occupiedEntryFor(entries, draft.fromPair) : null

  const sourceBusy = useMemo(
    () =>
      draft.source
        ? entries.find(
            (entry) =>
              entry.numberPair === draft.source?.numberPair &&
              entry.subject === draft.source?.removedSubject,
          )?.pendingChangeType != null
        : false,
    [draft.source, entries],
  )

  const blockedReason = useMemo(() => {
    if (!draft.groupId) return "Выберите группу"
    if (draft.targetPair == null) return "Выберите пару"

    if (needsSource) {
      if (pairEntries.length === 0)
        return `В паре ${draft.targetPair} нет занятий`
      if (!draft.source)
        return isReplace
          ? "Выберите в карточке занятие, которое снимаете"
          : "Выберите в карточке занятие, которое нужно снять"
      if (sourceBusy) return "На это занятие уже есть позиция в пакете"
      if (isRemove) return null
    }

    if (draft.teacherIds.length === 0) return "Выберите преподавателя"
    if (!draft.subject) return "Выберите предмет"

    // «вм.X» без занятия в паре X превратился бы в пустую пару.
    if (draft.fromPair != null) {
      if (draft.fromPair === draft.targetPair)
        return "Пара «откуда» должна отличаться от пары, в которую вводите"
      if (!fromEntry) return `В паре ${draft.fromPair} нет занятия`
    }

    return null
  }, [
    draft.groupId,
    isRemove,
    isReplace,
    needsSource,
    draft.source,
    draft.targetPair,
    draft.fromPair,
    draft.subject,
    draft.teacherIds.length,
    pairEntries.length,
    fromEntry,
    sourceBusy,
  ])

  /**
   * Примечание в том виде, в каком его понимает система: «вм.X» из
   * отдельного поля и «сам.р.» из переключателя, плюс свободный текст.
   */
  const buildNote = useCallback(() => {
    const parts: string[] = []
    if (draft.note.trim()) parts.push(draft.note.trim())
    if (draft.selfStudy) parts.push("сам.р.")
    if (draft.fromPair != null) parts.push(`вм.${draft.fromPair} п.`)
    return parts.length > 0 ? parts.join(" ") : null
  }, [draft.note, draft.selfStudy, draft.fromPair])

  const handleSubmit = async () => {
    if (blockedReason) {
      toast.error(blockedReason)
      return
    }
    const teacherNames = draft.teacherIds
      .map((id) => teachers.find((teacher) => teacher.id === id)?.fullName)
      .filter((name): name is string => Boolean(name))

    // Две операции превращаются в тип позиции:
    //   добавить в свободный слот           → Add
    // Тип позиции выводится из операции и примечания «вм.X».
    //   Снять                → Remove
    //   Добавить             → Add   (в паре Y просто второе занятие)
    //   Добавить + вм.X      → Move  (пара X освобождается — это перенос)
    //   Заменить             → Replace (снимаем выбранное из пары Y)
    //   Заменить + вм.X      → Replace с RemovedNumberPair = X:
    //                           движок дополнительно снимает вводимое занятие
    //                           из пары X — это замена с переносом.
    const moved = draft.fromPair != null && fromEntry != null
    const note = buildNote()

    const payload: CreateCorrectionPosition = {
      changeType: isRemove
        ? "Remove"
        : isReplace
          ? "Replace"
          : moved
            ? "Move"
            : "Add",
      groupId: draft.groupId,
      groupName: group?.name ?? "",
      numberPair: isRemove ? (draft.source?.numberPair ?? 1) : (draft.targetPair ?? 1),
      subject: isRemove ? null : draft.subject,
      teacherId: isRemove ? null : (draft.teacherIds[0] ?? null),
      teacherName:
        isRemove || teacherNames.length === 0 ? null : teacherNames.join("/"),
      // Снятие — выбранное занятие. Перенос — занятие из пары «вм.X».
      // Замена — выбранное занятие из пары Y.
      removedSubject: isRemove
        ? (draft.source?.removedSubject ?? null)
        : moved
          ? fromEntry?.subject
          : isReplace
            ? (draft.source?.removedSubject ?? null)
            : null,
      removedTeacherId: isRemove
        ? (draft.source?.removedTeacherId ?? null)
        : moved
          ? fromEntry?.teacherId
          : isReplace
            ? (draft.source?.removedTeacherId ?? null)
            : null,
      removedTeacherName: isRemove
        ? (draft.source?.removedTeacherName ?? null)
        : moved
          ? fromEntry?.teacherName
          : isReplace
            ? (draft.source?.removedTeacherName ?? null)
            : null,
      removedNumberPair: isRemove
        ? (draft.source?.numberPair ?? null)
        : moved
          ? draft.fromPair
          : isReplace
            ? (draft.targetPair ?? null)
            : null,
      note,
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
            Позиция добавляется в пакет за {formatDate(batchDate)}. Порядок
            шагов одинаковый у всех операций: сначала пара в карточке дня.
            Перенос отдельной операцией не нужен — он получается сам из
            примечания.
          </DialogDescription>
        </DialogHeader>

        <div className="grid min-h-0 flex-1 gap-5 overflow-y-auto pr-1">
          <Step index={1} title="Операция">
            <div
              className="grid gap-2 sm:grid-cols-2"
              role="radiogroup"
              aria-label="Операция"
            >
              {CHANGE_TYPE_CARDS.map((card) => (
                <button
                  key={card.value}
                  type="button"
                  role="radio"
                  aria-checked={draft.operation === card.value}
                  onClick={() =>
                    patch({
                      operation: card.value,
                      // Пара нужна всем трём операциям — её выбор не сбрасываем.
                      // Снятое занятие нужно снятию и замене, но не добавлению.
                      source: card.value === "add" ? null : draft.source,
                      // «вм.X» переносит занятие, поэтому теряет смысл, если
                      // операция больше не вводит занятие.
                      fromPair: card.value === "remove" ? null : draft.fromPair,
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
                  <span className="block text-sm font-semibold">{card.label}</span>
                  <span className="block text-xs opacity-80">{card.hint}</span>
                </button>
              ))}
            </div>
          </Step>

          <Step index={2} title="Группа">
            <SearchableSelect
              id="position-group"
              aria-label="Группа"
              options={groups.map((item) => ({ value: item.id, label: item.name }))}
              value={draft.groupId}
              onValueChange={(groupId) =>
                patch({
                  groupId,
                  targetPair: null,
                  source: null,
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

              {!loading && !loadError && (
                <GroupDayCard
                  groupName={group?.name ?? ""}
                  dateLabel={formatDate(batchDate)}
                  entries={entries}
                  selectedPair={draft.targetPair}
                  onSelectPair={(numberPair) =>
                    patch({
                      targetPair: numberPair,
                      // Снятое занятие принадлежало прежней паре — в новой
                      // его может не быть.
                      source:
                        draft.source?.numberPair === numberPair
                          ? draft.source
                          : null,
                    })
                  }
                  // При снятии и замене нужен не слот, а занятие: выбираем его
                  // прямо в карточке, чтобы не делать лишний шаг.
                  onSelectEntry={
                    needsSource
                      ? (entry) =>
                          patch({
                            targetPair: entry.numberPair,
                            source: {
                              numberPair: entry.numberPair,
                              removedSubject: entry.subject,
                              removedTeacherId: entry.teacherId,
                              removedTeacherName: entry.teacherName,
                            },
                          })
                      : undefined
                  }
                  selectedEntryKey={
                    draft.source
                      ? entryKey({
                          numberPair: draft.source.numberPair,
                          subject: draft.source.removedSubject,
                          teacherName: draft.source.removedTeacherName,
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
              title="Преподаватели"
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
                      })
                    }
                    placeholder="Преподаватели этой группы"
                    searchPlaceholder="Поиск по имени или предмету"
                    emptyLabel="Преподавателей не найдено"
                  />
                  <p className="text-xs text-muted-foreground">
                    Показаны только преподаватели, которые ведут занятия у группы.
                    Можно выбрать нескольких — они сохранятся через слеш.
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
                  Сначала выберите преподавателя — предметы берутся из его нагрузки
                  в этой группе.
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
                  onValueChange={(subject) => patch({ subject })}
                  placeholder="Предмет"
                  searchPlaceholder="Поиск предмета"
                  emptyLabel="Предметов не найдено"
                />
              )}
            </Step>
          )}

          <Step index={noteStep} title="Примечание">
            <div className="grid gap-3">
              {isRemove ? (
                // Снятие всегда убирает пару, поэтому отдельной отметки здесь
                // нет — показываем, что произойдёт с парой.
                <div className="grid gap-1.5">
                  <span className="text-sm font-medium">Что будет с парой</span>
                  <Badge
                    variant="outline"
                    className="w-fit gap-1 border-red-300 bg-red-50 text-red-800 dark:border-red-900 dark:bg-red-950/40 dark:text-red-200"
                  >
                    <Minus className="size-3" aria-hidden />
                    Снять — пара уйдёт из расписания
                  </Badge>
                  <p className="text-xs text-muted-foreground">
                    Пару нужно снять полностью. Если пара должна остаться, но с
                    отметкой для студентов, добавьте занятие заново через
                    «Добавить».
                  </p>
                </div>
              ) : (
                <div className="grid gap-1.5">
                  <span className="text-sm font-medium">Служебные отметки</span>
                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      aria-pressed={draft.selfStudy}
                      onClick={() => patch({ selfStudy: !draft.selfStudy })}
                      className={cn(
                        "inline-flex h-9 items-center gap-1.5 rounded-full border px-3 text-sm font-medium transition-colors",
                        "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                        draft.selfStudy
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-input bg-background text-muted-foreground hover:bg-muted",
                      )}
                    >
                      <BookOpen className="size-3.5" aria-hidden />
                      сам.р.
                    </button>
                    <span className="text-xs text-muted-foreground">
                      Пара остаётся в расписании и помечается для студентов
                    </span>
                  </div>
                </div>
              )}

              {!isRemove && (
                <div className="grid gap-1.5">
                  <Label htmlFor="position-from-pair">Перенос</Label>
                  <NativeSelect
                    value={
                      draft.fromPair == null ? "none" : String(draft.fromPair)
                    }
                    onValueChange={(value) =>
                      patch({ fromPair: value === "none" ? null : Number(value) })
                    }
                    aria-label="Перенос из другой пары"
                  >
                    <NativeSelectItem value="none">
                      Без переноса
                    </NativeSelectItem>
                    {Array.from({ length: MAX_PAIR }, (_, i) => i + 1)
                      .filter((n) => n !== draft.targetPair)
                      .map((n) => {
                        const from = occupiedEntryFor(entries, n)
                        return (
                          <NativeSelectItem key={n} value={String(n)}>
                            {`Из пары ${n} — ${from ? from.subject : "пусто"}`}
                          </NativeSelectItem>
                        )
                      })}
                  </NativeSelect>
                  <p className="text-xs text-muted-foreground">
                    Занятие переносится из указанной пары в ту, что выбрана
                    выше. Без переноса оно осталось бы в обеих.
                  </p>
                </div>
              )}

              <div className="grid gap-1.5">
                <Label htmlFor="position-note">Свободный текст</Label>
                <Input
                  id="position-note"
                  value={draft.note}
                  onChange={(event) => patch({ note: event.target.value })}
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

        <DialogFooter>
          <span className="mr-auto self-center text-xs text-muted-foreground">
            {blockedReason ?? "Позиция готова к сохранению"}
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
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleDateString("ru-RU")
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
