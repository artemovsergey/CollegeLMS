import api from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"

export interface ScheduleFilters {
  groupId?: string
  teacherId?: string
  dayOfWeek?: number
  period?: string
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
  startTime: string
  endTime: string
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
  startTime: string
  endTime: string
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
  row: number
  column: number
  message: string
}

export interface SchedulePreviewResult {
  totalEntries: number
  entries: SchedulePreviewEntry[]
  errors: ScheduleValidationError[]
}

export interface ScheduleImportResult {
  imported: number
  schedule: ScheduleResponse[]
}

export interface ScheduleCalendarResponse {
  weekStart: string
  days: {
    day: string
    dayOfWeek: number
    entries: ScheduleResponse[]
  }[]
}

export async function fetchScheduleCalendar(
  filters: Pick<ScheduleFilters, "groupId" | "teacherId"> = {},
): Promise<Result<ScheduleCalendarResponse>> {
  const params = new URLSearchParams({ view: "calendar" })
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)

  const { data } = await api.get<Result<ScheduleCalendarResponse>>(
    `/api/schedule?${params.toString()}`,
  )
  return data
}

export async function fetchSchedule(
  filters: ScheduleFilters = {},
): Promise<Result<PagedResponse<ScheduleResponse>>> {
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.dayOfWeek !== undefined)
    params.set("dayOfWeek", String(filters.dayOfWeek))
  if (filters.period) params.set("period", filters.period)
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

export function toIsoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
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

export interface JournalEntryItem {
  week: number
  date: string
  numberPairs: number[]
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
): Promise<Result<JournalResponse>> {
  const qs = teacherId ? `?teacherId=${teacherId}` : ""
  const { data } = await api.get<Result<JournalResponse>>(
    `/api/schedule/journal${qs}`,
  )
  return data
}

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
  layout: "grid" | "daycards" = "grid",
): Promise<void> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.period) params.set("period", filters.period)
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
): Promise<Result<ScheduleImportResult>> {
  const { data } = await api.post("/api/schedule/import/confirm", { entries })
  return data
}
