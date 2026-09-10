import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type {
  CorrectionPreviewResponse,
  CorrectionPreviewEntry,
  ConfirmResult,
  ScheduleHistoryItem,
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
): Promise<ConfirmResult> {
  return unwrap(
    await api.post<Result<ConfirmResult>>("/api/schedule/correction/confirm", {
      entries,
    }),
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
      `/api/schedule/correction/history${qs ? `?${qs}` : ""}`,
    ),
  )
}