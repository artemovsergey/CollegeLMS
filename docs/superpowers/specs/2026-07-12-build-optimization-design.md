# Build & Deployment Optimization Design

**Цель:** Ускорить процессы сборки Docker-образов и CI/CD пайплайна CollegeLMS

**Метод:** 5 быстрых побед (Quick Wins), не требующих архитектурных изменений

## Оптимизации

### 1. `.dockerignore` — сокращение Docker build context

- Добавить `frontend/`, `docs/`, `scripts/`, `import/` в корневой `.dockerignore`
- API собирается с `context: .` (корень), Dockerfile делает `COPY . ./`
- `frontend/` использует свой контекст (`context: ./frontend`) — не затронут
- Ожидание: контекст ~700 MB → ~50 MB, экономия ~15-20с на сборке

### 2. NuGet cache persistence на VPS

- Текущий `docker compose up --build` не кэширует NuGet между билдами на VPS
- BuildKit cache mount (`--mount=type=cache,id=nuget`) живёт в памяти билдера
- Решение: `docker buildx build --cache-to/--cache-from type=local`
- Ожидание: ~30-40с экономии на втором и последующих билдах

### 3. CSharpier — проверять только изменённые файлы

- `dotnet csharpier check .` проверяет весь репозиторий (40+ .cs файлов)
- Заменить на `git diff --name-only origin/master...HEAD -- *.cs`
- Ожидание: ~5-10с экономии в CI

### 4. Deploy polling — curl --retry

- Цикл `for i in $(seq 1 25); do curl ...; sleep 5; done` — 125c max
- Заменить на `curl --retry 25 --retry-delay 5 --retry-all-errors`
- Ожидание: упрощение кода, до 75с экономии при готовом API

### 5. Secrets — безопасная запись .env

- Python heredoc пишет .env — может показать traceback с secret в логах
- Заменить на `printf` с экранированием или `docker compose --env-file`
- Ожидание: безопаснее, проще поддерживать

## Ожидаемый результат

| Оптимизация | Экономия |
|-------------|----------|
| .dockerignore | 15-20с на билд |
| NuGet cache | 30-40с на 2+ билде |
| CSharpier diff | 5-10с в CI |
| curl --retry | до 75с deploy |
| Secrets | безопасность |
| **Суммарно** | **~30-40% локально, ~15-20% CI** |
