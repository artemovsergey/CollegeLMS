import api, { unwrap } from "@/lib/api"
import type { Result } from "@/types"

export type LiveLessonStatus = "InLesson" | "Finished" | "NoPairs" | "Waiting"

export interface LivePairInfo {
  numberPair: number
  startTime: string
  endTime: string
}

export interface LiveEntry {
  groupId: string
  groupName: string
  subject: string
  room: string
  numberPair: number
  startTime: string
  endTime: string
  lessonType: string
  isPractice: boolean
  practiceName: string | null
  teacherId: string | null
  teacherName: string | null
}

export interface LiveEntityStatus {
  id: string
  name: string
  status: LiveLessonStatus
  totalPairs: number
  currentPair: LivePairInfo | null
  nextPair: LivePairInfo | null
  entries: LiveEntry[]
}

export interface LiveDashboardResponse {
  now: string
  date: string
  week: number
  dayOfWeek: number
  isWorkingDay: boolean
  isNonWorking: boolean
  workingDayTitle: string | null
  nonWorkingTitle: string | null
  counts: {
    inLesson: number
    finished: number
    noPairs: number
    waiting: number
  }
  teachers: LiveEntityStatus[]
  groups: LiveEntityStatus[]
}

export const LIVE_STATUS_LABELS: Record<LiveLessonStatus, string> = {
  InLesson: "Идут",
  Finished: "Закончились",
  NoPairs: "Нет пар",
  Waiting: "Ожидание",
}

export async function fetchLiveDashboard(
  date?: string,
): Promise<LiveDashboardResponse> {
  const qs = date ? `?date=${encodeURIComponent(date)}` : ""
  return unwrap(
    await api.get<Result<LiveDashboardResponse>>(`/api/dispatcher/live${qs}`),
  )
}
