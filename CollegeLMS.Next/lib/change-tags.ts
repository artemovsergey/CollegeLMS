import type { ChangeTag, CorrectionChangeType } from "@/types/correction"

/** «сам.р+» — самостоятельная работа, которая всё же добавляется в расписание. */
const PHYSICAL_SELF_STUDY_NOTE_RE = /сам[\s./-]*р[\s.]*\+/i
/** «вм.4 п.» — пара, откуда переносится занятие. */
const MOVE_PAIR_NOTE_RE = /вм\.?\s*(\d{1,2})/i

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

/** Самостоятельная работа с добавлением в расписание («сам.р+»). */
export function isPhysicalSelfStudyNote(note: string | null | undefined): boolean {
  return PHYSICAL_SELF_STUDY_NOTE_RE.test(note ?? "")
}

/** Пара только для информирования: в расписание не встаёт, нужна пометка. */
export function isInformationalNote(note: string | null | undefined): boolean {
  return isSelfStudyNote(note) && !isPhysicalSelfStudyNote(note)
}

/** Пара «откуда» из примечания или null. */
export function movePairFromNote(note: string | null | undefined): number | null {
  const match = MOVE_PAIR_NOTE_RE.exec(note ?? "")
  return match ? Number(match[1]) : null
}

const CHANGE_LABEL: Record<CorrectionChangeType, string> = {
  Add: "Добавлено",
  Remove: "Снято",
  // Замена и перенос остались в базе после объединения операций. Показываем их
  // тем же, чем они и являются, — добавлением занятия в пару.
  Replace: "Добавлено",
  Move: "Добавлено",
}

function changeTypeLabel(changeType: CorrectionChangeType): string {
  return CHANGE_LABEL[changeType]
}

/** Исход изменения для показа: добавлено или снято. */
export function changeTagKind(
  changeType: CorrectionChangeType,
): "Add" | "Remove" {
  return changeType === "Remove" ? "Remove" : "Add"
}

/**
 * Приоритет бейджа в паре: важнее то, что студент обязан заметить в первую
 * очередь. Самостоятельная работа важнее снятия (пара остаётся в расписании и
 * просто помечается), снятие важнее добавления.
 */
const PRIORITY: Record<CorrectionChangeType, number> = {
  Remove: 0,
  Add: 3,
  Replace: 1,
  Move: 2,
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
  // Пара «только для информирования» и снятая с пометкой — это самостоятельная
  // работа: говорить «Добавлено» или «Снято» было бы неправдой.
  if (
    isSelfStudyNote(tag.note)
    && (tag.changeType === "Remove" || isInformationalNote(tag.note))
  )
    return "Сам.р."
  return changeTypeLabel(tag.changeType)
}

/** Нужен ли рядом с подписью операции отдельный бейдж «Сам.р.». */
export function showsSelfStudyTag(tag: ChangeTag): boolean {
  return (
    isSelfStudyNote(tag.note)
    && tag.changeType !== "Remove"
    && !isInformationalNote(tag.note)
  )
}

function tagDetail(tag: ChangeTag): string[] {
  const parts = [changeTagLabel(tag), `неделя ${tag.week}`]
  if (isSelfStudyNote(tag.note)) {
    parts.push(
      isInformationalNote(tag.note)
        ? "только для информирования, в расписание не встала"
        : "самостоятельная работа",
    )
  }
  if (tag.changeType !== "Remove") {
    const from = movePairFromNote(tag.note) ?? tag.removedNumberPair
    if (from != null && from !== tag.removedNumberPair) {
      parts.push(`с пары ${from}`)
    }
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
