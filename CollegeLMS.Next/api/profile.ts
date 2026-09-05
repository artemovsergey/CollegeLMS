import api from "@/lib/api"
import type { Result } from "@/types"
import type { ProfileResponse } from "@/types"

export async function fetchProfile(): Promise<Result<ProfileResponse>> {
  const { data } = await api.get<Result<ProfileResponse>>("/api/auth/profile")
  return data
}
