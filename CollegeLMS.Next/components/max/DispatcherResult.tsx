"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { CheckCheck, Download } from "lucide-react"
import {
  Button,
  CellList,
  CellSimple,
  MaxUI,
  Spinner,
  Typography,
} from "@maxhub/max-ui"
import { exportBatch, getHistory } from "@/api/correction"
import { handleDispatcherAuthError } from "@/api/dispatcher"
import { fetchScheduleMeta } from "@/api/schedule"
import type { CorrectionApplyResult, ScheduleHistoryItem } from "@/types/correction"
import ChangeCard from "@/components/max/ChangeCard"
import ScheduleError from "@/components/max/ScheduleError"

export default function DispatcherResult({
  applied,
}: {
  applied: CorrectionApplyResult
}) {
  const [items, setItems] = useState<ScheduleHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [downloading, setDownloading] = useState(false)
  const [semesterStart, setSemesterStart] = useState<string | undefined>(undefined)

  useEffect(() => {
    void fetchScheduleMeta()
      .then((res) => {
        if (res.isSuccess && res.data) setSemesterStart(res.data.semesterStart)
      })
      .catch(() => undefined)
  }, [])

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
        if (handleDispatcherAuthError(err)) return
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

  const downloadCorrection = useCallback(
    async (options: { silent?: boolean } = {}) => {
      setDownloading(true)
      if (!options.silent) setError(null)
      try {
        const { blob, fileName } = await exportBatch(applied.batchId)
        const url = URL.createObjectURL(blob)
        const link = document.createElement("a")
        link.href = url
        link.download = fileName
        link.click()
        URL.revokeObjectURL(url)
      } catch (err) {
        if (handleDispatcherAuthError(err)) return
        if (!options.silent) {
          setError(
            err instanceof Error
              ? err.message
              : "Не удалось скачать корректировку",
          )
        }
      } finally {
        setDownloading(false)
      }
    },
    [applied.batchId],
  )

  // UC-SCH-27: сразу после применения XLSX скачивается автоматически один раз.
  const autoDownloadedBatch = useRef<string | null>(null)
  useEffect(() => {
    if (autoDownloadedBatch.current === applied.batchId) return
    autoDownloadedBatch.current = applied.batchId
    void downloadCorrection({ silent: true })
  }, [applied.batchId, downloadCorrection])

  return (
    <MaxUI className="max-app__dispatcher-result">
      <Typography.Title>Изменения применены</Typography.Title>
      <div className="max-app__success-box">
        <CheckCheck size={16} aria-hidden /> Применено записей:{" "}
        {applied.applied}
      </div>

      <Button
        variant="secondary"
        stretched
        loading={downloading}
        iconBefore={<Download size={16} aria-hidden />}
        onClick={() => void downloadCorrection()}
      >
        Скачать корректировку (XLSX)
      </Button>

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
            <ChangeCard
              key={item.id}
              item={item}
              semesterStartIso={semesterStart}
            />
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
