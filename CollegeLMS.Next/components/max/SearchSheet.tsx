"use client"

import { useEffect, useState } from "react"
import { Search, X, GraduationCap, Users, Star } from "lucide-react"
import { Button, Input, Typography } from "@maxhub/max-ui"
import { searchSchedule } from "@/api/schedule"
import type {
  ScheduleSearchGroup,
  ScheduleSearchResponse,
  ScheduleSearchTeacher,
} from "@/api/schedule"
import {
  addLocalFavorite,
  listLocalFavorites,
  removeLocalFavorite,
} from "@/lib/max-favorites"
import type { FavoriteTargetType } from "@/lib/max-favorites"
import { useMaxContext, type ViewContext } from "@/lib/max-context"

export default function SearchSheet({
  open,
  onClose,
}: {
  open: boolean
  onClose: () => void
}) {
  const { setViewContext } = useMaxContext()
  const [query, setQuery] = useState("")
  const [result, setResult] = useState<ScheduleSearchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [favIds, setFavIds] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (!open) {
      setQuery("")
      setResult(null)
      setError(null)
      return
    }
    setFavIds(new Set(listLocalFavorites().map((f) => f.targetId)))
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
        setError(
          navigator.onLine === false
            ? "Нет соединения"
            : "Не удалось загрузить. Повторите",
        )
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

  const toggleFavorite = (
    targetType: FavoriteTargetType,
    targetId: string,
    name: string,
  ) => {
    if (favIds.has(targetId)) {
      removeLocalFavorite(targetType, targetId)
      setFavIds((prev) => {
        const next = new Set(prev)
        next.delete(targetId)
        return next
      })
    } else {
      addLocalFavorite(targetType, targetId, name)
      setFavIds((prev) => new Set(prev).add(targetId))
    }
  }

  if (!open) return null

  const total =
    (result?.groups.length ?? 0) + (result?.teachers.length ?? 0)

  return (
    <div
      className="max-app__sheet"
      role="dialog"
      aria-modal="true"
      aria-label="Поиск"
    >
      <div className="max-app__sheet-head max-app__sheet-head--bare">
        <Button
          size="small"
          variant="ghost"
          iconBefore={<X size={18} aria-hidden />}
          aria-label="Закрыть поиск"
          onClick={onClose}
        />
      </div>

      <div className="max-app__field">
        <Input
          id="max-search-input"
          autoFocus
          type="search"
          placeholder="Поиск"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          iconBefore={<Search size={18} aria-hidden />}
        />
      </div>

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
              favIds={favIds}
              renderLabel={(item) => (item as ScheduleSearchGroup).name}
              onOpen={(item) =>
                openItem({
                  groupId: item.id,
                  groupName: (item as ScheduleSearchGroup).name,
                })
              }
              onToggleFavorite={(item) =>
                toggleFavorite(
                  "Group",
                  item.id,
                  (item as ScheduleSearchGroup).name,
                )
              }
            />
          ) : null}
          {result.teachers.length > 0 ? (
            <SearchSection
              title="Преподаватели"
              icon={<GraduationCap size={14} aria-hidden />}
              items={result.teachers}
              favIds={favIds}
              renderLabel={(item) => (item as ScheduleSearchTeacher).fullName}
              onOpen={(item) =>
                openItem({
                  teacherId: item.id,
                  teacherName: (item as ScheduleSearchTeacher).fullName,
                })
              }
              onToggleFavorite={(item) =>
                toggleFavorite(
                  "Teacher",
                  item.id,
                  (item as ScheduleSearchTeacher).fullName,
                )
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
  favIds,
  renderLabel,
  onOpen,
  onToggleFavorite,
}: {
  title: string
  icon: React.ReactNode
  items: (ScheduleSearchGroup | ScheduleSearchTeacher)[]
  favIds: Set<string>
  renderLabel: (item: ScheduleSearchGroup | ScheduleSearchTeacher) => string
  onOpen: (item: ScheduleSearchGroup | ScheduleSearchTeacher) => void
  onToggleFavorite: (item: ScheduleSearchGroup | ScheduleSearchTeacher) => void
}) {
  return (
    <div className="max-app__search-section">
      <Typography.Label>
        <span className="max-app__search-section-label">
          {icon} {title}
        </span>
      </Typography.Label>
      {items.map((item) => {
        const fav = favIds.has(item.id)
        return (
          <div className="max-app__search-item" key={item.id}>
            <Typography.Body>{renderLabel(item)}</Typography.Body>
            <span className="max-app__search-actions">
              <Button
                size="small"
                variant="ghost"
                aria-label={fav ? "Убрать из избранного" : "В избранное"}
                className={
                  fav
                    ? "max-app__favorite max-app__favorite--on"
                    : "max-app__favorite"
                }
                onClick={() => onToggleFavorite(item)}
                iconBefore={
                  <Star
                    size={18}
                    fill={fav ? "currentColor" : "none"}
                    aria-hidden
                  />
                }
              />
              <Button
                size="small"
                variant="secondary"
                onClick={() => onOpen(item)}
              >
                Открыть
              </Button>
            </span>
          </div>
        )
      })}
    </div>
  )
}