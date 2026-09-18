import api, { unwrap } from "@/lib/api"
import type { Result, PagedResponse } from "@/types"

/** Нерабочий период (праздник/выходной). */
export interface NonWorkingDay {
  id: string
  /** Дата начала (ISO). При одиночной дате совпадает с dateTo. */
  dateFrom: string
  /** Дата окончания (ISO). */
  dateTo: string
  title: string
}

export interface NonWorkingDayRequest {
  dateFrom: string
  dateTo: string
  title: string
}

export interface NonWorkingDayFilters {
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export async function fetchNonWorkingDays(
  filters: NonWorkingDayFilters = {},
): Promise<PagedResponse<NonWorkingDay>> {
  const params = new URLSearchParams()
  if (filters.from) params.set("from", filters.from)
  if (filters.to) params.set("to", filters.to)
  if (filters.page) params.set("page", String(filters.page))
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize))
  const qs = params.toString()
  return unwrap(
    await api.get<Result<PagedResponse<NonWorkingDay>>>(
      `/api/non-working-days${qs ? `?${qs}` : ""}`,
    ),
  )
}

export async function createNonWorkingDay(
  body: NonWorkingDayRequest,
): Promise<NonWorkingDay> {
  return unwrap(
    await api.post<Result<NonWorkingDay>>("/api/non-working-days", body),
  )
}

export async function updateNonWorkingDay(
  id: string,
  body: NonWorkingDayRequest,
): Promise<NonWorkingDay> {
  return unwrap(
    await api.put<Result<NonWorkingDay>>(`/api/non-working-days/${id}`, body),
  )
}

export async function deleteNonWorkingDay(id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/non-working-days/${id}`)
  if (!res.data.isSuccess) {
    throw new Error(res.data.errorMessage ?? "Не удалось удалить запись")
  }
}
