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

  test("renders teacher dashboard with courses", async ({ page }) => {
    await page.route("**/api/teacher/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            courses: [
              { id: "c1", title: "Математика", groupNames: "ГР-01, ГР-02" },
              { id: "c2", title: "Физика", groupNames: "ГР-03" },
            ],
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/teacher/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByText("Здравствуйте, Преподаватель")).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
    await expect(page.getByText("ГР-01, ГР-02")).toBeVisible()
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

  test("renders student dashboard with courses and progress", async ({ page }) => {
    await page.route("**/api/my/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            courses: [
              { id: "c1", title: "Математика", teacherName: "Иван Петров", completionPercent: 50, completedItems: 3, totalItems: 6 },
              { id: "c2", title: "Физика", teacherName: "Мария Сидорова", completionPercent: 100, completedItems: 5, totalItems: 5 },
            ],
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/my/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByText("Здравствуйте, Студент")).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
    await expect(page.getByText("3 из 6 выполнено")).toBeVisible()
    await expect(page.getByText("5 из 5 выполнено")).toBeVisible()
  })

  test("shows empty state when no courses", async ({ page }) => {
    await page.route("**/api/my/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: { courses: [] },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/my/dashboard", { waitUntil: "networkidle" })
    await expect(page.getByText("У вас нет активных курсов")).toBeVisible()
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
