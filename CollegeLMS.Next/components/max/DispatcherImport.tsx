"use client"

import { useRef, useState } from "react"
import { Upload, AlertCircle, CheckCircle2 } from "lucide-react"
import { Button, CellList, CellSimple, MaxUI, Typography } from "@maxhub/max-ui"
import { previewCorrection, confirmCorrection } from "@/api/correction"
import type {
  CorrectionPreviewResponse,
  CorrectionPreviewEntry,
} from "@/types/correction"
import type { ConfirmResult } from "@/types/correction"
import { extractErrorMessage } from "@/lib/utils"
import { WEEKDAYS } from "@/lib/max-lesson"

function dayLabel(dayOfWeek: number): string {
  return WEEKDAYS.find((d) => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
}

const CHANGE_SHORT: Record<CorrectionPreviewEntry["changeType"], string> = {
  Add: "Добавлено",
  Remove: "Снято",
  Replace: "Замена",
  Move: "Перенос",
}

function entryTitle(entry: CorrectionPreviewEntry): string {
  if (entry.changeType === "Replace" || entry.changeType === "Move") {
    return `${entry.removedSubject ?? "—"} → ${entry.subject ?? "—"}`
  }
  if (entry.changeType === "Remove") return entry.removedSubject ?? "—"
  return entry.subject ?? "—"
}

function entryBadge(entry: CorrectionPreviewEntry): string {
  return CHANGE_SHORT[entry.changeType]
}

export default function DispatcherImport({
  onApplied,
}: {
  onApplied: (result: ConfirmResult) => void
}) {
  const fileRef = useRef<HTMLInputElement | null>(null)
  const [preview, setPreview] = useState<CorrectionPreviewResponse | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [idempotencyKey, setIdempotencyKey] = useState<string | null>(null)

  const pick = (file: File | null) => {
    setPreview(null)
    setIdempotencyKey(null)
    setError(null)
    if (!file) return
    setPreviewing(true)
    void previewCorrection(file)
      .then((result) => {
        setPreview(result)
        setIdempotencyKey(crypto.randomUUID())
      })
      .catch((err) => setError(extractErrorMessage(err) ?? "Ошибка превью"))
      .finally(() => setPreviewing(false))
  }

  const apply = async () => {
    if (!preview || !idempotencyKey || confirming) return
    setConfirming(true)
    setError(null)
    try {
      const result = await confirmCorrection(preview.entries, idempotencyKey)
      onApplied(result)
      setPreview(null)
      setIdempotencyKey(null)
      if (fileRef.current) fileRef.current.value = ""
    } catch (err) {
      setError(
        extractErrorMessage(err) ??
          "Ошибка применения. Повторите с новым файлом",
      )
      setIdempotencyKey(null)
    } finally {
      setConfirming(false)
    }
  }

  const hasErrors = (preview?.errors.length ?? 0) > 0

  return (
    <div className="max-app__dispatcher-import">
      <label className="max-app__file-drop">
        <input
          ref={fileRef}
          type="file"
          accept=".xlsx,.xls"
          aria-label="Выберите файл корректировок XLSX"
          onChange={(e) => pick(e.target.files?.[0] ?? null)}
        />
        <Upload size={24} aria-hidden className="max-app__state-icon" />
        <Typography.Body>Откройте Excel-файл корректировок</Typography.Body>
        <Typography.Body className="max-app__note">
          Формат: день, неделя, группа, пара, предметы, преподаватели
        </Typography.Body>
      </label>

      {previewing ? (
        <Typography.Body className="max-app__note">Читаем файл…</Typography.Body>
      ) : preview ? (
        <>
          <div className="max-app__success-box">
            Дата корректировки: {preview.correctionDate} ·{" "}
            {dayLabel(preview.dayOfWeek)} · неделя {preview.week} ·{" "}
            {preview.totalEntries} строк, {preview.entries.length} валидных,{" "}
            {preview.errors.length} с ошибками
          </div>

          {hasErrors ? (
            <div className="max-app__errors">
              <p className="max-app__error">
                <AlertCircle size={14} aria-hidden /> Ошибки (
                {preview.errors.length})
              </p>
              {preview.errors.map((err, i) => (
                <p key={i} className="max-app__note">
                  Строка {err.row}, стлб. {err.column}: {err.message}
                </p>
              ))}
            </div>
          ) : null}

          <CellList mode="island">
            {preview.entries.map((entry) => (
              <CellSimple
                key={`${entry.row}:${entry.numberPair}`}
                separator
                overline={`${dayLabel(entry.dayOfWeek)}, пар ${entry.numberPair}`}
                title={entryTitle(entry)}
                before={
                  <span className={`max-app__badge max-app__badge--${entry.changeType.toLowerCase()}`}>
                    <CheckCircle2 size={12} aria-hidden /> {entryBadge(entry)}
                  </span>
                }
                subtitle={`${entry.groupName} · ${entry.teacherName ?? entry.removedTeacherName ?? "—"}${entry.note ? ` · ${entry.note}` : ""}`}
              />
            ))}
          </CellList>

          {error ? (
            <Typography.Body className="max-app__error">{error}</Typography.Body>
          ) : null}

          <Button
            stretched
            loading={confirming}
            disabled={hasErrors && preview.entries.length === 0}
            onClick={() => void apply()}
          >
            Применить изменения
          </Button>
        </>
      ) : (
        <>
          {error ? (
            <Typography.Body className="max-app__error">{error}</Typography.Body>
          ) : null}
        </>
      )}
    </div>
  )
}