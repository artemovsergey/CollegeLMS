# CollegeLMS: инструкции для агентов bb

Этот файл bb добавляет к системному промпту всех провайдеров. Базовые правила
репозитория — в `AGENTS.md` в корне; здесь только то, что важно в bb-тредах.

## Рабочий цикл

1. Ветка от `master` до первого изменения файла: `git checkout -b fix/...`
   или `feat/...`. Прямые коммиты в `master` запрещены.
2. Правки → `dotnet build CollegeLMS.slnx` → таргетные тесты → при UI-правках
   `cd CollegeLMS.Next && npm run check:design-system`.
3. Коммит → merge в `master` → `git push origin master`. Push запускает
   `deploy.yml`; merge и push не ждут отдельного разрешения.

## Worktree

- Новый worktree получает `.env` из `.worktreeinclude`; зависимости ставит
  `.bb-env-setup.sh` (`dotnet tool restore`, `dotnet restore`, `npm ci`).
- Если зависимости не встали, сначала проверь `dotnet restore` и
  `npm --prefix CollegeLMS.Next ci`, не пересоздавай worktree.
- Не запускай `docker compose` из рабочего checkout. Прод-каталог —
  `/home/user1/CollegeLMS`; docker-команды только оттуда.

## Проверки

- Backend: `dotnet build CollegeLMS.slnx`; таргетно —
  `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}`.
- Frontend: `cd CollegeLMS.Next && npm run build`; дизайн-гейты —
  `npm run check:design-system`.
- E2E: `cd CollegeLMS.Next && npx playwright test {spec}`; стенд —
  `baseURL: http://localhost` через loadbalancer.
- Формат: `dotnet csharpier check .` (после `dotnet tool restore`).

## Секреты и данные

- `.env` — локальный, не коммитить; в worktree копируется автоматически.
- Пароли БД, JWT и токены не выводить в чат, логи и коммиты.
- Схему БД менять только через миграции EF Core; данные — через API или
  `psql`/плагин `sql`, без ручных DDL.

## bb

- Контекст: `bb status`, `bb pull-request`.
- Долгие команды (dev-сервер, сборка, тесты) — в terminal, не в блокирующем
  shell.
