import type { ScheduleInsert } from "@/api/inserts"
import type { ScheduleResponse } from "@/types/schedule"

export type DayRow =
  | { kind: "entry"; entry: ScheduleResponse }
  | { kind: "insert"; insert: ScheduleInsert }

function startTimeOf(row: DayRow): string {
  return row.kind === "entry" ? row.entry.startTime : row.insert.startTime
}

function toMinutes(time: string): number | null {
  const match = /^(\d{1,2}):(\d{2})/.exec(typeof time === "string" ? time : "")
  if (!match) return null
  const hours = Number(match[1])
  const minutes = Number(match[2])
  if (!Number.isFinite(hours) || !Number.isFinite(minutes)) return null
  return hours * 60 + minutes
}

export function mergeDayRows(
  entries: ScheduleResponse[],
  inserts: ScheduleInsert[],
): DayRow[] {
  const rows: DayRow[] = [
    ...entries.map((entry): DayRow => ({ kind: "entry", entry })),
    ...inserts.map((insert): DayRow => ({ kind: "insert", insert })),
  ]

  return rows.sort((a, b) => {
    const aTime = startTimeOf(a)
    const bTime = startTimeOf(b)
    if (!aTime && !bTime) return 0
    if (!aTime) return 1
    if (!bTime) return -1
    if (aTime === bTime) return 0
    return aTime < bTime ? -1 : 1
  })
}

export function isEntryNow(
  entry: ScheduleResponse,
  dayOfWeek: number,
  week: number,
  now: Date = new Date(),
): boolean {
  if (entry.dayOfWeek !== dayOfWeek) return false
  if (!entry.weeks.includes(week)) return false

  const startMinutes = toMinutes(entry.startTime)
  const endMinutes = toMinutes(entry.endTime)
  if (startMinutes === null || endMinutes === null) return false

  const currentMinutes = now.getHours() * 60 + now.getMinutes()
  return currentMinutes >= startMinutes && currentMinutes < endMinutes
}
