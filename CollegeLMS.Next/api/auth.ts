import api from "@/lib/api"

export interface MaxAuthProfile {
  maxUserId: number
  fullName?: string | null
  role: "Student" | "Teacher" | "Other"
  groupId?: string | null
  groupName?: string | null
  teacherId?: string | null
  teacherName?: string | null
}

export interface MaxAuthResponse {
  token: string
  profile: MaxAuthProfile
}

interface ResultEnvelope<T> {
  isSuccess: boolean
  data: T | null
  errorMessage?: string | null
}

export async function loginWithMax(initData: string): Promise<MaxAuthResponse> {
  const res = await api.post<ResultEnvelope<MaxAuthResponse>>("/api/auth/max", { initData })
  if (!res.data.isSuccess || !res.data.data) {
    throw new Error(res.data.errorMessage ?? "Не удалось войти через MAX")
  }
  return res.data.data
}
