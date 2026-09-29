import type { ScheduleInsert } from "@/api/inserts"
import type { ScheduleResponse } from "@/types/schedule"

export type DayRow =
  | { kind: "entry"; entry: MergedEntry }
  | { kind: "insert"; insert: ScheduleInsert }

/**
 * Пара после слияния. Пара — это время, а не предмет: в одном слоте законно
 * может быть несколько занятий (иностранный язык двумя преподавателями,
 * добавленная пара рядом с существующей). Такая пара показывается одной
 * карточкой со списком занятий, поэтому и бейдж изменений на неё один — иначе
 * «Добавлено» и «Снято» стояли бы рядом. `mergedEntries` хранит исходные
 * записи, чтобы по каждой можно было открыть редактирование.
 */
export interface MergedEntry extends ScheduleResponse {
  mergedEntries?: ScheduleResponse[]
}

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

function joinDistinct(values: (string | null | undefined)[]): string {
  const seen: string[] = []
  for (const value of values) {
    const text = (value ?? "").trim()
    if (text.length > 0 && !seen.includes(text)) seen.push(text)
  }
  return seen.join(" / ")
}

/**
 * Сливает записи одного слота: день, номер пары, группа и набор недель.
 * Предмет в ключ не входит намеренно — разные предметы в одном слоте это
 * одна пара, просто с несколькими занятиями.
 */
export function mergeSharedEntries(entries: ScheduleResponse[]): MergedEntry[] {
  const order: string[] = []
  const buckets = new Map<string, ScheduleResponse[]>()

  for (const entry of entries) {
    const key = [
      entry.dayOfWeek,
      entry.numberPair,
      entry.groupName.trim().toLowerCase(),
      [...entry.weeks].sort((a, b) => a - b).join(","),
    ].join("|")
    if (!buckets.has(key)) {
      buckets.set(key, [])
      order.push(key)
    }
    buckets.get(key)!.push(entry)
  }

  return order.map((key) => {
    const bucket = buckets.get(key)!
    if (bucket.length === 1) return { ...bucket[0] }

    const first = bucket[0]
    const tags = bucket
      .flatMap((entry) => entry.changeTags ?? [])
      .filter(
        (tag, index, all) =>
          all.findIndex(
            (other) =>
              other.changeType === tag.changeType &&
              other.week === tag.week &&
              other.removedSubject === tag.removedSubject &&
              other.note === tag.note,
          ) === index,
      )

    return {
      ...first,
      // В слоте может быть несколько предметов: в заголовке перечисляем их,
      // ниже каждое занятие показано своей строкой.
      subject: joinDistinct(bucket.map((entry) => entry.subject)),
      room: joinDistinct(bucket.map((entry) => entry.room)),
      teacherName: joinDistinct(bucket.map((entry) => entry.teacherName)) || null,
      changeTags: tags,
      mergedEntries: bucket,
    }
  })
}

export function mergeDayRows(
  entries: ScheduleResponse[],
  inserts: ScheduleInsert[],
): DayRow[] {
  const rows: DayRow[] = [
    ...mergeSharedEntries(entries).map((entry): DayRow => ({
      kind: "entry",
      entry,
    })),
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
