"use client"

import { useEffect, useState } from "react"
import { Search, X, GraduationCap, Users } from "lucide-react"
import { Button, Input, Typography } from "@maxhub/max-ui"
import { searchSchedule } from "@/api/schedule"
import type {
  ScheduleSearchGroup,
  ScheduleSearchResponse,
  ScheduleSearchTeacher,
} from "@/api/schedule"
import { useMaxContext, type ViewContext } from "@/lib/max-context"

export default function SearchSheet({
  open,
  onClose,
}: {
  open: boolean
  onClose: () => void
}) {
  const { setViewContext, isAuthed } = useMaxContext()
  const [query, setQuery] = useState("")
  const [result, setResult] = useState<ScheduleSearchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) {
      setQuery("")
      setResult(null)
      setError(null)
      return
    }
    const q = query.trim()
    if (q.length === 0) {
      setResult(null)
      setError(null)
      return
    }
    const timer = window.setTimeout(async () => {
      setLoading(true)
      setError(null)
      try {
        const res = await searchSchedule(q)
        if (!res.isSuccess || !res.data) {
          setError(res.errorMessage ?? "Не удалось загрузить результаты")
          return
        }
        setResult(res.data)
      } catch {
        setError(navigator.onLine === false ? "Нет соединения" : "Не удалось загрузить. Повторите")
      } finally {
        setLoading(false)
      }
    }, 300)
    return () => window.clearTimeout(timer)
  }, [open, query])

const openItem = (ctx: ViewContext) => {
    setViewContext(ctx)
    onClose()
  }

  if (!open) return null

  const total =
    (result?.groups.length ?? 0) + (result?.teachers.length ?? 0)

  return (
    <div className="max-app__sheet" role="dialog" aria-modal="true" aria-label="Поиск">
      <div className="max-app__sheet-head">
        <Typography.Title>Поиск</Typography.Title>
        <Button
          size="small"
          variant="ghost"
          iconBefore={<X size={18} aria-hidden />}
          aria-label="Закрыть поиск"
          onClick={onClose}
        />
      </div>

      <Input
        autoFocus
        type="search"
        placeholder="Группа или преподаватель…"
        aria-label="Поиск группы или преподавателя"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        iconBefore={<Search size={18} aria-hidden />}
      />

      {loading ? (
        <div className="max-app__state">
          <Typography.Body>Поиск…</Typography.Body>
        </div>
      ) : error ? (
        <div className="max-app__state">
          <Typography.Body>{error}</Typography.Body>
        </div>
      ) : query.trim().length === 0 ? (
        <div className="max-app__state">
          <Typography.Body className="max-app__muted">
            Начните вводить название группы или ФИО преподавателя
          </Typography.Body>
          {!isAuthed ? (
            <Typography.Body className="max-app__note">
              Поиск доступен авторизованным пользователям
            </Typography.Body>
          ) : null}
        </div>
      ) : !result ? (
        <div className="max-app__state">
          <Typography.Body className="max-app__muted">
            Введите хотя бы один символ
          </Typography.Body>
        </div>
      ) : total === 0 ? (
        <div className="max-app__state">
          <Typography.Title>Ничего не найдено</Typography.Title>
          <Typography.Body className="max-app__muted">
            Попробуйте изменить запрос
          </Typography.Body>
        </div>
      ) : (
        <div className="max-app__search-results">
          {result.groups.length > 0 ? (
            <SearchSection
              title="Группы"
              icon={<Users size={14} aria-hidden />}
              items={result.groups}
              renderLabel={(item) => (item as ScheduleSearchGroup).name}
              onOpen={(item) =>
                openItem({
                  groupId: item.id,
                  groupName: (item as ScheduleSearchGroup).name,
                })
              }
            />
          ) : null}
          {result.teachers.length > 0 ? (
            <SearchSection
              title="Преподаватели"
              icon={<GraduationCap size={14} aria-hidden />}
              items={result.teachers}
              renderLabel={(item) => (item as ScheduleSearchTeacher).fullName}
              onOpen={(item) =>
                openItem({
                  teacherId: item.id,
                  teacherName: (item as ScheduleSearchTeacher).fullName,
                })
              }
            />
          ) : null}
        </div>
      )}
    </div>
  )
}

function SearchSection({
  title,
  icon,
  items,
  renderLabel,
  onOpen,
}: {
  title: string
  icon: React.ReactNode
  items: (ScheduleSearchGroup | ScheduleSearchTeacher)[]
  renderLabel: (item: ScheduleSearchGroup | ScheduleSearchTeacher) => string
  onOpen: (item: ScheduleSearchGroup | ScheduleSearchTeacher) => void
}) {
  return (
    <div className="max-app__search-section">
      <Typography.Label>
        <span className="max-app__search-section-label">
          {icon} {title}
        </span>
      </Typography.Label>
      {items.map((item) => (
        <div className="max-app__search-item" key={item.id}>
          <Typography.Body>{renderLabel(item)}</Typography.Body>
          <Button size="small" variant="secondary" onClick={() => onOpen(item)}>
            Открыть
          </Button>
        </div>
      ))}
    </div>
  )
}