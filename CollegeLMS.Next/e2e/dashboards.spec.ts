import { test, expect } from "@playwright/test"

test.describe("Teacher dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({ id: "u2", email: "teacher@collegelms.ru", fullName: "Преподаватель", role: "Teacher" })
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
        JSON.stringify({ id: "u3", email: "student@collegelms.ru", fullName: "Студент", role: "Student" })
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

function json(body: unknown) {
  return {
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  }
}

const LIVE_DASHBOARD = {
  isSuccess: true,
  data: {
    now: "2026-09-22T10:00:00+03:00",
    date: "2026-09-22",
    week: 4,
    dayOfWeek: 2,
    isWorkingDay: false,
    isNonWorking: false,
    workingDayTitle: null,
    nonWorkingTitle: null,
    counts: { inLesson: 1, finished: 2, noPairs: 1, waiting: 3 },
    teachers: [
      {
        id: "t1",
        name: "Петренко В.Б.",
        status: "InLesson",
        totalPairs: 3,
        currentPair: {
          numberPair: 2,
          startTime: "09:40:00",
          endTime: "11:15:00",
        },
        nextPair: null,
        entries: [
          {
            groupId: "g1",
            groupName: "ПО262",
            subject: "Математика",
            room: "301",
            numberPair: 2,
            startTime: "09:40:00",
            endTime: "11:15:00",
            lessonType: "Lecture",
            isPractice: false,
            practiceName: null,
            teacherId: "t1",
            teacherName: "Петренко В.Б.",
          },
        ],
      },
    ],
    groups: [
      {
        id: "g1",
        name: "ПО262",
        status: "Waiting",
        totalPairs: 2,
        currentPair: null,
        nextPair: {
          numberPair: 3,
          startTime: "11:30:00",
          endTime: "13:05:00",
        },
        entries: [],
      },
    ],
  },
  errorMessage: null,
  statusCode: 200,
}

test.describe("Dispatcher dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "test-jwt-token")
      localStorage.setItem(
        "user",
        JSON.stringify({
          id: "u4",
          email: "dispatcher@collegelms.ru",
          fullName: "Диспетчер",
          roles: ["Dispatcher"],
        })
      )
    })
    await page.route("**/api/dispatcher/live**", (route) =>
      route.fulfill(json(LIVE_DASHBOARD))
    )
    await page.route("**/api/dispatcher/dashboard**", (route) =>
      route.fulfill(
        json({
          isSuccess: true,
          data: {
            date: "2026-09-22",
            week: 4,
            dayOfWeek: 2,
            slots: [],
            teachers: [],
          },
          errorMessage: null,
          statusCode: 200,
        })
      )
    )
    await page.route("**/api/schedule**", (route) =>
      route.fulfill(
        json({ isSuccess: true, data: [], errorMessage: null, statusCode: 200 })
      )
    )
  })

  test("live-блок «Текущие пары» перенесён на дашборд", async ({ page }) => {
    await page.goto("/dispatcher/dashboard", { waitUntil: "networkidle" })

    await expect(
      page.getByRole("heading", { name: "Текущие пары" })
    ).toBeVisible()
    // Кнопки управления датой/обновлением переехали сюда вместе с блоком.
    await expect(page.getByRole("button", { name: "День назад" })).toBeVisible()
    await expect(page.getByRole("button", { name: "Сегодня" })).toBeVisible()
    await expect(page.getByRole("button", { name: "День вперёд" })).toBeVisible()
    await expect(page.getByRole("button", { name: "Обновить" })).toBeVisible()

    // Плитки статусов.
    const summary = page.getByRole("region", { name: "Сводка по статусам" })
    await expect(summary.getByText("Идут")).toBeVisible()
    await expect(summary.getByText("Закончились")).toBeVisible()
    await expect(summary.getByText("Нет пар")).toBeVisible()
    await expect(summary.getByText("Ожидание")).toBeVisible()

    // Секции сущностей и данные.
    await expect(
      page.getByRole("heading", { name: /Преподаватели/ })
    ).toBeVisible()
    await expect(page.getByRole("heading", { name: /Группы/ })).toBeVisible()
    await expect(page.getByText("Петренко В.Б.")).toBeVisible()
    await expect(page.getByText("ПО262", { exact: true })).toBeVisible()

    // Карточка «Преподаватели на {date}» удалена.
    await expect(page.getByText(/Преподаватели на /)).toHaveCount(0)
  })

  test("/dispatcher/live редиректит на дашборд", async ({ page }) => {
    await page.goto("/dispatcher/live")

    await expect(page).toHaveURL(/\/dispatcher\/dashboard$/)
    await expect(
      page.getByRole("heading", { name: "Текущие пары" })
    ).toBeVisible()
  })
})
