import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"

/** Рабочий период (зеркало нерабочего дня): перенос занятий на выходной. */
export interface WorkingDay {
  id: string
  /** Дата начала (ISO). При одиночной дате совпадает с dateTo. */
  dateFrom: string
  /** Дата окончания (ISO). */
  dateTo: string
  /** День недели, за который идёт работа (1 = Пн … 5 = Пт). */
  substituteDayOfWeek: number | null
  title: string
}

export interface WorkingDayRequest {
  dateFrom: string
  dateTo: string
  substituteDayOfWeek: number | null
  title: string
}

export interface WorkingDayFilters {
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export async function fetchWorkingDays(
  filters: WorkingDayFilters = {},
): Promise<PagedResponse<WorkingDay>> {
  const params = new URLSearchParams()
  if (filters.from) params.set("from", filters.from)
  if (filters.to) params.set("to", filters.to)
  if (filters.page) params.set("page", String(filters.page))
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize))
  const qs = params.toString()
  return unwrap(
    await api.get<Result<PagedResponse<WorkingDay>>>(
      `/api/working-days${qs ? `?${qs}` : ""}`,
    ),
  )
}

export async function createWorkingDay(
  body: WorkingDayRequest,
): Promise<WorkingDay> {
  return unwrap(
    await api.post<Result<WorkingDay>>("/api/working-days", body),
  )
}

export async function updateWorkingDay(
  id: string,
  body: WorkingDayRequest,
): Promise<WorkingDay> {
  return unwrap(
    await api.put<Result<WorkingDay>>(`/api/working-days/${id}`, body),
  )
}

export async function deleteWorkingDay(id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/working-days/${id}`)
  if (!res.data.isSuccess) {
    throw new Error(res.data.errorMessage ?? "Не удалось удалить запись")
  }
}
