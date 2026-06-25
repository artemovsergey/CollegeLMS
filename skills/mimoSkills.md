# Skills — .NET API, React, Next.js, Deploy

> Извлечены из репозиториев: VPS, VendingAppFinal, scheduleTeacher, StudyPractice-SampleApp, .NET, ModuleBankApp, OmniFoodApp, SampleApp, TicTacToe

---

## 1. .NET API Backend

### Архитектура и структура

- Clean Architecture: `Project.API`, `Project.Tests`, `Project.Domain`
- Minimal API для простых сервисов, Controller-based для сложных
- Решение (.sln) содержит все проекты: API, Tests, иногда Angular/React клиент

### База данных

- PostgreSQL через Entity Framework Core
- Миграции: всегда генерировать через `dotnet ef migrations add <Name>`
- Типы: `uuid` с `gen_random_uuid()` для PK, `DateTime` с timezone
- Каскадное удаление через навигационные свойства + FK
- Connection string через переменные окружения: `ConnectionStrings__DefaultConnection`

### Аутентификация и авторизация

- JWT без refresh (MVP)
- Ключ ≥256 бит в `appsettings.json`
- Пакеты: `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`
- Минимальная конфигурация: `IssuerSigningKey`, `ValidateIssuerSigningKey = true`
- Хеширование паролей: BCrypt с `workFactor: 12`

### API-паттерны

- **Result Pattern**: единая модель ответа `Result<T>` с полем `Success`, `Message`, `Data`
- **Global Exception Handler Middleware**: убирает try-catch из контроллеров, обрабатывает исключения централизованно
- **Search/Filter/Sort/Pagination**: модель `Option` + `ApiResult<T>` с `TotalPages`, `HasNext`, `HasPreview`
- **Generic CRUD Controller**: базовый контроллер для типовых операций

### Экспорт данных

- CSV: `Magicodes.IE.Csv` → `CsvExporter.ExportAsByteArray()`
- HTML: `Magicodes.IE.Html` → `HtmlExporter.ExportListByTemplate()`
- PDF: `Magicodes.IE.Pdf` → `PdfExporter.ExportListBytesByTemplate()`

### Swagger / OpenAPI

- Пакеты: `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`
- Аннотации: `[SwaggerOperation(Summary = "...")]` на каждую точку
- JWT: автоматическая подстановка Bearer вswagger UI

### Тестирование

- xUnit для unit-тестов
- TestContainers для интеграционных тестов (требует Docker)
- Покрытия: `coverlet.msbuild` + `dotnet-reportgenerator-globaltool`
- Команда: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`

### Реальные-time

- SignalR: `IHubContext<T>`, асинхронные методы, хаб в `Hubs/`

### Импорт данных

- JSON: `JsonSerializer.Deserialize<T>()` с `JsonNamingPolicy.SnakeCaseLower`
- Excel: импорт через DBeaver (Ctrl+Shift+V)
- Копирование ресурсов в сборку: `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

---

## 2. React (Vite)

### Создание проекта

```bash
npm create vite@latest my-app -- --template react-ts
```

### Стилизация

- Tailwind CSS или CSS Modules
- Условная стилизация: тернарный оператор или `clsx`
- SCSS для сложных проектов

### Структура

- `src/` — компоненты, hooks, services, types
- Компоненты: функциональные с хуками
- API-вызовы: `fetch` или `axios` в сервисах

---

## 3. Next.js

### Создание проекта

```bash
npx create-next-app@latest myapp
```

### Настройки

- Tailwind CSS для стилей
- `clsx` для условной стилизации
- `next/image` для изображений (оптимизация)
- `next/link` для навигации (prefetch)

### Получение данных

```typescript
async function getData() {
  const res = await fetch("http://localhost:5083/api/Endpoint");
  const data = await res.json();
  return data;
}
```

### Особенности

- Шрифты через `next/font`
- SSR/SSG по умолчанию
- `try_files $uri $uri/ /index.html` для SPA в nginx

### Dockerfile (Next.js)

```dockerfile
FROM node:22-alpine AS base
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs
COPY --from=builder /app/public ./public
RUN mkdir .next && chown nextjs:nodejs .next
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
```

---

## 4. Deploy и DevOps

### Docker

- Multi-stage builds для уменьшения размера образа
- `node:22-alpine` для фронта, `mcr.microsoft.com/dotnet/sdk` для API
- `.dockerignore`: исключить `bin/`, `obj/`, `node_modules/`, `.git/`

### Docker Compose

```yaml
networks:
  home-network:
    driver: bridge

volumes:
  postgres_data:

services:
  nginx:
    container_name: ContainerNginx
    build:
      context: .
      dockerfile: loadbalancer/Dockerfile
    restart: always
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - db
      - api
      - react
    networks:
      - home-network

  api:
    container_name: ContainerAPI
    build:
      context: .
      dockerfile: API/Dockerfile
    ports:
      - 5001:5000
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:5000
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=MyDb;Username=postgres;Password=root
    depends_on:
      - db
    networks:
      - home-network

  db:
    container_name: ContainerPostgres
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: root
      POSTGRES_DB: MyDb
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - home-network
```

### Nginx (reverse proxy)

```nginx
events {
    worker_connections 1024;
}

http {
    upstream react {
        server react:80;
    }
    upstream api {
        server api:5000;
    }
    client_max_body_size 100M;

    server {
        listen 80;
        server_name localhost;

        location /api/ {
            proxy_pass http://api;
        }
        location / {
            proxy_pass http://react;
        }
    }
}
```

### VPS (Linux)

- SSH-ключи: `ssh-keygen -t rsa -b 4096`
- Подключение: `ssh user@ip`
- Права: создавать папки от текущего пользователя, не от root
- `sudo chmod -R 777 ./uploads` для монтируемых папок
- Очистка: `docker system prune -a --volumes`

### GitHub Actions CI/CD

- Тесты: `dotnet test`
- Деплой на VPS через SSH
- Бейджи покрытия на GitHub Pages

### Переменные окружения

- Вся конфигурация через env vars
- `game.env`, `.env` файлы для локальной разработки
- Docker Compose: `environment` секция

### Полезные команды

```bash
# Перезапуск с очисткой БД
docker-compose down -v && docker-compose up --build

# Генерация миграции
dotnet ef migrations add InitialCreate

# Применение миграций
dotnet ef database update

# Тесты с покрытием
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Отчет покрытия
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

---

## 5. Фронтенд (Angular/Ionic)

### Структура Angular проекта

- `src/` — компоненты, сервисы, модули
- `@defer` для отложенных представлений
- SSR и SEO для Next.js/Angular Universal

### Ресурсы

- Иконки: Phosphor, Ionicon, Icons8
- Изображения: Unsplash, Pexels (размер ×2 от отображаемого)
- Шрифты: Inter, Open Sans, Roboto (sans-serif), Merriweather (serif)
- Цвета: Tailwind CSS, Flat UI Colors, Coolors

### Дизайн

- Font size: 16px–32px, weight ≥400
- Line height: 1.5–2 (чем больше шрифт, тем больше)
- Текст по левому краю
- Caps для коротких заголовков

---

## 6. Git

### Коммиты

- Осмысленные сообщения: `feature:`, `fix:`, `docs:`
- README по структуре для каждого проекта

### Ветки

- `master` / `main` для основной ветки
- `gh-pages` для деплоя статических сайтов
