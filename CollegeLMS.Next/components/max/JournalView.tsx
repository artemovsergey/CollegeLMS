"use client"

import { useCallback, useEffect, useState } from "react"
import type { LucideIcon } from "lucide-react"
import {
  BookOpen,
  Plus,
  Minus,
  Repeat,
  ArrowRightLeft,
} from "lucide-react"
import {
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { fetchJournal } from "@/api/schedule"
import type { JournalResponse, JournalSubjectGroup } from "@/api/schedule"
import { useMaxContext } from "@/lib/max-context"
import { dayLabelFromInt } from "@/lib/max-lesson"
import ScheduleError from "@/components/max/ScheduleError"
import ScheduleEmpty from "@/components/max/ScheduleEmpty"

const CHANGE_BADGE: Record<
  string,
  { label: string; className: string; icon: LucideIcon }
> = {
  Add: { label: "Добавлено", className: "max-app__badge--add", icon: Plus },
  Replace: { label: "Замена", className: "max-app__badge--replace", icon: Repeat },
  Move: { label: "Перенос", className: "max-app__badge--move", icon: ArrowRightLeft },
  Remove: { label: "Снято", className: "max-app__badge--remove", icon: Minus },
  SelfStudy: {
    label: "Сам.р.",
    className: "max-app__badge--selfstudy",
    icon: BookOpen,
  },
}

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString("ru-RU", { day: "2-digit", month: "2-digit" })
}

// Карточка журнала — пара «группа + предмет», поэтому ключ составной.
function journalCardKey(
  group: Pick<JournalSubjectGroup, "groupId" | "subject">,
): string {
  return `${group.groupId}::${group.subject}`
}

function JournalBadges({ types }: { types: string[] }) {
  if (!types || types.length === 0) return null
  return (
    <span className="max-app__badges-row">
      {types.map((type) => {
        const meta = CHANGE_BADGE[type] ?? {
          label: type,
          className: "max-app__badge--replace",
          icon: BookOpen,
        }
        const Icon = meta.icon
        return (
          <span
            key={type}
            className={`max-app__badge ${meta.className}`}
          >
            <Icon size={12} aria-hidden /> {meta.label}
          </span>
        )
      })}
    </span>
  )
}

export default function JournalView() {
  const { isAuthed, profile, viewContext } = useMaxContext()
  const [journal, setJournal] = useState<JournalResponse | null>(null)
  // Полный список карточек без фильтра по группе — источник опций фильтра.
  const [allGroups, setAllGroups] = useState<JournalSubjectGroup[]>([])
  const [selectedGroupId, setSelectedGroupId] = useState("")
  const [subjectKey, setSubjectKey] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await fetchJournal(
        viewContext.teacherId,
        undefined,
        selectedGroupId || undefined,
      )
      if (!res.isSuccess || !res.data) {
        throw new Error(res.errorMessage ?? "Не удалось загрузить журнал")
      }
      const data = res.data
      setJournal(data)
      // Опции фильтра по группе строим только по полному ответу.
      if (!selectedGroupId) setAllGroups(data.subjects)
      setSubjectKey((prev) => {
        const keys = data.subjects.map(journalCardKey)
        return prev && keys.includes(prev) ? prev : (keys[0] ?? null)
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось загрузить журнал")
    } finally {
      setLoading(false)
    }
  }, [viewContext.teacherId, selectedGroupId])

  useEffect(() => {
    void load()
  }, [load])

  const isTeacher = profile?.role === "Teacher"

  const groupOptions = (() => {
    const map = new Map<string, string>()
    allGroups.forEach((group) => {
      if (group.groupId) map.set(group.groupId, group.groupName)
    })
    return Array.from(map, ([id, name]) => ({ id, name }))
  })()

  const subjectGroup =
    journal?.subjects.find((group) => journalCardKey(group) === subjectKey) ?? null

  const items = subjectGroup
    ? [...subjectGroup.items].sort((a, b) =>
        a.week !== b.week ? a.week - b.week : a.dayOfWeek - b.dayOfWeek,
      )
    : []

  if (!isAuthed || !isTeacher) {
    return (
      <MaxUI>
        <main className="max-app__page max-app__login-prompt">
          <BookOpen size={32} className="max-app__state-icon" aria-hidden />
          <Typography.Title>Журнал доступен преподавателю</Typography.Title>
          <Typography.Body className="max-app__muted">
            Журнал формируется из расписания занятий
          </Typography.Body>
        </main>
      </MaxUI>
    )
  }

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <Typography.Body className="max-app__muted">
            {journal?.teacherName ?? "Преподаватель"} · всего пар:{" "}
            {journal?.totalPairCount ?? 0}
          </Typography.Body>
        </header>

        {loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
          </div>
        ) : error ? (
          <ScheduleError message={error} onRetry={() => void load()} />
        ) : !journal || journal.subjects.length === 0 ? (
          <ScheduleEmpty />
        ) : (
          <>
            {groupOptions.length > 0 && (
              <div
                className="max-app__chips"
                role="group"
                aria-label="Фильтр по группе"
              >
                <button
                  type="button"
                  aria-pressed={selectedGroupId === ""}
                  className={
                    selectedGroupId === ""
                      ? "max-app__chip max-app__chip--on"
                      : "max-app__chip"
                  }
                  onClick={() => setSelectedGroupId("")}
                >
                  Все группы
                </button>
                {groupOptions.map((group) => {
                  const active = group.id === selectedGroupId
                  return (
                    <button
                      key={group.id}
                      type="button"
                      aria-pressed={active}
                      className={
                        active
                          ? "max-app__chip max-app__chip--on"
                          : "max-app__chip"
                      }
                      onClick={() => setSelectedGroupId(group.id)}
                    >
                      {group.name}
                    </button>
                  )
                })}
              </div>
            )}

            <div
              className="max-app__chips"
              role="group"
              aria-label="Фильтр по предмету"
            >
              {journal.subjects.map((group) => {
                const key = journalCardKey(group)
                const active = key === subjectKey
                return (
                  <button
                    key={key}
                    type="button"
                    aria-pressed={active}
                    title={group.groupName}
                    className={
                      active
                        ? "max-app__chip max-app__chip--on"
                        : "max-app__chip"
                    }
                    onClick={() => setSubjectKey(key)}
                  >
                    {group.subject}
                    <span className="max-app__note">{group.pairCount}</span>
                  </button>
                )
              })}
            </div>

            {subjectGroup ? (
              <CellList
                mode="island"
                header={
                  <div className="max-app__page-title">
                    <span>
                      {subjectGroup.groupName} · {subjectGroup.subject}
                    </span>
                    <span className="max-app__note">
                      {subjectGroup.pairCount} пар по расписанию
                    </span>
                  </div>
                }
              >
                {items.map((entry) => (
                  <CellSimple
                    key={`${subjectGroup.subject}:${entry.week}:${entry.dayOfWeek}`}
                    separator
                    title={`Неделя ${entry.week} · ${dayLabelFromInt(entry.dayOfWeek)}`}
                    subtitle={
                      <span className="max-schedule__subtitle-wrap">
                        <span className="max-app__note">
                          {formatDate(entry.date)}
                        </span>
                        <JournalBadges types={entry.changeTypes ?? []} />
                      </span>
                    }
                    after={
                      <Typography.Body>Пар: {entry.numberPairs.length}</Typography.Body>
                    }
                  />
                ))}
              </CellList>
            ) : null}
          </>
        )}
      </main>
    </MaxUI>
  )
}
