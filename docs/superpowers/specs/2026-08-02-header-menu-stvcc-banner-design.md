# Дизайн: меню хедера + плашка недоступности stvcc.ru

Дата: 2026-08-02
Статус: утверждено пользователем

## Контекст

Контент сайта парсится с WordPress stvcc.ru (новости через импорт в БД, статические страницы через `page-contents.json`, картинки грузятся с stvcc.ru напрямую). Если stvcc.ru недоступен, часть контента не загружается — нужна информационная плашка. Функция временная (только на время разработки).

Также упорядочивается меню хедера:

- «Сведения об ОО» есть в меню «Колледж» → убрать из хедера
- «Сотруднику» → убрать, вход через кнопку «Войти»
- «Достижения» → перенести в меню «Колледж» отдельным пунктом

## 1. Плашка недоступности stvcc.ru (временная функция)

### Backend (CollegeLMS.API)

- `Dtos/StvccHealthDto.cs` — `public record StvccHealthDto(bool Available)`
- `Interfaces/IStvccHealthService.cs` — `Task<Result<StvccHealthDto>> CheckAsync(CancellationToken ct)`
- `Services/StvccHealthService.cs` — primary constructor `(IHttpClientFactory httpClientFactory, IConfiguration config)`:
  - GET на `WordPress:BaseUrl` (appsettings, `http://stvcc.ru`), named client `"stvcc"` с таймаутом 5 с
  - Любой HTTP-ответ (включая 3xx/4xx/5xx) → `Available = true` (сайт жив)
  - `HttpRequestException` / `TaskCanceledException` / таймаут → `Available = false`
  - Всегда `Result.Ok(...)` — эндпоинт никогда не возвращает ошибку
- `Controllers/HealthController.cs` — `[Route("api/health")]`, `GET stvcc`, без `[Authorize]`, Swagger-атрибуты (summary на русском, `SwaggerResponse` 200)
- DI в `ServiceCollectionExtensions.AddApplicationServices`:
  - `services.AddHttpClient("stvcc", c => { c.Timeout = TimeSpan.FromSeconds(5); })`
  - `services.AddScoped<IStvccHealthService, StvccHealthService>();`

### Frontend (CollegeLMS.Next)

- `components/StvccUnavailableBanner.tsx` — клиентский компонент:
  - Проверка при монтировании и каждые 60 с: `api.get("/api/health/stvcc")` → `response.data.data.available`
  - `available === false` → плашка; при восстановлении скрывается автоматически
  - Без кнопки закрытия, некликабельная, `role="status"`, иконка `AlertTriangle`
  - Текст: «Источник данных (stvcc.ru) временно недоступен — часть контента может не загружаться»
  - Ошибка запроса к своему API (500 и т.п.) → плашку не показывать (тихий catch)
  - Комментарий в коде: плашка временная, убрать после перехода с stvcc.ru
- `app/(public)/layout.tsx` — плашка над `<Header />` (только публичный сайт; в ЛК контент stvcc.ru не показывается)

## 2. Меню хедера

Проблема: `SectionPage.tsx` вызывает `notFound()`, если секции нет в `siteNavigation` (`getSectionBySlug`). Страницы `/about/*` и `/achievements/*` используют секции `about` и `achievements` — удалять объекты секций нельзя, иначе 404.

### `data/site-content.ts`

- Интерфейс `Section`: добавить поле `inHeader?: boolean` (по умолчанию `true`)
- Секция «Сведения об ОО» → `inHeader: false`
- Секция «Достижения» → `inHeader: false`
- Секция «Сотруднику» → удалить целиком
- Меню «Колледж»: в конец subsections добавить
  `{ title: "Достижения", slug: "achievements", href: "/achievements", content: "" }`

### `components/Header.tsx`

- Desktop и mobile навигация: фильтр `siteNavigation.filter(s => s.inHeader !== false)`

### Удаление страницы

- Удалить папку `app/(public)/employee/` (страница `/employee` станет 404; других ссылок на неё нет — grep подтвердил)

## 3. Тесты

- `CollegeLMS.Tests/Unit/Services/StvccHealthServiceTests.cs`:
  - Fake `HttpMessageHandler`: 200 OK → `Available = true`
  - Бросок `HttpRequestException` → `Available = false`
  - Таймаут (`TaskCanceledException`) → `Available = false`
  - Без реальной сети

## 4. Git

- Ветка `feature/header-menu-stvcc-banner`, коммиты по фазам: backend → frontend → tests
- Merge в master, push (CD деплой)

## Гейты

- G1: `dotnet build`
- G2: `npm run build`
- G3: `dotnet test`
