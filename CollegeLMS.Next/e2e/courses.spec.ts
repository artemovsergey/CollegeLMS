import { test, expect } from "@playwright/test"

test.describe("Courses page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher" })
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
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupNames: "Группа А", status: "Active", lessonCount: 5, documentCount: 3 },
            { id: "c2", title: "Физика", description: "Курс физики", teacherId: "u2", teacherName: "Преподаватель", groupNames: "Группа Б", status: "Draft", lessonCount: 0, documentCount: 1 },
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
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupNames: "Группа А", status: "Active", lessonCount: 5, documentCount: 3 },
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
            { id: "c1", title: "Математика", description: "Курс математики", teacherId: "u2", teacherName: "Преподаватель", groupNames: "Группа А", status: "Active", lessonCount: 5, documentCount: 3 },
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

test.describe("Course actions (duplicate, active, delete)", () => {
  const TEACHER_COURSE = {
    id: "c1",
    title: "Математика",
    description: "",
    teacherId: "t1",
    teacherName: "Преподаватель",
    groupNames: "",
    status: "Active",
    lessonCount: 2,
    documentCount: 0,
    isActive: true,
    authorIds: [],
    authorNames: "",
  }

  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({
          id: "u2",
          email: "t@t.ru",
          fullName: "Преподаватель",
          role: "Teacher",
          teacherId: "t1",
        })
      )
    })
  })

  test("toggles course activity", async ({ page }) => {
    let course = { ...TEACHER_COURSE }
    await page.route("**/api/courses**", async route => {
      const req = route.request()
      if (req.method() === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [course],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else if (req.method() === "PATCH" && req.url().includes("/active")) {
        course = { ...course, isActive: false }
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ isSuccess: true, data: null, errorMessage: null, statusCode: 200 }),
        })
      } else {
        await route.continue()
      }
    })

    await page.goto("/courses", { waitUntil: "networkidle" })
    const toggle = page.getByRole("switch")
    await expect(toggle).toBeChecked()
    await toggle.click()
    await expect(toggle).not.toBeChecked()
  })

  test("duplicates a course", async ({ page }) => {
    await page.route("**/api/courses**", async route => {
      const req = route.request()
      if (req.method() === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [TEACHER_COURSE],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else if (req.method() === "POST" && req.url().includes("/duplicate")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: { ...TEACHER_COURSE, id: "c2", title: "Математика (копия)", isActive: false, status: "Draft" },
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else {
        await route.continue()
      }
    })

    await page.goto("/courses", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "Дублировать" }).click()
    await expect(page.getByText("Математика (копия)")).toBeVisible()
  })

  test("deletes a course after confirmation", async ({ page }) => {
    await page.route("**/api/courses**", async route => {
      const req = route.request()
      if (req.method() === "GET") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [TEACHER_COURSE],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else if (req.method() === "DELETE") {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ isSuccess: true, data: null, errorMessage: null, statusCode: 200 }),
        })
      } else {
        await route.continue()
      }
    })

    await page.goto("/courses", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "Удалить" }).click()
    await expect(page.getByText("Удалить курс?")).toBeVisible()
    await page.getByRole("button", { name: "Удалить", exact: true }).click()
    await expect(page.getByText("Нет курсов")).toBeVisible()
  })
})
