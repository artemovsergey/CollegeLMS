import { test, expect } from "@playwright/test"

test.describe("Student Progress page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "s1", login: "student", email: "student@collegelms.ru", fullName: "Студент", role: "Student", isActive: true })
      )
    })
  })

  test("shows progress for student", async ({ page }) => {
    await page.route("**/api/my/courses/c1/progress", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            courseId: "c1",
            courseTitle: "Математика",
            totalAssignments: 10,
            completedAssignments: 7,
            totalTests: 4,
            completedTests: 3,
            averageScore: 84,
            completionPercent: 71,
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/my/courses/c1/progress", { waitUntil: "networkidle" })
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("10")).toBeVisible()
    await expect(page.getByText("7")).toBeVisible()
    await expect(page.getByText("4")).toBeVisible()
    await expect(page.getByText("3")).toBeVisible()
    await expect(page.getByText("84")).toBeVisible()
    await expect(page.getByText("71%")).toBeVisible()
  })

  test("shows not found", async ({ page }) => {
    await page.route("**/api/my/courses/c1/progress", (route) =>
      route.fulfill({
        status: 404,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Курс не найден",
          statusCode: 404,
        }),
      })
    )

    await page.goto("/my/courses/c1/progress", { waitUntil: "networkidle" })
    await expect(page.getByText("Курс не найден")).toBeVisible()
  })
})

test.describe("Student Transfers on Students page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", login: "admin", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("shows transfer dialog", async ({ page }) => {
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "s1", fullName: "Алексей Иванов", email: "alex@test.ru", groupId: "g1", groupName: "Группа А", recordBookNumber: "12345" },
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
    await page.getByRole("button", { name: "Перевести" }).click()
    await expect(page.getByText("Перевод студента")).toBeVisible()
    await expect(page.getByLabel("Новая группа")).toBeVisible()
  })

  test("shows transfer history", async ({ page }) => {
    await page.route("**/api/students**", (route) => {
      if (route.request().url().includes("/transfers")) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              {
                id: "t1",
                studentId: "s1",
                fromGroupName: "Группа А",
                toGroupName: "Группа Б",
                reason: "Смена специализации",
                transferredAt: "2026-01-15T10:00:00Z",
              },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "s1", fullName: "Алексей Иванов", email: "alex@test.ru", groupId: "g1", groupName: "Группа А", recordBookNumber: "12345" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    })
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "g1", name: "Группа А", course: 1, studentCount: 15 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/students", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "История" }).click()
    await expect(page.getByText("История переводов")).toBeVisible()
    await expect(page.getByText("Группа А")).toBeVisible()
    await expect(page.getByText("Группа Б")).toBeVisible()
    await expect(page.getByText("Смена специализации")).toBeVisible()
  })
})

test.describe("Change Password in admin sidebar", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", login: "admin", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("opens change password dialog", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", login: "admin", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "Пароль" }).click()
    await expect(page.getByText("Сменить пароль")).toBeVisible()
    await expect(page.getByLabel("Текущий пароль")).toBeVisible()
    await expect(page.getByLabel("Новый пароль")).toBeVisible()
  })
})
