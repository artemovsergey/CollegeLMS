"use client"

import { useCallback, useEffect, useState } from "react"
import { BookOpen } from "lucide-react"
import {
  Button,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { fetchJournal } from "@/api/schedule"
import type { JournalResponse } from "@/api/schedule"
import { useMaxContext } from "@/lib/max-context"
import ScheduleError from "@/components/max/ScheduleError"
import ScheduleEmpty from "@/components/max/ScheduleEmpty"

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString("ru-RU", { day: "2-digit", month: "2-digit" })
}

export default function JournalView() {
  const { isAuthed, profile, viewContext } = useMaxContext()
  const [journal, setJournal] = useState<JournalResponse | null>(null)
  const [subject, setSubject] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await fetchJournal(viewContext.teacherId)
      if (!res.isSuccess || !res.data) {
        throw new Error(res.errorMessage ?? "Не удалось загрузить журнал")
      }
      setJournal(res.data)
      setSubject((prev) => {
        const subjects = res.data!.subjects.map((s) => s.subject)
        return prev && subjects.includes(prev) ? prev : (subjects[0] ?? null)
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : "Не удалось загрузить журнал")
    } finally {
      setLoading(false)
    }
  }, [viewContext.teacherId])

  useEffect(() => {
    void load()
  }, [load])

  const isTeacher = profile?.role === "Teacher"

  const subjectGroup =
    journal?.subjects.find((s) => s.subject === subject) ?? null

  const items = subjectGroup
    ? [...subjectGroup.items].sort((a, b) => a.week - b.week)
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
          <div>
            <Typography.Title>Журнал</Typography.Title>
            <Typography.Body className="max-app__muted">
              {journal?.teacherName ?? "Преподаватель"} · всего пар:{" "}
              {journal?.totalPairCount ?? 0}
            </Typography.Body>
          </div>
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
            <label className="max-app__note" htmlFor="max-journal-subject">
              Предмет
            </label>
            <select
              id="max-journal-subject"
              className="max-app__select"
              value={subject ?? ""}
              onChange={(e) => setSubject(e.target.value)}
            >
              {journal.subjects.map((s) => (
                <option key={s.subject} value={s.subject}>
                  {s.subject} — {s.pairCount} пар
                </option>
              ))}
            </select>

            {subjectGroup ? (
              <CellList
                mode="island"
                header={
                  <div className="max-app__page-title">
                    <span>Недели</span>
                    <span className="max-app__note">
                      {subjectGroup.pairCount} пар по расписанию
                    </span>
                  </div>
                }
              >
                {items.map((entry) => (
                  <CellSimple
                    key={`${subjectGroup.subject}:${entry.week}`}
                    separator
                    title={`Неделя ${entry.week}`}
                    subtitle={formatDate(entry.date)}
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