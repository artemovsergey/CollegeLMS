import type { CorrectionChangeType } from "@/types/correction"
import type { LessonKind } from "@/types"

/**
 * Оформление статусных меток. Единственный источник правды.
 *
 * Раньше один и тот же набор оттенков объявлялся шестью разными способами —
 * в `ChangeTagBadge`, `ChangeCard`, `RemovePairPicker`, журнале преподавателя,
 * `teacher/journal` и в MAX. Каждая копия жила своей жизнью: у части не было
 * тёмной темы, у части не было режима высокой контрастности, и фиолетовый
 * «Сам.р.» вообще не имел токена — только класс палитры Tailwind.
 *
 * Теперь цвет задаётся семантическим токеном (§3.1–3.2), а подложка — его
 * прозрачным вариантом. Поэтому плашка следует за темой сама, ничего не нужно
 * писать в `dark:`, и контраст проверяется гейтом §8.2.
 *
 * Словарь подписей живёт отдельно, в `lib/change-tags.ts`: там решения о том,
 * что показывать пользователю, здесь — как это выглядит.
 */

/** Плашка на подложке токена: подложка — сам токен на 10% прозрачности. */
function pill(token: "success" | "destructive" | "warning" | "primary" | "self-study"): string {
  return `bg-${token}/10 text-${token}`
}

/** Тот же приём для подложки при заливке текстом по токену. */
export function tinted(token: "success" | "destructive" | "warning" | "primary" | "self-study") {
  return `bg-${token}/10 text-${token} border-${token}/30`
}

/**
 * Исход корректировки для показа: добавлено или снято.
 *
 * Замена и перенос остались в базе после объединения операций, но для
 * пользователя это добавление занятия в пару, поэтому они делят вид с `Add`.
 * Явное отличие нужно там, где видно исход операции целиком, — в выборе
 * снимаемой позиции и в журнале преподавателя.
 */
export const CHANGE_KIND_STYLE: Record<"Add" | "Remove", string> = {
  Add: pill("success"),
  Remove: pill("destructive"),
}

/** Исход операции целиком — там, где видно, что именно применили. */
export const CHANGE_TYPE_STYLE: Record<CorrectionChangeType, string> = {
  Add: pill("success"),
  Remove: pill("destructive"),
  Replace: pill("primary"),
  Move: pill("warning"),
}

/**
 * Самостоятельная работа. Собственный токен, а не `--accent-lighter`: она
 * семантически другое состояние, чем «приглушённый брендовый», и в MAX тот же
 * тон приходит из `--color-tint-violet`.
 */
export const SELF_STUDY_STYLE = pill("self-study")
export const SELF_STUDY_ICON_STYLE = "text-self-study"

/** Плашка «Сам.р.» для мест, где она встаёт рядом с плашкой исхода. */
export const SELF_STUDY_BADGE_STYLE = pill("self-study")

/**
 * Цвет вида занятия. Тот же набор `--lesson-*`, что и в MAX и в сводке
 * диспетчера (§3.5), — порог 4.5:1 общий, потому что подпись 11–12px читается
 * как текст, а не как заливка.
 */
export const LESSON_KIND_COLOR: Record<LessonKind, string> = {
  Lecture: "var(--lesson-lecture)",
  Practice: "var(--lesson-practice)",
  SelfStudy: "var(--self-study)",
}
/**
 * Виды практики. Учебная и производственная различаются тоном, но обе —
 * предупреждение о том, что пара заменена практикой, поэтому их цвет берётся
 * из `--success` и `--warning`, а не из отдельной пары оттенков.
 */
export const PRACTICE_STYLE: Record<"Up" | "Pp", string> = {
  Up: pill("success"),
  Pp: pill("warning"),
}

/**
 * Практика в виде блока: подложка слабее плашки, рамка чуть плотнее. Числа
 * прозрачности заданы от токена, поэтому блок остаётся тем же в обеих темах и
 * в режиме высокой контрастности.
 */
export function practiceBlock(kind: "Up" | "Pp"): string {
  const token = kind === "Up" ? "success" : "warning"
  return `border-${token}/40 bg-${token}/5`
}

/** Пара под практикой в расписании: рамка акцентная, подложка слабая. */
export const PRACTICE_CELL = "border-success/50 bg-success/5"
/** Номер пары под практикой. */
export const PRACTICE_PAIR_NUMBER = "text-success"

/**
 * Нерабочий день и выходной: `--warning` в обоих состояниях. Это
 * предупреждение, а не ошибка, поэтому не `--destructive`.
 */
export const NON_WORKING_TEXT = "text-warning"
export const NON_WORKING_BLOCK =
  "border-warning/40 bg-warning/10 text-warning"

/** Пара снята в пакете корректировок: то же, что снятие, но тише. */
export const PENDING_CHANGE_TEXT: Record<"Add" | "Remove", string> = {
  Add: "text-success",
  Remove: "text-destructive",
}

/**
 * Состояние пакета корректировок. «Подготовлен» — ждёт применения, то есть
 * действие впереди, поэтому `--warning`, а не приглушённый серый.
 */
export const BATCH_STATUS_STYLE: Record<"Draft" | "Applied", string> = {
  Draft: pill("warning"),
  Applied: pill("success"),
}

/**
 * Карточка выбора позиции изменения. Подложка слабее, чем у плашки, рамка
 * плотнее: это управляющий элемент, а не статус, и он должен читаться как
 * кнопка выбора.
 */
export function choiceBlock(token: "success" | "destructive"): string {
  return `border-${token}/40 bg-${token}/5 text-${token}`
}

/**
 * Врезка-пояснение: «пара применена», «перенос», «только информация».
 * Подложка заметно слабее рамки, но текст читается в обеих темах.
 */
export function noticeBlock(
  token: "warning" | "self-study" | "destructive" | "success",
): string {
  return `border-${token}/40 bg-${token}/10 text-${token}`
}

/** Результат теста: пройден и не пройден. */
export const PASSED_TEXT = "text-success"
export const FAILED_TEXT = "text-warning"
