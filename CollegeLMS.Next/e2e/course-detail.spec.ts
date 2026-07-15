import { test, expect } from "@playwright/test"

test.describe("Course detail page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher", isActive: true })
      )
    })
  })

  test("renders course detail with tabs", async ({ page }) => {
    await page.route("**/api/courses/c1", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 2, assignmentCount: 1 },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/lectures**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "l1", courseId: "c1", title: "Введение", content: "Текст лекции", order: 1 },
            { id: "l2", courseId: "c1", title: "Основы", content: "Основной материал", order: 2 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/assignments**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "a1", courseId: "c1", title: "ДЗ 1", description: "Описание", dueDate: "2026-12-31T23:59:59Z", maxScore: 100, order: 1, submissionCount: 0 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/materials**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "m1", courseId: "c1", lectureId: null, assignmentId: null, fileName: "lecture1.pdf", fileSize: 1024, mimeType: "application/pdf", createdAt: "2026-01-01T00:00:00Z" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses/c1", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Математика" })).toBeVisible()
    await expect(page.getByText("Преподаватель", { exact: true })).toBeVisible()
    await expect(page.getByText("Группа А")).toBeVisible()
  })

  test("shows lectures tab by default", async ({ page }) => {
    await page.route("**/api/courses/c1", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 2, assignmentCount: 1 },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/lectures**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "l1", courseId: "c1", title: "Введение", content: "Текст лекции", order: 1 },
            { id: "l2", courseId: "c1", title: "Основы", content: "Основной материал", order: 2 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/assignments**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "a1", courseId: "c1", title: "ДЗ 1", description: "Описание", dueDate: "2026-12-31T23:59:59Z", maxScore: 100, order: 1, submissionCount: 0 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/materials**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "m1", courseId: "c1", lectureId: null, assignmentId: null, fileName: "lecture1.pdf", fileSize: 1024, mimeType: "application/pdf", createdAt: "2026-01-01T00:00:00Z" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses/c1", { waitUntil: "networkidle" })
    await expect(page.getByText("Введение")).toBeVisible()
    await expect(page.getByText("Основы")).toBeVisible()
  })

  test("shows add lecture button for teacher", async ({ page }) => {
    await page.route("**/api/courses/c1", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 2, assignmentCount: 1 },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/lectures**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "l1", courseId: "c1", title: "Введение", content: "Текст лекции", order: 1 },
            { id: "l2", courseId: "c1", title: "Основы", content: "Основной материал", order: 2 },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/assignments**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "a1", courseId: "c1", title: "ДЗ 1", description: "Описание", dueDate: "2026-12-31T23:59:59Z", maxScore: 100, order: 1, submissionCount: 0 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses/c1/materials**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "m1", courseId: "c1", lectureId: null, assignmentId: null, fileName: "lecture1.pdf", fileSize: 1024, mimeType: "application/pdf", createdAt: "2026-01-01T00:00:00Z" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/courses/c1", { waitUntil: "networkidle" })
    await expect(page.getByRole("button", { name: "Добавить лекцию" })).toBeVisible()
  })
})

test.describe("Course detail page (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/courses/c1")
    await expect(page).toHaveURL("/login")
  })
})
