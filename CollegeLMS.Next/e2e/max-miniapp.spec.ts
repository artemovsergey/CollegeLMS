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

// Ответ импорта XLSX (CorrectionImportResponse): одна валидная строка.
const IMPORT_RESULT = ok({
  batchId: "b1",
  correctionDate: "2026-09-07",
  week: 2,
  dayOfWeek: 1,
  totalEntries: 1,
  positions: [
    {
      id: "p1",
      row: 1,
      changeType: "Add",
      groupId: "g1",
      groupName: "ПО262",
      dayOfWeek: 1,
      week: 2,
      numberPair: 1,
      subject: "Математика",
      teacherId: null,
      teacherName: null,
      removedSubject: null,
      removedTeacherId: null,
      removedTeacherName: null,
      removedNumberPair: null,
      note: null,
      status: "Draft",
      historyId: null,
      errors: [],
    },
  ],
  errors: [],
})

const APPLY_RESULT = ok({ applied: 1, batchId: "b1", history: [] })

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

  test("Изменения открываются по deep link", async ({ page }) => {
    await page.goto("/max/changes", { waitUntil: "networkidle" })

    await expect(page.getByText("Изменения")).toBeVisible()
    await expect(page.getByText("Физика")).toBeVisible()
  })

  test("Диспетчер без токена показывает гейт", async ({ page }) => {
    await page.goto("/max/dispatcher", { waitUntil: "networkidle" })

    await expect(page.getByText("Доступ диспетчера")).toBeVisible()
    await expect(page.getByRole("button", { name: "Войти" })).toBeVisible()
    await expect(page.getByRole("tab", { name: "Файл XLSX" })).toHaveCount(0)
  })

  test("Диспетчер с token показывает режимы корректировки", async ({ page }) => {
    await page.addInitScript(() => {
      sessionStorage.setItem("dispatcherToken", "test")
    })
    await page.goto("/max/dispatcher", { waitUntil: "networkidle" })

    await expect(page.getByRole("tab", { name: "Файл XLSX" })).toBeVisible()
    await expect(page.getByRole("tab", { name: "Вручную" })).toBeVisible()
    await expect(page.getByText("Доступ диспетчера")).toHaveCount(0)
  })

  test("Импорт: шторка подтверждения и автоскачивание XLSX", async ({ page }) => {
    await page.addInitScript(() => {
      sessionStorage.setItem("dispatcherToken", "test")
    })
    await page.route("**/api/schedule/correction/batches/import", (route) =>
      route.fulfill(inlineJson(IMPORT_RESULT)),
    )
    await page.route("**/api/schedule/correction/batches/b1/apply", (route) =>
      route.fulfill(inlineJson(APPLY_RESULT)),
    )
    await page.route("**/api/schedule/correction/batches/b1/export", (route) =>
      route.fulfill({
        status: 200,
        contentType:
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        headers: { "Content-Disposition": 'attachment; filename="correction.xlsx"' },
        body: Buffer.from("PK"),
      }),
    )

    const downloadPromise = page.waitForEvent("download")
    await page.goto("/max/dispatcher", { waitUntil: "networkidle" })
    await page.locator('input[type="file"]').setInputFiles({
      name: "corrections.xlsx",
      mimeType:
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      buffer: Buffer.from("test"),
    })
    await expect(page.getByText("Математика")).toBeVisible()

    await page.getByRole("button", { name: "Применить изменения" }).click()
    const dialog = page.getByRole("dialog", {
      name: "Подтверждение применения изменений",
    })
    await expect(dialog).toBeVisible()
    await expect(dialog.getByText("Применить 1 изменение?")).toBeVisible()

    await dialog.getByRole("button", { name: "Отмена" }).click()
    await expect(dialog).toHaveCount(0)

    await page.getByRole("button", { name: "Применить изменения" }).click()
    await dialog
      .getByRole("button", { name: "Применить", exact: true })
      .click()

    await expect(page.getByText("Изменения применены")).toBeVisible()
    await downloadPromise
  })

  test("Вручную: перенос уходит без note — «вм.X» заполняет сервер", async ({ page }) => {
    await page.addInitScript(() => {
      sessionStorage.setItem("dispatcherToken", "test")
    })

    let positionBody: Record<string, unknown> | null = null
    await page.route("**/api/schedule**", (route) => {
      const url = route.request().url()
      if (url.includes("/meta")) return route.fulfill(inlineJson(META))
      if (url.includes("/search")) return route.fulfill(inlineJson(SEARCH))
      return route.fulfill(
        inlineJson(ok({ items: [ENTRY], totalCount: 1, page: 1, pageSize: 100 })),
      )
    })
    await page.route("**/api/schedule/correction/batches", (route) =>
      route.fulfill(inlineJson(ok({ id: "b-manual", status: "Draft" }))),
    )
    await page.route(
      "**/api/schedule/correction/batches/b-manual/positions",
      (route) => {
        positionBody = JSON.parse(route.request().postData() ?? "{}")
        return route.fulfill(inlineJson(ok({ id: "p1" })))
      },
    )
    await page.route("**/api/schedule/correction/batches/b-manual/apply", (route) =>
      route.fulfill(inlineJson(APPLY_RESULT)),
    )

    await page.goto("/max/dispatcher", { waitUntil: "networkidle" })
    await page.getByRole("tab", { name: "Вручную" }).click()

    await page
      .getByPlaceholder("Начните вводить название группы")
      .fill("ПО262")
    await page.getByRole("button", { name: "ПО262" }).click()

    await page.getByRole("tab", { name: "Перенести" }).click()
    await page.locator('input[name="removed"]').first().check()
    await page.getByLabel("На пару").selectOption("3")

    await page.getByRole("button", { name: "Добавить операцию" }).click()
    await page.getByRole("button", { name: "Применить изменения" }).click()

    const sheet = page.getByRole("dialog", { name: "Подтверждение корректировки" })
    await expect(sheet.getByText("1 → 3")).toBeVisible()
    await sheet.getByRole("button", { name: "Применить изменения" }).click()

    await expect(page.getByText("Изменения применены")).toBeVisible()
    expect(positionBody).not.toBeNull()
    expect((positionBody as unknown as Record<string, unknown>).note).toBeNull()
    expect(JSON.stringify(positionBody)).not.toContain("вм.")
  })

  test("Истёкший dispatcher-токен возвращает к гейту", async ({ page }) => {
    await page.addInitScript(() => {
      sessionStorage.setItem("dispatcherToken", "expired")
    })
    await page.route("**/api/schedule/correction/batches/import", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          isSuccess: false,
          data: null,
          errorMessage: "Не авторизован",
          statusCode: 401,
        }),
      }),
    )

    await page.goto("/max/dispatcher", { waitUntil: "networkidle" })
    await page.locator('input[type="file"]').setInputFiles({
      name: "corrections.xlsx",
      mimeType:
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      buffer: Buffer.from("test"),
    })

    // 401 не редиректит на /login, а возвращает к форме пароля.
    await expect(page.getByText("Доступ диспетчера")).toBeVisible()
    await expect(page).toHaveURL(/\/max\/dispatcher/)
    await expect(page.getByRole("tab", { name: "Файл XLSX" })).toHaveCount(0)
  })
})
