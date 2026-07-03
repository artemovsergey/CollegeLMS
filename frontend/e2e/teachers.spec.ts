import { test, expect } from "@playwright/test"

test.describe("Teachers page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders the teacher list", async ({ page }) => {
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", fullName: "Иван Петров", email: "ivan@collegelms.ru", department: "ИТ", position: "Доцент" },
            { id: "t2", fullName: "Мария Сидорова", email: "maria@collegelms.ru", department: "Мат", position: "Профессор" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teachers", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Преподаватели" })).toBeVisible()
    await expect(page.getByText("Иван Петров")).toBeVisible()
    await expect(page.getByText("maria@collegelms.ru")).toBeVisible()
  })

  test("shows loading state", async ({ page }) => {
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/teachers")
    await expect(page.locator(".animate-spin")).toBeVisible()
  })

  test("shows create button for admin", async ({ page }) => {
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", fullName: "Иван Петров", email: "ivan@collegelms.ru", department: "ИТ", position: "Доцент" },
            { id: "t2", fullName: "Мария Сидорова", email: "maria@collegelms.ru", department: "Мат", position: "Профессор" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teachers", { waitUntil: "networkidle" })
    await expect(page.getByRole("button", { name: "+ Создать" })).toBeVisible()
  })

  test("opens create dialog", async ({ page }) => {
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", fullName: "Иван Петров", email: "ivan@collegelms.ru", department: "ИТ", position: "Доцент" },
            { id: "t2", fullName: "Мария Сидорова", email: "maria@collegelms.ru", department: "Мат", position: "Профессор" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teachers", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать преподавателя")).toBeVisible()
    await expect(page.getByLabel("Email")).toBeVisible()
    await expect(page.getByLabel("Пароль")).toBeVisible()
  })
})

test.describe("Teachers page (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/teachers")
    await expect(page).toHaveURL("/login")
  })
})
