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

/** Преподаватель практики (ответ API). */
export interface PracticeTeacher {
  id: string
  name: string
}

/** День учебной практики с числом пар (1..8). */
export interface PracticeDay {
  /** Дата (ISO). */
  date: string
  /** Количество пар. */
  pairCount: number
}

export interface Practice {
  id: string
  kind: PracticeKind
  /** Название: «УП 01», «ПП 09». Может отсутствовать в старых данных. */
  name?: string | null
  groupId: string
  groupName: string
  /** Идентификаторы преподавателей практики (≥1). */
  teacherIds?: string[] | null
  /** Преподаватели практики (≥1). */
  teachers?: PracticeTeacher[] | null
  /** Legacy-поле до контракта 3B — fallback для деградации. */
  teacherName?: string | null
  dateFrom: string
  dateTo: string
  /** Дни УП с числом пар (только для вида УП). */
  days?: PracticeDay[] | null
  note: string | null
}

export interface PracticeRequest {
  kind: PracticeKind
  /** Название обязательно: «УП 01», «ПП 09». */
  name: string
  groupId: string
  /** Список преподавателей (≥1). */
  teacherIds: string[]
  dateFrom: string
  dateTo: string
  /** Дни УП с числом пар (только для вида УП). */
  days?: PracticeDay[]
  note?: string | null
}

export interface PracticeFilters {
  name?: string
  groupId?: string
  teacherId?: string
  kind?: PracticeKind
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

/** Название практики с безопасным fallback на метку вида. */
export function practiceName(practice: Practice): string {
  const name = practice.name?.trim()
  return name && name.length > 0 ? name : PRACTICE_KIND_SHORT[practice.kind]
}

/** Преподаватели практики: новый список или legacy-поле. */
export function practiceTeachers(practice: Practice): PracticeTeacher[] {
  if (practice.teachers && practice.teachers.length > 0) return practice.teachers
  const legacy = practice.teacherName?.trim()
  return legacy ? [{ id: "", name: legacy }] : []
}

/** Преподаватели одной строкой через запятую. */
export function practiceTeacherNames(practice: Practice): string {
  return practiceTeachers(practice)
    .map((teacher) => teacher.name)
    .join(", ")
}

/** Дни УП практики (пустой список для ПП). */
export function practiceDays(practice: Practice): PracticeDay[] {
  return practice.days ?? []
}

/** Строка импорта XLSX (сырые значения; используется и в preview, и в confirm). */
export interface PracticeImportRow {
  row: number
  kind: string
  name: string
  groupName: string
  dateFrom: string
  dateTo: string
  /** Преподаватели одной ячейкой; несколько — через «;». */
  teacherName: string
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
  if (filters.name) params.set("name", filters.name)
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
