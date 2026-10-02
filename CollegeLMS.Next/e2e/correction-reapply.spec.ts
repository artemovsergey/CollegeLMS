import { expect, test } from "@playwright/test"

import type { APIRequestContext } from "@playwright/test"

import { apiLogin, CORRECTION_PATH, type DispatcherSession } from "./helpers/corrections"

/**
 * Группа с парами в пятницу — на ней проверяется редактирование уже
 * применённого пакета. Пакет создаётся на сегодня, проверяется и удаляется:
 * удаление применённого пакета возвращает расписание по его изменениям,
 * поэтому стенд остаётся как был.
 */
const GROUP = {
  id: "89cc7036-4a8c-44aa-808a-b2cff00977ec",
  name: "РЗ 242",
} as const

const TEACHER = {
  id: "851c3d2f-c472-4564-822f-80fe325fe39d",
  name: "Киркоров О.Р.",
} as const

/** Пара 3 в пятницу — её снимаем, затем вводим заново под своим предметом. */
const PAIR = 3

let session: DispatcherSession

/** Позиция пакета, созданного этой проверкой. */
const TEST_SUBJECT = "ТЕСТ ЧЕРНОВИКА"

/**
 * Удаляет пакеты, оставшиеся от неудачных прогонов: у них в позициях остаётся
 * тестовый предмет. Это же убирает мусор, из-за которого в списке нельзя
 * отличить свой пакет от чужого: строки одного дня подписаны одинаково.
 */
async function removeLeftovers(
  request: APIRequestContext,
  headers: Record<string, string>,
  today: string
): Promise<void> {
  const listed = await request.get(
    "/api/schedule/correction/batches?page=1&pageSize=100",
    { headers }
  )
  const items =
    ((await listed.json()).data as { items: { id: string; correctionDate: string }[] })
      .items ?? []

  for (const batch of items.filter(
    (b) => b.correctionDate.slice(0, 10) === today
  )) {
    await request.delete(`/api/schedule/correction/batches/${batch.id}`, { headers })
  }
}

test.describe("Редактирование применённого пакета", () => {
  test.beforeEach(async ({ page, request }) => {
    session = await apiLogin(request)
    await page.addInitScript(
      ([token, user]) => {
        window.localStorage.setItem("token", token as string)
        window.localStorage.setItem("user", JSON.stringify(user))
      },
      [session.token, session.user] as const
    )
  })

  test("частичное применение, правка и удаление пакета", async ({ page, request }) => {
    const headers = {
      Authorization: `Bearer ${session.token}`,
      "Content-Type": "application/json",
    }

    const today = new Date().toISOString().slice(0, 10)
    await removeLeftovers(request, headers, today)

    // Пакет с одной позицией: предмет и преподаватель обязательны вместе,
    // иначе движок не примет позицию.
    const created = await request.post("/api/schedule/correction/batches", {
      headers,
      data: { correctionDate: `${today}T00:00:00` },
    })
    expect(created.ok()).toBeTruthy()
    const batchId = ((await created.json()).data as { id: string }).id

    const position = await request.post(
      `/api/schedule/correction/batches/${batchId}/positions`,
      {
        headers,
        data: {
          changeType: "Add",
          groupId: GROUP.id,
          groupName: GROUP.name,
          numberPair: PAIR,
          subject: "ТЕСТ ЧЕРНОВИКА",
          teacherId: TEACHER.id,
          teacherName: TEACHER.name,
        },
      }
    )
    expect(position.ok()).toBeTruthy()
    const positionId = ((await position.json()).data as { id: string }).id

    await page.goto(CORRECTION_PATH)
    const dateLabel = new Date(`${today}T00:00:00`).toLocaleDateString("ru-RU")

    /** Заголовок редактора: кнопки применения есть и у каждой строки списка. */
    const header = () =>
      page.locator('[data-slot="card-title"]').filter({ hasText: "Редактор пакета" })

    const editor = page.locator("main")

    /**
     * Открыть пакет из списка. На широком экране строка таблицы подписана датой,
     * на узком та же кнопка называется «Открыть», поэтому берём видимую.
     * После применения редактор закрывается сам — открывать приходится снова.
     */
    const openBatch = async () => {
      const byDate = page
        .getByLabel(`Открыть пакет за ${dateLabel}`)
        .filter({ visible: true })
      if (await byDate.count()) {
        await byDate.first().click()
      } else {
        await page
          .getByRole("button", { name: "Открыть", exact: true })
          .filter({ visible: true })
          .first()
          .click()
      }
      await expect(header()).toBeVisible()
    }

    /** Вернуться из редактора к списку. */
    const backToList = async () => {
      await header().getByRole("button", { name: "Назад" }).click()
      await expect(header()).toBeHidden()
    }

    await openBatch()
    // Открыт именно наш пакет: строки одного дня в списке неразличимы.
    await expect(page.getByText(TEST_SUBJECT)).toBeVisible()

    // Черновик применяется из интерфейса, после чего редактор возвращает к списку.
    await expect(header().getByRole("button", { name: "Применить" })).toBeEnabled()
    await header().getByRole("button", { name: "Применить" }).click()
    await page.getByRole("button", { name: "Применить", exact: true }).last().click()
    await expect(header()).toBeHidden()

    // У применённого пакета с пустым набором черновиков кнопка заблокирована с
    // понятной подсказкой, а не «пакет уже применён»: пакет вполне можно
    // дополнить и применить снова.
    await openBatch()
    const appliedButton = header().getByRole("button", { name: "Применить" })
    await expect(appliedButton).toBeDisabled()
    await expect(appliedButton).toHaveAttribute(
      "title",
      "Все позиции пакета уже применены"
    )

    // Правка применённой позиции обещает отменить прежнее изменение.
    await editor.getByLabel(`Редактировать позицию 1`).click()
    const dialog = page.getByRole("dialog")
    await expect(dialog.getByText("Позиция уже применена")).toBeVisible()
    await page.keyboard.press("Escape")

    // Правка возвращает позицию в черновики, и её снова можно применить.
    const edited = await request.put(
      `/api/schedule/correction/batches/${batchId}/positions/${positionId}`,
      {
        headers,
        data: {
          changeType: "Add",
          groupId: GROUP.id,
          groupName: GROUP.name,
          numberPair: PAIR,
          subject: "ТЕСТ ЧЕРНОВИКА 2",
          teacherId: TEACHER.id,
          teacherName: TEACHER.name,
        },
      }
    )
    expect(edited.ok()).toBeTruthy()
    // historyId приходит только когда он есть: null-поля не сериализуются.
    const editedData = (await edited.json()).data as { status: string; historyId?: string }
    expect(editedData.status).toBe("Draft")
    expect(editedData.historyId).toBeUndefined()

    // Правка снова открывает пакет для применения: кнопка живая, а редактор
// прямо говорит, что позиция попадёт в расписание по кнопке «Применить».
    await backToList()
    await openBatch()
    await expect(header().getByRole("button", { name: "Применить" })).toBeEnabled()
    await expect(
      page.getByText("Позиция попадёт в расписание по кнопке «Применить»")
    ).toBeVisible()

    const applied = await request.post(
      `/api/schedule/correction/batches/${batchId}/apply`,
      { headers }
    )
    expect(applied.ok()).toBeTruthy()
    expect(((await applied.json()).data as { applied: number }).applied).toBe(1)

    // Неверная правка не отменяет уже применённое изменение.
    const rejected = await request.put(
      `/api/schedule/correction/batches/${batchId}/positions/${positionId}`,
      {
        headers,
        data: {
          changeType: "Add",
          groupId: GROUP.id,
          groupName: GROUP.name,
          numberPair: 9,
          subject: "ТЕСТ ЧЕРНОВИКА 3",
          teacherId: TEACHER.id,
          teacherName: TEACHER.name,
        },
      }
    )
    expect(rejected.status()).toBe(400)

    const state = await request.get(
      `/api/schedule/correction/batches/${batchId}`,
      { headers }
    )
    const stateData = (await state.json()).data as {
      pendingCount: number
      positions: { status: string }[]
    }
    expect(stateData.pendingCount).toBe(0)
    expect(stateData.positions.map((p) => p.status)).toEqual(["Applied"])

    // Применённую позицию не удаляют, пакет удаляют вместе с откатом.
    const deletePosition = await request.delete(
      `/api/schedule/correction/batches/${batchId}/positions/${positionId}`,
      { headers }
    )
    expect(deletePosition.status()).toBe(409)

    const removed = await request.delete(
      `/api/schedule/correction/batches/${batchId}`,
      { headers }
    )
    expect(removed.ok()).toBeTruthy()
    expect(
      ((await removed.json()).data as { reverted: number }).reverted
    ).toBeGreaterThan(0)

    const gone = await request.get(`/api/schedule/correction/batches/${batchId}`, {
      headers,
    })
    expect(gone.status()).toBe(404)
  })
})