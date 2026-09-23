import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type { ScheduleValidationError } from "@/api/schedule"

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

/** Номера пар в учебном дне УП (1..8). */
export const PRACTICE_PAIR_NUMBERS = [1, 2, 3, 4, 5, 6, 7, 8] as const

/** Преподаватель практики (ответ API). */
export interface PracticeTeacher {
  id: string
  name: string
}

/** День учебной практики с точными номерами пар (1..8, без дублей). */
export interface PracticeDay {
  /** Дата (ISO). */
  date: string
  /** Номера пар в этот день. */
  pairNumbers: number[]
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
  /** Дни УП с номерами пар (только для вида УП). */
  days?: PracticeDay[] | null
  note: string | null
  /** Номер кабинета (для УП). */
  room?: string | null
  /** Номер подгруппы (для УП). */
  subgroup?: number | null
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
  /** Дни УП с номерами пар (только для вида УП). */
  days?: PracticeDay[]
  note?: string | null
  /** Номер кабинета (для УП). */
  room?: string | null
  /** Номер подгруппы (для УП). */
  subgroup?: number | null
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

/** Сумма пар по всем дням практики. */
export function practiceTotalPairs(practice: Practice): number {
  return practiceDays(practice).reduce(
    (sum, day) => sum + day.pairNumbers.length,
    0,
  )
}

/** Номера пар без дублей, только 1..8, по возрастанию. */
export function normalizePairNumbers(values: number[]): number[] {
  return Array.from(
    new Set(values.filter((value) => value >= 1 && value <= 8)),
  ).sort((a, b) => a - b)
}

/** День графика УП: дата и точные номера пар. */
export interface PracticeGraphDay {
  date: string
  pairNumbers: number[]
}

/** Строка графика УП — подгруппа с темой, кабинетом, днями и преподавателем. */
export interface PracticeGraphRow {
  row: number
  subgroup?: number | null
  name: string
  room?: string | null
  teacherName: string
  days: PracticeGraphDay[]
  note?: string | null
}

/** Превью импорта графика УП из DOCX. */
export interface PracticeGraphPreviewResponse {
  groupName?: string | null
  practiceName?: string | null
  dateFrom?: string | null
  dateTo?: string | null
  totalRows: number
  rows: PracticeGraphRow[]
  errors: ScheduleValidationError[]
}

/** Подтверждение импорта графика УП. */
export interface PracticeGraphConfirmRequest {
  groupName: string
  name: string
  dateFrom: string
  dateTo: string
  rows: PracticeGraphRow[]
}

export interface PracticeGraphConfirmResponse {
  imported: number
  practices: Practice[]
}

export interface PracticeGraphExportRequest {
  groupId: string
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

/** Превью импорта графика УП из DOCX (multipart, поле file). */
export async function previewPracticeGraphImport(
  file: File,
): Promise<PracticeGraphPreviewResponse> {
  const formData = new FormData()
  formData.append("file", file)
  return unwrap(
    await api.post<Result<PracticeGraphPreviewResponse>>(
      "/api/practices/import/graph/preview",
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  )
}

/** Подтверждение импорта графика УП (создаёт практики в транзакции). */
export async function confirmPracticeGraphImport(
  payload: PracticeGraphConfirmRequest,
): Promise<PracticeGraphConfirmResponse> {
  return unwrap(
    await api.post<Result<PracticeGraphConfirmResponse>>(
      "/api/practices/import/graph/confirm",
      payload,
    ),
  )
}

/** Ошибка эндпоинта экспорта приходит Blob'ом — извлекаем текст Result. */
async function readBlobErrorMessage(err: unknown): Promise<string | null> {
  const data = (err as { response?: { data?: unknown } })?.response?.data
  if (!(data instanceof Blob)) return null
  try {
    const text = await data.text()
    const parsed = JSON.parse(text) as {
      errorMessage?: string
      message?: string
    }
    return parsed.errorMessage ?? parsed.message ?? null
  } catch {
    return null
  }
}

/**
 * Имя файла из заголовка Content-Disposition. Сервер отдаёт оба варианта
 * (`filename=` с ASCII-заглушкой и `filename*=UTF-8''` с кириллицей) —
 * приоритет у второго, иначе в `a.download` попадёт «хвост» заголовка.
 */
function parseDownloadFilename(
  disposition: string | null | undefined,
  fallback: string,
): string {
  if (!disposition) return fallback

  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)
  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1].trim())
    } catch {
      return fallback
    }
  }

  const plain = disposition.match(/filename="?([^";]+)"?/i)
  return plain?.[1]?.trim() || fallback
}

/**
 * Экспорт графика УП группы в DOCX. Ответ — файл (не Result),
 * поэтому скачиваем как blob через createObjectURL. Имя берём из
 * Content-Disposition, при его отсутствии — «График УП {группа}.docx».
 */
export async function exportPracticeGraph(
  groupId: string,
  groupName?: string,
): Promise<void> {
  const safeGroup = groupName?.trim()
  const fallback = safeGroup ? `График УП ${safeGroup}.docx` : "График УП.docx"

  let blob: Blob
  let disposition: string | null = null
  try {
    const res = await api.post<Blob>(
      "/api/practices/graph/export",
      { groupId } satisfies PracticeGraphExportRequest,
      { responseType: "blob" },
    )
    blob = res.data
    disposition =
      (res.headers?.["content-disposition"] as string | undefined) ?? null
  } catch (err) {
    throw new Error(
      (await readBlobErrorMessage(err)) ??
        "Не удалось сформировать график УП",
    )
  }

  const filename = parseDownloadFilename(disposition, fallback)
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  // Отзываем ссылку с задержкой: синхронный revoke может прервать скачивание.
  window.setTimeout(() => URL.revokeObjectURL(url), 10_000)
}
