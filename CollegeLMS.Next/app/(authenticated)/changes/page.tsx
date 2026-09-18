"use client"

import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { useRouter } from "next/navigation"
import { History, Inbox, RefreshCw } from "lucide-react"
import type { ScheduleHistoryItem } from "@/types/correction"
import { getHistory } from "@/api/correction"
import { fetchScheduleMeta } from "@/api/schedule"
import { useAuth } from "@/lib/auth"
import { extractErrorMessage } from "@/lib/utils"
import ChangeCard from "@/components/ChangeCard"
import ChangeFilters, {
  hasActiveChangeFilters,
  type ChangeFiltersValue,
} from "@/components/ChangeFilters"
import Pagination from "@/components/ui/pagination"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import LoadingSpinner from "@/components/LoadingSpinner"
import ErrorBanner from "@/components/ErrorBanner"

const PAGE_SIZE = 20

export default function ChangesPage() {
  const { user, token, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const [filters, setFilters] = useState<ChangeFiltersValue>({})
  const [items, setItems] = useState<ScheduleHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [initialLoading, setInitialLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [totalWeeks, setTotalWeeks] = useState(0)
  const [semesterStartIso, setSemesterStartIso] = useState<string | undefined>()

  const requestIdRef = useRef(0)
  const loadedOnceRef = useRef(false)
  const prefillAppliedRef = useRef(false)

  const isTeacherRole = user?.roles?.includes("Teacher") ?? false

  useEffect(() => {
    if (!authLoading && !token) router.push("/login")
  }, [authLoading, token, router])

  // Преподаватель по умолчанию видит только свои изменения.
  useEffect(() => {
    if (prefillAppliedRef.current) return
    if (isTeacherRole && user?.teacherId) {
      prefillAppliedRef.current = true
      setFilters((prev) => (prev.teacherId ? prev : { ...prev, teacherId: user.teacherId! }))
    }
  }, [isTeacherRole, user?.teacherId])

  useEffect(() => {
    let cancelled = false
    fetchScheduleMeta()
      .then((res) => {
        if (!cancelled && res.isSuccess && res.data) {
          setTotalWeeks(res.data.totalWeeks)
          setSemesterStartIso(res.data.semesterStart)
        }
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  const load = useCallback(
    async (targetPage: number) => {
      const requestId = ++requestIdRef.current
      const isInitial = !loadedOnceRef.current
      setInitialLoading(isInitial)
      setRefreshing(!isInitial)
      setError(null)
      try {
        const res = await getHistory({
          week: filters.week,
          date: filters.date,
          from: filters.from,
          to: filters.to,
          groupId: filters.groupId,
          teacherId: filters.teacherId,
          changeType: filters.changeType,
          page: targetPage,
          pageSize: PAGE_SIZE,
        })
        if (requestId !== requestIdRef.current) return
        setItems(res.items)
        setPage(res.page)
        setTotalPages(res.totalPages)
        setTotalCount(res.totalCount)
        loadedOnceRef.current = true
      } catch (err) {
        if (requestId === requestIdRef.current) {
          setError(extractErrorMessage(err) ?? "Не удалось загрузить изменения")
        }
      } finally {
        if (requestId === requestIdRef.current) {
          setInitialLoading(false)
          setRefreshing(false)
        }
      }
    },
    [
      filters.week,
      filters.date,
      filters.from,
      filters.to,
      filters.groupId,
      filters.teacherId,
      filters.changeType,
    ],
  )

  useEffect(() => {
    if (!token) return
    void load(1)
  }, [token, load])

  const handleFilterChange = useCallback((patch: Partial<ChangeFiltersValue>) => {
    setFilters((prev) => ({ ...prev, ...patch }))
  }, [])

  const handleReset = useCallback(() => {
    setFilters(isTeacherRole && user?.teacherId ? { teacherId: user.teacherId } : {})
  }, [isTeacherRole, user?.teacherId])

  const handleRefresh = useCallback(() => {
    void load(page)
  }, [load, page])

  const filtersActive = useMemo(() => hasActiveChangeFilters(filters), [filters])

  if (authLoading) return <LoadingSpinner className="min-h-screen" />
  if (!token) return null

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 p-4 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <History className="size-5 text-primary" aria-hidden />
          <h2 className="text-xl font-semibold">Изменения расписания</h2>
        </div>
        <div className="flex items-center gap-2">
          {totalCount > 0 && (
            <span className="text-sm text-muted-foreground">
              Найдено: {totalCount}
            </span>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={initialLoading || refreshing}
          >
            <RefreshCw
              className={`size-3.5 ${refreshing ? "animate-spin" : ""}`}
              aria-hidden
            />
            Обновить
          </Button>
        </div>
      </div>

      <ChangeFilters
        value={filters}
        onChange={handleFilterChange}
        onReset={handleReset}
        totalWeeks={totalWeeks}
      />

      {error && (
        <div className="flex flex-wrap items-center gap-3">
          <ErrorBanner message={error} className="flex-1" />
          <Button variant="outline" size="sm" onClick={() => void load(page)}>
            Повторить
          </Button>
        </div>
      )}

      {initialLoading ? (
        <div className="flex min-h-[40vh] items-center justify-center">
          <LoadingSpinner size="lg" />
        </div>
      ) : items.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
            <Inbox className="size-10 text-muted-foreground" aria-hidden />
            <p className="text-base font-medium">Изменений не найдено</p>
            <p className="text-sm text-muted-foreground">
              {filtersActive
                ? "Попробуйте изменить фильтры или сбросить их"
                : "Обновления расписания появятся здесь"}
            </p>
            {filtersActive && (
              <Button variant="outline" size="sm" onClick={handleReset}>
                Сбросить фильтры
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div
          className={`flex flex-col gap-3 ${refreshing ? "opacity-60 transition-opacity" : ""}`}
          aria-live="polite"
          aria-busy={refreshing}
        >
          {items.map((item) => (
            <ChangeCard
              key={item.id}
              item={item}
              semesterStartIso={semesterStartIso}
            />
          ))}
        </div>
      )}

      {!initialLoading && items.length > 0 && (
        <Pagination page={page} totalPages={totalPages} onPageChange={(p) => void load(p)} />
      )}
    </div>
  )
}
