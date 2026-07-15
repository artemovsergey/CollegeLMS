import { test, expect } from "@playwright/test"

test.describe("Courses page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher", isActive: true })
      )
    })
  })

  test("renders the course list", async ({ page }) => {
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 5, assignmentCount: 3 },
            { id: "c2", title: "Физика", description: "Курс физики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g2", groupName: "Группа Б", status: "Draft", lectureCount: 0, assignmentCount: 1 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Курсы" })).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
  })

  test("shows create button for teacher", async ({ page }) => {
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 5, assignmentCount: 3 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses", { waitUntil: "networkidle" })
    await expect(page.getByRole("button", { name: "+ Создать" })).toBeVisible()
  })

  test("navigates to create page", async ({ page }) => {
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 5, assignmentCount: 3 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page).toHaveURL("/courses/new")
  })
})

test.describe("Courses page (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/courses")
    await expect(page).toHaveURL("/login")
  })
})
