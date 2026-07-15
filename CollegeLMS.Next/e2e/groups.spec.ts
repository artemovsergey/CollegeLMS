import { test, expect } from "@playwright/test"

test.describe("Groups page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders the group list", async ({ page }) => {
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "g1", name: "Группа А", course: 1, studentCount: 15 },
            { id: "g2", name: "Группа Б", course: 2, studentCount: 20 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/groups", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Группы" })).toBeVisible()
    await expect(page.getByText("Группа А")).toBeVisible()
    await expect(page.getByText("Группа Б")).toBeVisible()
  })

  test("shows loading state initially", async ({ page }) => {
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/groups")
    await expect(page.locator(".animate-spin")).toBeVisible()
  })

  test("displays empty state", async ({ page }) => {
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/groups", { waitUntil: "networkidle" })
    await expect(page.getByText("Нет групп")).toBeVisible()
  })

  test("shows create button for admin", async ({ page }) => {
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "g1", name: "Группа А", course: 1, studentCount: 15 },
            { id: "g2", name: "Группа Б", course: 2, studentCount: 20 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/groups", { waitUntil: "networkidle" })
    await expect(page.getByRole("button", { name: "+ Создать" })).toBeVisible()
  })

  test("opens create dialog", async ({ page }) => {
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "g1", name: "Группа А", course: 1, studentCount: 15 },
            { id: "g2", name: "Группа Б", course: 2, studentCount: 20 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/groups", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать группу")).toBeVisible()
    await expect(page.getByLabel("Название группы")).toBeVisible()
    await expect(page.getByLabel("Курс")).toBeVisible()
  })
})

test.describe("Groups page (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/groups")
    await expect(page).toHaveURL("/login")
  })
})
