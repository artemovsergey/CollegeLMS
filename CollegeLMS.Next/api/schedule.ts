import api from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"

export interface ScheduleFilters {
  groupId?: string
  teacherId?: string
  dayOfWeek?: number
  period?: string
  week?: number
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
  if (filters.page) params.set("page", String(filters.page))
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize))

  const qs = params.toString()
  const { data } = await api.get<
    Result<PagedResponse<ScheduleResponse>>
  >(`/api/schedule${qs ? `?${qs}` : ""}`)
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
