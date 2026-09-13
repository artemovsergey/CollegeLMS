import api from "@/lib/api"
import type { Result } from "@/types"

export interface NotificationSettingsDto {
  enabled: boolean
  time: string
  days: number[]
  nextNotifyAt?: string | null
}

export type NotificationSettingsInput = Pick<
  NotificationSettingsDto,
  "enabled" | "time" | "days"
>

function unwrap(res: {
  data: Result<NotificationSettingsDto>
}): NotificationSettingsDto {
  if (!res.data.isSuccess || !res.data.data)
    throw new Error(res.data.errorMessage ?? "Ошибка настроек")
  return res.data.data
}

export async function getNotificationSettings(): Promise<NotificationSettingsDto> {
  const res = await api.get<Result<NotificationSettingsDto>>(
    "/api/notifications/settings",
  )
  return unwrap(res)
}

export async function updateNotificationSettings(
  body: NotificationSettingsInput,
): Promise<NotificationSettingsDto> {
  const res = await api.put<Result<NotificationSettingsDto>>(
    "/api/notifications/settings",
    body,
  )
  return unwrap(res)
}