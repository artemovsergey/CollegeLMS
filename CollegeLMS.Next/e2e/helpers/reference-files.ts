import { readdirSync, readFileSync } from "node:fs"
import { join } from "node:path"
import type { PositionPlan } from "./corrections"
import { readXlsx, type Workbook } from "./xlsx"

/**
 * Эталонные файлы диспетчера из `import/Корректировки` — папки по учебным
 * неделям. Тест читает их тем же разбором, что и система, чтобы сравнивать
 * сгенерированный файл с тем, что диспетчер присылает на импорт.
 */
export const REFERENCE_ROOT = join(process.cwd(), "..", "import", "Корректировки")

export interface ReferenceFile {
  /** Путь относительно папки `import/Корректировки`, например «1 неделя/01.09.xlsx». */
  relative: string
  /** Учебная неделя — из названия папки. */
  week: number
  fileName: string
  /** Настоящий файл, а не «~$…» — служебный файл Excel. */
  readable: boolean
  workbook: Workbook | null
}

const WEEK_FOLDER = /^(\d+)\s+неделя$/

function folderWeek(name: string): number {
  const week = WEEK_FOLDER.exec(name)?.[1]
  if (!week) throw new Error(`Папка «${name}» не похожа на учебную неделю`)
  return Number(week)
}

/** Все файлы во всех папках недель, отсортированные по неделе и имени. */
export function listReferenceFiles(): ReferenceFile[] {
  const files: ReferenceFile[] = []
  for (const folder of readdirSync(REFERENCE_ROOT).sort()) {
    if (!WEEK_FOLDER.test(folder)) continue
    const week = folderWeek(folder)
    for (const fileName of readdirSync(join(REFERENCE_ROOT, folder)).sort()) {
      if (!fileName.endsWith(".xlsx")) continue
      const path = join(REFERENCE_ROOT, folder, fileName)
      let workbook: Workbook | null = null
      try {
        workbook = readXlsx(readFileSync(path))
      } catch {
        // Служебный файл «~$19.09.xlsx», который Excel держит открытым, zip не
        // является: система обязана отклонить его с понятным сообщением.
      }
      files.push({
        relative: `${folder}/${fileName}`,
        week,
        fileName,
        readable: workbook != null,
        workbook,
      })
    }
  }
  return files
}

/** Дата корректировки из шапки файла: «на 04.09.2026 г. (пятница)». */
export function referenceDate(workbook: Workbook): string {
  const sheet = workbook.sheets[0]
  const header = workbook.cell(sheet, 3, 1)
  const match = /на\s+(\d{2})\.(\d{2})\.(\d{4})/.exec(header)
  if (!match) throw new Error(`Не найдена дата в шапке: «${header}»`)
  return `${match[3]}-${match[2]}-${match[1]}`
}

/** Номера строк с данными: непустые ячейки начиная с седьмой. */
export function referenceDataRows(workbook: Workbook): number[] {
  return workbook.dataRows(workbook.sheets[0])
}

export function referenceCell(
  workbook: Workbook,
  row: number,
  column: number
): string {
  return workbook.cell(workbook.sheets[0], row, column)
}

/**
 * Строка файла корректировки: группа, снимаемое, вводимое, № пары, примечание.
 * Значения обрезаны по краям — система тоже читает ячейки с обрезкой.
 */
export function referenceRow(workbook: Workbook, row: number) {
  const cell = (column: number) => referenceCell(workbook, row, column).trim()
  return {
    row,
    group: cell(1),
    removedSubject: cell(2),
    removedTeacher: cell(3),
    addedSubject: cell(4),
    addedTeacher: cell(5),
    pair: cell(6),
    note: cell(7),
  }
}

/**
 * № пары из ячейки в том же виде, в каком его понимает система: число, «2 п»
 * или «2п.». Пустая ячейка или негодный текст — пара не задана.
 */
export function parsePairCell(value: string): number {
  const text = value.trim()
  if (/^\d{1,2}$/.test(text)) return Number(text)
  const match = /^(\d{1,2})\s*п?\.?$/.exec(text)
  return match ? Number(match[1]) : 0
}

/** Операция строки по заполненным колонкам: снимается без вводимого — снятие. */
export function referenceChangeType(line: ReturnType<typeof referenceRow>) {
  const hasRemove = line.removedSubject.length > 0 || line.removedTeacher.length > 0
  const hasAdd = line.addedSubject.length > 0 || line.addedTeacher.length > 0
  return hasRemove && !hasAdd ? "Remove" : "Add"
}

/**
 * Ожидаемая строка файла корректировки для позиции. Порядок колонок — как в
 * эталонных файлах: группа, снимаемый предмет, снимаемый преподаватель,
 * вводимый предмет, вводимый преподаватель, № пары, примечание.
 */
export function expectedRow(plan: PositionPlan): string[] {
  if (plan.kind === "remove") {
    // У снятия снимаемое занятие — это и есть предмет позиции.
    return [
      plan.group,
      plan.subject,
      plan.removedTeacherName,
      "",
      "",
      String(plan.pair),
      "снять",
    ]
  }

  const note =
    plan.kind === "selfstudy"
      ? "сам.р."
      : plan.kind === "selfstudy-schedule"
        ? "сам.р+"
        : plan.kind === "move"
          ? `вм.${plan.fromPair} п.`
          : ""

  const removed: [string, string] =
    plan.kind === "replace"
      ? [plan.removedSubject, plan.removedTeacherName]
      : // У переноса в колонке «снимается» стоит освобождаемое занятие — оно же
        // вводимое, а номер пары «откуда» живёт в примечании.
        plan.kind === "move"
        ? [plan.subject, plan.teacher]
        : ["", ""]

  return [
    plan.group,
    ...removed,
    plan.subject,
    plan.teacher,
    String(plan.pair),
    note,
  ]
}

/** Эталон, который диспетчер присылал на эту дату: «2026-09-04» → «04.09.xlsx». */
export function referenceForDate(date: string): ReferenceFile | undefined {
  const [, month, day] = date.split("-")
  const name = `${day}.${month}.xlsx`
  return listReferenceFiles().find(
    (file) => file.readable && file.fileName === name
  )
}
