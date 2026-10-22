/**
 * Переходы между расписанием и журналом.
 *
 * Журнал собран по карточкам «группа + предмет», поэтому и ссылка на пару
 * несёт все три идентификатора: по одной паре её карточку можно открыть сразу,
 * без выбора группы и предмета вручную. Одинаково для веба (`/teacher/journal`)
 * и мини-аппа MAX (`/max/journal`) — отличается только адрес.
 */

import { isValidDate, parseIsoDate } from "@/api/schedule"

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

/** Число и месяц даты занятия — по частям, а не одной строкой «01.09». */
export interface JournalDateParts {
  day: string
  month: string
}

/**
 * Число и месяц даты занятия.
 *
 * Разбирается через `parseIsoDate`, а не `new Date`: бэкенд отдаёт метку
 * времени с `Z`, и в отрицательном смещении такая дата съезжала на день
 * назад. Пустые части — на случай мусора в deep-link, тогда строка
 * собирается из одной подписи «—».
 */
export function journalDateParts(value: string): JournalDateParts {
  const date = parseIsoDate(value)
  if (!isValidDate(date)) return { day: "", month: "" }
  const pad = (n: number) => String(n).padStart(2, "0")
  return { day: pad(date.getDate()), month: pad(date.getMonth() + 1) }
}

/**
 * Дата словами — текстом для программы чтения с экрана.
 *
 * Плитка даты показывает число и месяц по отдельности, и без этой подписи
 * они читались бы как два несвязанных числа.
 */
export function journalDateLabel(value: string): string {
  const date = parseIsoDate(value)
  if (!isValidDate(date)) return ""
  return date.toLocaleDateString("ru-RU", {
    day: "numeric",
    month: "long",
    year: "numeric",
  })
}

/**
 * Номера пар дня: каждая пара выводится отдельной записью журнала, поэтому
 * в строке обычно один номер. Словарь общий для веба и MAX — строка журнала
 * одна и та же, различается только подача.
 */
export function formatPairs(numbers: number[]): string {
  if (numbers.length === 0) return "Пар нет"
  if (numbers.length === 1) return `Пара ${numbers[0]}`
  return `Пары ${numbers.join(", ")}`
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
