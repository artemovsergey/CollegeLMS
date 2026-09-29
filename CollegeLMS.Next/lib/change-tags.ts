import type { ChangeTag, CorrectionChangeType } from "@/types/correction"

const SELF_STUDY_NOTE_RE = /сам[\s./-]*р/i
/**
 * Разделитель замены и переноса. Тонкий и приглушённый: раньше стрелка была
 * заметнее самих названий предметов и перетягивала на себя внимание.
 */
export const REPLACE_ARROW_CLASS =
  "text-xs font-normal text-muted-foreground/70"

export function isSelfStudyNote(note: string | null | undefined): boolean {
  return SELF_STUDY_NOTE_RE.test(note ?? "")
}

const CHANGE_LABEL: Record<CorrectionChangeType, string> = {
  Add: "Добавлено",
  Remove: "Снято",
  Replace: "Замена",
  Move: "Перенос",
}

function changeTypeLabel(changeType: CorrectionChangeType): string {
  return CHANGE_LABEL[changeType]
}

/**
 * Приоритет бейджа в паре: важнее то, что студент обязан заметить в первую очередь.
 * Самостоятельная работа важнее снятия (пара остаётся в расписании и просто
 * помечается), снятие важнее замены, замена — переноса и добавления.
 */
const PRIORITY: Record<CorrectionChangeType, number> = {
  Remove: 0,
  Replace: 1,
  Move: 2,
  Add: 3,
}

function rank(tag: ChangeTag): number {
  if (isSelfStudyNote(tag.note)) return -1
  return PRIORITY[tag.changeType] ?? 9
}

/**
 * Главный бейдж пары. Пара может получить несколько корректировок подряд —
 * показываем только один, чтобы бейджи не наезжали друг на друга, а остальные
 * показываем в подсказке.
 */
export function primaryChangeTag(tags: ChangeTag[]): ChangeTag | null {
  if (tags.length === 0) return null
  return [...tags].sort((a, b) => rank(a) - rank(b))[0]
}

/**
 * Подпись главного бейджа пары.
 *
 * «Сам.р.» подменяет подпись только при снятии: там пара остаётся в
 * расписании, поэтому «Снято» было бы неправдой. В остальных случаях пара
 * действительно меняется, и «Сам.р.» добавляется вторым бейджем, а не вместо
 * первого.
 */
export function changeTagLabel(tag: ChangeTag): string {
  if (isSelfStudyNote(tag.note) && tag.changeType === "Remove") return "Сам.р."
  return changeTypeLabel(tag.changeType)
}

/** Нужен ли рядом с подписью операции отдельный бейдж «Сам.р.». */
export function showsSelfStudyTag(tag: ChangeTag): boolean {
  return isSelfStudyNote(tag.note) && tag.changeType !== "Remove"
}

function tagDetail(tag: ChangeTag): string[] {
  const parts = [changeTagLabel(tag), `неделя ${tag.week}`]
  if (isSelfStudyNote(tag.note)) parts.push("самостоятельная работа")
  if (tag.changeType === "Move" && tag.removedNumberPair != null) {
    parts.push(`перенос с пары ${tag.removedNumberPair}`)
  }
  if (tag.removedSubject) {
    parts.push(`вместо: ${tag.removedSubject}`)
  }
  if (tag.note) {
    parts.push(`примечание: ${tag.note}`)
  }
  return parts
}

/**
 * Текст подсказки для пары: главный бейдж и, если корректировок было несколько,
 * остальные — иначе часть изменений была бы не видна.
 */
export function changeTagTooltip(tags: ChangeTag[]): string {
  if (tags.length === 0) return ""
  return [...tags]
    .sort((a, b) => rank(a) - rank(b))
    .map((tag) => tagDetail(tag).join(" — "))
    .join("\n")
}
