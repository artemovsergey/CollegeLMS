import { test, expect } from "@playwright/test"

test.describe("Users page", () => {
  test("renders the user list", async ({ page }) => {
    await page.goto("/")

    await expect(page.getByText("Пользователи")).toBeVisible()
  })

  test("shows loading state initially", async ({ page }) => {
    await page.goto("/")

    await expect(page.locator(".animate-spin")).toBeVisible()
  })

  test("displays error state on failure", async ({ page }) => {
    await page.route("/api/users", (route) => route.abort())
    await page.goto("/")

    await expect(page.getByText("Ошибка")).toBeVisible()
  })
})
