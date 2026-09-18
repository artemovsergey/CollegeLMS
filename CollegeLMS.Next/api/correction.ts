import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type {
  CorrectionChangeType,
  CorrectionPreviewResponse,
  CorrectionPreviewEntry,
  ConfirmResult,
  ScheduleHistoryItem,
  CorrectionBatch,
  CorrectionBatchStatus,
  CorrectionDayResponse,
  CorrectionPosition,
  CreateCorrectionPosition,
  CorrectionImportResponse,
  CorrectionApplyResult,
} from "@/types/correction"

export interface HistoryParams {
  groupId?: string
  teacherId?: string
  week?: number
  /** Дата проведения занятия (YYYY-MM-DD) — сервер сам вычисляет неделю и день. */
  date?: string
  /** Начало периода по дате применения изменения (YYYY-MM-DD). */
  from?: string
  /** Конец периода по дате применения изменения (YYYY-MM-DD). */
  to?: string
  changeType?: CorrectionChangeType
  page?: number
  pageSize?: number
}

export async function previewCorrection(
  file: File,
): Promise<CorrectionPreviewResponse> {
  const formData = new FormData()
  formData.append("file", file)
  return unwrap(
    await api.post<Result<CorrectionPreviewResponse>>(
      "/api/schedule/correction/preview",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  )
}

export interface ManualCorrectionRow {
  groupName: string
  removedSubject: string
  removedTeacherName: string
  addedSubject: string
  addedTeacherName: string
  numberPair: number
  note: string
}

export async function exportManualCorrection(
  correctionDate: string,
  rows: ManualCorrectionRow[],
): Promise<Blob> {
  const response = await api.post<Blob>(
    "/api/schedule/correction/export",
    { correctionDate, rows },
    { responseType: "blob" },
  )
  return response.data
}

export async function confirmCorrection(
  entries: CorrectionPreviewEntry[],
  idempotencyKey: string,
): Promise<ConfirmResult> {
  return unwrap(
    await api.post<Result<ConfirmResult>>(
      "/api/schedule/correction/confirm",
      { entries },
      { headers: { "Idempotency-Key": idempotencyKey } },
    ),
  )
}

export async function getHistory(
  params: HistoryParams = {},
): Promise<PagedResponse<ScheduleHistoryItem>> {
  const urlParams = new URLSearchParams()
  if (params.groupId) urlParams.set("groupId", params.groupId)
  if (params.teacherId) urlParams.set("teacherId", params.teacherId)
  if (params.week !== undefined)
    urlParams.set("week", String(params.week))
  if (params.date) urlParams.set("date", params.date)
  if (params.from) urlParams.set("from", params.from)
  if (params.to) urlParams.set("to", params.to)
  if (params.changeType) urlParams.set("changeType", params.changeType)
  if (params.page) urlParams.set("page", String(params.page))
  if (params.pageSize) urlParams.set("pageSize", String(params.pageSize))

  const qs = urlParams.toString()
  return unwrap(
    await api.get<Result<PagedResponse<ScheduleHistoryItem>>>(
      `/api/schedule/history${qs ? `?${qs}` : ""}`,
    ),
  )
}

const BATCH_BASE = "/api/schedule/correction/batches"

export async function createBatch(correctionDate: string): Promise<CorrectionBatch> {
  return unwrap(
    await api.post<Result<CorrectionBatch>>(`${BATCH_BASE}`, { correctionDate }),
  )
}

export interface BatchListParams {
  status?: CorrectionBatchStatus
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export async function getBatches(
  params: BatchListParams = {},
): Promise<PagedResponse<CorrectionBatch>> {
  const qs = new URLSearchParams()
  if (params.status) qs.set("status", params.status)
  if (params.from) qs.set("from", params.from)
  if (params.to) qs.set("to", params.to)
  if (params.page) qs.set("page", String(params.page))
  if (params.pageSize) qs.set("pageSize", String(params.pageSize))
  const suffix = qs.toString()
  return unwrap(
    await api.get<Result<PagedResponse<CorrectionBatch>>>(
      `${BATCH_BASE}${suffix ? `?${suffix}` : ""}`,
    ),
  )
}

export async function getDaySchedule(params: {
  groupId: string
  date: string
  batchId?: string
}): Promise<CorrectionDayResponse> {
  const qs = new URLSearchParams({ groupId: params.groupId, date: params.date })
  if (params.batchId) qs.set("batchId", params.batchId)
  return unwrap(
    await api.get<Result<CorrectionDayResponse>>(
      `/api/schedule/correction/day?${qs.toString()}`,
    ),
  )
}

export async function getBatch(id: string): Promise<CorrectionBatch> {
  return unwrap(await api.get<Result<CorrectionBatch>>(`${BATCH_BASE}/${id}`))
}

export async function deleteBatch(id: string): Promise<void> {
  await api.delete(`${BATCH_BASE}/${id}`)
}

export async function addPosition(
  batchId: string,
  position: CreateCorrectionPosition,
): Promise<CorrectionPosition> {
  return unwrap(
    await api.post<Result<CorrectionPosition>>(
      `${BATCH_BASE}/${batchId}/positions`,
      position,
    ),
  )
}

export async function updatePosition(
  batchId: string,
  positionId: string,
  position: CreateCorrectionPosition,
): Promise<CorrectionPosition> {
  return unwrap(
    await api.put<Result<CorrectionPosition>>(
      `${BATCH_BASE}/${batchId}/positions/${positionId}`,
      position,
    ),
  )
}

export async function deletePosition(
  batchId: string,
  positionId: string,
): Promise<void> {
  await api.delete(`${BATCH_BASE}/${batchId}/positions/${positionId}`)
}

export async function importCorrection(file: File): Promise<CorrectionImportResponse> {
  const formData = new FormData()
  formData.append("file", file)
  return unwrap(
    await api.post<Result<CorrectionImportResponse>>(
      `${BATCH_BASE}/import`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  )
}

/**
 * Имя файла формируется на бэкенде (FILE-3). Читаем его из Content-Disposition,
 * поддерживая RFC 5987 (`filename*=UTF-8''...`) и обычный `filename="..."`.
 * Если заголовка нет — генерируем по той же маске.
 */
function extractFileName(
  disposition: string | null | undefined,
): string | null {
  if (!disposition) return null
  const utf8 = disposition.match(/filename\*\s*=\s*UTF-8'[^']*'([^;]+)/i)
  if (utf8?.[1]) {
    try {
      return decodeURIComponent(utf8[1].trim().replace(/^"|"$/g, ""))
    } catch {
      /* некорректный percent-encoding — пробуем обычный filename */
    }
  }
  const plain = disposition.match(/filename\s*=\s*"([^"]+)"|filename\s*=\s*([^;]+)/i)
  const value = plain?.[1] ?? plain?.[2]
  return value ? value.trim() : null
}

export function buildCorrectionFileName(date: Date = new Date()): string {
  const pad = (value: number) => String(value).padStart(2, "0")
  return `Корректировка_${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}_${pad(date.getHours())}-${pad(date.getMinutes())}-${pad(date.getSeconds())}.xlsx`
}

export async function exportBatch(
  batchId: string,
): Promise<{ blob: Blob; fileName: string }> {
  const response = await api.post<Blob>(
    `${BATCH_BASE}/${batchId}/export`,
    {},
    { responseType: "blob" },
  )
  const disposition = response.headers?.["content-disposition"] as
    | string
    | undefined
  return {
    blob: response.data,
    fileName: extractFileName(disposition) ?? buildCorrectionFileName(),
  }
}

export async function applyBatch(
  batchId: string,
  idempotencyKey: string,
): Promise<CorrectionApplyResult> {
  return unwrap(
    await api.post<Result<CorrectionApplyResult>>(
      `${BATCH_BASE}/${batchId}/apply`,
      {},
      { headers: { "Idempotency-Key": idempotencyKey } },
    ),
  )
}