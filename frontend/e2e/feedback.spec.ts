import { test, expect } from "@playwright/test"

test.describe("Feedback form (public)", () => {
  test("renders feedback form on homepage", async ({ page }) => {
    await page.goto("/")
    await expect(page.getByRole("heading", { name: "Обратная связь" })).toBeVisible()
    await expect(page.getByLabel("Имя")).toBeVisible()
    await expect(page.getByLabel("Email")).toBeVisible()
    await expect(page.getByLabel("Сообщение")).toBeVisible()
    await expect(page.getByRole("button", { name: "Отправить" })).toBeVisible()
  })

  test("shows success after submitting feedback", async ({ page }) => {
    await page.route("**/api/feedback", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { message: "Сообщение отправлено" },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/")
    await page.getByLabel("Имя").fill("Тестовый Пользователь")
    await page.getByLabel("Email").fill("test@test.ru")
    await page.getByLabel("Сообщение").fill("Тестовое сообщение")
    await page.getByRole("button", { name: "Отправить" }).click()

    await expect(page.getByText("Сообщение отправлено! Мы свяжемся с вами в ближайшее время.")).toBeVisible()
  })

  test("shows error on failed submission", async ({ page }) => {
    await page.route("**/api/feedback", (route) =>
      route.fulfill({
        status: 429,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Вы уже отправляли сообщение. Попробуйте позже.",
          statusCode: 429,
        }),
      })
    )

    await page.goto("/")
    await page.getByLabel("Имя").fill("Тестовый Пользователь")
    await page.getByLabel("Email").fill("test@test.ru")
    await page.getByLabel("Сообщение").fill("Тестовое сообщение")
    await page.getByRole("button", { name: "Отправить" }).click()

    await expect(page.getByText("Не удалось отправить сообщение. Попробуйте позже.")).toBeVisible()
  })
})

test.describe("Admin feedback page (authenticated)", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("shows feedback list", async ({ page }) => {
    await page.route("**/api/feedback", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "f1", name: "Иван Иванов", email: "ivan@test.ru", message: "Отличный сайт!", createdAt: "2026-07-10T10:00:00Z" },
            { id: "f2", name: "Петр Петров", email: "petr@test.ru", message: "Хотелось бы больше информации о поступлении.", createdAt: "2026-07-09T15:30:00Z" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/feedback", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Обратная связь" })).toBeVisible()
    await expect(page.getByText("Иван Иванов")).toBeVisible()
    await expect(page.getByText("Петр Петров")).toBeVisible()
    await expect(page.getByText("2 сообщений")).toBeVisible()
  })

  test("shows empty state when no feedback", async ({ page }) => {
    await page.route("**/api/feedback", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/feedback", { waitUntil: "networkidle" })
    await expect(page.getByText("Нет сообщений")).toBeVisible()
    await expect(page.getByText("0 сообщений")).toBeVisible()
  })

  test("shows error on load failure", async ({ page }) => {
    await page.route("**/api/feedback", (route) =>
      route.fulfill({
        status: 500,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Ошибка загрузки сообщений",
          statusCode: 500,
        }),
      })
    )

    await page.goto("/admin/feedback", { waitUntil: "networkidle" })
    await expect(page.getByText("Ошибка загрузки сообщений")).toBeVisible()
  })

  test("feedback nav link is visible in admin menu", async ({ page }) => {
    await page.goto("/admin", { waitUntil: "networkidle" })
    await expect(page.getByRole("link", { name: "Обратная связь" })).toBeVisible()
  })
})
