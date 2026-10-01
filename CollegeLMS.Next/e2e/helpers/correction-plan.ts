import type { GroupDay, PositionPlan } from "./corrections"

/**
 * План позиций строится из настоящего расписания на дату корректировки:
 * интерфейс не даёт ввести заведомо неверную позицию, поэтому «занятая пара»,
 * «свободная пара» и «пара, откуда переносится» берутся из данных, а не
 * выдумываются. Обход групп и пар детерминирован, чтобы падение теста
 * воспроизводилось.
 */

const MAX_PAIR = 7

/** Занятия пары, которые действительно стоят в расписании (пометки — нет). */
function physical(day: GroupDay, pair: number) {
  return day.entries.filter(
    (entry) => entry.numberPair === pair && !entry.informational
  )
}

/** Пары, в которых стоит ровно одно занятие: иначе в форме пришлось бы выбирать. */
function soloPairs(day: GroupDay): number[] {
  const out: number[] = []
  for (let pair = 1; pair <= MAX_PAIR; pair++) {
    if (physical(day, pair).length === 1) out.push(pair)
  }
  return out
}

function freePairs(day: GroupDay): number[] {
  const out: number[] = []
  for (let pair = 1; pair <= MAX_PAIR; pair++) {
    if (physical(day, pair).length === 0) out.push(pair)
  }
  return out
}

/** Преподаватель с предметом, отличным от `avoid`: замена не должна быть «на то же самое». */
function otherTeacher(
  day: GroupDay,
  avoid: string
): { teacher: string; subject: string } | null {
  for (const teacher of day.teachers) {
    const subject = teacher.subjects.find((item) => item !== avoid)
    if (subject) return { teacher: teacher.fullName, subject }
  }
  return null
}

function anyTeacher(day: GroupDay): { teacher: string; subject: string } | null {
  const teacher = day.teachers[0]
  const subject = teacher?.subjects[0]
  return teacher && subject ? { teacher: teacher.fullName, subject } : null
}

/** Условия, которым группа должна удовлетворять для нужного вида позиции. */
type Matcher = (day: GroupDay) => PositionPlan | null

const MATCHERS: { kind: PositionPlan["kind"]; match: Matcher }[] = [
  // Снятие занятия из пары.
  {
    kind: "remove",
    match: (day) => {
      const pair = soloPairs(day)[0]
      if (pair == null) return null
      const current = physical(day, pair)[0]
      return {
        kind: "remove",
        group: day.groupName,
        pair,
        subject: current.subject,
        removedTeacherName: current.teacherName ?? "",
      }
    },
  },
  // Замена: прежнее занятие пары снимается, вводится другое.
  {
    kind: "replace",
    match: (day) => {
      const pair = soloPairs(day)[0]
      if (pair == null) return null
      const current = physical(day, pair)[0]
      const next = otherTeacher(day, current.subject)
      if (!next) return null
      return {
        kind: "replace",
        group: day.groupName,
        pair,
        removedSubject: current.subject,
        removedTeacherName: current.teacherName ?? "",
        subject: next.subject,
        teacher: next.teacher,
      }
    },
  },
  // Ввод в свободную пару.
  {
    kind: "add",
    match: (day) => {
      const pair = freePairs(day)[0]
      if (pair == null) return null
      const next = anyTeacher(day)
      if (!next) return null
      return {
        kind: "add",
        group: day.groupName,
        pair,
        subject: next.subject,
        teacher: next.teacher,
      }
    },
  },
  // Ввод вторым занятием в занятую пару: прежнее остаётся.
  {
    kind: "add-parallel",
    match: (day) => {
      const pair = soloPairs(day)[0]
      if (pair == null) return null
      const next = otherTeacher(day, physical(day, pair)[0].subject)
      if (!next) return null
      return {
        kind: "add-parallel",
        group: day.groupName,
        pair,
        subject: next.subject,
        teacher: next.teacher,
      }
    },
  },
  // Перенос: одно и то же занятие преподавателя стоит в двух парах этого дня,
  // вводим его в свободную пару — в файле это «вм.X».
  {
    kind: "move",
    match: (day) => {
      const target = freePairs(day)[0]
      if (target == null) return null
      for (let from = 1; from <= MAX_PAIR; from++) {
        for (const entry of physical(day, from)) {
          if (!entry.teacherName) continue
          const same = (other: (typeof entry) & { pair?: number }) =>
            other.subject === entry.subject && other.teacherName === entry.teacherName
          if (physical(day, from).filter(same).length !== 1) continue
          if (physical(day, target).some(same)) continue
          return {
            kind: "move",
            group: day.groupName,
            pair: target,
            fromPair: from,
            subject: entry.subject,
            teacher: entry.teacherName,
          }
        }
      }
      return null
    },
  },
  // Самостоятельная работа только пометкой — в расписание не встаёт.
  {
    kind: "selfstudy",
    match: (day) => {
      const pair = freePairs(day)[0]
      if (pair == null) return null
      const next = anyTeacher(day)
      if (!next) return null
      return {
        kind: "selfstudy",
        group: day.groupName,
        pair,
        subject: next.subject,
        teacher: next.teacher,
      }
    },
  },
  // Самостоятельная работа с вводом пары в расписание.
  {
    kind: "selfstudy-schedule",
    match: (day) => {
      const pair = freePairs(day)[0]
      if (pair == null) return null
      const next = anyTeacher(day)
      if (!next) return null
      return {
        kind: "selfstudy-schedule",
        group: day.groupName,
        pair,
        subject: next.subject,
        teacher: next.teacher,
      }
    },
  },
]

/**
 * Подбирает по одной позиции каждого вида на дату. Каждая позиция — в своей
 * группе: иначе шаги формы видели бы пометки предыдущих позиций пакета.
 * Возвращает `null`, если для какого-то вида позиции в данных не хватило.
 */
export function buildPlan(date: string, days: GroupDay[]): PositionPlan[] | null {
  const used = new Set<string>()
  const plans: PositionPlan[] = []

  for (const { match } of MATCHERS) {
    let found: PositionPlan | null = null
    for (const day of days) {
      if (used.has(day.groupId)) continue
      const plan = match(day)
      if (!plan) continue
      used.add(day.groupId)
      found = plan
      break
    }
    if (!found) return null
    plans.push(found)
  }

  return plans
}
