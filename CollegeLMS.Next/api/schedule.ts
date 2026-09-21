import api from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type {
  ScheduleDayView,
  ScheduleMonthView,
  ScheduleResponse,
  ScheduleSemesterView,
  ScheduleWeekView,
} from "@/types/schedule"

export interface ScheduleFilters {
  groupId?: string
  teacherId?: string
  dayOfWeek?: number
  week?: number
  date?: string
  page?: number
  pageSize?: number
}

export interface CreateScheduleRequest {
  groupId: string
  teacherId?: string | null
  subject: string
  room: string
  dayOfWeek: number
  numberPair: number
  weeks: number[]
  lessonType: string
}

export interface UpdateScheduleRequest {
  groupId: string
  teacherId?: string | null
  subject: string
  room: string
  dayOfWeek: number
  numberPair: number
  weeks: number[]
  lessonType: string
}

export interface SchedulePreviewEntry {
  groupName: string
  day: string
  pair: number
  subject: string
  room: string
  teacherName: string
  weeks: number[]
  startTime: string
  endTime: string
}

export interface ScheduleValidationError {
  sheet: string
  row: number
  column: number
  level: string
  message: string
}

export interface SchedulePreviewResult {
  totalEntries: number
  entries: SchedulePreviewEntry[]
  errors: ScheduleValidationError[]
}

export interface ScheduleImportResult {
  isSuccess: boolean
  imported: number
  groups: number
  teachers: number
  schedule: ScheduleResponse[]
  errors: ScheduleValidationError[]
}

export class ScheduleImportError extends Error {
  readonly result: ScheduleImportResult

  constructor(result: ScheduleImportResult) {
    super(result.errors[0]?.message ?? "Не удалось загрузить расписание")
    this.name = "ScheduleImportError"
    this.result = result
  }
}

function isScheduleImportResult(value: unknown): value is ScheduleImportResult {
  if (typeof value !== "object" || value === null) return false
  const candidate = value as ScheduleImportResult
  return candidate.isSuccess === false && Array.isArray(candidate.errors)
}

export async function fetchSchedule(
  filters: ScheduleFilters = {},
): Promise<Result<PagedResponse<ScheduleResponse>>> {
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.dayOfWeek !== undefined)
    params.set("dayOfWeek", String(filters.dayOfWeek))
  if (filters.week !== undefined)
    params.set("week", String(filters.week))
  if (filters.date) params.set("date", filters.date)
  if (filters.page) params.set("page", String(filters.page))
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize))

  const qs = params.toString()
  const { data } = await api.get<
    Result<PagedResponse<ScheduleResponse>>
  >(`/api/schedule${qs ? `?${qs}` : ""}`)
  return data
}

export interface ScheduleMeta {
  semesterStart: string
  totalWeeks: number
  currentWeek: number
  currentDate: string
}

export async function fetchScheduleMeta(): Promise<Result<ScheduleMeta>> {
  const { data } = await api.get<Result<ScheduleMeta>>("/api/schedule/meta")
  return data
}

/** Серверный вид дня: пары, вставки, практики, нерабочий день. */
export async function fetchDayView(params: {
  date: string
  groupId?: string
  teacherId?: string
  room?: string
}): Promise<Result<ScheduleDayView>> {
  const qs = new URLSearchParams({ view: "day", date: params.date })
  if (params.groupId) qs.set("groupId", params.groupId)
  if (params.teacherId) qs.set("teacherId", params.teacherId)
  if (params.room) qs.set("room", params.room)
  const { data } = await api.get<Result<ScheduleDayView>>(
    `/api/schedule?${qs.toString()}`,
  )
  return data
}

/** Серверный вид недели: Пн–Сб с датами и слоями. */
export async function fetchWeekView(params: {
  week?: number
  date?: string
  groupId?: string
  teacherId?: string
}): Promise<Result<ScheduleWeekView>> {
  const qs = new URLSearchParams({ view: "week" })
  if (params.week !== undefined) qs.set("week", String(params.week))
  if (params.date) qs.set("date", params.date)
  if (params.groupId) qs.set("groupId", params.groupId)
  if (params.teacherId) qs.set("teacherId", params.teacherId)
  const { data } = await api.get<Result<ScheduleWeekView>>(
    `/api/schedule?${qs.toString()}`,
  )
  return data
}

/** Серверный вид семестра: требуется ровно один из groupId/teacherId. */
export async function fetchSemesterView(params: {
  groupId?: string
  teacherId?: string
}): Promise<Result<ScheduleSemesterView>> {
  const qs = new URLSearchParams({ view: "semester" })
  if (params.groupId) qs.set("groupId", params.groupId)
  if (params.teacherId) qs.set("teacherId", params.teacherId)
  const { data } = await api.get<Result<ScheduleSemesterView>>(
    `/api/schedule?${qs.toString()}`,
  )
  return data
}

/** Серверный вид календаря месяца: количество пар и маркеры по дням. */
export async function fetchMonthView(params: {
  month: string
  groupId?: string
  teacherId?: string
}): Promise<Result<ScheduleMonthView>> {
  const qs = new URLSearchParams({ view: "calendar", month: params.month })
  if (params.groupId) qs.set("groupId", params.groupId)
  if (params.teacherId) qs.set("teacherId", params.teacherId)
  const { data } = await api.get<Result<ScheduleMonthView>>(
    `/api/schedule?${qs.toString()}`,
  )
  return data
}

export function toIsoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

export function toIsoMonth(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, "0")
  return `${year}-${month}`
}

const DATE_ONLY_RE = /^\d{4}-\d{2}-\d{2}$/

// Бэкенд отдаёт DateTime в полном ISO-формате ("2026-09-14T00:00:00Z"),
// а deep-link может содержать мусор — приводим всё к YYYY-MM-DD.
export function normalizeDateOnly(value: unknown): string {
  if (typeof value !== "string" || value.length === 0) return ""
  const candidate = value.length > 10 ? value.slice(0, 10) : value
  return DATE_ONLY_RE.test(candidate) ? candidate : ""
}

export function isValidDate(date: unknown): date is Date {
  return date instanceof Date && !Number.isNaN(date.getTime())
}

export function parseIsoDate(value: string): Date {
  const normalized = normalizeDateOnly(value)
  if (!normalized) return new Date(Number.NaN)
  const [year, month, day] = normalized.split("-").map(Number)
  return new Date(year, month - 1, day)
}

export function toDateFromTime(time: string, base: Date = new Date()): Date {
  const [hours, minutes] = time.split(":").map(Number)
  const result = new Date(base)
  result.setHours(hours, minutes, 0, 0)
  return result
}

export async function fetchDaySchedule(params: {
  date: string
  groupId?: string
  teacherId?: string
}): Promise<Result<PagedResponse<ScheduleResponse>>> {
  return fetchSchedule({
    date: params.date,
    groupId: params.groupId,
    teacherId: params.teacherId,
    pageSize: 200,
  })
}

export interface ScheduleSearchGroup {
  id: string
  name: string
  course: number
}

export interface ScheduleSearchTeacher {
  id: string
  fullName: string
  position: string | null
}

export interface ScheduleSearchResponse {
  groups: ScheduleSearchGroup[]
  teachers: ScheduleSearchTeacher[]
  totalGroups: number
  totalTeachers: number
}

export async function searchSchedule(
  q: string,
  page = 1,
  pageSize = 20,
): Promise<Result<ScheduleSearchResponse>> {
  const qs = new URLSearchParams({
    q,
    page: String(page),
    pageSize: String(pageSize),
  })
  const { data } = await api.get<Result<ScheduleSearchResponse>>(
    `/api/schedule/search?${qs.toString()}`,
  )
  return data
}

export interface SubjectsResponse {
  subjects: string[]
}

export async function fetchSubjects(
  q = "",
  teacherId?: string,
): Promise<Result<SubjectsResponse>> {
  const qs = new URLSearchParams()
  if (q) qs.set("q", q)
  if (teacherId) qs.set("teacherId", teacherId)
  const suffix = qs.toString()
  const { data } = await api.get<Result<SubjectsResponse>>(
    `/api/schedule/subjects${suffix ? `?${suffix}` : ""}`,
  )
  return data
}

export interface JournalEntryItem {
  week: number
  dayOfWeek: number
  date: string
  numberPairs: number[]
  /** Бейджи корректировок: Add, Replace, Move, Remove, SelfStudy. */
  changeTypes: string[]
}

export interface JournalSubjectGroup {
  subject: string
  items: JournalEntryItem[]
  pairCount: number
}

export interface JournalResponse {
  teacherId: string
  teacherName: string
  subjects: JournalSubjectGroup[]
  totalPairCount: number
}

export async function fetchJournal(
  teacherId?: string,
  subject?: string,
): Promise<Result<JournalResponse>> {
  const qs = new URLSearchParams()
  if (teacherId) qs.set("teacherId", teacherId)
  if (subject) qs.set("subject", subject)
  const suffix = qs.toString()
  const { data } = await api.get<Result<JournalResponse>>(
    `/api/schedule/journal${suffix ? `?${suffix}` : ""}`,
  )
  return data
}

export interface ScheduleContextResponse {
  teacherId: string | null
  teacherName: string | null
  groupId: string | null
  groupName: string | null
  role: string
}

/** Личный контекст расписания текущего пользователя (роль, группа, преподаватель). */
export async function fetchScheduleContext(): Promise<
  Result<ScheduleContextResponse>
> {
  const { data } = await api.get<Result<ScheduleContextResponse>>(
    "/api/schedule/context",
  )
  return data
}

export type ScheduleExportScope = "day" | "week" | "semester"

export async function createSchedule(
  body: CreateScheduleRequest,
): Promise<Result<ScheduleResponse>> {
  const { data } = await api.post("/api/schedule", body)
  return data
}

export async function updateSchedule(
  id: string,
  body: UpdateScheduleRequest,
): Promise<Result<ScheduleResponse>> {
  const { data } = await api.put(`/api/schedule/${id}`, body)
  return data
}

export async function deleteSchedule(
  id: string,
): Promise<Result<null>> {
  const { data } = await api.delete(`/api/schedule/${id}`)
  return data
}

export async function exportSchedule(
  filters: ScheduleFilters,
  format: "pdf" | "xlsx",
  layout: "grid" | "daycards",
  scope: ScheduleExportScope,
  opts: { date?: string; week?: number } = {},
): Promise<void> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  params.set("scope", scope)
  if (opts.date) params.set("date", opts.date)
  if (opts.week !== undefined) params.set("week", String(opts.week))
  params.set("format", format)
  params.set("layout", layout)

  const response = await fetch(
    `/api/schedule/export?${params.toString()}`,
    {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    },
  )

  if (!response.ok) {
    const err = await response.json().catch(() => null)
    throw new Error(err?.errorMessage ?? "Ошибка экспорта")
  }

  const blob = await response.blob()
  const disposition = response.headers.get("Content-Disposition")
  const match = disposition?.match(/filename="?(.+?)"?$/)
  const filename = match?.[1] ?? `schedule.${format}`
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(url)
}

export async function previewScheduleImport(
  file: File,
): Promise<Result<SchedulePreviewResult>> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await api.post("/api/schedule/import/preview", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}

export async function confirmScheduleImport(
  entries: SchedulePreviewEntry[],
): Promise<ScheduleImportResult> {
  try {
    const { data } = await api.post<ScheduleImportResult>(
      "/api/schedule/import/confirm",
      { entries },
    )
    return data
  } catch (err: unknown) {
    const response = (
      err as { response?: { status?: number; data?: unknown } }
    )?.response
    if (response?.status === 400 && isScheduleImportResult(response.data)) {
      throw new ScheduleImportError(response.data)
    }
    throw err
  }
}
