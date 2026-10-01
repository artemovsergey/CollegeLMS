import { inflateRawSync } from "node:zlib"

/**
 * Чтение XLSX без внешних зависимостей: xlsx — это zip с XML внутри, а
 * распаковать его можно встроенным `zlib`. Нужен, чтобы e2e проверял не
 * только «файл скачался», но и что в нём написано ровно то, что диспетчер
 * видел в интерфейсе.
 */

interface ZipEntry {
  name: string
  data: Buffer
}

const EOCD_SIGNATURE = 0x06054b50
const CENTRAL_SIGNATURE = 0x02014b50

/**
 * Распаковывает нужные файлы zip-архива. Шаблон с завершающим слэшем
 * означает «всё внутри папки», иначе — точное имя либо его суффикс.
 */
function unzip(buffer: Buffer, wanted: string[]): Map<string, Buffer> {
  const matches = (name: string) =>
    wanted.some((pattern) =>
      pattern.endsWith("/") ? name.startsWith(pattern) : name.endsWith(pattern)
    )

  let eocd = -1
  for (let i = buffer.length - 22; i >= 0; i--) {
    if (buffer.readUInt32LE(i) === EOCD_SIGNATURE) {
      eocd = i
      break
    }
  }
  if (eocd < 0) throw new Error("Не найден конец zip-архива")

  const count = buffer.readUInt16LE(eocd + 10)
  let offset = buffer.readUInt32LE(eocd + 16)
  const out = new Map<string, Buffer>()

  for (let n = 0; n < count; n++) {
    if (buffer.readUInt32LE(offset) !== CENTRAL_SIGNATURE) {
      throw new Error(`Повреждён central directory zip (запись ${n})`)
    }
    const method = buffer.readUInt16LE(offset + 10)
    const compressedSize = buffer.readUInt32LE(offset + 20)
    const nameLength = buffer.readUInt16LE(offset + 28)
    const extraLength = buffer.readUInt16LE(offset + 30)
    const commentLength = buffer.readUInt16LE(offset + 32)
    const localOffset = buffer.readUInt32LE(offset + 42)
    const name = buffer.toString("utf8", offset + 46, offset + 46 + nameLength)

    if (matches(name)) {
      // В локальном заголовке длина имени может отличаться от central directory.
      const localNameLength = buffer.readUInt16LE(localOffset + 26)
      const localExtraLength = buffer.readUInt16LE(localOffset + 28)
      const start = localOffset + 30 + localNameLength + localExtraLength
      const raw = buffer.subarray(start, start + compressedSize)
      out.set(
        name,
        method === 0 ? Buffer.from(raw) : inflateRawSync(new Uint8Array(raw))
      )
    }

    offset += 46 + nameLength + extraLength + commentLength
  }

  return out
}

function unescapeXml(value: string): string {
  return value
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&#(\d+);/g, (_, code) => String.fromCodePoint(Number(code)))
    .replace(/&amp;/g, "&")
}

/** Числовая колонка по буквенной ссылке ячейки: «A» → 1, «AB» → 28. */
function columnIndex(letters: string): number {
  let index = 0
  for (const char of letters) index = index * 26 + (char.charCodeAt(0) - 64)
  return index
}

/**
 * Имя элемента с необязательным префиксом пространства имён. Файлы диспетчера
 * разбираются без префикса, а выгрузка ClosedXML — с префиксом `x:`.
 */
function tag(name: string): string {
  return `(?:[\\w-]+:)?${name}`
}

function textOf(xml: string): string {
  return [...xml.matchAll(new RegExp(`<${tag("t")}(?:\\s[^>]*)?>([\\s\\S]*?)<\\/${tag("t")}>`, "g"))]
    .map((match) => unescapeXml(match[1]))
    .join("")
}

function readSharedStrings(xml: string): string[] {
  if (!xml) return []
  return [...xml.matchAll(new RegExp(`<${tag("si")}>([\\s\\S]*?)<\\/${tag("si")}>`, "g"))].map(
    (match) => textOf(match[1])
  )
}

/** Значение ячейки: строка, число или boolean. */
export type CellValue = string | number | boolean

function readSheet(
  xml: string,
  shared: string[]
): Map<number, Map<number, CellValue>> {
  const rows = new Map<number, Map<number, CellValue>>()
  const sheetData =
    new RegExp(`<${tag("sheetData")}>([\\s\\S]*?)<\\/${tag("sheetData")}>`).exec(xml)?.[1] ??
    xml

  const cellPattern = new RegExp(
    `<${tag("c")}\\s([^>]*?)(?:\\/>|>([\\s\\S]*?)<\\/${tag("c")}>)`,
    "g"
  )
  for (const cell of sheetData.matchAll(cellPattern)) {
    const attrs = cell[1]
    const body = cell[2] ?? ""
    const ref = /r="([A-Z]+)(\d+)"/.exec(attrs)
    if (!ref) continue
    const col = columnIndex(ref[1])
    const row = Number(ref[2])

    const type = /t="([^"]+)"/.exec(attrs)?.[1] ?? "n"
    const valuePattern = new RegExp(`<${tag("v")}>([\\s\\S]*?)<\\/${tag("v")}>`)
    let value: CellValue | undefined
    if (type === "s") {
      const index = Number(valuePattern.exec(body)?.[1])
      value = shared[index]
    } else if (type === "inlineStr") {
      value = textOf(body)
    } else if (type === "b") {
      value = new RegExp(`<${tag("v")}>1<\\/${tag("v")}>`).test(body)
    } else {
      const raw = valuePattern.exec(body)?.[1]
      if (raw != null && raw !== "") value = Number(raw)
    }
    if (value == null || value === "") continue

    const line = rows.get(row) ?? new Map<number, CellValue>()
    line.set(col, value)
    rows.set(row, line)
  }

  return rows
}

export interface Sheet {
  /** Название листа — «Лист1» в эталонных файлах диспетчера. */
  name: string
  /** Ячейки по индексу строки (1-based) и колонки (1-based). */
  cells: Map<number, Map<number, CellValue>>
  rowCount: number
}

export interface Workbook {
  sheets: Sheet[]
  /** Ячейка как строка; пустая ячейка — «». */
  cell(sheet: Sheet | undefined, row: number, column: number): string
  /** Непустые строки листа начиная с 7-й — тело таблицы корректировки. */
  dataRows(sheet: Sheet | undefined, firstDataRow?: number): number[]
}

/** Разбирает книгу XLSX и отдаёт листы с раскрытыми значениями ячеек. */
export function readXlsx(buffer: Buffer): Workbook {
  const entries = unzip(buffer, [
    "xl/workbook.xml",
    "xl/_rels/workbook.xml.rels",
    "xl/sharedStrings.xml",
    "xl/worksheets/",
  ])

  const workbookXml = entries.get("xl/workbook.xml")?.toString("utf8") ?? ""
  const relsXml =
    entries.get("xl/_rels/workbook.xml.rels")?.toString("utf8") ?? ""
  const shared = readSharedStrings(
    entries.get("xl/sharedStrings.xml")?.toString("utf8") ?? ""
  )

  const rels = new Map<string, string>()
  const relationshipPattern = new RegExp(`<${tag("Relationship")}\\b[^>]*/?>`, "g")
  for (const rel of relsXml.matchAll(relationshipPattern)) {
    const id = /Id="([^"]+)"/.exec(rel[0])?.[1]
    const target = /Target="([^"]+)"/.exec(rel[0])?.[1]
    if (id && target) rels.set(id, target)
  }

  const sheets: Sheet[] = []
  const sheetPattern = new RegExp(`<${tag("sheet")}\\b[^>]*/?>`, "g")
  for (const match of workbookXml.matchAll(sheetPattern)) {
    const tag = match[0]
    const name = /name="([^"]*)"/.exec(tag)?.[1] ?? ""
    const relId = /r:id="([^"]+)"/.exec(tag)?.[1]
    const target = relId ? rels.get(relId) : undefined
    if (!target) continue
    const path = target.startsWith("/") ? target.slice(1) : `xl/${target}`
    const xml = entries.get(path)?.toString("utf8")
    if (xml == null) continue
    const cells = readSheet(xml, shared)
    sheets.push({ name, cells, rowCount: cells.size })
  }

  return {
    sheets,
    cell(sheet, row, column) {
      const value = sheet?.cells.get(row)?.get(column)
      return value == null ? "" : String(value)
    },
    dataRows(sheet, firstDataRow = 7) {
      return [...(sheet?.cells.keys() ?? [])]
        .filter((row) => row >= firstDataRow)
        .sort((a, b) => a - b)
    },
  }
}
