"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import {
  CalendarDays,
  CircleAlert,
  GraduationCap,
  LoaderCircle,
  Lock,
  UserRound,
} from "lucide-react"
import { getCorrectionReferences } from "@/api/correction"
import type {
  CorrectionChangeType,
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
import { NoteChips, NOTE_CHIPS } from "@/components/NoteChips"
import RemovePairPicker, {
  type RemovedPairSelection,
} from "@/components/RemovePairPicker"
import GroupDayCard, {
  isPairOccupied,
  type MovedLesson,
} from "@/components/GroupDayCard"
import {
  SearchableMultiSelect,
  SearchableSelect,
} from "@/components/SearchableSelect"

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

const CHANGE_TYPE_CARDS: {
  value: CorrectionChangeType
  label: string
  hint: string
  className: string
}[] = [
  {
    value: "Add",
    label: "Добавлено",
    hint: "Новое занятие в свободную пару",
    className:
      "border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-200",
  },
  {
    value: "Remove",
    label: "Снято",
    hint: "Занятие убирается из пары",
    className:
      "border-red-300 bg-red-50 text-red-800 dark:border-red-900 dark:bg-red-950/40 dark:text-red-200",
  },
  {
    value: "Replace",
    label: "Замена",
    hint: "Вместо одного занятия — другое",
    className:
      "border-blue-300 bg-blue-50 text-blue-800 dark:border-blue-900 dark:bg-blue-950/40 dark:text-blue-200",
  },
  {
    value: "Move",
    label: "Перенос",
    hint: "Занятие переносится в другую пару",
    className:
      "border-amber-300 bg-amber-50 text-amber-900 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200",
  },
]

interface Draft {
  changeType: CorrectionChangeType
  groupId: string
  /** Пара назначения: свободная — для добавления и переноса. */
  targetPair: number | null
  /** Снимаемое/заменяемое/переносимое занятие. */
  source: RemovedPairSelection | null
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
    changeType: "Add",
    groupId: "",
    targetPair: null,
    source: null,
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

function draftFromPosition(position: CorrectionPosition): Draft {
  const groupId =
    position.groupId && position.groupId !== EMPTY_GUID ? position.groupId : ""
  const source: RemovedPairSelection | null = position.removedSubject
    ? {
        // Для замены пара одна и та же, для переноса — берём исходную.
        numberPair:
          position.changeType === "Move"
            ? (position.removedNumberPair ?? position.numberPair)
            : position.numberPair,
        removedSubject: position.removedSubject,
        removedTeacherId: position.removedTeacherId ?? null,
        removedTeacherName: position.removedTeacherName ?? null,
      }
    : null

  return {
    changeType: position.changeType,
    groupId,
    targetPair:
      position.changeType === "Add" || position.changeType === "Move"
        ? position.numberPair
        : null,
    source,
    teacherIds: position.teacherId ? [position.teacherId] : [],
    teacherNameHint: position.teacherName ?? "",
    subject: position.subject ?? "",
    note: position.note ?? "",
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

  const needsSource = draft.changeType !== "Add"
  // «Преподаватели» и «Предмет» есть не у всех операций. Номера шагов считаем
  // явно, иначе у «Снято» после третьего шага сразу шёл шестой — выглядит
  // как пропавшие поля.
  const showLessonSteps = draft.changeType !== "Remove"
  const subjectStep = showLessonSteps ? 5 : 4
  const noteStep = showLessonSteps ? 6 : 4
  // «Замена» меняет занятие в той же паре, поэтому отдельный выбор пары не нужен.
  const needsFreeTarget =
    draft.changeType === "Add" || draft.changeType === "Move"
  const sourcePair = draft.source?.numberPair ?? null
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
    if (needsSource) {
      if (!draft.source) {
        return draft.changeType === "Remove"
          ? "Выберите занятие, которое нужно снять"
          : "Выберите занятие, которое заменяете или переносите"
      }
      if (sourceBusy) return "На это занятие уже есть позиция в пакете"
    }
    if (needsFreeTarget && draft.targetPair == null)
      return "Выберите пару для занятия"
    if (draft.changeType !== "Remove") {
      if (draft.teacherIds.length === 0) return "Выберите преподавателя"
      if (!draft.subject) return "Выберите предмет"
    }
    return null
  }, [
    draft.groupId,
    draft.changeType,
    draft.source,
    draft.targetPair,
    draft.subject,
    draft.teacherIds.length,
    needsSource,
    needsFreeTarget,
    sourceBusy,
  ])

  // Занятота слота назначения: пара не блокируется, а просто будет второй
  // в этом слоте. Предупреждаем, чтобы это не было сюрпризом.
  const targetOccupied =
    draft.targetPair != null && isPairOccupied(entries, draft.targetPair)

  // Занятие, выбранное для переноса. Отдельного списка нет: перенос делается
  // прямо в карточке дня.
  const movedLesson = useMemo<MovedLesson | null>(() => {
    if (draft.changeType !== "Move" || !draft.source) return null
    const pair = draft.source.numberPair
    const entry = entries.find(
      (item) =>
        item.numberPair === pair && item.subject === draft.source?.removedSubject,
    )
    if (!entry) return null
    return { entry, numberPair: pair }
  }, [draft.changeType, draft.source, entries])

  const selectLesson = useCallback(
    (lesson: MovedLesson | null) => {
      if (!lesson) {
        patch({ source: null, targetPair: null })
        return
      }
      patch({
        source: {
          numberPair: lesson.numberPair,
          removedSubject: lesson.entry.subject,
          removedTeacherId: lesson.entry.teacherId,
          removedTeacherName: lesson.entry.teacherName,
        },
        targetPair: null,
      })
    },
    [],
  )

  const patchSource = useCallback(
    (source: RemovedPairSelection | null) =>
      patch({
        source,
        // Пара назначения не должна совпадать с исходной.
        targetPair:
          needsFreeTarget && source && draft.targetPair === source.numberPair
            ? null
            : draft.targetPair,
      }),
    [needsFreeTarget, draft.targetPair],
  )

  const handleSubmit = async () => {
    if (blockedReason) {
      toast.error(blockedReason)
      return
    }
    const teacherNames = draft.teacherIds
      .map((id) => teachers.find((teacher) => teacher.id === id)?.fullName)
      .filter((name): name is string => Boolean(name))

    const isRemove = draft.changeType === "Remove"

    // Тип позиции сохраняется ровно тот, что выбрал диспетчер. Раньше
    // «Добавлено» в занятую пару молча превращалось в «Замену» и сносило
    // существующее занятие — но по правилам в слоте может быть несколько пар.
    const payload: CreateCorrectionPosition = {
      changeType: draft.changeType,
      groupId: draft.groupId,
      groupName: group?.name ?? "",
      numberPair: needsFreeTarget
        ? (draft.targetPair ?? 1)
        : (draft.source?.numberPair ?? 1),
      subject: isRemove ? null : draft.subject,
      teacherId: draft.teacherIds[0] ?? null,
      teacherName:
        isRemove || teacherNames.length === 0 ? null : teacherNames.join("/"),
      removedSubject: needsSource ? (draft.source?.removedSubject ?? null) : null,
      removedTeacherId: needsSource
        ? (draft.source?.removedTeacherId ?? null)
        : null,
      removedTeacherName: needsSource
        ? (draft.source?.removedTeacherName ?? null)
        : null,
      removedNumberPair: needsSource ? (draft.source?.numberPair ?? null) : null,
      note: draft.note.trim() || null,
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
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {editing ? "Редактирование позиции" : "Новая позиция корректировки"}
          </DialogTitle>
          <DialogDescription>
            Позиция добавляется в пакет за {formatDate(batchDate)}. Шаги идут
            сверху вниз: каждый следующий список ограничен реальными данными
            группы, поэтому ошибиться выбором нельзя.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-5">
          <Step index={1} title="Тип операции">
            <div
              className="grid gap-2 sm:grid-cols-2"
              role="radiogroup"
              aria-label="Тип операции"
            >
              {CHANGE_TYPE_CARDS.map((card) => (
                <button
                  key={card.value}
                  type="button"
                  role="radio"
                  aria-checked={draft.changeType === card.value}
                  onClick={() =>
                    patch({
                      changeType: card.value,
                      // Тип операции меняет смысл выбранной позиции — сбрасываем выбор.
                      targetPair:
                        card.value === "Add" || card.value === "Move"
                          ? draft.targetPair
                          : null,
                      source: card.value === "Add" ? null : draft.source,
                      note: /^вм\.\d+$/.test(draft.note.trim())
                        ? ""
                        : draft.note,
                    })
                  }
                  className={cn(
                    "rounded-md border px-3 py-2 text-left transition-colors",
                    "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
                    draft.changeType === card.value
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
              title="Позиция"
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
                <div className="grid gap-4">
                  {draft.changeType === "Remove" ? (
                    <div className="grid gap-1.5">
                      <span className="text-sm font-medium">
                        Снимаемое занятие
                      </span>
                      <RemovePairPicker
                        value={draft.source}
                        onChange={patchSource}
                        entries={entries}
                      />
                      <p className="text-xs text-muted-foreground">
                        Без примечания «сам.р.» пара снимается с расписания;
                        с ним — остаётся и помечается для студентов.
                      </p>
                    </div>
                  ) : draft.changeType === "Replace" ? (
                    // У замены слот не меняется, поэтому карточка дня с
                    // некликабельными рядами была лишней: выбираем только
                    // заменяемое занятие из списка дня.
                    <div className="grid gap-1.5">
                      <span className="text-sm font-medium">
                        Заменяемое занятие
                      </span>
                      <RemovePairPicker
                        value={draft.source}
                        onChange={patchSource}
                        entries={entries}
                      />
                      <p className="text-xs text-muted-foreground">
                        Новое занятие встанет в ту же пару {draft.source?.numberPair ?? "—"}.
                      </p>
                    </div>
                  ) : (
                    // Добавление и перенос: карточка дня — единственный
                    // выбор. Для переноса занятие перетаскивается на строку.
                    <GroupDayCard
                      groupName={group?.name ?? ""}
                      dateLabel={formatDate(batchDate)}
                      entries={entries}
                      selectedPair={draft.targetPair}
                      onSelectPair={(numberPair) => patch({ targetPair: numberPair })}
                      movedLesson={
                        draft.changeType === "Move" ? movedLesson : null
                      }
                      onSelectLesson={
                        draft.changeType === "Move"
                          ? selectLesson
                          : undefined
                      }
                      selectable
                    />
                  )}

                  {targetOccupied && (
                    <p className="rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
                      В паре {draft.targetPair} уже есть занятие — новое встанет
                      в неё вторым. Если нужно заменить, снимите старое или
                      выберите тип «Замена».
                    </p>
                  )}
                </div>
              )}
            </Step>
          )}

          {draft.groupId && draft.changeType !== "Remove" && (
            <Step
              index={4}
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

          {draft.groupId && draft.changeType !== "Remove" && (
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
            <Input
              value={draft.note}
              onChange={(event) => patch({ note: event.target.value })}
              placeholder="Необязательно"
              aria-label="Примечание"
            />
            <NoteChips
              value={draft.note}
              onChange={(note) => patch({ note })}
              className="mt-2"
            />
            <p className="mt-2 text-xs text-muted-foreground">
              «{NOTE_CHIPS.join(", ")}» — служебное слово: пара остаётся в
              расписании и помечается для студентов. Остальной текст — свободное
              примечание.
            </p>
            {draft.changeType === "Move" && sourcePair != null && (
              <Badge
                variant="outline"
                className="mt-2 w-fit gap-1 bg-muted text-muted-foreground"
              >
                <Lock className="size-3" aria-hidden />
                вм.{sourcePair} — подставится автоматически
              </Badge>
            )}
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
