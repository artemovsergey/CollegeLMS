# DeepSeek Skills

Скиллы для разработки на основе анализа репозиториев `artemovsergey/*`.
Стек: .NET API, React, Next.js, Deploy (Docker/VPS).

> **Контекст CollegeLMS:** проект использует .NET 10, Next.js 14, PostgreSQL 16, Redis (сессии), Tailwind CSS 4, Docker Compose, GitHub Actions CI/CD.

---

## 1. .NET API (`dotnet-api`)

### Область
REST API на ASP.NET Core с PostgreSQL, JWT, Clean Architecture.

### Целевая версия
- **.NET 10** (актуальная, CollegeLMS), .NET 9, .NET 8 (легаси)
- Multi-targeting (`net9.0` + `net7.0`) применимо только для class library проектов (не для Web API)

### Архитектура и структура проекта

```
Solution.sln
├── {App}.API/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Production.json
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Endpoints/          # Minimal API
│   │   └── GameEndpoints.cs
│   ├── Controllers/        # Контроллеры
│   │   └── UsersController.cs
│   ├── Models/
│   ├── Interfaces/
│   ├── Repositories/
│   ├── Middleware/
│   │   └── ExceptionHandlerMiddleware.cs
│   └── Extensions/
│       └── ServiceRegistration.cs
├── {App}.Tests/
│   ├── UnitTests/
│   └── IntegrationTests/
├── docker-compose.yml
└── .github/workflows/
```

**Подход**: монолит, Clean Architecture (влияние, без строгого разделения Domain/Application/Infrastructure). Папки вместо проектов.

### API стиль
- **Minimal API** (рекомендуемый): эндпоинты в `/Endpoints`, регистрация через `app.UseXxxEndpoints()`
- **Controllers** (альтернатива): наследование от `ControllerBase`, группировка CRUD
- Пример (`TicTacToeApp.API/Endpoints/GameEndpoints.cs`):
```csharp
public static class GameEndpoints
{
    public static void UseGameEndpoints(this WebApplication app)
    {
        app.MapPost("/api/games/{id:int}/move", async (
            int id, MoveRequest request,
            IGameAsyncRepository repository,
            CancellationToken ct) =>
        {
            // ...
        });
    }
}
```

### База данных
| Компонент | Библиотека |
|---|---|
| ORM | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Миграции | `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools` |
| Healthcheck | `AspNetCore.HealthChecks.NpgSql` |

Настройка в `Program.cs`:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
```

Connection string в Docker: `Host=db;Port=5432;Database=AppDb;Username=postgres;Password=root`

### Аутентификация
- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **BCrypt** для хеширования паролей (`BCrypt.Net-Next`)
- Настройка в `appsettings.json`: `"tokenKey": "<256-bit string>"`
- Validation: `ValidateIssuerSigningKey = true`, `ValidateIssuer = false`, `ValidateAudience = false`
- Swagger: `AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" })`

### Обработка ошибок (Result паттерн)
Глобальный middleware, без try-catch в эндпоинтах:
```csharp
public class ExceptionHandlerMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (ArgumentException ex) { context.Response.StatusCode = 400; /* ... */ }
        catch (Exception ex) { context.Response.StatusCode = 500; /* ... */ }
    }
}
```
Модель ответа: `Result<T>` или `ApiResult<T>` с поддержкой пагинации, поиска, сортировки и фильтрации (`System.Linq.Dynamic.Core`).

### Валидация
- **FluentValidation** (`FluentValidation.DependencyInjectionExtensions`)
- Регистрация: `builder.Services.AddValidatorsFromAssemblyContaining<T>()`

### Документация API
- **Swashbuckle.AspNetCore** + Annotations + Filters
- Каждый эндпоинт: `[SwaggerOperation(Summary = "...")]` (русский язык)
- JSON: `SnakeCaseLower` naming policy

### Тестирование
- **xUnit** + **coverlet** + **ReportGenerator**
- Интеграционные тесты с **TestContainers** (требует Docker)
- CI: GitHub Actions с PostgreSQL service container
```yaml
services:
  postgres:
    image: postgres:latest
    env:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: root
    options: --health-cmd pg_isready --health-interval 10s --health-timeout 5s --health-retries 5
```

### Пул пакетов (основные — MVP)

| Назначение | Пакет |
|---|---|
| ORM | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `BCrypt.Net-Next` |
| Валидация | `FluentValidation` |
| Swagger | `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations` |
| Логирование | `Serilog.AspNetCore` |
| Фейковые данные (тесты) | `Bogus` |
| Healthchecks | `AspNetCore.HealthChecks.NpgSql` |

> **CollegeLMS:** в MVP запрещены message brokers, gRPC, WebSocket (SignalR). Redis используется только для сессий.

### Пул пакетов (опциональные — продвинутые сценарии)

| Назначение | Пакет |
|---|---|
| CQRS | `MediatR` |
| Фоновые задачи | `Hangfire.AspNetCore` + `Hangfire.PostgreSql` |
| Очереди | `RabbitMQ.Client` |
| Resilience | `Polly` |
| Реалтайм (SignalR) | `Microsoft.AspNetCore.SignalR` |
| Экспорт | `Magicodes.IE.*`, `SixLabors.ImageSharp` |
| Кэш | `Microsoft.Extensions.Caching.StackExchangeRedis` |

### Конвенции кода
- Асинхронность: `async Task` везде, `CancellationToken` пробрасывается
- DTO: ручные мапперы (без AutoMapper)
- Swagger: аннотировать все эндпоинты
- Git: префиксы `feature:` / `fix:` в коммитах
- Форматирование: CSharpier (`Shift+Alt+F`)

### Референсы в репозиториях
- `artemovsergey/.NET` — Wiki/журнал по ASP.NET Core
- `artemovsergey/ModuleBankApp` — микросервис счетов (MediatR, Hangfire, RabbitMQ, Keycloak)
- `artemovsergey/TicTacToe` — Minimal API + CI/CD + TestContainers
- `artemovsergey/VendingAppFinal` — .NET 8, SignalR, Result паттерн, Flutter frontend
- `artemovsergey/SampleApp` — Multi-targeting (net7.0+net9.0 class library), Aspire, Angular frontend
- `artemovsergey/StudyPractice-SampleApp` — Учебный курс React + .NET

---

## 2. React (`react`)

### Область
SPA на React 19 + TypeScript + Vite + Tailwind CSS 4.

> **CollegeLMS:** основной фронтенд на Next.js 14, React-скилл применим для ReactNative клиента и справочно.

### Стек
| Технология | Версия |
|---|---|
| React | 19 |
| Vite | 7 |
| TypeScript | 5.x (strict) |
| Tailwind CSS | 4 |
| React Router | 7 |
| ESLint | 9 |

### Структура проекта
```
sampleapp.react/
├── src/
│   ├── components/
│   │   ├── Header.tsx
│   │   ├── Home.tsx
│   │   ├── Pair.tsx
│   │   ├── ScheduleByDay.tsx
│   │   ├── CreatePair.tsx
│   │   ├── Journal.tsx
│   │   └── Footer.tsx
│   ├── models/
│   │   └── schedule.ts
│   ├── data/
│   │   └── scheduleData.ts
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── index.html
├── vite.config.ts
├── tsconfig.json
├── package.json
└── eslint.config.js
```

### Ключевые паттерны

**Поднятие состояния (Lifting State Up)**:
- Корневой компонент `App.tsx` держит общее состояние
- Передача через пропсы вниз, колбэки наверх
- `localStorage` для персистентности

**Маршрутизация** (`React Router v7`):
```tsx
<HashRouter>
  <Header currentSchedule={currentSchedule} />
  <Routes>
    <Route path="/" element={<Home ... />} />
    <Route path="/create" element={<CreatePair ... />} />
    <Route path="/journal" element={<Journal ... />} />
    <Route path="*" element={<Home ... />} />
  </Routes>
  <Footer />
</HashRouter>
```
HashRouter — для GitHub Pages (нет поддержки SPA fallback).

**Работа с API** (native `fetch` + AbortController):
```tsx
useEffect(() => {
    const abort = new AbortController()

    fetch("http://localhost:5188/api/Microposts", { signal: abort.signal })
        .then(r => r.json())
        .then(setPosts)
        .catch(err => {
            if (err.name !== "AbortError")
                console.log("Ошибка получения данных из API", err)
        })

    return () => abort.abort()
}, [])
```

Или через async/await (c IIFE):
```tsx
useEffect(() => {
    const abort = new AbortController()

    ;(async () => {
        try {
            const res = await fetch("http://localhost:5188/api/Microposts", { signal: abort.signal })
            const data = await res.json()
            setPosts(data)
        } catch (err) {
            if (err instanceof DOMException && err.name === "AbortError") return
            console.log("Ошибка получения данных из API", err)
        }
    })()

    return () => abort.abort()
}, [])
```

**Формы** (контролируемые компоненты):
```tsx
const [form, setForm] = useState({ title: "", content: "" })

const onSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    fetch("http://localhost:5188/api/Microposts", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form)
    })
}
```

**Типизация пропсов**:
```tsx
interface HeaderProps {
    currentSchedule: ScheduleTeacher[] | undefined
}

const Header: React.FC<HeaderProps> = ({ currentSchedule }) => {
    // ...
}
```

### Стилизация
- **Tailwind CSS 4** через `@tailwindcss/vite` плагин
- Входной `index.css`: только `@import "tailwindcss"`
- Условные классы: `className={clsx('border', condition && 'bg-blue-500')}`

### Сборка и запуск
```bash
npm run dev        # Vite dev server
npm run build      # Vite production build
npm run preview    # Превью билда
```

### Деплой
- GitHub Pages через пакет `gh-pages`
- `"homepage": "https://artemovsergey.github.io/{repo}/"` в `package.json`
- HashRouter (не BrowserRouter) для GitHub Pages

### Конвенции кода
- Типизированные пропсы (интерфейс + `React.FC`)
- Компоненты в отдельных файлах в `components/`
- Модели данных в `models/`
- Статические данные в `data/`
- ESLint 9 + `eslint-plugin-react-hooks` + `eslint-plugin-react-refresh`

### Референсы в репозиториях
- `artemovsergey/StudyPractice-SampleApp/sampleapp.react/` — основной React проект
- `artemovsergey/scheduleTeacher` — билд React приложения на GitHub Pages

---

## 3. Next.js (`nextjs`)

### Область
Full-stack Next.js приложения (App Router, SSR, Server Components).

### Статус: основной фронтенд CollegeLMS
Проект CollegeLMS использует **Next.js 14** как основной клиент. В проанализированных репозиториях Next.js практически не встречается (все React проекты на Vite SPA), поэтому скилл построен на смежных знаниях и требованиях проекта.

### Ключевые концепции для изучения

**App Router (рекомендуемый)**:
```
app/
├── page.tsx          — /
├── layout.tsx        — корневой layout
├── api/
│   └── posts/
│       └── route.ts  — API Route
└── posts/
    ├── page.tsx      — /posts
    └── [id]/
        └── page.tsx  — /posts/:id
```

**Server vs Client Components**:
```tsx
// Серверный компонент (по умолчанию)
async function PostsPage() {
    const posts = await db.posts.findMany()
    return <PostList posts={posts} />
}

// Клиентский компонент (директива)
"use client"
function PostForm() {
    const [title, setTitle] = useState("")
    return <input value={title} onChange={e => setTitle(e.target.value)} />
}
```

**Data Fetching**:
- `fetch` в Server Components (кешируется по умолчанию)
- Server Actions для мутаций
- `revalidatePath` / `revalidateTag` для инвалидации кеша

**Интеграция с .NET API**:
```tsx
// В Server Component
async function getPosts() {
    const res = await fetch("http://api:5000/api/posts", {
        next: { revalidate: 60 }  // ISR: 60 сек
    })
    return res.json()
}
```

### Dockerfile для Next.js (из `artemovsergey/VPS`)
```dockerfile
FROM node:22-alpine AS base
FROM base AS deps
    npm ci
FROM base AS builder
    npm run build
FROM base AS runner
    COPY --from=builder /app/.next/standalone ./
    USER nextjs
    EXPOSE 3000
    ENV HOSTNAME="0.0.0.0"
    CMD ["node", "server.js"]
```

### Маршрутизация из React Router -> Next.js App Router

| React Router | Next.js App Router |
|---|---|
| `<Route path="/" element={<Home />} />` | `app/page.tsx` |
| `<Route path="/create" element={<Create />} />` | `app/create/page.tsx` |
| `<Route path="/:id" element={<Detail />} />` | `app/[id]/page.tsx` |
| `<Route path="/api/posts" ... />` | `app/api/posts/route.ts` |
| `useNavigate()` | `redirect()` или `useRouter()` |
| `useParams()` | `params` prop в Server Component |

### Референсы
- `artemovsergey/VPS/README.md` — Dockerfile для Next.js (multi-stage)
- `artemovsergey/StudyPractice-SampleApp/README.md` — Next.js упомянут как следующий этап изучения
- `artemovsergey/.NET` — Wiki (может содержать заметки по Next.js)

---

## 4. Deploy (`deploy`)

### Область
Docker Compose, CI/CD (GitHub Actions), VPS развертывание.

### Типовая архитектура Docker Compose

```yaml
services:
  nginx:
    image: nginx:alpine
    ports: ["80:80"]
    volumes: ["./nginx.conf:/etc/nginx/conf.d/default.conf"]
    depends_on: [api]

  api:
    build: ./backend
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://*:8080
      ConnectionStrings__PostgreSQL: Host=db;Port=5432;Database=AppDb;Username=postgres;Password=root
    depends_on:
      db: { condition: service_healthy }

  db:
    image: postgres:16-alpine
    volumes: ["postgres_data:/var/lib/postgresql/data"]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
```

### Dockerfile: .NET API (multi-stage)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
    USER $APP_UID
    WORKDIR /app
    EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
    WORKDIR /src
    COPY ["src/Api.csproj", "."]
    RUN dotnet restore
    COPY . .
    RUN dotnet build -c Release -o /app/build

FROM build AS publish
    RUN dotnet publish -c Release -o /app/publish

FROM base AS final
    COPY --from=publish /app/publish .
    COPY appsettings.Production.json .
    ENTRYPOINT ["dotnet", "Api.dll"]
```

### NGINX Reverse Proxy

```nginx
upstream api {
    server api:8080;
}
upstream frontend {
    server frontend:3000;
}

server {
    listen 80;
    client_max_body_size 100M;

    location /api/ {
        proxy_pass http://api/;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";
    }

    location /swagger {
        proxy_pass http://api/swagger;
    }

    location / {
        proxy_pass http://frontend;
        try_files $uri $uri/ /index.html;
    }
}
```

### CI/CD: GitHub Actions

**CI (тесты + coverage)**:
```yaml
name: dotnet-test
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:latest
        env: { POSTGRES_PASSWORD: root }
        options: --health-cmd pg_isready --health-interval 10s --health-timeout 5s --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0" }
      - run: dotnet restore
      - run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
      - run: dotnet tool install -g dotnet-reportgenerator-globaltool
      - run: reportgenerator -reports:**/coverage.opencover.xml -targetdir:coveragereport
      - uses: peaceiris/actions-gh-pages@v3
        with: { github_token: ${{ secrets.GITHUB_TOKEN }}, publish_dir: ./coveragereport }
```

**CD (VPS через SSH)**:
```yaml
name: deploy
on: { push: { branches: [master] } }
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USERNAME }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd /home/user1/app
            git pull
            docker compose down
            docker compose up -d --build
```

### Deploy скрипт (`scripts/vps.sh`)
```bash
cd /home/user1/tictactoe/
docker compose down
docker compose pull
docker compose up -d --build
docker ps
```

### Docker — best practices

| Правило | Реализация |
|---|---|
| Non-root user | `USER $APP_UID` в .NET, `nextjs` (UID 1001) в Node |
| Healthcheck | `pg_isready -U postgres`, 5s interval |
| Named volumes | `postgres_data:/var/lib/postgresql/data` |
| .dockerignore | `.env`, `.git`, `node_modules`, `bin/obj`, `.vs` |
| env для конфигов | `env_file: game.env` + `environment:` inline |
| Производственный конфиг | `appsettings.Production.json` копируется в образ |

### Переменные окружения
- Nested connection strings: `ConnectionStrings__PostgreSQL=Host=db;Port=5432;...`
- ASP.NET: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://*:8080`
- GitHub Secrets: `VPS_HOST`, `VPS_USERNAME`, `SSH_PRIVATE_KEY`

### Безопасность
- SSH key-based auth (`ssh-keygen -t rsa -b 4096`)
- Non-root пользователи в контейнерах
- Secrets в GitHub Actions (не в коде)
- `.env` в `.dockerignore` и `.gitignore`
- Keycloak как IDP (ModuleBankApp)

### Референсы в репозиториях
- `artemovsergey/VPS` — документация: Dockerfile (Next.js, React+Nginx, NGINX LB), Docker Compose, SSH, VPS
- `artemovsergey/TicTacToe` — полный пайплайн: GitHub Actions CI/CD, VPS deploy, docker-compose, TestContainers
- `artemovsergey/ModuleBankApp` — сложная инфраструктура: RabbitMQ, Keycloak, dual PostgreSQL, Hangfire
- `artemovsergey/.NET` — Wiki (Docker, Docker Compose, GitHub Actions, load balancing)
- `artemovsergey/SampleApp` — docker-compose + Nginx LB config

---

## Приложение: ProfApp

Репозиторий `ProfApp` — приватный. Требуется авторизация:

```bash
gh auth login
gh repo clone artemovsergey/ProfApp
```

После клонирования дополнить скиллы данными из этого репозитория.
