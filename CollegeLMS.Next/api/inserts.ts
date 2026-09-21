import api, { unwrap } from "@/lib/api"
import type { Result } from "@/types"

/** Дни недели, допустимые для вставки (воскресенье запрещено). */
export type InsertDayOfWeek =
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday"

/**
 * Вставка в расписание.
 * Бэкенд отдаёт dayOfWeek числом (0 — воскресенье … 6 — суббота),
 * а принимает строкой ("Monday"…"Saturday").
 */
export interface ScheduleInsert {
  id: string
  title: string
  dayOfWeek: number
  startTime: string
  endTime: string
  course: number | null
  isActive: boolean
}

export interface ScheduleInsertRequest {
  title: string
  dayOfWeek: InsertDayOfWeek
  startTime: string
  endTime: string
  course: number | null
  isActive: boolean
}

export interface InsertFilters {
  dayOfWeek?: InsertDayOfWeek
  course?: number
  activeOnly?: boolean
}

export async function fetchInserts(
  filters: InsertFilters = {},
): Promise<ScheduleInsert[]> {
  const params = new URLSearchParams()
  if (filters.dayOfWeek) params.set("dayOfWeek", filters.dayOfWeek)
  if (filters.course !== undefined) params.set("course", String(filters.course))
  if (filters.activeOnly !== undefined)
    params.set("activeOnly", String(filters.activeOnly))
  const qs = params.toString()
  return unwrap(
    await api.get<Result<ScheduleInsert[]>>(
      `/api/schedule/inserts${qs ? `?${qs}` : ""}`,
    ),
  )
}

export async function createInsert(
  body: ScheduleInsertRequest,
): Promise<ScheduleInsert> {
  return unwrap(
    await api.post<Result<ScheduleInsert>>("/api/schedule/inserts", body),
  )
}

export async function updateInsert(
  id: string,
  body: ScheduleInsertRequest,
): Promise<ScheduleInsert> {
  return unwrap(
    await api.put<Result<ScheduleInsert>>(`/api/schedule/inserts/${id}`, body),
  )
}

export async function deleteInsert(id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/schedule/inserts/${id}`)
  if (!res.data.isSuccess) {
    throw new Error(res.data.errorMessage ?? "Не удалось удалить событие")
  }
}
