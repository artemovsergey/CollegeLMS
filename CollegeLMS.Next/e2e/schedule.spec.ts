import { test, expect } from "@playwright/test"

function json(body: unknown) {
  return {
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  }
}

const META = {
  isSuccess: true,
  data: {
    semesterStart: "2026-09-01T00:00:00Z",
    totalWeeks: 17,
    currentWeek: 4,
    currentDate: "2026-09-22T00:00:00Z",
  },
  errorMessage: null,
  statusCode: 200,
}

const CONTEXT = {
  isSuccess: true,
  data: {
    role: "Admin",
    groupId: null,
    groupName: null,
    teacherId: null,
    teacherName: null,
  },
  errorMessage: null,
  statusCode: 200,
}

const WEEK_VIEW = {
  isSuccess: true,
  data: {
    week: 4,
    weekStart: "2026-09-21T00:00:00Z",
    days: [],
  },
  errorMessage: null,
  statusCode: 200,
}

const MONTH_VIEW = {
  isSuccess: true,
  data: { year: 2026, month: 9, days: [] },
  errorMessage: null,
  statusCode: 200,
}

test.describe("Schedule page toolbar", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({
          id: "u1",
          email: "admin@collegelms.ru",
          fullName: "Администратор",
          roles: ["Admin"],
        })
      )
    })

    await page.route(
      (url) => url.pathname.startsWith("/api/schedule"),
      (route) => {
        const requestUrl = route.request().url()
        if (requestUrl.includes("/meta")) return route.fulfill(json(META))
        if (requestUrl.includes("/context")) return route.fulfill(json(CONTEXT))
        if (requestUrl.includes("view=calendar"))
          return route.fulfill(json(MONTH_VIEW))
        return route.fulfill(json(WEEK_VIEW))
      }
    )
    await page.route("**/api/groups**", (route) =>
      route.fulfill(json({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }))
    )
    await page.route("**/api/teachers**", (route) =>
      route.fulfill(json({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }))
    )
  })

  test("Тулбар: меню экспорта, переименованные фильтры, без «Добавить»", async ({
    page,
  }) => {
    await page.goto("/schedule?view=week&week=4", { waitUntil: "networkidle" })

    await expect(
      page.getByRole("heading", { name: "Расписание" })
    ).toBeVisible()

    // Фильтры переименованы: «Все группы» → «Группы», «Все преподаватели» → «Преподаватели».
    await expect(page.locator("select").nth(0)).toContainText("Группы")
    await expect(page.locator("select").nth(0)).not.toContainText("Все группы")
    await expect(page.locator("select").nth(1)).toContainText("Преподаватели")
    await expect(page.locator("select").nth(1)).not.toContainText(
      "Все преподаватели"
    )

    // Кнопка «Добавить» удалена, «Импорт» для администратора остаётся.
    await expect(page.getByRole("button", { name: "Добавить" })).toHaveCount(0)
    await expect(page.getByRole("button", { name: "Импорт" })).toBeVisible()

    // «Экспорт» — кнопка с выпадающим меню; подпись зависит от вида.
    const exportButton = page.getByRole("button", {
      name: "Экспорт расписания",
    })
    await expect(exportButton).toContainText("Экспорт: неделя")
    await exportButton.click()

    await expect(page.getByRole("menuitem", { name: "PDF — сетка" })).toBeVisible()
    await expect(
      page.getByRole("menuitem", { name: "PDF — по дням" })
    ).toBeVisible()
    await expect(
      page.getByRole("menuitem", { name: "Excel — сетка" })
    ).toBeVisible()
    await expect(
      page.getByRole("menuitem", { name: "Excel — по дням" })
    ).toBeVisible()
  })

  test("В виде «Календарь» кнопка экспорта скрыта", async ({ page }) => {
    await page.goto("/schedule?view=calendar&month=2026-09", {
      waitUntil: "networkidle",
    })

    await expect(page.getByRole("button", { name: "Календарь" })).toBeVisible()
    await expect(
      page.getByRole("button", { name: "Экспорт расписания" })
    ).toHaveCount(0)
  })

  test("Экспорт: имя файла берётся из filename* (кириллица)", async ({
    page,
  }) => {
    await page.route("**/api/schedule/export**", (route) =>
      route.fulfill({
        status: 200,
        headers: {
          "content-type":
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          // Как отдаёт nginx/API: ASCII-фолбэк для старых клиентов + filename* с кириллицей.
          "content-disposition":
            "attachment; filename=________________23.09.2026_11-00-00.xlsx; filename*=UTF-8''" +
            encodeURIComponent("Расписание_неделя_4.xlsx"),
        },
        body: "PK\u0003\u0004",
      })
    )

    await page.goto("/schedule?view=week&week=4", { waitUntil: "networkidle" })
    const downloadPromise = page.waitForEvent("download")
    await page.getByRole("button", { name: "Экспорт расписания" }).click()
    // Меню должно быть у экрана: Radix позиционирует его только тогда, когда
    // триггер пробрасывает ref (иначе оно уезжает за пределы вьюпорта).
    const item = page.getByRole("menuitem", { name: "Excel — сетка" })
    await expect(item).toBeInViewport()
    await item.click()
    const download = await downloadPromise

    expect(download.suggestedFilename()).toBe("Расписание_неделя_4.xlsx")
  })

  test("Экспорт: имя файла из filename= без filename*", async ({ page }) => {
    await page.route("**/api/schedule/export**", (route) =>
      route.fulfill({
        status: 200,
        headers: {
          "content-type": "application/pdf",
          "content-disposition": 'attachment; filename="schedule-grid.pdf"',
        },
        body: "%PDF-1.4",
      })
    )

    await page.goto("/schedule?view=week&week=4", { waitUntil: "networkidle" })
    const downloadPromise = page.waitForEvent("download")
    await page.getByRole("button", { name: "Экспорт расписания" }).click()
    const item = page.getByRole("menuitem", { name: "PDF — сетка" })
    await expect(item).toBeInViewport()
    await item.click()
    const download = await downloadPromise

    expect(download.suggestedFilename()).toBe("schedule-grid.pdf")
  })
})
