/**
 * Переходы между расписанием и журналом.
 *
 * Журнал собран по карточкам «группа + предмет», поэтому и ссылка на пару
 * несёт все три идентификатора: по одной паре её карточку можно открыть сразу,
 * без выбора группы и предмета вручную. Одинаково для веба (`/teacher/journal`)
 * и мини-аппа MAX (`/max/journal`) — отличается только адрес.
 */

/** Карточка журнала — пара «группа + предмет», поэтому ключ составной. */
export function journalCardKey(group: {
  groupId: string
  subject: string
}): string {
  return `${group.groupId}::${group.subject}`
}

/** Часть пары, достаточная для адреса журнала. */
export interface JournalTarget {
  teacherId: string | null
  groupId: string
  subject: string
}

/**
 * Адрес карточки журнала для пары расписания.
 * Без преподавателя журнала не существует (он строится по парам преподавателя),
 * поэтому возвращается пустая строка — вызывающий код ничего не показывает.
 */
export function journalHref(base: string, entry: JournalTarget): string {
  if (!entry.teacherId) return ""
  const params = new URLSearchParams({
    teacherId: entry.teacherId,
    groupId: entry.groupId,
    subject: entry.subject,
  })
  return `${base}?${params.toString()}`
}

export interface JournalLink {
  teacherId?: string
  groupId?: string
  subject?: string
}

/** Разбирает query-ссылку на журнал (из веба или из мини-аппа MAX). */
export function parseJournalLink(
  search: string | URLSearchParams,
): JournalLink {
  const params =
    typeof search === "string" ? new URLSearchParams(search) : search
  const link: JournalLink = {}
  const teacherId = params.get("teacherId")
  const groupId = params.get("groupId")
  const subject = params.get("subject")
  if (teacherId) link.teacherId = teacherId
  if (groupId) link.groupId = groupId
  if (subject) link.subject = subject
  return link
}
