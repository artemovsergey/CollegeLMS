import { test, expect } from "@playwright/test"

const adminUser = {
  id: "00000000-0000-0000-0000-000000000001",
  email: "admin@collegelms.ru",
  fullName: "Администратор",
  role: "Admin",
  isActive: true,
}

const loginResponse = {
  isSuccess: true,
  data: {
    token: "test-jwt-token",
    user: adminUser,
  },
  errorMessage: null,
  statusCode: 200,
}

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
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(loginResponse) })
    )

    await page.goto("/login")
    await page.fill("#email", "admin@collegelms.ru")
    await page.fill("#password", "admin")
    await page.click("button[type='submit']")

    await expect(page).toHaveURL("/", { timeout: 5000 })
    await expect(page.getByText("CollegeLMS")).toBeVisible()
    await expect(page.getByText("admin@collegelms.ru")).toBeVisible()
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
    await page.fill("#email", "wrong@test.ru")
    await page.fill("#password", "wrong")
    await page.click("button[type='submit']")

    await expect(page.getByText("Неверный email или пароль")).toBeVisible()
  })

  test("logout clears session and redirects to login", async ({ page }) => {
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(loginResponse) })
    )

    await page.goto("/login")
    await page.fill("#email", "admin@collegelms.ru")
    await page.fill("#password", "admin")
    await page.click("button[type='submit']")
    await expect(page).toHaveURL("/", { timeout: 5000 })

    await page.getByText("Выйти").click()
    await expect(page).toHaveURL("/login")
  })
})

test.describe("User management (authenticated)", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify(adminUser)
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
          data: [adminUser],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/")
    await expect(page.getByText("Пользователи")).toBeVisible()
    await expect(page.getByText("admin@collegelms.ru")).toBeVisible()
  })

  test("shows create user form", async ({ page }) => {
    await page.route("**/api/users", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [adminUser],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/")
    await page.getByText("+ Создать").click()
    await expect(page.getByText("Создать пользователя")).toBeVisible()
  })
})
