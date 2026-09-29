"use client"

import { useMemo } from "react"
import { FileSpreadsheet } from "lucide-react"
import type { CorrectionBatch, CorrectionPosition } from "@/types/correction"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

const DAY_RU: Record<number, string> = {
  1: "понедельник",
  2: "вторник",
  3: "среда",
  4: "четверг",
  5: "пятница",
  6: "суббота",
  0: "воскресенье",
}

/** Преподаватель в том же виде, что в файле: «Иванов И.И.». */
function shortTeacher(value: string | null | undefined): string {
  if (!value) return ""
  return value
    .split("/")
    .map((part) => {
      const parts = part.trim().split(/\s+/).filter(Boolean)
      if (parts.length < 2) return part.trim()
      const initials = parts
        .slice(1)
        .map((p) => `${p[0].toUpperCase()}.`)
        .join("")
      return `${parts[0]} ${initials}`
    })
    .join("/")
}

interface XlsxPreviewRow {
  groupName: string
  removed: string
  added: string
  numberPair: number
  note: string
}

function toRow(position: CorrectionPosition): XlsxPreviewRow {
  const removed =
    position.changeType === "Remove" || position.changeType === "Replace"
      ? [position.removedSubject, shortTeacher(position.removedTeacherName)]
          .filter(Boolean)
          .join(" ")
      : ""
  const added =
    position.changeType !== "Remove"
      ? [position.subject, shortTeacher(position.teacherName)]
          .filter(Boolean)
          .join(" ")
      : ""
  return {
    groupName: position.groupName,
    removed,
    added,
    numberPair: position.numberPair,
    note:
      position.note ??
      (position.changeType === "Move" && position.removedNumberPair != null
        ? `вм.${position.removedNumberPair}`
        : ""),
  }
}

interface CorrectionFilePreviewProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  batch: CorrectionBatch | null
}

/**
 * Предпросмотр пакета в виде файла корректировки: те же колонки, шапка и
 * примечание, что уйдут в XLSX. Экспериментальная функция — чтобы проверить
 * пакет, не скачивая и не открывая файл.
 */
export function CorrectionFilePreview({
  open,
  onOpenChange,
  batch,
}: CorrectionFilePreviewProps) {
  const rows = useMemo(
    () =>
      [...(batch?.positions ?? [])]
        .sort((a, b) => a.row - b.row)
        .map(toRow),
    [batch],
  )

  if (!batch) return null

  const date = new Date(batch.correctionDate)
  const dayName = DAY_RU[batch.dayOfWeek] ?? ""

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Предпросмотр файла корректировки</DialogTitle>
          <DialogDescription>
            Экспериментальный просмотр: так пакет будет выглядеть в XLSX.
          </DialogDescription>
        </DialogHeader>

        <div className="scroll-stable max-h-[65vh] overflow-auto rounded-md border">
          <div className="min-w-[720px] font-[family-name:var(--font-xlsx,serif)]">
            <p className="px-3 py-2 text-center text-sm font-bold uppercase">
              Корректировка расписания
            </p>
            <p className="px-3 pb-2 text-center text-sm italic">
              на {date.toLocaleDateString("ru-RU")} г. ({dayName})
            </p>
            <Table className="text-sm">
              <TableHeader className="bg-muted/50 text-xs uppercase text-muted-foreground [&_th]:h-auto [&_th]:font-bold [&_th]:text-muted-foreground [&_tr]:border-b-0">
                <TableRow>
                  <TableHead className="px-2 py-2 text-center">Группа</TableHead>
                  <TableHead className="px-2 py-2 text-center">
                    Снимается по расписанию
                  </TableHead>
                  <TableHead className="px-2 py-2 text-center">Преподаватель</TableHead>
                  <TableHead className="px-2 py-2 text-center">Вводится</TableHead>
                  <TableHead className="px-2 py-2 text-center">Преподаватель</TableHead>
                  <TableHead className="px-2 py-2 text-center">Пара</TableHead>
                  <TableHead className="px-2 py-2 text-center">Примечание</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row, index) => (
                  <TableRow key={index}>
                    <TableCell className="px-2 py-1.5 text-center">
                      {row.groupName}
                    </TableCell>
                    <TableCell className="px-2 py-1.5 text-center">
                      {row.removed}
                    </TableCell>
                    <TableCell className="px-2 py-1.5 text-center" />
                    <TableCell className="px-2 py-1.5 text-center">
                      {row.added}
                    </TableCell>
                    <TableCell className="px-2 py-1.5 text-center" />
                    <TableCell className="px-2 py-1.5 text-center">
                      {row.numberPair}
                    </TableCell>
                    <TableCell className="px-2 py-1.5 text-center">
                      {row.note}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <Badge variant="outline">
            <FileSpreadsheet className="size-3" aria-hidden />
            Корректировка_{date.toLocaleDateString("ru-RU").replace(/\D/g, "").slice(0, 6)}.xlsx
          </Badge>
          <span>Позиций: {rows.length}</span>
          <span>·</span>
          <span>{batch.week} неделя</span>
        </div>
      </DialogContent>
    </Dialog>
  )
}
