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
