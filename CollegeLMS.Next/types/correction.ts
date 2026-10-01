import type { PagedResponse } from "@/types"

export type CorrectionChangeType = "Add" | "Remove" | "Replace" | "Move"

export interface CorrectionPreviewEntry {
  row: number
  groupId: string
  groupName: string
  changeType: CorrectionChangeType
  dayOfWeek: number
  week: number
  numberPair: number
  subject: string | null
  teacherId: string | null
  teacherName: string | null
  removedSubject: string | null
  removedTeacherId: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string | null
}

export interface ScheduleValidationError {
  row: number
  column: number
  level: string
  message: string
}

export interface CorrectionPreviewResponse {
  correctionDate: string
  week: number
  dayOfWeek: number
  totalEntries: number
  entries: CorrectionPreviewEntry[]
  errors: ScheduleValidationError[]
}

export interface ConfirmResult {
  applied: number
  history: ScheduleHistoryItem[]
}

export interface ScheduleHistoryItem {
  id: string
  changeType: CorrectionChangeType
  appliedAt: string
  appliedByUserId: string | null
  groupId: string
  groupName: string
  teacherId: string | null
  teacherName: string | null
  subject: string
  room: string | null
  dayOfWeek: string
  numberPair: number
  week: number
  note: string | null
  removedSubject: string | null
  removedTeacherName: string | null
  removedRoom: string | null
  removedNumberPair: number | null
}

/** Ответ отката применённой корректировки из журнала. */
export interface CorrectionRevertResult {
  historyId: string
  changeType: CorrectionChangeType
  groupName: string
  dayOfWeek: string
  week: number
  /** Вернулась ли пара в расписание; false — изменена только запись журнала. */
  scheduleChanged: boolean
  message: string
}

export interface ChangeTag {
  changeType: CorrectionChangeType
  week: number
  removedNumberPair: number | null
  removedSubject: string | null
  removedTeacherName: string | null
  note: string | null
}

/** Строка файла корректировки в том же виде, что уходит в XLSX. */
export interface CorrectionExportRow {
  groupName: string
  removedSubject: string | null
  removedTeacherName: string | null
  addedSubject: string | null
  addedTeacherName: string | null
  numberPair: number
  note: string | null
}

export type CorrectionBatchStatus = "Draft" | "Applied"
export type CorrectionPositionStatus = "Draft" | "Applied"

export interface CorrectionPosition {
  id: string
  row: number
  changeType: CorrectionChangeType
  groupId: string
  groupName: string
  dayOfWeek: number
  week: number
  numberPair: number
  subject: string | null
  teacherId: string | null
  teacherName: string | null
  removedSubject: string | null
  removedTeacherId: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string | null
  status: CorrectionPositionStatus
  historyId: string | null
  errors: ScheduleValidationError[]
}

export interface CorrectionBatch {
  id: string
  correctionDate: string
  week: number
  dayOfWeek: number
  status: CorrectionBatchStatus
  createdAt: string
  appliedByUserId: string | null
  appliedByName: string | null
  appliedAt: string | null
  positionCount: number
  positions: CorrectionPosition[]
  errors: ScheduleValidationError[]
}

/** Ответ списка пакетов: серверная пагинация. */
export type CorrectionBatchPage = PagedResponse<CorrectionBatch>

export interface CorrectionDayEntry {
  numberPair: number
  subject: string
  room: string
  teacherId: string | null
  teacherName: string | null
  note: string | null
  isSelfStudy: boolean
  /**
   * Пара только для информирования («сам.р.» без «+»): в расписании её нет,
   * слот она не занимает и снять её нельзя.
   */
  informational?: boolean
  pendingChangeType: CorrectionChangeType | null
  changeTags: ChangeTag[]
}

export interface CorrectionDayResponse {
  date: string
  week: number
  dayOfWeek: number
  groupId: string
  groupName: string
  entries: CorrectionDayEntry[]
}

/** Преподаватель, ведущий занятия у группы, с его предметами в этой группе. */
export interface CorrectionGroupTeacher {
  id: string
  fullName: string
  subjects: string[]
}

/**
 * Справочники для пошаговой формы корректировки: расписание группы на дату
 * и преподаватели этой группы с их предметами.
 */
export interface CorrectionReferences {
  date: string
  week: number
  dayOfWeek: number
  groupId: string
  groupName: string
  entries: CorrectionDayEntry[]
  teachers: CorrectionGroupTeacher[]
}

export interface CreateCorrectionPosition {
  changeType: CorrectionChangeType
  groupId: string
  groupName: string
  numberPair: number
  subject: string | null
  teacherId: string | null
  teacherName: string | null
  removedSubject: string | null
  removedTeacherId: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string | null
}

export interface CorrectionImportResponse {
  batchId: string | null
  correctionDate: string
  week: number
  dayOfWeek: number
  totalEntries: number
  positions: CorrectionPosition[]
  errors: ScheduleValidationError[]
}

export interface CorrectionApplyResult {
  applied: number
  batchId: string
  history: ScheduleHistoryItem[]
}

/** Итог удаления пакета: сколько пакетов удалено и сколько изменений возвращено. */
export interface CorrectionBatchDeleteResult {
  batches: number
  reverted: number
  message: string
}