import { test, expect } from "@playwright/test"

const adminUser = {
  id: "00000000-0000-0000-0000-000000000001",
  email: "admin@collegelms.ru",
  fullName: "Администратор",
  role: "Admin",
  isActive: true,
}

test.describe("Users page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem("user", JSON.stringify(adminUser))
    })
  })

  test("renders the user list", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [adminUser],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/")
    await expect(page.getByText("Пользователи")).toBeVisible()
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
    await page.goto("/")

    await expect(page.getByText("Ошибка загрузки")).toBeVisible()
  })
})
