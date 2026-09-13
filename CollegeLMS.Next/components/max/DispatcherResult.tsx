"use client"

import { useCallback, useEffect, useState } from "react"
import { CheckCheck, Download } from "lucide-react"
import {
  Button,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { getHistory } from "@/api/correction"
import { exportSchedule } from "@/api/schedule"
import type { ConfirmResult, ScheduleHistoryItem } from "@/types/correction"
import ChangeCard from "@/components/max/ChangeCard"
import ScheduleError from "@/components/max/ScheduleError"

export default function DispatcherResult({
  applied,
  groupId,
}: {
  applied: ConfirmResult
  groupId?: string
}) {
  const [items, setItems] = useState<ScheduleHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(
    async (nextPage: number, replace: boolean) => {
      if (replace) setLoading(true)
      else setLoadingMore(true)
      setError(null)
      try {
        const res = await getHistory({ page: nextPage, pageSize: 20 })
        setItems((prev) => (replace ? res.items : [...prev, ...res.items]))
        setTotal(res.totalCount)
        setPage(nextPage)
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Не удалось загрузить историю",
        )
      } finally {
        setLoading(false)
        setLoadingMore(false)
      }
    },
    [],
  )

  useEffect(() => {
    void load(1, true)
  }, [load, applied])

  const hasMore = items.length < total

  return (
    <MaxUI className="max-app__dispatcher-result">
      <Typography.Title>Изменения применены</Typography.Title>
      <div className="max-app__success-box">
        <CheckCheck size={16} aria-hidden /> Применено записей:{" "}
        {applied.applied}
      </div>

      {groupId ? (
        <Button
          variant="secondary"
          stretched
          iconBefore={<Download size={16} aria-hidden />}
          onClick={() => void exportSchedule({ groupId }, "xlsx")}
        >
          Скачать расписание (XLSX)
        </Button>
      ) : null}

      {loading ? (
        <div className="max-app__state">
          <Spinner size={24} />
        </div>
      ) : error ? (
        <ScheduleError message={error} onRetry={() => void load(1, true)} />
      ) : items.length === 0 ? (
        <div className="max-app__state">
          <Typography.Body className="max-app__muted">
            История пуста
          </Typography.Body>
        </div>
      ) : (
        <CellList mode="island">
          {items.map((item) => (
            <ChangeCard key={item.id} item={item} />
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
    </MaxUI>
  )
}