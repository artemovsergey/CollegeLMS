import { test, expect } from "@playwright/test"

test.describe("Exams page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders exam list with data", async ({ page }) => {
    await page.route("**/api/exams**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "e1", subject: "Математика", groupId: "g1", groupName: "Группа А", examDate: "2026-01-15T10:00:00", type: "Exam", teacherId: "t1", teacherName: "Иван Петров", semesterId: "sem1", semesterName: "Осень 2025", status: "Scheduled" },
            { id: "e2", subject: "Физика", groupId: "g2", groupName: "Группа Б", examDate: "2026-01-20T10:00:00", type: "Credit", teacherId: "t2", teacherName: "Мария Сидорова", semesterId: "sem1", semesterName: "Осень 2025", status: "Completed" },
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
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/exams", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Экзамены" })).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.getByText("Группа А")).toBeVisible()
    await expect(page.getByText("Экзамен")).toBeVisible()
    await expect(page.getByText("Зачёт")).toBeVisible()
    await expect(page.getByText("Запланирован")).toBeVisible()
    await expect(page.getByText("Проведён")).toBeVisible()
  })

  test("opens create exam dialog", async ({ page }) => {
    await page.route("**/api/exams**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "e1", subject: "Математика", groupId: "g1", groupName: "Группа А", examDate: "2026-01-15T10:00:00", type: "Exam", teacherId: "t1", teacherName: "Иван Петров", semesterId: "sem1", semesterName: "Осень 2025", status: "Scheduled" },
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
          data: [{ id: "g1", name: "Группа А", course: 1, studentCount: 15 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "t1", fullName: "Иван Петров", email: "ivan@test.ru", department: "Математика", position: "Доцент" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "sem1", name: "Осень 2025", startDate: "2025-09-01", endDate: "2025-12-31", type: "Autumn" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/exams", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать экзамен")).toBeVisible()
  })

  test("shows retakes dialog", async ({ page }) => {
    await page.route("**/api/exams**", async (route) => {
      const url = route.request().url()
      if (url.includes("/retakes")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "r1", examId: "e1", studentId: "s1", studentName: "Алексей Иванов", retakeDate: "2026-02-01T10:00:00", status: "Scheduled" },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "e1", subject: "Математика", groupId: "g1", groupName: "Группа А", examDate: "2026-01-15T10:00:00", type: "Exam", teacherId: "t1", teacherName: "Иван Петров", semesterId: "sem1", semesterName: "Осень 2025", status: "Scheduled" },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      }
    })
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/teachers**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/students**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/exams", { waitUntil: "networkidle" })
    await page.getByText("Математика").click()
    await expect(page.getByRole("dialog")).toBeVisible()
    await expect(page.getByText("Пересдачи")).toBeVisible()
    await expect(page.getByText("Алексей Иванов")).toBeVisible()
    await expect(page.getByText("Запланирована")).toBeVisible()
  })
})

test.describe("Semesters page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders semester list", async ({ page }) => {
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "sem1", name: "Осень 2025", startDate: "2025-09-01T00:00:00", endDate: "2025-12-31T00:00:00", type: "Autumn" },
            { id: "sem2", name: "Весна 2026", startDate: "2026-02-01T00:00:00", endDate: "2026-06-30T00:00:00", type: "Spring" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/semesters", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Семестры" })).toBeVisible()
    await expect(page.getByText("Осень 2025")).toBeVisible()
    await expect(page.getByText("Весна 2026")).toBeVisible()
    await expect(page.getByText("Осенний")).toBeVisible()
    await expect(page.getByText("Весенний")).toBeVisible()
  })

  test("opens create dialog", async ({ page }) => {
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "sem1", name: "Осень 2025", startDate: "2025-09-01T00:00:00", endDate: "2025-12-31T00:00:00", type: "Autumn" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/semesters", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать" }).click()
    await expect(page.getByText("Создать семестр")).toBeVisible()
  })

  test("opens edit dialog", async ({ page }) => {
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "sem1", name: "Осень 2025", startDate: "2025-09-01T00:00:00", endDate: "2025-12-31T00:00:00", type: "Autumn" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/semesters", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "Ред." }).click()
    await expect(page.getByText("Редактировать семестр")).toBeVisible()
  })
})

test.describe("Specialties page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders specialty list", async ({ page }) => {
    await page.route("**/api/specialties**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС", isActive: true },
            { id: "sp2", code: "38.02.01", name: "Экономика и бухгалтерский учёт", description: "Подготовка бухгалтеров", isActive: false },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/specialties", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Специальности" })).toBeVisible()
    await expect(page.getByText("09.02.07")).toBeVisible()
    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("38.02.01")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).toBeVisible()
    await expect(page.getByText("Активна")).toBeVisible()
    await expect(page.getByText("Неактивна")).toBeVisible()
  })

  test("search filters specialties", async ({ page }) => {
    await page.route("**/api/specialties**", async (route) => {
      const url = route.request().url()
      if (url.includes("search=09")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС", isActive: true },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      } else {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            isSuccess: true,
            data: [
              { id: "sp1", code: "09.02.07", name: "Информационные системы и программирование", description: "Подготовка специалистов по ИС", isActive: true },
              { id: "sp2", code: "38.02.01", name: "Экономика и бухгалтерский учёт", description: "Подготовка бухгалтеров", isActive: false },
            ],
            errorMessage: null,
            statusCode: 200,
          }),
        })
      }
    })

    await page.goto("/admin/specialties", { waitUntil: "networkidle" })
    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).toBeVisible()

    await page.getByPlaceholder("Поиск по коду или названию...").fill("09")
    await page.waitForTimeout(500)

    await expect(page.getByText("Информационные системы и программирование")).toBeVisible()
    await expect(page.getByText("Экономика и бухгалтерский учёт")).not.toBeVisible()
  })
})

test.describe("Stipends page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders stipend list", async ({ page }) => {
    await page.route("**/api/stipends**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "st1", title: "Академическая стипендия — Осень 2025", semesterId: "sem1", semesterName: "Осень 2025", studentCount: 45, createdAt: "2026-01-10T12:00:00" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/stipends", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Стипендии" })).toBeVisible()
    await expect(page.getByText("Академическая стипендия — Осень 2025")).toBeVisible()
    await expect(page.getByText("Осень 2025")).toBeVisible()
    await expect(page.getByText("45")).toBeVisible()
  })

  test("opens generate dialog", async ({ page }) => {
    await page.route("**/api/stipends**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/semesters**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "sem1", name: "Осень 2025", startDate: "2025-09-01T00:00:00", endDate: "2025-12-31T00:00:00", type: "Autumn" }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )

    await page.goto("/admin/stipends", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Сформировать" }).click()
    await expect(page.getByText("Сформировать стипендию")).toBeVisible()
  })
})

test.describe("Testing page", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u1", email: "admin@collegelms.ru", fullName: "Администратор", role: "Admin", isActive: true })
      )
    })
  })

  test("renders test list", async ({ page }) => {
    await page.route("**/api/tests**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", title: "Контрольная работа №1", description: "Первая контрольная", courseId: "c1", courseName: "Математика", maxAttempts: 1, timeLimitMinutes: 90, passingScore: 60, type: "Control" },
            { id: "t2", title: "Самостоятельная работа №1", description: "Самостоятельная по теме", courseId: "c2", courseName: "Физика", maxAttempts: 3, timeLimitMinutes: 45, passingScore: 50, type: "SelfStudy" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/testing", { waitUntil: "networkidle" })
    await expect(page.getByRole("heading", { name: "Тестирование" })).toBeVisible()
    await expect(page.getByText("Контрольная работа №1")).toBeVisible()
    await expect(page.getByText("Самостоятельная работа №1")).toBeVisible()
    await expect(page.getByText("Контрольная")).toBeVisible()
    await expect(page.getByText("Самостоятельная")).toBeVisible()
  })

  test("opens create test dialog", async ({ page }) => {
    await page.route("**/api/tests**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [
            { id: "t1", title: "Контрольная работа №1", description: "Первая контрольная", courseId: "c1", courseName: "Математика", maxAttempts: 1, timeLimitMinutes: 90, passingScore: 60, type: "Control" },
          ],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/courses**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: true,
          data: [{ id: "c1", title: "Математика", description: "Курс математики", teacherId: "t1", teacherName: "Иван Петров", groupId: "g1", groupName: "Группа А", status: "Active", lectureCount: 10, assignmentCount: 5 }],
          errorMessage: null,
          statusCode: 200,
        }),
      })
    )
    await page.route("**/api/groups**", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 }),
      })
    )

    await page.goto("/admin/testing", { waitUntil: "networkidle" })
    await page.getByRole("button", { name: "+ Создать тест" }).click()
    await expect(page.getByText("Создать тест")).toBeVisible()
  })
})
