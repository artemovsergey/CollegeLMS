import { test, expect } from "@playwright/test"

test.describe("Students page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders the student list", async ({ page }) => {
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "s1", fullName: "Алексей Иванов", email: "alex@test.ru", groupId: "g1", groupName: "Группа А", recordBookNumber: "12345" },
            { id: "s2", fullName: "Ольга Петрова", email: "olga@test.ru", groupId: "g2", groupName: "Группа Б", recordBookNumber: "67890" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/students", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Студенты" })).toBeVisible()
    await expect(page.getByText("Алексей Иванов")).toBeVisible()
    await expect(page.getByText("Группа А")).toBeVisible()
  })

  test("shows loading state", async ({ page }) => {
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/students")
    await expect(page.locator(".animate-spin")).toBeVisible()
  })

  test("shows create button for admin", async ({ page }) => {
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "s1", fullName: "Алексей Иванов", email: "alex@test.ru", groupId: "g1", groupName: "Группа А", recordBookNumber: "12345" },
            { id: "s2", fullName: "Ольга Петрова", email: "olga@test.ru", groupId: "g2", groupName: "Группа Б", recordBookNumber: "67890" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
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

    await page.goto("/students", { waitUntil: "networkidle" })
    await expect(page.getByRole("button", { name: "+ Создать" })).toBeVisible()
  })

  test("opens create dialog", async ({ page }) => {
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "s1", fullName: "Алексей Иванов", email: "alex@test.ru", groupId: "g1", groupName: "Группа А", recordBookNumber: "12345" },
            { id: "s2", fullName: "Ольга Петрова", email: "olga@test.ru", groupId: "g2", groupName: "Группа Б", recordBookNumber: "67890" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
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

    await page.goto("/students", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать студента")).toBeVisible()
  })
})

test.describe("Students page (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/students")
    await expect(page).toHaveURL("/login")
  })
})
