import api from "@/lib/api"
import type { Result } from "@/types"

export interface DispatcherLoginResponse {
  token: string
  expiresAt: string
}

export async function dispatcherLogin(
  password: string,
): Promise<DispatcherLoginResponse> {
  const { data } = await api.post<Result<DispatcherLoginResponse>>(
    "/api/dispatcher/login",
    { password },
  )
  if (!data.isSuccess || !data.data)
    throw new Error(data.errorMessage ?? "Ошибка входа")
  sessionStorage.setItem("dispatcherToken", data.data.token)
  return data.data
}

export function dispatcherToken(): string | null {
  return typeof window !== "undefined"
    ? sessionStorage.getItem("dispatcherToken")
    : null
}

export function dispatcherLogout(): void {
  sessionStorage.removeItem("dispatcherToken")
}

export function notifyDispatcherSession(): void {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new Event("max:dispatcher"))
  }
}