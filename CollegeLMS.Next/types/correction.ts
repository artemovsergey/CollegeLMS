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
  level: number
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
  removedRoom: string | null
  removedNumberPair: number | null
}

export interface ChangeTag {
  changeType: CorrectionChangeType
  week: number
  removedNumberPair: number | null
  removedSubject: string | null
  note: string | null
}

export type CorrectionBatchStatus = "Draft" | "Applied" | "Cancelled"
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
}

export interface CorrectionBatch {
  id: string
  correctionDate: string
  week: number
  dayOfWeek: number
  status: CorrectionBatchStatus
  createdAt: string
  positionCount: number
  positions: CorrectionPosition[]
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