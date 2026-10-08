"use client"

import { useMemo } from "react"
import { FileSpreadsheet } from "lucide-react"
import type { CorrectionBatch, CorrectionPosition } from "@/types/correction"
import type { CorrectionRowPreview } from "@/lib/correction-row"
import { toCorrectionRow } from "@/lib/correction-row"
import { cn } from "@/lib/utils"
import { noticeBlock } from "@/lib/status-style"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table"

/** Шапка файла повторяет шаблон 1:1 — иначе предпросмотр врёт. */
const DAY_RU: Record<number, string> = {
  1: "понедельник",
  2: "вторник",
  3: "среда",
  4: "четверг",
  5: "пятница",
  6: "суббота",
  0: "воскресенье",
}

/** Позиция пакета в виде строки файла корректировки. */
function toRow(position: CorrectionPosition): CorrectionRowPreview {
  return toCorrectionRow({
    changeType: position.changeType,
    groupName: position.groupName,
    numberPair: position.numberPair,
    subject: position.subject,
    teacherName: position.teacherName,
    removedSubject: position.removedSubject,
    removedTeacherName: position.removedTeacherName,
    removedNumberPair: position.removedNumberPair,
    note: position.note,
  })
}

/**
 * Оформление как в выгрузке: Times New Roman, рамка, центрирование.
 * Перенос по словам обязателен: без него длинный предмет или два
 * преподавателя через слеш вылезают за границу ячейки и наезжают на
 * соседнюю колонку — таблица HTML сама текст не рвёт.
 */
const CELL =
  "border border-foreground/40 px-1.5 py-1 text-center align-middle break-words"
const HEAD_CELL = `${CELL} font-bold`
const TITLE = "px-2 py-0.5 text-center text-[13px] font-bold leading-tight"

interface CorrectionFilePreviewProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  batch: CorrectionBatch | null
}

/**
 * Предпросмотр пакета в виде файла корректировки: те же три строки заголовка,
 * двухстрочная шапка и колонки, что уходят в XLSX. Экспериментальная функция —
 * чтобы проверять пакет, не скачивая и не открывая файл.
 */
export function CorrectionFilePreview({
  open,
  onOpenChange,
  batch,
}: CorrectionFilePreviewProps) {
  const rows = useMemo(
    () =>
      [...(batch?.positions ?? [])].sort((a, b) => a.row - b.row).map(toRow),
    [batch],
  )

  const movedRows = rows.filter((row) => row.movedFrom)

  if (!batch) return null

  const date = new Date(batch.correctionDate)
  const dayName = DAY_RU[date.getDay()] ?? ""
  const stamp = `${String(date.getDate()).padStart(2, "0")}${String(
    date.getMonth() + 1,
  ).padStart(2, "0")}`

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="scroll-stable sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Предпросмотр файла корректировки</DialogTitle>
          <DialogDescription>
            Экспериментальный просмотр: так пакет будет выглядеть в XLSX.
          </DialogDescription>
        </DialogHeader>

        <div className="scroll-stable max-h-[65vh] overflow-auto rounded-md border bg-white p-3 dark:bg-card">
          <div className="min-w-[680px] font-[family-name:var(--font-xlsx,'Times_New_Roman',serif)] text-[13px] text-foreground">
            <p className={TITLE}>КОРРЕКТИРОВКА</p>
            <p className={TITLE}>стабильного расписания учебных занятий</p>
            <p className={TITLE}>
              на {date.toLocaleDateString("ru-RU")} г. ({dayName})
            </p>

            <Table className="mt-1 text-[13px]">
              <TableBody>
                <TableRow>
                  <TableCell
                    colSpan={2}
                    className={`${HEAD_CELL} w-[16%] py-0.5`}
                  >
                    Группа
                  </TableCell>
                  <TableCell colSpan={2} className={`${HEAD_CELL} w-[44%] py-0.5`}>
                    Снимается по расписанию
                  </TableCell>
                  <TableCell colSpan={2} className={`${HEAD_CELL} w-[30%] py-0.5`}>
                    Вводится в расписание
                  </TableCell>
                  <TableCell className={`${HEAD_CELL} w-[4%] px-1 py-0.5`}>
                    №
                  </TableCell>
                  <TableCell className={`${HEAD_CELL} w-[10%] px-1 py-0.5`}>
                    Примеч.
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell className={`${CELL} py-0.5`} />
                  <TableCell className={`${CELL} py-0.5`} />
                  <TableCell className={`${HEAD_CELL} py-0.5`}>Предмет</TableCell>
                  <TableCell className={`${HEAD_CELL} py-0.5`}>
                    Преподаватель
                  </TableCell>
                  <TableCell className={`${HEAD_CELL} py-0.5`}>Предмет</TableCell>
                  <TableCell className={`${HEAD_CELL} py-0.5`}>
                    Преподаватель
                  </TableCell>
                  <TableCell className={`${CELL} py-0.5`} />
                  <TableCell className={`${CELL} py-0.5`} />
                </TableRow>

                {rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={8} className={`${CELL} py-3`}>
                      Позиций нет — файл будет пустым
                    </TableCell>
                  </TableRow>
                )}

                {rows.map((row, index) => (
                  <TableRow key={index}>
                    <TableCell colSpan={2} className={CELL}>
                      {row.groupName}
                    </TableCell>
                    <TableCell className={CELL}>{row.removedSubject}</TableCell>
                    <TableCell className={CELL}>{row.removedTeacher}</TableCell>
                    <TableCell className={CELL}>{row.addedSubject}</TableCell>
                    <TableCell className={CELL}>{row.addedTeacher}</TableCell>
                    <TableCell className={`${CELL} px-1`}>
                      {row.numberPair}
                    </TableCell>
                    <TableCell className={`${CELL} px-1`}>{row.note}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>

        {/* Пара «откуда» при переносе живёт только в примечании «вм.X» —
            напоминаем её под таблицей, чтобы перенос читался однозначно. */}
        {movedRows.length > 0 && (
          <ul className={cn(
              "grid gap-1 rounded-md border px-3 py-2 text-xs",
              noticeBlock("warning"),
            )}>
            {movedRows.map((row, index) => (
              <li key={index}>
                Строка {index + 1}: перенос — освобождается пара {row.movedFrom!.pair} ({" "}
                {row.movedFrom!.lesson})
              </li>
            ))}
          </ul>
        )}

        <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <Badge variant="outline">
            <FileSpreadsheet className="size-3" aria-hidden />
            Корректировка_{stamp}.xlsx
          </Badge>
          <span>Позиций: {rows.length}</span>
          <span>·</span>
          <span>{batch.week} неделя</span>
        </div>
      </DialogContent>
    </Dialog>
  )
}
