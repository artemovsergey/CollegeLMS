import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type {
  CorrectionPreviewResponse,
  CorrectionPreviewEntry,
  ConfirmResult,
  ScheduleHistoryItem,
  CorrectionBatch,
  CorrectionPosition,
  CreateCorrectionPosition,
  CorrectionImportResponse,
  CorrectionApplyResult,
} from "@/types/correction"

export interface HistoryParams {
  groupId?: string
  teacherId?: string
  week?: number
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

export async function getBatches(
  status?: string,
): Promise<CorrectionBatch[]> {
  const qs = status ? `?status=${status}` : ""
  return unwrap(await api.get<Result<CorrectionBatch[]>>(`${BATCH_BASE}${qs}`))
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

export async function exportBatch(batchId: string): Promise<Blob> {
  const response = await api.post<Blob>(
    `${BATCH_BASE}/${batchId}/export`,
    {},
    { responseType: "blob" },
  )
  return response.data
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