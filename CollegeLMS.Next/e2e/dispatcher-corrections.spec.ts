import { mkdirSync, readFileSync, writeFileSync } from "node:fs"
import { join } from "node:path"
import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from "@playwright/test"
import {
  addPositionByUi,
  apiLogin,
  createBatchByUi,
  deleteBatch,
  exportBatchByUi,
  getBatch,
  importFileByUi,
  loadGroupDays,
  loginAsDispatcher,
  openCorrectionPage,
  PLAN_KIND,
  useDispatcherSession,
  type ApiPosition,
  type DispatcherSession,
} from "./helpers/corrections"
import { buildPlan } from "./helpers/correction-plan"
import {
  expectedRow,
  listReferenceFiles,
  parsePairCell,
  referenceCell,
  referenceChangeType,
  referenceDataRows,
  referenceDate,
  referenceForDate,
  referenceRow,
  REFERENCE_ROOT,
} from "./helpers/reference-files"
import { readXlsx, type Workbook } from "./helpers/xlsx"

/**
 * Операция «корректировка» проверяется в обе стороны.
 *
 * Импорт: каждая корректировка, которую диспетчер присылал на четырёх учебных
 * неделях, заливается через интерфейс — система обязана создать пакет, узнать
 * группы и преподавателей и найти ошибки в тех файлах, где они есть.
 *
 * Обратная операция: корректировки собираются через интерфейс, файл
 * выгружается и импортируется заново — позиции должны совпасть, а файл,
 * выгруженный из повторного импорта, — быть тем же самым.
 */

test.skip(
  ({ isMobile }) => isMobile,
  "Пошаговая форма позиции на мобильном экране не проверяется"
)

/** Сгенерированные файлы: их удобно открыть и посмотреть глазами. */
const ARTIFACTS = join(process.cwd(), "test-results", "корректировки")

/** Ошибки, которые система обязана находить в файлах диспетчера. */
const MISSING_PAIR = /некорректный № пары/
const IMPOSSIBLE_MOVE = /перенос невозможен/
const UNKNOWN_TEACHER = /преподаватель «.+» не найден/
const ANY_KNOWN_ERROR = new RegExp(
  `${MISSING_PAIR.source}|${IMPOSSIBLE_MOVE.source}|${UNKNOWN_TEACHER.source}`
)

/** Вторник и пятница каждой из четырёх учебных недель. */
const ROUND_TRIP_DATES = [
  "2026-09-01",
  "2026-09-04",
  "2026-09-08",
  "2026-09-11",
  "2026-09-15",
  "2026-09-18",
  "2026-09-22",
  "2026-09-25",
]

/** Позиция без технических полей — для сравнения «до» и «после» импорта. */
function comparable(position: ApiPosition) {
  return {
    row: position.row,
    changeType: position.changeType,
    groupName: position.groupName,
    numberPair: position.numberPair,
    subject: position.subject,
    teacherName: position.teacherName,
    removedSubject: position.removedSubject,
    removedTeacherName: position.removedTeacherName,
    // Пустое примечание приходит из файла строкой, а из базы — null.
    note: position.note ?? "",
    // У снятия номер пары «откуда» в файле не хранится: он и есть № пары.
    removedNumberPair:
      position.changeType === "Remove" ? null : position.removedNumberPair,
  }
}

function rowsOf(workbook: Workbook) {
  const sheet = workbook.sheets[0]
  return workbook.dataRows(sheet).map((row) =>
    [1, 2, 3, 4, 5, 6, 7].map((column) => workbook.cell(sheet, row, column))
  )
}

/**
 * Расхождения между двумя файлами корректировки — список строк, где значения
 * отличаются. Сравнивается содержимое, а не байты: внутри zip один и тот же
 * файл, выгруженный в разные секунды, различается отметкой времени в
 * `[Content_Types].xml`, и к содержимому корректировки это отношения не имеет.
 */
function contentDiff(left: Workbook, right: Workbook): string[] {
  const lines: string[] = []
  if (left.sheets.length !== right.sheets.length) {
    return [`листов: ${left.sheets.length} → ${right.sheets.length}`]
  }
  for (let index = 0; index < left.sheets.length; index++) {
    const a = left.sheets[index]
    const b = right.sheets[index]
    if (a.name !== b.name) lines.push(`лист ${index + 1}: «${a.name}» → «${b.name}»`)
    const rows = new Set([...a.cells.keys(), ...b.cells.keys()])
    for (const row of [...rows].sort((x, y) => x - y)) {
      const columns = new Set([
        ...(a.cells.get(row)?.keys() ?? []),
        ...(b.cells.get(row)?.keys() ?? []),
      ])
      for (const column of [...columns].sort((x, y) => x - y)) {
        const left2 = a.cells.get(row)?.get(column)
        const right2 = b.cells.get(row)?.get(column)
        if (left2 === right2) continue
        lines.push(
          `«${a.name}» ${row}/${column}: ${JSON.stringify(left2)} → ${JSON.stringify(right2)}`
        )
      }
    }
  }
  return lines
}

function referenceBytes(relative: string): Buffer {
  return readFileSync(join(REFERENCE_ROOT, relative))
}

async function systemGroupNames(
  request: APIRequestContext,
  token: string
): Promise<Set<string>> {
  const response = await request.get("/api/groups?pageSize=200", {
    headers: { Authorization: `Bearer ${token}` },
  })
  const groups = (await response.json()).data as { name: string }[]
  return new Set(groups.map((group) => group.name))
}

/**
 * Сессия одна на весь файл: вход на странице логина ограничен пятью попытками
 * в минуту, а проверок здесь больше. Саму форму входа проверяет отдельный тест.
 */
let session: DispatcherSession

test.beforeAll(async ({ request }) => {
  session = await apiLogin(request)
})

/** Открывает страницу корректировок от имени диспетчера. */
async function correctionPage(page: Page): Promise<void> {
  await useDispatcherSession(page, session)
  await openCorrectionPage(page)
}

test.describe("Импорт корректировок диспетчера", () => {
  test.setTimeout(600000)

  test("диспетчер входит по логину и паролю", async ({ page }) => {
    await loginAsDispatcher(page)
    await openCorrectionPage(page)
  })

  test("в папке лежат корректировки за все четыре учебные недели", () => {
    const files = listReferenceFiles()
    const weeks = [...new Set(files.map((file) => file.week))].sort()
    expect(weeks, "в папке должны быть недели 1–4").toEqual([1, 2, 3, 4])
    for (const week of weeks) {
      const days = files.filter((file) => file.week === week && file.readable)
      expect(days.length, `в неделе ${week} должны быть корректировки`).toBeGreaterThan(3)
    }
  })

  test("каждый файл недели импортируется и разбирается без потерь", async ({
    page,
    request,
  }) => {
    const files = listReferenceFiles().filter((file) => file.readable)
    expect(files.length, "корректировок за четыре недели").toBeGreaterThanOrEqual(15)

    const { token } = session
    const groups = await systemGroupNames(request, token)

    await correctionPage(page)

    for (const file of files) {
      const { batchId, status } = await importFileByUi(
        page,
        file.fileName,
        referenceBytes(file.relative)
      )
      expect(status, `${file.relative}: импорт должен пройти`).toBe(200)
      expect(batchId, `${file.relative}: пакет должен быть создан`).toBeTruthy()

      try {
        const batch = await getBatch(request, token, batchId!)

        // Дата и учебная неделя берутся из шапки файла, а не из его имени.
        expect(batch.correctionDate.slice(0, 10), file.relative).toBe(
          referenceDate(file.workbook!)
        )
        expect(batch.week, `${file.relative}: неделя из даты`).toBe(file.week)

        const dataRows = referenceDataRows(file.workbook!)
        expect(batch.positionCount, `${file.relative}: число позиций`).toBe(dataRows.length)
        expect(
          batch.positions.map((position) => position.row),
          `${file.relative}: строки идут подряд`
        ).toEqual(dataRows.map((_, index) => index + 1))

        // Группы узнаются по названию из файла — «ПО262» и «ПО 262» одно и то же.
        for (const position of batch.positions) {
          expect(
            groups.has(position.groupName),
            `${file.relative}: группа «${position.groupName}» не найдена в системе`
          ).toBe(true)
          expect(position.groupId, `${file.relative}: у группы есть id`).toBeTruthy()
        }

        // Каждая колонка файла попадает в своё поле позиции: № пары — в пару,
        // «вводится» — в предмет и преподавателя, примечание переносится как есть.
        // Пустые строки файла в пакет не попадают, поэтому строки сверяются по
        // номеру среди непустых, а не по номеру строки книги.
        for (const position of batch.positions) {
          const line = referenceRow(file.workbook!, dataRows[position.row - 1])
          const where = `${file.relative}: строка ${position.row}`
          expect(parsePairCell(line.pair), `${where}: № пары`).toBe(
            position.numberPair
          )
          expect(position.note ?? "", `${where}: примечание`).toBe(line.note)
          expect(position.changeType, `${where}: операция`).toBe(
            referenceChangeType(line)
          )
          expect(position.subject ?? null, `${where}: вводимый предмет`).toBe(
            line.addedSubject || null
          )
          expect(position.teacherName ?? null, `${where}: вводимый преподаватель`).toBe(
            line.addedTeacher || null
          )
        }

        // Каждая ошибка объяснима содержимым файла: либо не заполнен № пары,
        // либо перенос ссылается на пару, где занятия нет, либо нет преподавателя.
        const rowsWithoutPair = dataRows.filter(
          (row) => !referenceCell(file.workbook!, row, 6).trim()
        ).length
        expect(
          batch.errors.filter((error) => MISSING_PAIR.test(error.message)).length,
          `${file.relative}: строк без № пары`
        ).toBe(rowsWithoutPair)
        for (const error of batch.errors) {
          expect(
            error.message,
            `${file.relative}: неожиданная ошибка`
          ).toMatch(ANY_KNOWN_ERROR)
        }

        // Служебные слова примечания разбираются: «вм.X» становится парой «откуда»,
        // но только если она отличается от вводимой — перенос в ту же пару
        // невозможен, и такую строку система оставляет обычным добавлением.
        for (const position of batch.positions) {
          const note = position.note ?? ""
          const move = /вм\.?\s*(\d{1,2})/i.exec(note)
          if (!move) continue
          const from = Number(move[1])
          expect(
            position.removedNumberPair ?? null,
            `${file.relative}: «${note}» в строке ${position.row} (пара ${position.numberPair})`
          ).toBe(from === position.numberPair ? null : from)
        }
      } finally {
        await deleteBatch(request, token, batchId!)
      }
    }
  })

  test("служебный файл Excel отклоняется с понятным сообщением", async ({
    page,
  }) => {
    const locked = listReferenceFiles().find((file) => !file.readable)
    expect(locked, "в папке должен лежать служебный файл «~$…»").toBeTruthy()

    await correctionPage(page)

    const { batchId, status } = await importFileByUi(
      page,
      locked!.fileName,
      referenceBytes(locked!.relative)
    )
    expect(status).toBe(400)
    expect(batchId, "пакет из служебного файла создавать нельзя").toBeNull()
    await expect(page.getByText(/не является корректным XLSX/)).toBeVisible()
    // Интерфейс остался на вкладке импорта — редактор не открылся.
    await expect(page.getByText("Импорт корректировок из XLSX")).toBeVisible()
  })
})

test.describe("Обратная операция: интерфейс → XLSX → импорт", () => {
  test.setTimeout(300000)

  for (const date of ROUND_TRIP_DATES) {
    test(`корректировка за ${date} выдерживает круг`, async ({ page, request }) => {
      const { token } = session
      const created: string[] = []

      try {
        const days = await loadGroupDays(request, token, date)
        const plans = buildPlan(date, days)
        expect(
          plans,
          `на ${date} в расписании должно хватать данных на все виды позиций`
        ).not.toBeNull()

        await correctionPage(page)
        const firstId = await createBatchByUi(page, date)
        created.push(firstId)

        for (const plan of plans!) {
          await addPositionByUi(page, plan)
        }
        await expect(page.getByText(`Позиций: ${plans!.length}`)).toBeVisible()

        // Что сохранил интерфейс — эталон для сравнения с повторным импортом.
        const first = await getBatch(request, token, firstId)
        expect(first.errors, `пакет за ${date} собран без ошибок`).toEqual([])
        expect(first.positions.map((position) => position.row)).toEqual(
          plans!.map((_, index) => index + 1)
        )

        const file = await exportBatchByUi(page)
        expect(file.fileName).toBe(
          `Корректировка_${date.slice(8)}${date.slice(5, 7)}.xlsx`
        )

        const workbook = readXlsx(file.content)
        const sheet = workbook.sheets[0]
        expect(sheet?.name, "имя листа как в эталонных файлах").toBe("Лист1")

        // Шапка и заголовки колонок — те же, что у файлов диспетчера.
        const reference = referenceForDate(date)
        expect(reference, `для ${date} есть эталонный файл`).toBeTruthy()
        for (const row of [1, 2, 3, 5, 6]) {
          for (const column of [1, 2, 3, 4, 5, 6, 7]) {
            expect(
              workbook.cell(sheet, row, column),
              `${date}: ячейка ${row}/${column} как в эталоне`
            ).toBe(referenceCell(reference!.workbook!, row, column))
          }
        }

        // Строки файла — ровно те позиции, которые собраны в интерфейсе.
        expect(rowsOf(workbook)).toEqual(plans!.map((plan) => expectedRow(plan)))

        // Обратная операция: тот же файл импортируется заново.
        const { batchId: secondId, status } = await importFileByUi(
          page,
          file.fileName,
          file.content
        )
        expect(status, `повторный импорт файла за ${date}`).toBe(200)
        expect(secondId).toBeTruthy()
        created.push(secondId!)

        const second = await getBatch(request, token, secondId!)
        expect(second.errors, `повторный импорт за ${date} — без ошибок`).toEqual([])
        expect(second.correctionDate.slice(0, 10), "дата из шапки файла").toBe(date)
        expect(second.positionCount).toBe(first.positionCount)
        expect(second.positions.map(comparable)).toEqual(
          first.positions.map(comparable)
        )

        // И выгрузка из повторного импорта даёт тот же самый файл.
        const again = await exportBatchByUi(page)
        expect(again.fileName).toBe(file.fileName)
        const diff = contentDiff(workbook, readXlsx(again.content))
        if (diff.length > 0) {
          mkdirSync(ARTIFACTS, { recursive: true })
          writeFileSync(join(ARTIFACTS, `повтор-${file.fileName}`), again.content)
        }
        expect(
          diff,
          `${date}: файл повторного импорта отличается от исходного:\n${diff.join("\n")}`
        ).toEqual([])

        // Файлы остаются в test-results, чтобы их можно было открыть и посмотреть.
        mkdirSync(ARTIFACTS, { recursive: true })
        writeFileSync(join(ARTIFACTS, file.fileName), file.content)
        test.info().annotations.push({
          type: "позиции",
          description: plans!.map((plan) => PLAN_KIND[plan.kind]).join(", "),
        })
      } finally {
        for (const batchId of created) {
          await deleteBatch(request, token, batchId).catch(() => undefined)
        }
      }
    })
  }
})
