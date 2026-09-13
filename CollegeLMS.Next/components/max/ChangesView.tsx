"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { History, Filter } from "lucide-react"
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
import { fetchScheduleMeta } from "@/api/schedule"
import ChangeCard from "@/components/max/ChangeCard"
import ScheduleError from "@/components/max/ScheduleError"

export default function ChangesView() {
  const { viewContext } = useMaxContext()
  const [items, setItems] = useState<ScheduleHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [weekFilter, setWeekFilter] = useState<number | undefined>(undefined)
  const [totalWeeks, setTotalWeeks] = useState<number | null>(null)
  const [highlightId, setHighlightId] = useState<string | null>(null)
  const focusedRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    const link = parseMaxDeepLink(
      typeof window !== "undefined" ? window.location.search : "",
    )
    if ((link.route === "correction" || link.route === "changes") && link.id) {
      setHighlightId(link.id)
    }
    void fetchScheduleMeta()
      .then((res) => {
        if (res.isSuccess && res.data) setTotalWeeks(res.data.totalWeeks)
      })
      .catch(() => undefined)
  }, [])

  const load = useCallback(
    async (nextPage: number, replace: boolean) => {
      if (replace) setLoading(true)
      else setLoadingMore(true)
      setError(null)
      try {
        const res = await getHistory({
          groupId: viewContext.groupId,
          teacherId: viewContext.teacherId,
          week: weekFilter,
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
    [viewContext.groupId, viewContext.teacherId, weekFilter],
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

  return (
    <MaxUI>
      <main className="max-app__page">
        <header className="max-app__page-title">
          <div>
            <Typography.Title>Изменения</Typography.Title>
            <Typography.Body className="max-app__muted">
              {viewContext.groupName ?? viewContext.teacherName ?? "Лента изменений"}
            </Typography.Body>
          </div>
          <Filter size={18} aria-hidden className="max-app__muted" />
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
                <ChangeCard item={item} />
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