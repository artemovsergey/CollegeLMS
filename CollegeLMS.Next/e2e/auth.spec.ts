import { test, expect } from "@playwright/test"

const TEACHER_LOGIN_MOCK = {
  isSuccess: true,
  data: {
    token: "test-jwt-token",
    user: { id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher" },
  },
  errorMessage: null,
  statusCode: 200,
}

const TEACHER_DASHBOARD_MOCK = {
  isSuccess: true,
  data: {
    courses: [
      { id: "c1", title: "Математика", groupNames: "ГР-01, ГР-02" },
    ],
  },
  errorMessage: null,
  statusCode: 200,
}

test.describe("Auth flow", () => {
  test("login page renders", async ({ page }) => {
    await page.goto("/login")
    await expect(page.getByText("Личный кабинет")).toBeVisible()
    await expect(page.getByLabel("Логин")).toBeVisible()
    await expect(page.getByLabel("Пароль")).toBeVisible()
    await expect(page.getByRole("button", { name: "Войти" })).toBeVisible()
    await expect(page.getByLabel("Логин").locator("..")).not.toContainText("*")
  })

  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/my/dashboard")
    await expect(page).toHaveURL("/login")
  })

  test("successful teacher login redirects to teacher dashboard", async ({ page }) => {
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(TEACHER_LOGIN_MOCK),
      })
    )
    await page.route("**/api/teacher/dashboard**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(TEACHER_DASHBOARD_MOCK),
      })
    )
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/login")
    await page.getByLabel("Логин").fill("teacher")
    await page.getByLabel("Пароль").fill("teacher")
    await page.getByRole("button", { name: "Войти" }).click()

    await page.waitForURL("**/teacher/dashboard", { timeout: 5000 })
    await expect(page.getByText("Здравствуйте, Преподаватель")).toBeVisible()
  })

  test("failed login shows error message", async ({ page }) => {
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Неверный логин или пароль",
          statusCode: 401,
        }),
      })
    )

    await page.goto("/login")
    await page.getByLabel("Логин").fill("wrong")
    await page.getByLabel("Пароль").fill("wrong")
    await page.getByRole("button", { name: "Войти" }).click()

    await expect(page.getByText("Неверный логин или пароль")).toBeVisible()
  })

  test("logout clears session and redirects to login", async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" })
      )
    })
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/admin/**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: {}, errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "Профиль" }).click()
    await page.getByRole("button", { name: "Выйти" }).click()
    await expect(page).toHaveURL("/login")
  })
})

test.describe("User management (authenticated)", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" })
      )
    })
  })

  test("shows users list", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Пользователи" })).toBeVisible()
    await expect(page.getByRole("cell", { name: "admin@collegelms.ru" })).toBeVisible()
  })

  test("shows create user form", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать пользователя")).toBeVisible()
  })
})