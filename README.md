# CollegeLMS

Единая цифровая среда Ставропольского колледжа связи имени Героя Советского Союза В.А. Петрова.

Объединяет публичный сайт колледжа (WP → Next.js) и закрытую LMS (расписание, курсы, тесты, личные кабинеты).

## Стек

- **Backend:** .NET 10, ASP.NET Core Web API
- **Frontend:** Next.js 14, TypeScript, Tailwind CSS 4 + shadcn/ui
- **DB:** PostgreSQL 16
- **Cache:** Redis (сессии)
- **Deploy:** Docker Compose, GitHub Actions CD

## Архитектура

Монолит, Clean Architecture (папки, а не проекты). REST API, JWT auth (без refresh токенов), `Result<T>` везде.

| Проект | Назначение |
|--------|-----------|
| `CollegeLMS.API/` | Web API — контроллеры, сервисы, EF Core |
| `CollegeLMS.Next/` | Next.js 14 — публичный сайт + SPA |
| `CollegeLMS.Tests/` | xUnit — unit + integration тесты |
| `CollegeLMS.TelegramBot/` | Telegram-бот для расписания |

## Быстрый старт

```bash
docker compose --profile telegram-bot up --build -d
```

- Сайт: http://localhost/
- API: http://localhost:5026/
- Swagger: http://localhost:5026/swagger/
- Почта: admin@collegelms.ru / admin

## Структура API

| Сервис | Описание |
|--------|---------|
| SiteService | Публичный сайт, новости, страницы |
| AuthService | Аутентификация и роли |
| ScheduleService | Расписание занятий |
| LearningService | Курсы, лекции, задания, материалы |
| TestingService | Тесты и результаты |
| DashboardService | Личные кабинеты студента и преподавателя |

## CI/CD

- Push в master → GitHub Actions → Deploy на VPS
- Тесты запускаются только локально (`dotnet test`)
