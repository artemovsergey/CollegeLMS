import { test, expect } from "@playwright/test"

test.describe("Specialties page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" })
      )
    })
  })

  test("renders specialty list", async ({ page }) => {
    await page.route("**/api/specialties**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС" },
            { id: "sp2", code: "38.02.01", name: "Экономика и бухгалтерский учёт", description: "Подготовка бухгалтеров", isActive: false },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/specialties", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Специальности" })).toBeVisible()
    await expect(page.getByText("09.02.07")).toBeVisible()
    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("38.02.01")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).toBeVisible()
    await expect(page.getByText("Активна")).toBeVisible()
    await expect(page.getByText("Неактивна")).toBeVisible()
  })

  test("search filters specialties", async ({ page }) => {
    await page.route("**/api/specialties**", async (route) => {
      const url = route.request().url()
      if (url.includes("search=09")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС" },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС" },
              { id: "sp2", code: "38.02.01", name: "Экономика и бухгалтерский учёт", description: "Подготовка бухгалтеров", isActive: false },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      }
    })

    await page.goto("/admin/specialties", { waitUntil: "networkidle" })
    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).toBeVisible()

    await page.getByPlaceholder("Поиск по коду или названию...").fill("09")
    await page.waitForTimeout(500)

    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).not.toBeVisible()
  })
})

test.describe("Testing page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" })
      )
    })
  })

  test("renders test list", async ({ page }) => {
    await page.route("**/api/tests**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", title: "Контрольная работа №1", description: "Первая контрольная", courseId: "c1", courseTitle: "Математика", maxAttempts: 1, timeLimitMinutes: 90, passingScore: 60, type: "Control" },
            { id: "t2", title: "Самостоятельная работа №1", description: "Самостоятельная по теме", courseId: "c2", courseTitle: "Физика", maxAttempts: 3, timeLimitMinutes: 45, passingScore: 50, type: "SelfStudy" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/testing", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Тестирование" })).toBeVisible()
    await expect(page.getByText("Контрольная работа №1")).toBeVisible()
    await expect(page.getByText("Самостоятельная работа №1")).toBeVisible()
    await expect(page.getByText("Контрольная")).toBeVisible()
    await expect(page.getByText("Самостоятельная")).toBeVisible()
  })

  test("opens create test dialog", async ({ page }) => {
    await page.route("**/api/tests**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", title: "Контрольная работа №1", description: "Первая контрольная", courseId: "c1", courseTitle: "Математика", maxAttempts: 1, timeLimitMinutes: 90, passingScore: 60, type: "Control" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "c1", title: "Математика", description: "Курс математики", teacherId: "t1", teacherName: "Иван Петров", groupNames: "Группа А", status: "Active", lectureCount: 10, assignmentCount: 5 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/testing", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать тест" }).click()
    await expect(page.getByText("Создать тест")).toBeVisible()
  })
})
