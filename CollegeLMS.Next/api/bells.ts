import api, { unwrap } from "@/lib/api"
import type { Result } from "@/types"

/** Пара (звонок) из справочника времени. */
export interface BellSlot {
  id: string
  numberPair: number
  /** Время в формате "HH:mm:ss". */
  startTime: string
  /** Время в формате "HH:mm:ss". */
  endTime: string
}

/** Большая перемена. */
export interface BigBreak {
  id: string
  afterPair: number
  startTime: string
  endTime: string
}

/** Диапазон дат, на который действует профиль. */
export interface BellProfileDate {
  id: string
  dateFrom: string
  dateTo: string
}

/**
 * Профиль звонков: слоты, большая перемена, дни недели и диапазоны дат.
 * Default-профиль применяется, если дата и день недели не нашли своего профиля.
 */
export interface BellProfile {
  id: string
  name: string
  isDefault: boolean
  /** 1 = Пн … 7 = Вс. У default-профиля пусто. */
  daysOfWeek: number[]
  slots: BellSlot[]
  bigBreak: BigBreak | null
  dates: BellProfileDate[]
}

/** Справочник звонков default-профиля (обратная совместимость). */
export type BellSchedule = BellProfile

/** Слот на сохранение (Id назначает бэкенд). */
export interface BellSlotInput {
  numberPair: number
  startTime: string
  endTime: string
}

/** Большая перемена на сохранение. */
export interface BigBreakInput {
  afterPair: number
  startTime: string
  endTime: string
}

/** Диапазон дат на сохранение. */
export interface BellProfileDateInput {
  dateFrom: string
  dateTo: string
}

/** Тело PUT /api/bells — справочник default-профиля заменяется целиком. */
export interface UpdateBellScheduleRequest {
  slots: BellSlotInput[]
  bigBreak: BigBreakInput | null
}

/** Тело POST/PUT /api/bells/profiles. */
export interface BellProfileRequest {
  name: string
  daysOfWeek: number[]
  slots: BellSlotInput[]
  bigBreak: BigBreakInput | null
  dates: BellProfileDateInput[]
}

/** GET /api/bells — default-профиль звонков. */
export async function fetchBells(): Promise<BellProfile> {
  return unwrap(await api.get<Result<BellProfile>>("/api/bells"))
}

/** PUT /api/bells — обновить default-профиль (слоты и большая перемена). */
export async function updateBells(
  body: UpdateBellScheduleRequest,
): Promise<BellProfile> {
  return unwrap(await api.put<Result<BellProfile>>("/api/bells", body))
}

/** GET /api/bells/profiles — все профили, включая default. */
export async function fetchBellProfiles(): Promise<BellProfile[]> {
  return unwrap(
    await api.get<Result<BellProfile[]>>("/api/bells/profiles"),
  )
}

/** POST /api/bells/profiles — создать профиль (не-default). */
export async function createBellProfile(
  body: BellProfileRequest,
): Promise<BellProfile> {
  return unwrap(
    await api.post<Result<BellProfile>>("/api/bells/profiles", body),
  )
}

/** PUT /api/bells/profiles/{id} — обновить профиль. */
export async function updateBellProfile(
  id: string,
  body: BellProfileRequest,
): Promise<BellProfile> {
  return unwrap(
    await api.put<Result<BellProfile>>(`/api/bells/profiles/${id}`, body),
  )
}

/** DELETE /api/bells/profiles/{id} — удалить профиль (default нельзя). */
export async function deleteBellProfile(id: string): Promise<void> {
  const res = await api.delete<Result<null>>(`/api/bells/profiles/${id}`)
  if (!res.data.isSuccess) {
    throw new Error(res.data.errorMessage ?? "Не удалось удалить профиль")
  }
}

/** GET /api/bells/resolved?date=YYYY-MM-DD — профиль, действующий на дату. */
export async function fetchResolvedBells(date: string): Promise<BellProfile> {
  const qs = new URLSearchParams({ date })
  return unwrap(
    await api.get<Result<BellProfile>>(`/api/bells/resolved?${qs.toString()}`),
  )
}
