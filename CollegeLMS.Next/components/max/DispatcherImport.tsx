"use client"

import { useRef, useState } from "react"
import { Upload, AlertCircle, CheckCircle2, Pencil } from "lucide-react"
import { Button, CellList, CellSimple, Input, MaxUI, Typography } from "@maxhub/max-ui"
import { applyBatch, importCorrection, updatePosition } from "@/api/correction"
import type { CorrectionImportResponse, CorrectionPosition, CorrectionApplyResult } from "@/types/correction"
import { extractErrorMessage } from "@/lib/utils"
import { WEEKDAYS } from "@/lib/max-lesson"

function dayLabel(dayOfWeek: number): string {
  return WEEKDAYS.find((d) => d.value === dayOfWeek)?.full ?? String(dayOfWeek)
}

const CHANGE_SHORT: Record<CorrectionPosition["changeType"], string> = {
  Add: "Добавлено",
  Remove: "Снято",
  Replace: "Замена",
  Move: "Перенос",
}

function entryTitle(entry: CorrectionPosition): string {
  if (entry.changeType === "Replace" || entry.changeType === "Move") {
    return `${entry.removedSubject ?? "—"} → ${entry.subject ?? "—"}`
  }
  if (entry.changeType === "Remove") return entry.removedSubject ?? "—"
  return entry.subject ?? "—"
}

function entryBadge(entry: CorrectionPosition): string {
  return CHANGE_SHORT[entry.changeType]
}

export default function DispatcherImport({
  onApplied,
}: {
  onApplied: (result: CorrectionApplyResult) => void
}) {
  const fileRef = useRef<HTMLInputElement | null>(null)
  const [preview, setPreview] = useState<CorrectionImportResponse | null>(null)
  const [entries, setEntries] = useState<CorrectionPosition[]>([])
  const [editing, setEditing] = useState<number | null>(null)
  const [previewing, setPreviewing] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const pick = (file: File | null) => {
    setPreview(null)
    setEntries([])
    setEditing(null)
    setError(null)
    if (!file) return
    setPreviewing(true)
    void importCorrection(file)
      .then((result) => {
        setPreview(result)
        setEntries(result.positions)
      })
      .catch((err) => setError(extractErrorMessage(err) ?? "Ошибка превью"))
      .finally(() => setPreviewing(false))
  }

  const updateEntry = (row: number, patch: Partial<CorrectionPosition>) => {
    setEntries((prev) =>
      prev.map((e) => (e.row === row ? { ...e, ...patch } : e))
    )
  }

  const saveEntry = async (entry: CorrectionPosition) => {
    if (!preview?.batchId) return
    setError(null)
    try {
      const saved = await updatePosition(preview.batchId, entry.id, {
        changeType: entry.changeType,
        groupId: entry.groupId,
        groupName: entry.groupName,
        numberPair: entry.numberPair,
        subject: entry.subject,
        teacherId: entry.teacherId,
        teacherName: entry.teacherName,
        removedSubject: entry.removedSubject,
        removedTeacherId: entry.removedTeacherId,
        removedTeacherName: entry.removedTeacherName,
        removedNumberPair: entry.removedNumberPair,
        note: entry.note,
      })
      setEntries((prev) => prev.map((item) => item.id === saved.id ? saved : item))
      setEditing(null)
    } catch (err) {
      setError(extractErrorMessage(err) ?? "Не удалось сохранить строку")
    }
  }

  const apply = async () => {
    if (entries.length === 0 || confirming) return
    setConfirming(true)
    setError(null)
    try {
      if (!preview?.batchId) throw new Error("Сервер не создал пакет корректировки")
      const result = await applyBatch(preview.batchId, crypto.randomUUID())
      onApplied(result)
      setPreview(null)
      setEntries([])
      setEditing(null)
      if (fileRef.current) fileRef.current.value = ""
    } catch (err) {
      setError(
        extractErrorMessage(err) ??
          "Ошибка применения. Повторите с новым файлом",
      )
    } finally {
      setConfirming(false)
    }
  }

  const hasErrors = (preview?.errors.length ?? 0) > 0 || entries.some((entry) => entry.errors.length > 0)

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
            {preview.totalEntries} строк, {entries.length} валидных,{" "}
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
            {entries.map((entry) => (
              <CellSimple
                key={`${entry.row}:${entry.numberPair}`}
                separator
                overline={`${dayLabel(entry.dayOfWeek)}, пар ${entry.numberPair}`}
                title={
                  editing === entry.row ? (
                    <Input
                      value={entry.subject ?? ""}
                      placeholder="Предмет"
                      onChange={(e) => updateEntry(entry.row, { subject: e.target.value })}
                    />
                  ) : (
                    entryTitle(entry)
                  )
                }
                before={
                  <span className={`max-app__badge max-app__badge--${entry.changeType.toLowerCase()}`}>
                    <CheckCircle2 size={12} aria-hidden /> {entryBadge(entry)}
                  </span>
                }
                subtitle={
                  editing === entry.row ? (
                    <div className="max-app__inline-edit">
                      <Input
                        value={entry.teacherName ?? ""}
                        placeholder="Преподаватель"
                        onChange={(e) => updateEntry(entry.row, { teacherName: e.target.value })}
                      />
                      <Input
                        type="number"
                        min={1}
                        max={8}
                        value={entry.numberPair}
                        onChange={(e) => updateEntry(entry.row, { numberPair: Number(e.target.value) })}
                      />
                      <Input
                        value={entry.note ?? ""}
                        placeholder="Примечание"
                        onChange={(e) => updateEntry(entry.row, { note: e.target.value })}
                      />
                    </div>
                  ) : (
                    `${entry.groupName} · ${entry.teacherName ?? entry.removedTeacherName ?? "—"}${entry.note ? ` · ${entry.note}` : ""}`
                  )
                }
                after={
                  <Button
                    size="xsmall"
                    variant="ghost"
                    aria-label={editing === entry.row ? "Сохранить" : "Редактировать"}
                    onClick={() => {
                      if (editing === entry.row) void saveEntry(entry)
                      else setEditing(entry.row)
                    }}
                    iconBefore={<Pencil size={14} aria-hidden />}
                  />
                }
              />
            ))}
          </CellList>

          {error ? (
            <Typography.Body className="max-app__error">{error}</Typography.Body>
          ) : null}

          <Button
            stretched
            loading={confirming}
            disabled={entries.length === 0 || hasErrors || !preview.batchId}
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
