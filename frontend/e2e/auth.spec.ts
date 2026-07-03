import { test, expect } from "@playwright/test"

test.describe("Auth flow", () => {
  test("login page renders", async ({ page }) => {
    await page.goto("/login")
    await expect(page.getByText("Вход в систему")).toBeVisible()
    await expect(page.getByLabel("Email")).toBeVisible()
    await expect(page.getByLabel("Пароль")).toBeVisible()
    await expect(page.getByRole("button", { name: "Войти" })).toBeVisible()
  })

  test("redirects to login when not authenticated", async ({ page }) => {
    await page.goto("/")
    await expect(page).toHaveURL("/login")
  })

  test("successful login redirects to home", async ({ page }) => {
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: {
            token: "test-jwt-token",
            user: { id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true },
          },
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/login")
    await page.getByLabel("Email").fill("admin@collegelms.ru")
    await page.getByLabel("Пароль").fill("admin")
    await page.getByRole("button", { name: "Войти" }).click()

    await page.waitForURL("/", { timeout: 5000 })
    await expect(page.getByRole("heading", { name: "CollegeLMS" })).toBeVisible()
  })

  test("failed login shows error message", async ({ page }) => {
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Неверный email или пароль",
          statusCode: 401,
        }),
      })
    )

    await page.goto("/login")
    await page.getByLabel("Email").fill("wrong@test.ru")
    await page.getByLabel("Пароль").fill("wrong")
    await page.getByRole("button", { name: "Войти" }).click()

    await expect(page.getByText("Неверный email или пароль")).toBeVisible()
  })

  test("logout clears session and redirects to login", async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/", { waitUntil: "networkidle" })
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
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
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
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/", { waitUntil: "networkidle" })
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
          data: [{ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать пользователя")).toBeVisible()
  })
})
