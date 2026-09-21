"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { History, Search, X } from "lucide-react"
import {
  Button,
  CellList,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { getHistory } from "@/api/correction"
import type { ScheduleHistoryItem } from "@/types/correction"
import { useMaxContext } from "@/lib/max-context"
import { parseMaxDeepLink } from "@/lib/max-deeplink"
import { fetchScheduleMeta, searchSchedule } from "@/api/schedule"
import type { ScheduleSearchResponse } from "@/api/schedule"
import ChangeCard from "@/components/max/ChangeCard"
import ScheduleError from "@/components/max/ScheduleError"

interface LocalScope {
  groupId?: string
  groupName?: string
  teacherId?: string
  teacherName?: string
}

export default function ChangesView() {
  const { viewContext, deepLink } = useMaxContext()
  const [items, setItems] = useState<ScheduleHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [weekFilter, setWeekFilter] = useState<number | undefined>(undefined)
  const [dateFilter, setDateFilter] = useState("")
  const [totalWeeks, setTotalWeeks] = useState<number | null>(null)
  const [semesterStart, setSemesterStart] = useState<string | undefined>(undefined)
  const [highlightId, setHighlightId] = useState<string | null>(null)
  const [scope, setScope] = useState<LocalScope>({})
  const [searchQuery, setSearchQuery] = useState("")
  const [searchResult, setSearchResult] = useState<ScheduleSearchResponse | null>(null)
  const [searchLoading, setSearchLoading] = useState(false)
  const focusedRef = useRef<HTMLDivElement | null>(null)

  const needsRecipientSearch = !viewContext.groupId && !viewContext.teacherId
  const effectiveGroupId = viewContext.groupId ?? scope.groupId
  const effectiveTeacherId = viewContext.teacherId ?? scope.teacherId

  useEffect(() => {
    void fetchScheduleMeta()
      .then((res) => {
        if (res.isSuccess && res.data) {
          setTotalWeeks(res.data.totalWeeks)
          setSemesterStart(res.data.semesterStart)
        }
      })
      .catch(() => undefined)
  }, [])

  // Deep-link: id изменения из start_param (MAX Bridge) или query-параметров.
  useEffect(() => {
    const link = deepLink ?? parseMaxDeepLink(window.location.search)
    if (link.route === "changes" && link.id) {
      setHighlightId(link.id)
    }
  }, [deepLink])

  // Поиск получателя, если в контексте нет группы/преподавателя.
  useEffect(() => {
    if (!needsRecipientSearch) return
    const q = searchQuery.trim()
    if (q.length < 2) {
      setSearchResult(null)
      setSearchLoading(false)
      return
    }
    let cancelled = false
    const timer = window.setTimeout(async () => {
      setSearchLoading(true)
      try {
        const res = await searchSchedule(q)
        if (!cancelled && res.isSuccess && res.data) setSearchResult(res.data)
      } catch {
        if (!cancelled) setSearchResult(null)
      } finally {
        if (!cancelled) setSearchLoading(false)
      }
    }, 300)
    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [needsRecipientSearch, searchQuery])

  const load = useCallback(
    async (nextPage: number, replace: boolean) => {
      if (replace) setLoading(true)
      else setLoadingMore(true)
      setError(null)
      try {
        const res = await getHistory({
          groupId: effectiveGroupId,
          teacherId: effectiveTeacherId,
          week: weekFilter,
          date: dateFilter || undefined,
          page: nextPage,
          pageSize: 20,
        })
        setItems((prev) => (replace ? res.items : [...prev, ...res.items]))
        setTotal(res.totalCount)
        setPage(nextPage)
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Не удалось загрузить изменения",
        )
      } finally {
        setLoading(false)
        setLoadingMore(false)
      }
    },
    [effectiveGroupId, effectiveTeacherId, weekFilter, dateFilter],
  )

  useEffect(() => {
    void load(1, true)
  }, [load])

  useEffect(() => {
    if (highlightId && items.length > 0) {
      const timer = window.setTimeout(() => {
        focusedRef.current?.scrollIntoView({
          block: "center",
          behavior: "smooth",
        })
      }, 150)
      return () => window.clearTimeout(timer)
    }
  }, [highlightId, items])

  const hasMore = items.length < total

  const weekOptions = useMemo(() => {
    if (!totalWeeks) return []
    return Array.from({ length: totalWeeks }, (_, i) => i + 1)
  }, [totalWeeks])

  const scopeLabel = effectiveGroupId
    ? (viewContext.groupName ?? scope.groupName ?? "Группа")
    : effectiveTeacherId
      ? (viewContext.teacherName ?? scope.teacherName ?? "Преподаватель")
      : null

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <div>
            <Typography.Body className="max-app__muted">
              {scopeLabel ?? "Лента изменений"}
            </Typography.Body>
          </div>
        </header>

        {weekOptions.length > 0 ? (
          <div className="max-app__filter-row">
            <label className="max-app__note" htmlFor="max-week-filter">
              Неделя:
            </label>
            <select
              id="max-week-filter"
              value={weekFilter ?? 0}
              onChange={(e) => {
                const value = Number(e.target.value)
                setWeekFilter(value === 0 ? undefined : value)
              }}
            >
              <option value={0}>Все</option>
              {weekOptions.map((w) => (
                <option key={w} value={w}>
                  {w}
                </option>
              ))}
            </select>
          </div>
        ) : null}

        <div className="max-app__filter-row">
          <label className="max-app__note" htmlFor="max-date-filter">
            Дата:
          </label>
          <input
            id="max-date-filter"
            type="date"
            className="max-app__select"
            value={dateFilter}
            onChange={(e) => setDateFilter(e.target.value)}
          />
          {dateFilter ? (
            <Button
              size="small"
              variant="ghost"
              aria-label="Сбросить дату"
              iconBefore={<X size={16} aria-hidden />}
              onClick={() => setDateFilter("")}
            />
          ) : null}
        </div>

        {needsRecipientSearch && scopeLabel ? (
          <div className="max-app__filter-row">
            <span className="max-app__chip max-app__chip--on">{scopeLabel}</span>
            <Button
              size="small"
              variant="ghost"
              aria-label="Сбросить получателя"
              iconBefore={<X size={16} aria-hidden />}
              onClick={() => {
                setScope({})
                setSearchQuery("")
                setSearchResult(null)
              }}
            />
          </div>
        ) : null}

        {needsRecipientSearch && !scopeLabel ? (
          <div className="max-app__search-wrap">
            <label className="max-app__note" htmlFor="max-recipient-search">
              Группа или преподаватель
            </label>
            <div className="max-app__field">
              <input
                id="max-recipient-search"
                type="search"
                className="max-app__select"
                placeholder="Начните вводить название или ФИО"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                autoComplete="off"
              />
            </div>
            {searchLoading ? (
              <Typography.Body className="max-app__note">
                <Search size={14} aria-hidden /> Поиск…
              </Typography.Body>
            ) : null}
            {searchResult &&
            (searchResult.groups.length > 0 || searchResult.teachers.length > 0) ? (
              <div className="max-app__search-results">
                {searchResult.groups.map((group) => (
                  <div className="max-app__search-item" key={group.id}>
                    <Typography.Body>{group.name}</Typography.Body>
                    <Button
                      size="small"
                      variant="secondary"
                      onClick={() => {
                        setScope({ groupId: group.id, groupName: group.name })
                        setSearchQuery("")
                        setSearchResult(null)
                      }}
                    >
                      Группа
                    </Button>
                  </div>
                ))}
                {searchResult.teachers.map((teacher) => (
                  <div className="max-app__search-item" key={teacher.id}>
                    <Typography.Body>{teacher.fullName}</Typography.Body>
                    <Button
                      size="small"
                      variant="secondary"
                      onClick={() => {
                        setScope({
                          teacherId: teacher.id,
                          teacherName: teacher.fullName,
                        })
                        setSearchQuery("")
                        setSearchResult(null)
                      }}
                    >
                      Преподаватель
                    </Button>
                  </div>
                ))}
              </div>
            ) : null}
            {searchQuery.trim().length >= 2 &&
            !searchLoading &&
            searchResult &&
            searchResult.groups.length === 0 &&
            searchResult.teachers.length === 0 ? (
              <Typography.Body className="max-app__note">
                Ничего не найдено
              </Typography.Body>
            ) : null}
          </div>
        ) : null}

        {loading ? (
          <div className="max-app__state">
            <Spinner size={24} />
          </div>
        ) : error ? (
          <ScheduleError message={error} onRetry={() => void load(1, true)} />
        ) : items.length === 0 ? (
          <div className="max-app__state">
            <History size={32} className="max-app__state-icon" aria-hidden />
            <Typography.Title>Изменений нет</Typography.Title>
            <Typography.Body className="max-app__muted">
              Обновления расписания появятся здесь
            </Typography.Body>
          </div>
        ) : (
          <CellList mode="island">
            {items.map((item) => (
              <div
                key={item.id}
                ref={item.id === highlightId ? focusedRef : undefined}
                className={
                  item.id === highlightId
                    ? "max-app__change-card--highlight"
                    : undefined
                }
              >
                <ChangeCard
                  item={item}
                  semesterStartIso={semesterStart}
                />
              </div>
            ))}
          </CellList>
        )}

        {hasMore && !loading ? (
          <Button
            variant="secondary"
            stretched
            loading={loadingMore}
            onClick={() => void load(page + 1, false)}
          >
            Ещё
          </Button>
        ) : null}
      </main>
    </MaxUI>
  )
}
