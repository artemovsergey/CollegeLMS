"use client"

import { useEffect, useState } from "react"
import {
  CalendarRange,
  Download,
  FileSpreadsheet,
  FileText,
  Loader2,
} from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import {
  downloadSchedule,
  downloadTemplate,
  getTemplates,
  type DocumentTemplate,
} from "@/api/documents"
import { toast } from "sonner"

function formatSize(bytes: number): string {
  return bytes > 0 ? `${(bytes / 1024).toFixed(0)} КБ` : "—"
}

export default function DispatcherDocumentsPage() {
  const [templates, setTemplates] = useState<DocumentTemplate[]>([])
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)

  useEffect(() => {
    getTemplates()
      .then(setTemplates)
      .catch((err) => toast.error(err instanceof Error ? err.message : "Ошибка загрузки"))
      .finally(() => setLoading(false))
  }, [])

  const handleDownload = (fileName: string) => {
    downloadTemplate(fileName).catch(() => toast.error("Не удалось скачать шаблон"))
  }

  const handleExportSchedule = async () => {
    setExporting(true)
    try {
      await downloadSchedule()
      toast.success("Файл расписания сформирован")
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : "Не удалось сформировать файл расписания",
      )
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-5xl mx-auto">
      <h2 className="text-xl font-semibold">Документы</h2>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <CalendarRange size={16} /> Итоговое расписание
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <p className="text-sm text-muted-foreground">
            Расписание из системы с учётом применённых корректировок — в том же
            формате, что и файл для импорта. Можно загрузить обратно без правок.
          </p>
          <p className="text-xs text-muted-foreground">
            Занятия с примечанием «сам.р.» в файл не попадают: они не являются
            парами расписания и хранятся в журнале изменений.
          </p>
          <Button
            onClick={() => void handleExportSchedule()}
            disabled={exporting}
            className="w-fit"
          >
            {exporting ? (
              <>
                <Loader2 size={16} className="animate-spin" aria-hidden />
                Формирование...
              </>
            ) : (
              <>
                <FileSpreadsheet size={16} aria-hidden />
                Скачать Расписание.xlsx
              </>
            )}
          </Button>
        </CardContent>
      </Card>

      {loading && <Loader2 className="size-6 animate-spin text-muted-foreground mx-auto py-20" />}

      {!loading && templates.length === 0 && (
        <Card>
          <CardContent>
            <p className="text-sm text-muted-foreground py-10 text-center">Шаблоны не найдены</p>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {templates.map((t) => (
          <Card key={t.fileName}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-base">
                <FileText size={16} /> {t.name}
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <p className="text-sm text-muted-foreground">{t.description}</p>
              <p className="text-xs text-muted-foreground">
                {t.fileName} · {formatSize(t.size)}
              </p>
              <Button onClick={() => handleDownload(t.fileName)} disabled={t.size === 0}>
                <Download size={16} /> Скачать
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
