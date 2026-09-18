import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"

/** Вид практики: УП — учебная, ПП — производственная. */
export type PracticeKind = "Up" | "Pp"

export const PRACTICE_KIND_LABELS: Record<PracticeKind, string> = {
  Up: "УП — учебная",
  Pp: "ПП — производственная",
}

export const PRACTICE_KIND_SHORT: Record<PracticeKind, string> = {
  Up: "УП",
  Pp: "ПП",
}

export interface Practice {
  id: string
  kind: PracticeKind
  groupId: string
  groupName: string
  teacherId: string
  teacherName: string
  dateFrom: string
  dateTo: string
  organization: string | null
  note: string | null
}

export interface PracticeRequest {
  kind: PracticeKind
  groupId: string
  teacherId: string
  dateFrom: string
  dateTo: string
  organization?: string | null
  note?: string | null
}

export interface PracticeFilters {
  groupId?: string
  teacherId?: string
  kind?: PracticeKind
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

/** Строка импорта XLSX (сырые значения; используется и в preview, и в confirm). */
export interface PracticeImportRow {
  row: number
  kind: string
  groupName: string
  dateFrom: string
  dateTo: string
  teacherName: string
  organization?: string | null
  note?: string | null
}

/** Ошибка импорта: «Строка N». */
export interface PracticeImportError {
  row: number
  column: number
  level: string
  message: string
}

export interface PracticeImportPreview {
  totalRows: number
  rows: PracticeImportRow[]
  errors: PracticeImportError[]
}

export interface PracticeImportConfirmResult {
  imported: number
  practices: Practice[]
}

export async function fetchPractices(
  filters: PracticeFilters = {},
): Promise<PagedResponse<Practice>> {
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.kind) params.set("kind", filters.kind)
  if (filters.from) params.set("from", filters.from)
  if (filters.to) params.set("to", filters.to)
  if (filters.page) params.set("page", String(filters.page))
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize))
  const qs = params.toString()
  return unwrap(
    await api.get<Result<PagedResponse<Practice>>>(
      `/api/practices${qs ? `?${qs}` : ""}`,
    ),
  )
}

export async function createPractice(body: PracticeRequest): Promise<Practice> {
  return unwrap(await api.post<Result<Practice>>("/api/practices", body))
}

export async function updatePractice(
  id: string,
  body: PracticeRequest,
): Promise<Practice> {
  return unwrap(await api.put<Result<Practice>>(`/api/practices/${id}`, body))
}

export async function deletePractice(id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/practices/${id}`)
  if (!res.data.isSuccess) {
    throw new Error(res.data.errorMessage ?? "Не удалось удалить практику")
  }
}

export async function previewPracticeImport(
  file: File,
): Promise<PracticeImportPreview> {
  const formData = new FormData()
  formData.append("file", file)
  return unwrap(
    await api.post<Result<PracticeImportPreview>>(
      "/api/practices/import/preview",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  )
}

export async function confirmPracticeImport(
  rows: PracticeImportRow[],
): Promise<PracticeImportConfirmResult> {
  return unwrap(
    await api.post<Result<PracticeImportConfirmResult>>(
      "/api/practices/import/confirm",
      { rows },
    ),
  )
}
