import { test, expect } from "@playwright/test"

test.describe("Teacher dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher", isActive: true })
      )
    })
  })

  test("renders teacher dashboard", async ({ page }) => {
    await page.route("**/api/teacher/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            coursesCount: 3, studentsCount: 25,
            recentSubmissions: [
              { id: "sub1", assignmentId: "a1", studentId: "s1", studentName: "Алексей", filePath: "work.pdf", comment: null, score: null, submittedAt: "2026-06-01T10:00:00Z" },
            ],
            courses: [{ id: "c1", title: "Математика" }, { id: "c2", title: "Физика" }],
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teacher/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Панель преподавателя" })).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
  })

  test("shows recent submissions table", async ({ page }) => {
    await page.route("**/api/teacher/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            coursesCount: 3, studentsCount: 25,
            recentSubmissions: [{ id: "sub1", assignmentId: "a1", studentId: "s1", studentName: "Алексей", filePath: "work.pdf", comment: null, score: null, submittedAt: "2026-06-01T10:00:00Z" }],
            courses: [{ id: "c1", title: "Математика" }],
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teacher/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByText("Последние работы")).toBeVisible()
    await expect(page.getByText("Алексей")).toBeVisible()
  })
})

test.describe("Teacher dashboard (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/teacher/dashboard")
    await expect(page).toHaveURL("/login")
  })
})

test.describe("Student dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u3", email: "student@collegelms.ru", fullName: "Студент", role: "Student", isActive: true })
      )
    })
  })

  test("renders student dashboard", async ({ page }) => {
    await page.route("**/api/my/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { coursesCount: 2, recentGrades: [{ courseName: "Математика", score: 85 }], upcomingDeadlines: [{ assignmentTitle: "ДЗ 1", dueDate: "2026-12-31T23:59:59Z" }] },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/my/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Моя панель" })).toBeVisible()
    await expect(page.getByText("ДЗ 1")).toBeVisible()
    await expect(page.getByText("85")).toBeVisible()
  })
})

test.describe("Student dashboard (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/my/dashboard")
    await expect(page).toHaveURL("/login")
  })
})

test.describe("My submissions", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u3", email: "student@collegelms.ru", fullName: "Студент", role: "Student", isActive: true })
      )
    })
  })

  test("renders submissions list", async ({ page }) => {
    await page.route("**/api/my/submissions**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "sub1", assignmentId: "a1", studentId: "s1", studentName: "Студент", filePath: "work.pdf", comment: null, score: 85, submittedAt: "2026-06-01T10:00:00Z", assignmentTitle: "ДЗ 1", courseTitle: "Математика" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/my/submissions", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Мои работы" })).toBeVisible()
    await expect(page.getByText("ДЗ 1")).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
  })
})

test.describe("My submissions (no auth)", () => {
  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/my/submissions")
    await expect(page).toHaveURL("/login")
  })
})
