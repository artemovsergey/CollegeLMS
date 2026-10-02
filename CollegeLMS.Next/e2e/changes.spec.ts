import { expect, test, type Page } from "@playwright/test"

/**
 * Журнал изменений. Карточка должна показывать, что именно изменилось в
 * расписании: у снятия вводимого занятия нет, поэтому в строке предмета
 * показывается снимаемое. Раньше на его месте был прочерк, и по карточке
 * было не видно, что убрали.
 */

const DISPATCHER = { login: "dispatcher", password: "dispatcher" }
const TOKEN_KEY = "token"
const USER_KEY = "user"

/** Сессия диспетчера: вход на странице логина ограничен пятью попытками в минуту. */
async function useDispatcher(page: Page): Promise<string> {
  const response = await page.request.post("/api/auth/login", {
    data: DISPATCHER,
  })
  expect(response.ok(), `вход диспетчера: ${response.status()}`).toBeTruthy()
  const session = (await response.json()).data
  await page.addInitScript(
    (data) => {
      localStorage.setItem("token", data.token)
      localStorage.setItem("user", JSON.stringify(data.user))
    },
    session
  )
  return session.token as string
}

/** Строки карточки без пустых. */
function linesOf(card: string): string[] {
  return card
    .split("\n")
    .map((line) => line.trim())
    .filter(Boolean)
}

/**
 * Прочерк отдельной строкой — значит, предмет не выведен. Иначе в карточке
 * есть строка «Применено: …», так что целой строки из одного тире быть не может.
 */
function hasBareDash(card: string): boolean {
  return linesOf(card).includes("—")
}

test.describe("Журнал изменений", () => {
  test("карточка снятия показывает снимаемый предмет, а не прочерк", async ({
    page,
  }) => {
    test.setTimeout(120000)
    const token = await useDispatcher(page)

    await page.goto("/changes")
    await expect(page.getByRole("heading", { name: /Изменения/i })).toBeVisible()
    await page.locator("#changes-type").selectOption("Remove")
    await expect(page.getByRole("article").first()).toBeVisible()

    const cards = page.getByRole("article")
    const shown = await cards.count()
    expect(shown, "снятий должно быть").toBeGreaterThan(0)

    // Ожидаемое состояние берём из самого журнала, а не из файлов корректировок:
    // карточка обязана показать ровно то, что записано в истории.
    const expected = await page.request.get(
      "/api/schedule/history?changeType=Remove&pageSize=20",
      { headers: { Authorization: `Bearer ${token}` } }
    )
    const expectedItems = (await expected.json()).data.items as {
      subject: string
      groupName: string
      numberPair: number
    }[]

    for (let index = 0; index < shown; index++) {
      const card = (await cards.nth(index).innerText()).trim()
      expect(hasBareDash(card), `карточка ${index + 1}: вместо предмета прочерк`).toBe(
        false
      )
      const item = expectedItems[index]
      if (!item) continue
      expect(
        card,
        `карточка ${index + 1}: предмет должен совпадать с журналом`
      ).toContain(item.subject)
    }
  })

  test("карточка добавления показывает введённый предмет", async ({ page }) => {
    test.setTimeout(120000)
    await useDispatcher(page)

    await page.goto("/changes")
    await expect(page.getByRole("heading", { name: /Изменения/i })).toBeVisible()
    await page.locator("#changes-type").selectOption("Add")
    await expect(page.getByRole("article").first()).toBeVisible()

    const cards = page.getByRole("article")
    const shown = await cards.count()
    for (let index = 0; index < shown; index++) {
      const card = (await cards.nth(index).innerText()).trim()
      expect(
        hasBareDash(card),
        `карточка ${index + 1}: вместо предмета прочерк`
      ).toBe(false)
    }
  })
})
