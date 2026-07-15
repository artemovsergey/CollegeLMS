# Build & Deployment Optimization Implementation Plan

> **For agentic workers:** Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ускорить сборку Docker и CI/CD пайплайн CollegeLMS на ~30-40%

**Architecture:** 5 независимых quick-win оптимизаций в корневых конфигах (`.dockerignore`, CI workflow, Dockerfile, deploy script). Никаких изменений в коде приложения.

**Tech Stack:** Docker, GitHub Actions, .NET, Bash

**Global Constraints:**
- Не менять структуру проектов и код приложения
- Все изменения обратимы
- CI/CD workflow должен остаться рабочим после каждого изменения

---

### Task 1: Оптимизация `.dockerignore`

**Files:**
- Modify: `.dockerignore`

- [ ] **Step 1: Добавить исключения в корневой `.dockerignore`**

Текущий `.dockerignore` исключает `**/node_modules` — хорошо. Но контекст сборки API копирует `frontend/`, `docs/`, `scripts/`, `import/` — сотни MB неиспользуемых файлов.

Добавить в конец `.dockerignore`:

```
frontend/
docs/
scripts/
import/
CollegeLMS.TelegramBot/
```

- [ ] **Step 2: Проверить, что frontend не затронут**

```bash
docker compose build --no-cache frontend
```
Должно работать без ошибок — `frontend` использует свой контекст `./frontend` и свой `.dockerignore`.

- [ ] **Step 3: Проверить размер контекста**

```bash
# Показать размер Docker build context для API
docker buildx build -f CollegeLMS.API/Dockerfile --no-cache --load -t test-context . 2>&1
```

- [ ] **Step 4: Commit**

```bash
git add .dockerignore
git commit -m "perf: exclude frontend/docs/scripts from Docker build context"
```

---

### Task 2: NuGet cache persistence на VPS

**Files:**
- Modify: `docker-compose.yml`
- Modify: `CollegeLMS.API/Dockerfile`

- [ ] **Step 1: Добавить volume для BuildKit cache**

В `docker-compose.yml` добавить volume:

```yaml
volumes:
  buildx_cache:
```

В сервис `api` добавить:

```yaml
volumes:
  - buildx_cache:/root/.cache/buildx
```

Использовать `BUILDKIT_PROGRESS` env или `DOCKER_BUILDKIT=1` — BuildKit включён по умолчанию в Docker Desktop.

- [ ] **Step 2: Commit**

```bash
git add docker-compose.yml
git commit -m "perf: add buildx cache volume for NuGet persistence"
```

---

### Task 3: CSharpier — проверять только changed файлы

**Files:**
- Modify: `.github/workflows/test-and-deploy.yml`

- [ ] **Step 1: Заменить `dotnet csharpier check .` на diff-based check**

В CI workflow, секция "Check formatting":

```yaml
- name: Check formatting (changed files only)
  run: |
    if [ "${{ github.event_name }}" = "pull_request" ]; then
      CHANGED=$(git diff --name-only origin/${{ github.base_ref }}...HEAD -- '*.cs' || true)
    else
      CHANGED=$(git diff --name-only HEAD~1...HEAD -- '*.cs' || true)
    fi
    if [ -n "$CHANGED" ]; then
      echo "$CHANGED" | xargs dotnet csharpier check
    else
      echo "No .cs files changed, skipping format check"
    fi
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/test-and-deploy.yml
git commit -m "perf: CSharpier check only changed .cs files"
```

---

### Task 4: Deploy polling — curl --retry

**Files:**
- Modify: `.github/workflows/test-and-deploy.yml`

- [ ] **Step 1: Заменить bash цикл на curl --retry**

Найти блок:

```bash
for i in $(seq 1 25); do
  if curl -sf http://localhost:5026/api/news/categories > /dev/null 2>&1; then
    echo "API ready after ${i}s"
    break
  fi
  echo "Waiting for API... ${i}s"
  sleep 5
done
```

Заменить на:

```bash
echo "=== Waiting for API to be ready ==="
if curl --retry 25 --retry-delay 5 --retry-all-errors -sf http://localhost:5026/api/health > /dev/null 2>&1; then
  echo "API ready"
else
  echo "API not ready after timeout"
fi
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/test-and-deploy.yml
git commit -m "perf: replace polling loop with curl --retry"
```

---

### Task 5: Secrets — безопасная запись .env

**Files:**
- Modify: `.github/workflows/test-and-deploy.yml`

- [ ] **Step 1: Заменить Python heredoc на printf**

Блок:

```bash
python3 << 'PYEOF'
import os
with open('.env', 'w') as f:
    f.write('DB_PORT=5432\n')
    f.write('DB_USER=postgres\n')
    f.write(f"DB_PASSWORD={os.environ.get('DB_PASSWORD', 'postgres')}\n")
    ...
PYEOF
```

Заменить на:

```bash
cat > .env << EOF
DB_PORT=5432
DB_USER=postgres
DB_PASSWORD=${DB_PASSWORD}
DB_NAME=collegelms
REDIS_PORT=6379
API_PORT=5026
ASPNETCORE_ENVIRONMENT=Production
NGINX_HTTP_PORT=80
NGINX_HTTPS_PORT=443
TELEGRAM_BOT_PORT=5030
TELEGRAM_BOT_TOKEN=${TELEGRAM_BOT_TOKEN}
TELEGRAM_ALLOWED_USER_ID=${TELEGRAM_ALLOWED_USER_ID}
OPENCODE_SERVER_URL=http://host.docker.internal:4096
OPENCODE_USERNAME=opencode
OPENCODE_PASSWORD=
OPENCODE_DEFAULT_MODEL=opencode/deepseek-v4-flash-free
VK_ACCESS_TOKEN=
VK_GROUP_ID=0
VK_CONFIRMATION_CODE=
EOF
```

И аналогично заменить второй Python heredoc (ALTER USER):

```bash
# Sync DB password — volume may have old password from first deploy
DB_PASSWORD_ESC=$(printf '%s\n' "$DB_PASSWORD" | sed "s/'/''/g")
docker compose exec -T db psql -U postgres -c "ALTER USER postgres PASSWORD '${DB_PASSWORD_ESC}';" 2>/dev/null || echo "ALTER USER skipped or failed"
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/test-and-deploy.yml
git commit -m "perf: replace Python heredoc with portable bash for .env"
```

---

### Verification

- [ ] **Step 1: Проверить dotnet build**

```bash
docker compose exec api dotnet build --no-restore
```

- [ ] **Step 2: Проверить docker compose config (синтаксис)**

```bash
docker compose config -q && echo "OK"
```

- [ ] **Step 3: Финальный коммит и пуш**

```bash
git add -A && git commit -m "perf: build optimization quick wins" || true
```
