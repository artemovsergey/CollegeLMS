import { readFileSync } from "node:fs"
import { expect, type APIRequestContext, type Page } from "@playwright/test"

/** Учётная запись диспетчера на стенде. */
export const DISPATCHER = { login: "dispatcher", password: "dispatcher" }

export const CORRECTION_PATH = "/dispatcher/correction"

/** Подписи полей поиска в пошаговой форме позиции. */
const SEARCH = {
  group: "Поиск группы по названию",
  teacher: "Поиск по имени или предмету",
  subject: "Поиск предмета",
} as const

/** Вход диспетчером через настоящую форму — заодно проверяются права доступа. */
export async function loginAsDispatcher(page: Page): Promise<void> {
  await page.goto("/login")
  await page.getByLabel("Логин").fill(DISPATCHER.login)
  await page.getByLabel("Пароль").fill(DISPATCHER.password)
  await page.getByRole("button", { name: /^Войти$/ }).click()
  // Вход состоялся, если ушли со страницы логина и в шапке появился профиль.
  await expect(page.getByRole("button", { name: "Профиль" })).toBeVisible()
  await expect(page).not.toHaveURL(/\/login/)
}

export interface DispatcherSession {
  token: string
  user: unknown
}

/**
 * Вход через API. На странице логина вход ограничен пятью попытками в минуту,
 * поэтому форма проверяется одним отдельным тестом, а остальные работают с
 * сессией, подставленной в localStorage.
 */
export async function apiLogin(
  request: APIRequestContext
): Promise<DispatcherSession> {
  const response = await request.post("/api/auth/login", {
    data: { login: DISPATCHER.login, password: DISPATCHER.password },
  })
  expect(response.ok(), `вход диспетчера: ${response.status()}`).toBeTruthy()
  return (await response.json()).data as DispatcherSession
}

/** Подставляет готовую сессию диспетчера до открытия страницы. */
export async function useDispatcherSession(
  page: Page,
  session: DispatcherSession
): Promise<void> {
  await page.addInitScript(
    (data) => {
      localStorage.setItem("token", data.token)
      localStorage.setItem("user", JSON.stringify(data.user))
    },
    { token: session.token, user: session.user }
  )
}

export async function openCorrectionPage(page: Page): Promise<void> {
  await page.goto(CORRECTION_PATH)
  await expect(
    page.getByRole("heading", { name: "Корректировка расписания" })
  ).toBeVisible()
}

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

export interface ApiPosition {
  row: number
  changeType: "Add" | "Remove" | "Replace" | "Move"
  groupId: string
  groupName: string
  numberPair: number
  subject: string | null
  teacherName: string | null
  removedSubject: string | null
  removedTeacherName: string | null
  removedNumberPair: number | null
  note: string | null
}

export interface ApiBatch {
  id: string
  correctionDate: string
  week: number
  dayOfWeek: number
  status: string
  positionCount: number
  positions: ApiPosition[]
  errors: { row: number; level: string; message: string }[]
}

/** Пакет по идентификатору — эталон того, что сохранил интерфейс. */
export async function getBatch(
  request: APIRequestContext,
  token: string,
  batchId: string
): Promise<ApiBatch> {
  const response = await request.get(
    `/api/schedule/correction/batches/${batchId}`,
    { headers: auth(token) }
  )
  expect(response.ok(), `чтение пакета ${batchId}: ${response.status()}`).toBeTruthy()
  return (await response.json()).data as ApiBatch
}

export async function deleteBatch(
  request: APIRequestContext,
  token: string,
  batchId: string
): Promise<void> {
  const response = await request.delete(
    `/api/schedule/correction/batches/${batchId}`,
    { headers: auth(token) }
  )
  expect(response.ok(), `удаление пакета ${batchId}: ${response.status()}`).toBeTruthy()
}

// ───────────────────────────── Расписание для плана позиций ─────────────────────────────

export interface DayEntry {
  numberPair: number
  subject: string
  teacherName: string | null
  informational: boolean
  isSelfStudy: boolean
}

export interface GroupTeacher {
  id: string
  fullName: string
  subjects: string[]
}

export interface GroupDay {
  groupId: string
  groupName: string
  entries: DayEntry[]
  teachers: GroupTeacher[]
}

/** Расписание и преподаватели всех групп на дату — из него строится план. */
export async function loadGroupDays(
  request: APIRequestContext,
  token: string,
  date: string
): Promise<GroupDay[]> {
  const groups = (
    await (
      await request.get("/api/groups?pageSize=200", { headers: auth(token) })
    ).json()
  ).data as { id: string; name: string }[]

  const days: GroupDay[] = []
  for (const group of groups) {
    const response = await request.get(
      `/api/schedule/correction/references?groupId=${group.id}&date=${date}`,
      { headers: auth(token) }
    )
    if (!response.ok()) continue
    days.push({ groupId: group.id, groupName: group.name, ...(await response.json()).data })
  }
  return days
}

// ───────────────────────────────── Позиции: план и ввод ─────────────────────────────────

export type PositionPlan =
  /** Занятая пара, прежнее занятие снимается — замена. */
  | {
      kind: "replace"
      group: string
      pair: number
      removedSubject: string
      removedTeacherName: string
      subject: string
      teacher: string
    }
  /** Дополнительное занятие в паре — вторая пара в слоте или в свободный. */
  | { kind: "add"; group: string; pair: number; subject: string; teacher: string }
  /** Занятая пара, прежнее занятие остаётся: в паре их станет два. */
  | { kind: "add-parallel"; group: string; pair: number; subject: string; teacher: string }
  /** Перенос: занятие преподавателя уезжает из `fromPair` в `pair`. */
  | {
      kind: "move"
      group: string
      pair: number
      fromPair: number
      subject: string
      teacher: string
    }
  /** Снятие занятия из пары. */
  | {
      kind: "remove"
      group: string
      pair: number
      subject: string
      removedTeacherName: string
    }
  /** Самостоятельная работа только пометкой — в расписание не встаёт. */
  | { kind: "selfstudy"; group: string; pair: number; subject: string; teacher: string }
  /** Самостоятельная работа с вводом пары в расписание. */
  | {
      kind: "selfstudy-schedule"
      group: string
      pair: number
      subject: string
      teacher: string
    }

export const PLAN_KIND: Record<PositionPlan["kind"], string> = {
  replace: "замена",
  add: "ввод в свободную пару",
  "add-parallel": "ввод вторым занятием в пару",
  move: "перенос",
  remove: "снятие",
  selfstudy: "сам.р. (только пометка)",
  "selfstudy-schedule": "сам.р+ (в расписание)",
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
}

/** Выбирает значение в списке с поиском: группа, преподаватель или предмет. */
async function pickSearchable(
  page: Page,
  triggerId: string,
  search: string,
  value: string
): Promise<void> {
  const trigger = page.locator(`#${triggerId}`)
  await trigger.click()
  const input = page.getByRole("textbox", { name: search })
  await expect(input).toBeVisible()
  await input.fill(value)
  await page.getByRole("option", { name: new RegExp(escapeRegExp(value)) }).first().click()

  // Множественный выбор не закрывается сам. Закрывать его клавишей Escape
  // нельзя: Escape закрывает всё окно, поэтому кликаем по самому списку.
  if (await input.isVisible().catch(() => false)) {
    await trigger.click()
    await expect(input).toBeHidden()
  }
}

/** Создаёт пакет корректировки через интерфейс и остаётся в редакторе. */
export async function createBatchByUi(page: Page, date: string): Promise<string> {
  await page.getByRole("button", { name: "Создать пакет" }).click()
  const dialog = page.getByRole("dialog")
  await expect(dialog).toBeVisible()
  await dialog.locator("#batch-correction-date").fill(date)

  const [response] = await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes("/api/schedule/correction/batches") &&
        r.request().method() === "POST"
    ),
    dialog.getByRole("button", { name: "Создать пакет" }).click(),
  ])
  await expect(dialog).toBeHidden()
  await expect(page.getByText("Редактор пакета")).toBeVisible()
  return (await response.json()).data.id as string
}

/** Создаёт позицию корректировки через пошаговую форму. */
export async function addPositionByUi(
  page: Page,
  plan: PositionPlan
): Promise<void> {
  await page.getByRole("button", { name: "Добавить позицию" }).click()
  const dialog = page.getByRole("dialog")
  await expect(dialog).toBeVisible()

  await dialog
    .getByRole("radio", { name: plan.kind === "remove" ? /^Снять/ : /^Добавить/ })
    .click()

  await pickSearchable(page, "position-group", SEARCH.group, plan.group)

  // Шаг «Пара» появляется после загрузки расписания группы на дату пакета.
  await expect(dialog.getByText(/— выберите пару/)).toBeVisible()

  if (plan.kind === "remove") {
    // При снятии выбирается само занятие: в строке пары первая кнопка — её номер.
    const row = dialog.locator(`li:has(button[aria-label="Пара ${plan.pair}"])`)
    await row.getByRole("button").nth(1).click()
    await expect(
      dialog.getByText("Снимается", { exact: true }).locator("..")
    ).toContainText(`${plan.subject} ${plan.removedTeacherName}`.trim())
  } else {
    await dialog.getByRole("button", { name: `Пара ${plan.pair}`, exact: true }).click()
    await pickSearchable(page, "position-teachers", SEARCH.teacher, plan.teacher)
    await pickSearchable(page, "position-subject", SEARCH.subject, plan.subject)

    // Занятая пара требует решения: заменить прежнее или поставить в параллель.
    const slot = dialog.getByRole("radiogroup", { name: "Действие" })
    if (await slot.isVisible().catch(() => false)) {
      if (plan.kind === "replace") {
        await slot.getByText("Заменить", { exact: true }).click()
        await dialog
          .getByRole("radiogroup", { name: "Занятие" })
          .getByRole("radio")
          .filter({ hasText: plan.removedSubject })
          .first()
          .click()
      } else {
        await slot.getByText("Поставить в параллель", { exact: true }).click()
      }
    }
  }

  if (plan.kind === "selfstudy" || plan.kind === "selfstudy-schedule") {
    await dialog.getByRole("switch", { name: "Это самостоятельная работа?" }).click()
  }
  if (plan.kind === "selfstudy-schedule") {
    await dialog.getByRole("switch", { name: "Ввести пару в расписание?" }).click()
  }
  if (plan.kind === "remove") {
    await dialog.getByRole("button", { name: "снять", exact: true }).click()
  }
  if (plan.kind === "move") {
    await dialog
      .getByLabel("Перенос из другой пары")
      .selectOption({ label: `Из пары ${plan.fromPair} — освободится` })
  }

  const submit = dialog.getByRole("button", { name: "Добавить позицию" })
  await expect(submit).toBeEnabled()
  await submit.click()
  await expect(dialog).toBeHidden()
}

/** Скачивает XLSX пакета кнопкой «XLSX» в редакторе. */
export async function exportBatchByUi(page: Page): Promise<{
  fileName: string
  content: Buffer
}> {
  const [download] = await Promise.all([
    page.waitForEvent("download"),
    page.getByRole("button", { name: "XLSX", exact: true }).click(),
  ])
  return {
    fileName: download.suggestedFilename(),
    content: readFileSync(await download.path()),
  }
}

/** Переходит на вкладку импорта. */
async function openImportTab(page: Page): Promise<void> {
  await page.getByRole("button", { name: "Импорт", exact: true }).click()
  await expect(page.getByText("Импорт корректировок из XLSX")).toBeVisible()
}

/**
 * Загружает XLSX на вкладке импорта и дожидается ответа. Идентификатор
 * созданного пакета берём из ответа API — так тест проверяет ровно тот запрос,
 * который отправляет интерфейс.
 */
export async function importFileByUi(
  page: Page,
  fileName: string,
  content: Buffer
): Promise<{ batchId: string | null; status: number }> {
  await openImportTab(page)
  await page.locator('input[type="file"]').setInputFiles({
    name: fileName,
    mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    buffer: content,
  })
  const [response] = await Promise.all([
    page.waitForResponse(
      (r) =>
        r.url().includes("/api/schedule/correction/batches/import") &&
        r.request().method() === "POST"
    ),
    page.getByRole("button", { name: "Создать пакет из файла" }).click(),
  ])
  const body = await response.json().catch(() => ({}))
  return { batchId: body?.data?.batchId ?? null, status: response.status() }
}
