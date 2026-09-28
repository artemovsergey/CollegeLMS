import api, { unwrap } from "@/lib/api"
import type { Result } from "@/types"

export interface DocumentTemplate {
  fileName: string
  name: string
  description: string
  size: number
}

export async function getTemplates(): Promise<DocumentTemplate[]> {
  const res = await api.get<Result<DocumentTemplate[]>>("/api/dispatcher/documents/templates")
  return unwrap(res)
}

export async function downloadTemplate(fileName: string): Promise<void> {
  const res = await api.get<Blob>(
    `/api/dispatcher/documents/templates/${encodeURIComponent(fileName)}/download`,
    { responseType: "blob" },
  )
  saveBlob(res.data, fileName)
}

/**
 * Скачать итоговое расписание: то, что сейчас в системе, с учётом применённых
 * корректировок. Формируется на сервере в формате файла импорта.
 */
export async function downloadSchedule(): Promise<void> {
  const res = await api.get<Blob>("/api/dispatcher/documents/schedule.xlsx", {
    responseType: "blob",
  })
  saveBlob(res.data, extractFileName(res.headers?.["content-disposition"]) ?? buildScheduleFileName())
}

/**
 * Имя файла приходит в Content-Disposition: в RFC 5987 (`filename*=UTF-8''…`)
 * с настоящим именем, в обычном `filename="…"` Framework заменяет кириллицу
 * подчёркиваниями. Читаем оба, как при скачивании корректировки.
 */
function extractFileName(disposition: string | undefined): string | null {
  if (!disposition) return null
  const utf8 = disposition.match(/filename\*\s*=\s*UTF-8'[^']*'([^;]+)/i)
  if (utf8?.[1]) {
    try {
      return decodeURIComponent(utf8[1].trim().replace(/^"|"$/g, ""))
    } catch {
      /* некорректный percent-encoding — пробуем обычный filename */
    }
  }
  const plain = disposition.match(/filename\s*=\s*"([^"]+)"|filename\s*=\s*([^;]+)/i)
  const value = plain?.[1] ?? plain?.[2]
  return value ? value.trim() : null
}

function buildScheduleFileName(date: Date = new Date()): string {
  const pad = (value: number) => String(value).padStart(2, "0")
  return `Расписание_${pad(date.getDate())}${pad(date.getMonth() + 1)}${String(
    date.getFullYear(),
  ).slice(-2)}_${pad(date.getHours())}${pad(date.getMinutes())}.xlsx`
}

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}