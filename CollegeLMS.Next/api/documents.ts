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
  const url = URL.createObjectURL(res.data)
  const a = document.createElement("a")
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}