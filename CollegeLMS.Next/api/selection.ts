import api from "@/lib/api"
import type { MaxAuthResponse, ResultEnvelope } from "@/api/auth"

/**
 * Сохраняет выбор группы или преподавателя в боте MAX.
 * Ровно одна цель: либо groupId, либо teacherId.
 * Ответ — тот же формат, что и у POST /api/auth/max.
 */
export async function saveMaxSelection(target: {
  groupId?: string
  teacherId?: string
}): Promise<MaxAuthResponse> {
  const res = await api.post<ResultEnvelope<MaxAuthResponse>>(
    "/api/auth/max/selection",
    target,
  )
  if (!res.data.isSuccess || !res.data.data) {
    throw new Error(res.data.errorMessage ?? "Не удалось сохранить выбор")
  }
  return res.data.data
}
