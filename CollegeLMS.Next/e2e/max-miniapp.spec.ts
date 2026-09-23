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

// Контекст зрителя: без выбранной группы ScheduleView показывает приглашение
// «Выберите расписание» и не рендерит ленту расписания.
const VIEW_CONTEXT = { groupId: "g1", groupName: "ПО262" }

const ENTRY = {
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
}

const INSERT = {
  id: "i1",
  title: "Кураторский час",
  dayOfWeek: 1,
  startTime: "12:20:00",
  endTime: "13:00:00",
  course: null,
  isActive: true,
}

const PRACTICE = {
  id: "p1",
  kind: "Pp",
  name: "ПП 09",
  groupId: "g1",
  groupName: "ПО262",
  teachers: [{ id: "t1", name: "Петренко В.Б." }],
  dateFrom: "2026-09-11T00:00:00Z",
  dateTo: "2026-09-11T00:00:00Z",
  days: [],
  note: null,
}

function ok(data: unknown) {
  return { isSuccess: true, data, errorMessage: null, statusCode: 200 }
}

// День по контракту view=day (ScheduleDayView).
function dayView(overrides: Record<string, unknown> = {}) {
  return {
    date: "2026-09-07T00:00:00Z",
    week: 2,
    dayOfWeek: 1,
    isSunday: false,
    isNonWorking: false,
    nonWorkingTitle: null,
    practices: [],
    inserts: [],
    entries: [],
    ...overrides,
  }
}

const DAY_VIEW = ok(dayView({ entries: [ENTRY], inserts: [INSERT] }))

// Неделя по контракту view=week (ScheduleWeekView): 6 дней Пн–Сб со слоями.
const WEEK_VIEW = ok({
  week: 2,
  weekStart: "2026-09-07T00:00:00Z",
  days: [
    dayView({
      date: "2026-09-07T00:00:00Z",
      dayOfWeek: 1,
      entries: [ENTRY],
      inserts: [INSERT],
    }),
    dayView({ date: "2026-09-08T00:00:00Z", dayOfWeek: 2 }),
    dayView({
      date: "2026-09-09T00:00:00Z",
      dayOfWeek: 3,
      isNonWorking: true,
      nonWorkingTitle: "День учителя",
    }),
    dayView({ date: "2026-09-10T00:00:00Z", dayOfWeek: 4 }),
    dayView({
      date: "2026-09-11T00:00:00Z",
      dayOfWeek: 5,
      practices: [PRACTICE],
    }),
    dayView({ date: "2026-09-12T00:00:00Z", dayOfWeek: 6 }),
  ],
})

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
        removedSubject: "Математика",
        removedRoom: null,
        removedNumberPair: 2,
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
    await page.addInitScript((ctx) => {
      localStorage.setItem("max-view-context", JSON.stringify(ctx))
    }, VIEW_CONTEXT)

    await page.route("**/api/schedule**", (route) => {
      const url = route.request().url()
      if (url.includes("/meta")) return route.fulfill(inlineJson(META))
      if (url.includes("/search")) return route.fulfill(inlineJson(SEARCH))
      if (url.includes("/history")) return route.fulfill(inlineJson(HISTORY))
      // Мок журнала обязан идти раньше общих моков дня/недели, иначе
      // /api/schedule/journal перехватывается как расписание.
      if (url.includes("/journal"))
        return route.fulfill(inlineJson(ok({ subjects: [], entries: [] })))
      if (url.includes("view=week")) return route.fulfill(inlineJson(WEEK_VIEW))
      return route.fulfill(inlineJson(DAY_VIEW))
    })
  })

  test("Главная открывается без ошибок", async ({ page }) => {
    await page.goto("/max?route=today", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByRole("button", { name: "Поиск" })).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    await expect(page.getByText("undefined")).toHaveCount(0)
  })

  test("Вкладки мини-аппа без диспетчера", async ({ page }) => {
    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    const tabbar = page.locator(".max-app__tabbar")
    await expect(tabbar.getByText("Расписание")).toBeVisible()
    await expect(tabbar.getByText("Избранное")).toBeVisible()
    await expect(tabbar.getByText("Изменения")).toBeVisible()
    await expect(tabbar.getByText("Диспетчер")).toHaveCount(0)
  })

  test("Deep link на дату открывает день расписания", async ({ page }) => {
    await page.goto("/max/schedule?route=day&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    await expect(page.getByText("Расписание")).toBeVisible()
    await expect(page.getByText("Математика")).toBeVisible()
    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    // Слои дня: вставка отображается строкой «HH:mm–HH:mm Название».
    await expect(page.getByText("События")).toBeVisible()
    await expect(page.getByText("Кураторский час")).toBeVisible()
    await expect(page.getByText("12:20–13:00")).toBeVisible()
  })

  test("Полный ISO-формат метаданных не ломает даты", async ({ page }) => {
    await page.goto("/max/schedule?route=week&date=2026-09-07T00:00:00Z", {
      waitUntil: "networkidle",
    })

    await expect(page.getByText("Расписание")).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    await expect(page.getByText("undefined")).toHaveCount(0)
    // Диапазон недели — Пн–Сб (6 дней), как в ленте.
    await expect(page.getByText("07.09 – 12.09")).toBeVisible()
    // Слои недели: нерабочий день и практика-бейдж.
    await expect(page.getByText("День учителя")).toBeVisible()
    await expect(page.getByText("ПП", { exact: true })).toBeVisible()
  })

  test("Поиск возвращает группы и преподавателей", async ({ page }) => {
    await page.goto("/max/schedule?route=week&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    await page.getByRole("button", { name: "Поиск" }).click()

    const sheet = page.getByRole("dialog", { name: "Поиск" })
    await sheet.getByRole("searchbox").fill("по")

    await expect(sheet.getByText("ПО262")).toBeVisible()
    await expect(sheet.getByText("Петренко В.Б.")).toBeVisible()
  })

  test("Изменения: карточка как в веб-версии и deep link", async ({ page }) => {
    await page.goto("/max/changes", { waitUntil: "networkidle" })

    await expect(page.getByText("Изменения")).toBeVisible()

    const card = page.locator(".max-app__change-card")
    await expect(card).toHaveCount(1)
    // Тип, предмет (старый зачёркнут, новый показан) и пара «2 → 3».
    await expect(card.getByText("Замена")).toBeVisible()
    await expect(card.getByText("Математика")).toBeVisible()
    await expect(card.getByText("Физика")).toBeVisible()
    await expect(card.getByText("пара 2 → 3")).toBeVisible()
    // Группа, аудитория, дата занятия и неделя.
    await expect(card.getByText("ПО262")).toBeVisible()
    await expect(card.getByText("ауд. 204")).toBeVisible()
    await expect(card.getByText("Вторник, 2-я неделя")).toBeVisible()
    await expect(card.getByText("сентября 2026")).toBeVisible()
    // Преподаватель не указан, примечания нет, есть время применения.
    await expect(card.getByText("Преподаватель не указан")).toBeVisible()
    await expect(card.getByText("Применено:")).toBeVisible()
    await expect(page.getByText("Invalid Date")).toHaveCount(0)
    await expect(page.getByText("undefined")).toHaveCount(0)
  })

  test("MAX initData: гость входит и видит расписание", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "auth_date=1&user=%7B%22id%22%3A1%7D&hash=stub",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            token: "max-jwt",
            profile: {
              maxUserId: 1,
              fullName: "Гость",
              role: "Student",
              groupId: "g1",
              groupName: "ПО262",
              teacherId: null,
              teacherName: null,
            },
          }),
        ),
      ),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByText("Расписание")).toBeVisible()
  })

  test("MAX initData: роль Teacher показывает вкладку Журнал", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "auth_date=1&user=%7B%22id%22%3A2%7D&hash=stub",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            token: "max-jwt",
            profile: {
              maxUserId: 2,
              fullName: "Преподаватель",
              role: "Teacher",
              groupId: null,
              groupName: null,
              teacherId: "t1",
              teacherName: "Петренко В.Б.",
            },
          }),
        ),
      ),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar").getByText("Журнал")).toBeVisible()
  })

  test("MAX initData: ошибка входа не выкидывает на /login", async ({ page }) => {
    await page.addInitScript(() => {
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "bad",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          errorMessage: "bad",
          statusCode: 401,
        }),
      }),
    )

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page).toHaveURL(/\/max\//)
  })
})
