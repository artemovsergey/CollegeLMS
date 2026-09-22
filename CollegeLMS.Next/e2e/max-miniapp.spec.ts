import { test, expect, type Page } from "@playwright/test"

const META = {
  isSuccess: true,
  data: {
    // Бэкенд отдаёт DateTime в полном ISO-формате — моки повторяют прод
    semesterStart: "2026-09-01T00:00:00Z",
    totalWeeks: 17,
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

// Ответ POST /api/auth/max/selection повторяет формат /api/auth/max:
// новый MAX-JWT и профиль с выбранной целью (ровно одна из groupId/teacherId).
const GROUP_SELECTION = ok({
  token: "max-jwt-group",
  profile: {
    maxUserId: 1,
    role: "Student",
    groupId: "g1",
    groupName: "ПО262",
    teacherId: null,
    teacherName: null,
  },
})

const TEACHER_SELECTION = ok({
  token: "max-jwt-teacher",
  profile: {
    maxUserId: 1,
    role: "Teacher",
    groupId: null,
    groupName: null,
    teacherId: "t1",
    teacherName: "Петренко В.Б.",
  },
})

function inlineJson(body: object) {
  return {
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  }
}

// Открывает шторку поиска и возвращает её диалог.
async function openSearchSheet(page: Page) {
  await page.getByRole("button", { name: "Поиск" }).click()
  return page.getByRole("dialog", { name: "Поиск" })
}

// Вход через MAX Bridge: профиль бота — источник правды о сохранённом выборе,
// поэтому бейдж «Текущий» и строка «Просмотр без сохранения» зависят от него,
// а не от localStorage max-view-context.
async function loginMaxProfile(
  page: Page,
  profile: Record<string, unknown>,
  token = "max-jwt",
) {
  await page.addInitScript(() => {
    ;(window as unknown as { WebApp?: unknown }).WebApp = {
      initData: "auth_date=1&user=%7B%22id%22%3A1%7D&hash=stub",
      initDataUnsafe: {},
    }
  })
  await page.route("**/api/auth/max", (route) =>
    route.fulfill(inlineJson(ok({ token, profile }))),
  )
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
    // Подпись «События» над вставками удалена — остаётся только сама строка.
    await expect(page.getByText("События")).toHaveCount(0)
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
    // Профиль бота отдаёт текущую группу g1: именно он, а не localStorage,
    // определяет бейдж «Текущий» в поиске.
    await loginMaxProfile(page, {
      maxUserId: 1,
      role: "Student",
      groupId: "g1",
      groupName: "ПО262",
      teacherId: null,
      teacherName: null,
    })

    await page.goto("/max/schedule?route=week&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    await page.getByRole("button", { name: "Поиск" }).click()

    const sheet = page.getByRole("dialog", { name: "Поиск" })
    await sheet.getByRole("searchbox").fill("по")

    await expect(sheet.getByText("ПО262")).toBeVisible()
    await expect(sheet.getByText("Петренко В.Б.")).toBeVisible()

    // Кнопка выбора в поиске — «Выбрать», а у текущей группы (из профиля
    // бота) показан бейдж «Текущий». Старой кнопки «Открыть» больше нет.
    const groupRow = sheet
      .locator(".max-app__search-item")
      .filter({ hasText: "ПО262" })
    await expect(groupRow.getByText("Текущий")).toBeVisible()
    await expect(groupRow.getByRole("button", { name: "Выбрать" })).toBeVisible()
    await expect(
      sheet.getByRole("button", { name: "Открыть", exact: true })
    ).toHaveCount(0)
  })

  test("Изменения: карточка как в веб-версии и deep link", async ({ page }) => {
    await page.goto("/max/changes", { waitUntil: "networkidle" })

    await expect(page.getByText("Изменения")).toBeVisible()

    // Фильтр по неделям удалён — вместо него компактная кнопка-календарь
    // с aria-label «Выбрать дату».
    await expect(page.locator("#max-week-filter")).toHaveCount(0)
    await expect(page.getByRole("button", { name: "Выбрать дату" })).toBeVisible()

    const card = page.locator(".max-app__change-card")
    await expect(card).toHaveCount(1)
    // Тип, предмет (старый зачёркнут, новый показан) и пара «2 → 3».
    await expect(card.getByText("Замена")).toBeVisible()
    await expect(card.locator(".max-app__badge--replace")).toHaveText(/Замена/)
    // Компактные бейджи без суффикса «· нед. N».
    await expect(page.getByText(/нед\./)).toHaveCount(0)
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

  test("Изменения: кнопка-календарь фильтрует по дате и сбрасывается", async ({
    page,
  }) => {
    await page.goto("/max/changes", { waitUntil: "networkidle" })

    await expect(page.getByRole("button", { name: "Выбрать дату" })).toBeVisible()
    // Нативный input визуально скрыт, но дата применяется через него.
    await page.locator(".max-app__date-input").fill("2026-09-07")

    await expect(page.locator(".max-app__chip--on")).toContainText("07.09")
    const reset = page.getByRole("button", { name: "Сбросить дату" })
    await expect(reset).toBeVisible()

    await reset.click()
    await expect(page.locator(".max-app__chip--on")).toHaveCount(0)
    await expect(page.getByRole("button", { name: "Сбросить дату" })).toHaveCount(0)
  })

  test("Расписание: компактные бейджи изменений без суффикса недели", async ({
    page,
  }) => {
    const badgeEntry = {
      ...ENTRY,
      changeTags: [
        {
          changeType: "Add",
          week: 2,
          removedNumberPair: null,
          removedSubject: null,
          note: "сам.р.",
        },
        {
          changeType: "Move",
          week: 2,
          removedNumberPair: 1,
          removedSubject: null,
          note: null,
        },
      ],
    }
    await page.route(
      (url) =>
        url.pathname === "/api/schedule" && url.searchParams.get("view") === "day",
      (route) =>
        route.fulfill(inlineJson(ok(dayView({ entries: [badgeEntry] })))),
    )

    await page.goto("/max/schedule?route=day&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    // Компактные подписи: тип + отдельный бейдж «Сам.р.».
    await expect(page.getByText("Добавлено")).toBeVisible()
    await expect(page.getByText("Сам.р.")).toBeVisible()
    await expect(page.getByText("Перенос")).toBeVisible()
    await expect(page.getByText(/нед\./)).toHaveCount(0)
  })

  test("Поиск: «Выбрать» сохраняет группу в боте и делает её контекстом", async ({
    page,
  }) => {
    // Стартуем без получателя — тогда выбор должен стать контекстом.
    await page.addInitScript(() => {
      localStorage.setItem("max-view-context", JSON.stringify({}))
    })
    let body: unknown = null
    await page.route("**/api/auth/max/selection", (route) => {
      body = route.request().postDataJSON()
      return route.fulfill(inlineJson(GROUP_SELECTION))
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })
    await expect(page.getByText("Выберите расписание")).toBeVisible()

    const sheet = await openSearchSheet(page)
    await sheet.getByRole("searchbox").fill("по")
    const groupRow = sheet
      .locator(".max-app__search-item")
      .filter({ hasText: "ПО262" })
    await groupRow.getByRole("button", { name: "Выбрать" }).click()

    // Шторка закрылась, выбранная группа стала контекстом расписания.
    await expect(sheet).toHaveCount(0)
    await expect(page.locator(".max-schedule__context")).toContainText("ПО262")
    // В теле ровно одна цель — без имён и второго id.
    expect(body).toEqual({ groupId: "g1" })
    await expect
      .poll(() => page.evaluate(() => localStorage.getItem("max-token")))
      .toBe("max-jwt-group")
  })

  test("Поиск: «Выбрать» сохраняет преподавателя и делает его контекстом", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      localStorage.setItem("max-view-context", JSON.stringify({}))
    })
    let body: unknown = null
    await page.route("**/api/auth/max/selection", (route) => {
      body = route.request().postDataJSON()
      return route.fulfill(inlineJson(TEACHER_SELECTION))
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })
    await expect(page.getByText("Выберите расписание")).toBeVisible()

    const sheet = await openSearchSheet(page)
    await sheet.getByRole("searchbox").fill("пе")
    const teacherRow = sheet
      .locator(".max-app__search-item")
      .filter({ hasText: "Петренко В.Б." })
    await teacherRow.getByRole("button", { name: "Выбрать" }).click()

    await expect(sheet).toHaveCount(0)
    await expect(page.locator(".max-schedule__context")).toContainText(
      "Петренко В.Б.",
    )
    expect(body).toEqual({ teacherId: "t1" })
    await expect
      .poll(() => page.evaluate(() => localStorage.getItem("max-token")))
      .toBe("max-jwt-teacher")
  })

  test("Расписание: несохранённый просмотр предлагает «Сделать текущим»", async ({
    page,
  }) => {
    // Локально выбранная группа ещё не сохранена в боте (профиль пуст),
    // поэтому расписание показывает строку «Просмотр без сохранения».
    let body: unknown = null
    await page.route("**/api/auth/max/selection", (route) => {
      body = route.request().postDataJSON()
      return route.fulfill(inlineJson(GROUP_SELECTION))
    })

    await page.goto("/max/schedule?route=day&date=2026-09-07", {
      waitUntil: "networkidle",
    })

    const notice = page.locator(".max-schedule__notice")
    await expect(notice).toContainText("Просмотр без сохранения")
    await notice.getByRole("button", { name: "Сделать текущим" }).click()

    expect(body).toEqual({ groupId: "g1" })
    // Выбор сохранён — он стал текущим, и строка исчезает.
    await expect(page.locator(".max-schedule__notice")).toHaveCount(0)
  })

  test("Расписание без выбора показывает подсказку о сохранении в боте", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      localStorage.setItem("max-view-context", JSON.stringify({}))
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.getByText("Выберите расписание")).toBeVisible()
    await expect(page.getByText(/Выбор ещё не задан\./)).toBeVisible()
    await expect(
      page.getByText(/выбор сохранится в боте, и уведомления начнут приходить\./),
    ).toBeVisible()
  })

  test("Избранное: у текущего выбора бейдж, у остальных — просмотр и сохранение", async ({
    page,
  }) => {
    // Профиль бота: текущая группа g1. В избранном — другая группа и преподаватель.
    await loginMaxProfile(page, {
      maxUserId: 1,
      role: "Student",
      groupId: "g1",
      groupName: "ПО262",
      teacherId: null,
      teacherName: null,
    })
    await page.addInitScript(() => {
      localStorage.setItem(
        "max-favorites",
        JSON.stringify([
          { id: "Group:g2", targetType: "Group", targetId: "g2", name: "ПО101" },
          {
            id: "Teacher:t1",
            targetType: "Teacher",
            targetId: "t1",
            name: "Петренко В.Б.",
          },
        ]),
      )
    })

    await page.goto("/max/favorites", { waitUntil: "networkidle" })

    // Текущая группа из профиля: подпись «Текущий выбор» и бейдж «Текущий».
    await expect(page.getByText("Текущий выбор")).toBeVisible()
    await expect(page.getByText("Текущий", { exact: true })).toHaveCount(1)

    // У каждой строки есть просмотр без сохранения (Eye); у двух нетекущих —
    // ещё и «Сделать текущим» (Target). Старой кнопки «Открыть» нет.
    await expect(
      page.getByRole("button", { name: "Открыть расписание без сохранения" }),
    ).toHaveCount(3)
    await expect(
      page.getByRole("button", { name: "Сделать текущим" }),
    ).toHaveCount(2)
    await expect(
      page.getByRole("button", { name: "Открыть", exact: true })
    ).toHaveCount(0)
  })

  test("Избранное: просмотр не сохраняет выбор, а «Сделать текущим» — сохраняет", async ({
    page,
  }) => {
    await loginMaxProfile(page, {
      maxUserId: 1,
      role: "Student",
      groupId: "g1",
      groupName: "ПО262",
      teacherId: null,
      teacherName: null,
    })
    await page.addInitScript(() => {
      localStorage.setItem(
        "max-favorites",
        JSON.stringify([
          {
            id: "Teacher:t1",
            targetType: "Teacher",
            targetId: "t1",
            name: "Петренко В.Б.",
          },
        ]),
      )
    })

    let selectionCalls = 0
    let body: unknown = null
    await page.route("**/api/auth/max/selection", (route) => {
      selectionCalls += 1
      body = route.request().postDataJSON()
      return route.fulfill(inlineJson(TEACHER_SELECTION))
    })

    await page.goto("/max/favorites", { waitUntil: "networkidle" })

    // Просмотр (Eye) подменяет локальный контекст и открывает расписание,
    // но в боте ничего не сохраняет.
    await page
      .getByRole("button", { name: "Открыть расписание без сохранения" })
      .first()
      .click()
    await expect(page).toHaveURL(/\/max\/schedule/)
    await expect(page.locator(".max-schedule__context")).toContainText(
      "Петренко В.Б.",
    )
    await expect(page.locator(".max-schedule__notice")).toContainText(
      "Просмотр без сохранения",
    )
    expect(selectionCalls).toBe(0)
  })

  test("Избранное: «Сделать текущим» сохраняет выбор в боте", async ({ page }) => {
    await loginMaxProfile(page, {
      maxUserId: 1,
      role: "Student",
      groupId: "g1",
      groupName: "ПО262",
      teacherId: null,
      teacherName: null,
    })
    await page.addInitScript(() => {
      localStorage.setItem(
        "max-favorites",
        JSON.stringify([
          {
            id: "Teacher:t1",
            targetType: "Teacher",
            targetId: "t1",
            name: "Петренко В.Б.",
          },
        ]),
      )
    })

    let body: unknown = null
    await page.route("**/api/auth/max/selection", (route) => {
      body = route.request().postDataJSON()
      return route.fulfill(inlineJson(TEACHER_SELECTION))
    })

    await page.goto("/max/favorites", { waitUntil: "networkidle" })

    await page.getByRole("button", { name: "Сделать текущим" }).first().click()

    expect(body).toEqual({ teacherId: "t1" })
    // Преподаватель стал текущим: бейдж один, кнопок сохранения не осталось.
    await expect(page.getByText("Текущий", { exact: true })).toHaveCount(1)
    await expect(
      page.getByRole("button", { name: "Сделать текущим" }),
    ).toHaveCount(0)
  })

  test("MAX initData: гость входит и видит расписание", async ({ page }) => {
    // Счётчик обращений к моку: без реального обмена initData запрос не состоится.
    let authRequests = 0
    const MAX_TOKEN = "max-jwt"
    const MAX_INIT_DATA = "auth_date=1&user=%7B%22id%22%3A1%7D&hash=stub"
    await page.addInitScript((initData) => {
      // Бридж MAX при загрузке перезаписывает window.WebApp и берёт initData
      // из sessionStorage.WebAppData — дублируем туда, чтобы вход не зависел
      // от порядка загрузки бриджа.
      sessionStorage.setItem("WebAppData", initData)
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData,
        initDataUnsafe: {},
      }
    }, MAX_INIT_DATA)
    await page.route("**/api/auth/max", (route) => {
      authRequests += 1
      return route.fulfill(
        inlineJson(
          ok({
            token: MAX_TOKEN,
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
      )
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    await expect(page.getByText("Расписание")).toBeVisible()

    // Анонимный /max рендерит тот же таббар, поэтому проверяем сам факт входа:
    // обмен initData состоялся и выданный токен сохранён в отдельном ключе.
    await expect
      .poll(() => page.evaluate(() => localStorage.getItem("max-token")), {
        message: "токен, выданный /api/auth/max, должен быть сохранён в max-token",
      })
      .toBe(MAX_TOKEN)
    // В dev React Strict Mode монтирует провайдер дважды (поэтому и запросы
    // /api/schedule дублируются), так что эффект reload() вызывается дважды —
    // проверяем, что обмен initData действительно состоялся.
    expect(authRequests).toBeGreaterThanOrEqual(1)
  })

  test("MAX initData: смена выбора в боте перекрывает устаревший локальный", async ({
    page,
  }) => {
    // Локально сохранён старый выбор (группа g9), а бот при входе отдаёт
    // актуальный (g1): профиль бота — источник правды, выбор могли поменять
    // в чате, пока мини-апп был закрыт.
    await page.addInitScript(() => {
      localStorage.setItem(
        "max-view-context",
        JSON.stringify({ groupId: "g9", groupName: "ПО000" }),
      )
      ;(window as unknown as { WebApp?: unknown }).WebApp = {
        initData: "auth_date=1&user=%7B%22id%22%3A4%7D&hash=stub",
        initDataUnsafe: {},
      }
    })
    await page.route("**/api/auth/max", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            token: "max-jwt-fresh",
            profile: {
              maxUserId: 4,
              fullName: "Студент",
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

    await expect(page.locator(".max-schedule__context")).toContainText("ПО262")
    await expect(page.locator(".max-schedule__context")).not.toContainText("ПО000")
    // Свежий выбор перезаписан и в localStorage: остальные вкладки читают его.
    await expect
      .poll(() => page.evaluate(() => localStorage.getItem("max-view-context")), {
        message: "устаревший локальный выбор должен быть заменён выбором из бота",
      })
      .toContain("g1")
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

  test("Журнал: в шапке нет заголовка, только ФИО и число пар", async ({ page }) => {
    await loginMaxProfile(page, {
      maxUserId: 2,
      role: "Teacher",
      groupId: null,
      groupName: null,
      teacherId: "t1",
      teacherName: "Петренко В.Б.",
    })
    await page.route("**/api/schedule/journal**", (route) =>
      route.fulfill(
        inlineJson(
          ok({
            teacherId: "t1",
            teacherName: "Петренко В.Б.",
            subjects: [],
            totalPairCount: 12,
          }),
        ),
      ),
    )

    await page.goto("/max/journal", { waitUntil: "networkidle" })

    // Заголовок «Журнал» из шапки убран, остаётся строка «ФИО · всего пар: N».
    await expect(page.getByRole("heading", { name: "Журнал" })).toHaveCount(0)
    await expect(page.getByText(/Петренко В\.Б\. · всего пар: 12/)).toBeVisible()
  })

  test("MAX initData: поздняя загрузка бриджа всё равно логинит", async ({ page }) => {
    let authRequests = 0
    const MAX_TOKEN = "max-jwt-late"
    const MAX_INIT_DATA = "auth_date=1&user=%7B%22id%22%3A3%7D&hash=stub"

    // Бридж появляется через 400 мс после старта документа — уже после первого
    // рендера провайдера, но в пределах окна ожидания (1500 мс).
    await page.addInitScript((initData) => {
      setTimeout(() => {
        ;(window as unknown as { WebApp?: unknown }).WebApp = {
          initData,
          initDataUnsafe: {},
        }
      }, 400)
    }, MAX_INIT_DATA)

    await page.route("**/api/auth/max", (route) => {
      authRequests += 1
      return route.fulfill(
        inlineJson(
          ok({
            token: MAX_TOKEN,
            profile: {
              maxUserId: 3,
              fullName: "Поздний гость",
              role: "Student",
              groupId: "g1",
              groupName: "ПО262",
              teacherId: null,
              teacherName: null,
            },
          }),
        ),
      )
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })

    await expect(page.locator(".max-app__tabbar")).toBeVisible()
    // Вход состоялся, несмотря на позднюю загрузку MAX Bridge.
    await expect
      .poll(() => page.evaluate(() => localStorage.getItem("max-token")), {
        message: "вход должен состояться после поздней загрузки бриджа",
      })
      .toBe(MAX_TOKEN)
    expect(authRequests).toBeGreaterThanOrEqual(1)
  })

  test("MAX initData: без моста /api/auth/max не вызывается", async ({ page }) => {
    let authRequests = 0
    await page.route("**/api/auth/max", (route) => {
      authRequests += 1
      return route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ isSuccess: false, errorMessage: "no bridge", statusCode: 401 }),
      })
    })

    await page.goto("/max/schedule", { waitUntil: "networkidle" })
    await expect(page.locator(".max-app__tabbar")).toBeVisible()

    // Ждём дольше окна ожидания бриджа (1500 мс): позднего запроса быть не должно.
    await page.waitForTimeout(1800)
    expect(authRequests).toBe(0)
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
