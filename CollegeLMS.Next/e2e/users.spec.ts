import { test, expect } from "@playwright/test"

test.describe("Users page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders the user list", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Пользователи" })).toBeVisible()
  })

  test("shows loading state initially", async ({ page }) => {
    await page.route("**/api/users", (route) =>
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

    await page.goto("/")
    await expect(page.locator(".animate-spin")).toBeVisible()
  })

  test("displays error state on failure", async ({ page }) => {
    await page.route("**/api/users", (route) => route.abort())
    await page.goto("/", { waitUntil: "networkidle" })

    await expect(page.getByText("Ошибка загрузки")).toBeVisible()
  })
})
