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

/** Справочник звонков целиком. */
export interface BellSchedule {
  slots: BellSlot[]
  bigBreak: BigBreak | null
}

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

/** Тело PUT /api/bells — справочник заменяется целиком. */
export interface UpdateBellScheduleRequest {
  slots: BellSlotInput[]
  bigBreak: BigBreakInput | null
}

export async function fetchBells(): Promise<BellSchedule> {
  return unwrap(await api.get<Result<BellSchedule>>("/api/bells"))
}

export async function updateBells(
  body: UpdateBellScheduleRequest,
): Promise<BellSchedule> {
  return unwrap(await api.put<Result<BellSchedule>>("/api/bells", body))
}
