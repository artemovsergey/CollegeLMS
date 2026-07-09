import api from "@/lib/api"
import type { Result, PagedResponse } from "@/types"
import type { ScheduleResponse } from "@/types/schedule"

export interface ScheduleFilters {
  groupId?: string
  teacherId?: string
  dayOfWeek?: number
  period?: string
  page?: number
  pageSize?: number
}

export interface CreateScheduleRequest {
  groupId: string
  teacherId?: string | null
  subject: string
  room: string
  dayOfWeek: number
  startTime: string
  endTime: string
  lessonType: string
}

export interface UpdateScheduleRequest {
  groupId: string
  teacherId?: string | null
  subject: string
  room: string
  dayOfWeek: number
  startTime: string
  endTime: string
  lessonType: string
}

export interface ScheduleImportResult {
  imported: number
  skipped: number
  errors: { row: number; message: string }[]
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
): Promise<void> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null
  const params = new URLSearchParams()
  if (filters.groupId) params.set("groupId", filters.groupId)
  if (filters.teacherId) params.set("teacherId", filters.teacherId)
  if (filters.period) params.set("period", filters.period)
  params.set("format", format)

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

export async function importSchedule(
  file: File,
): Promise<Result<ScheduleImportResult>> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await api.post("/api/schedule/import", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}
