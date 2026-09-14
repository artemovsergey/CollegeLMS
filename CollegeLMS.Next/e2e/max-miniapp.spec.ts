import { test, expect } from "@playwright/test"

const META = {
  isSuccess: true,
  data: {
    // Бэкенд отдаёт DateTime в полном ISO-формате — моки повторяют прод
    semesterStart: "2026-09-01T00:00:00Z",
    totalWeeks: 16,
    currentWeek: 2,
    currentDate: "2026-09-07T00:00:00Z",
  },
  errorMessage: null,
  statusCode: 200,
}

const DAY_ENTRIES = {
  isSuccess: true,
  data: {
    items: [
      {
        id: "s1",
        groupId: "g1",
        groupName: "ПО262",
        teacherId: "t1",
        teacherName: "Петренко В.Б.",
        subject: "Математика",
        room: "301",
        dayOfWeek: 1,
        numberPair: 1,
        startTime: "08:30:00",
        endTime: "10:05:00",
        weeks: [2],
        lessonType: "Лекция",
        changeTags: [],
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 50,
  },
  errorMessage: null,
  statusCode: 200,
}

const WEEK_ENTRIES = {
  ...DAY_ENTRIES,
  data: {
    items: [
      {
        id: "s1",
        groupId: "g1",
        groupName: "ПО262",
        teacherId: "t1",
        teacherName: "Петренко В.Б.",
        subject: "Математика",
        room: "301",
        dayOfWeek: 1,
        numberPair: 1,
        startTime: "08:30:00",
        endTime: "10:05:00",
        weeks: [2],
        lessonType: "Лекция",
        changeTags: [],
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 50,
  },
}

const HISTORY = {
  isSuccess: true,
  data: {
    items: [
      {
        id: "h1",
        changeType: "Replace",
        appliedAt: "2026-09-06T10:00:00Z",
        appliedByUserId: "disp-1",
        groupId: "g1",
        groupName: "ПО262",
        teacherId: null,
        teacherName: null,
        subject: "Физика",
        room: "204",
        dayOfWeek: "Вторник",
        numberPair: 3,
        week: 2,
        note: null,
        removedSubject: null,
        removedRoom: null,
        removedNumberPair: null,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  errorMessage: null,
  statusCode: 200,
}

const SEARCH = {
  isSuccess: true,
  data: {
    groups: [{ id: "g1", name: "ПО262", course: 2 }],
    teachers: [{ id: "t1", fullName: "Петренко В.Б." }],
    totalGroups: 1,
    totalTeachers: 1,
  },
  errorMessage: null,
  statusCode: 200,
}

function inlineJson(body: object) {
  return {
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  }
}

test.describe("MAX mini-app", () => {
  test.beforeEach(async ({ page }) => {
    await page.route("**/api/schedule**", (route) => {
      const url = route.request().url()
      if (url.includes("/meta")) return route.fulfill(inlineJson(META))
      if (url.includes("/search")) return route.fulfill(inlineJson(SEARCH))
      if (url.includes("/history")) return route.fulfill(inlineJson(HISTORY))
      return route.fulfill(inlineJson(DAY_ENTRIES))
    })
  })

  test("Главная открывается без ошибок", async ({ page }) => {
    await page.goto("/max?route=today", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByRole("button", { name: "Сменить просмотр" })).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    await expect(page.getByText("undefined")).toHaveCount(0)
  })

  test("Deep link на дату открывает день расписания", async ({ page }) => {
    await page.goto("/max/schedule?route=day&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    await expect(page.getByText("Расписание")).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
  })

  test("Полный ISO-формат метаданных не ломает даты", async ({ page }) => {
    await page.goto("/max/schedule?route=week&date=2026-09-07T00:00:00Z", {
      waitUntil: "networkidle",
    })

    await expect(page.getByText("Расписание")).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    await expect(page.getByText("undefined")).toHaveCount(0)
  })

  test("Поиск возвращает группы и преподавателей", async ({ page }) => {
    await page.goto("/max/schedule?route=week&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    await page.getByRole("button", { name: "Сменить просмотр" }).click()
    await page.getByRole("searchbox", { name: /поиск/i }).fill("по")

    await expect(page.getByText("ПО262")).toBeVisible()
    await expect(page.getByText("Петренко В.Б.")).toBeVisible()
  })

  test("Изменения открываются по deep link", async ({ page }) => {
    await page.goto("/max/changes", { waitUntil: "networkidle" })

    await expect(page.getByText("Изменения")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
  })
})