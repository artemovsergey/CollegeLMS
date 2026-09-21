---
name: feature-workflow
description: Orchestrate full vertical-slice feature development — planning, backend, tests, frontend, E2E, CI/CD, merge. Use when starting a new feature in CollegeLMS
---

# feature-workflow

Полный вертикальный срез фичи в CollegeLMS. Фазы 0–5 делегируются субагентам через `task`; фазы 0 и 6 ведёт главный агент (Architect).

## Pipeline (7 фаз)

| Фаза | Owner | Гейт | Коммит |
|------|-------|------|--------|
| 0 Planning | Architect | спека утверждена | — |
| 1 Backend | `BackendAgent` | `dotnet build` | `phase 1: {feature} backend` |
| 2 Tests | `TesterAgent` | `dotnet test --filter ...` | `phase 2: {feature} tests` |
| 3 Frontend | `FrontendAgent` | `npm run dev` | `phase 3: {feature} frontend` |
| 4 E2E | `TesterAgent` | `npx playwright test {spec}` | `phase 4: {feature} e2e` |
| 5 CI/CD | `DevOpsAgent` | статическая проверка конфигов | `phase 5: {feature} devops` |
| 6 Merge | Architect | review + CI/CD зелёный | `merge: {service} — {description}` |

## Общие правила

- Ветка `feature/{service}-{feature}` от свежего `master`; перед стартом: `git fetch origin && git pull --rebase origin master`.
- Гейт фазы N+1 — успешное прохождение проверки фазы N.
- Коммит каждой фазы: `git add -A && git commit -m "phase N: {feature} ..."`.
- **Сплит тестов:** локально — только таргетно (`--filter`), полный прогон — в CI/CD.
- Независимые фазы (например, Frontend и Tests) можно запускать **параллельно** (`dispatching-parallel-agents`, `delegate`) — они не конфликтуют по файлам.
- Пуш только при заданном `GITHUB_TOKEN`/`GH_TOKEN`.

---

## Phase 0: Planning (Architect)

Load: `brainstorming` → `writing-plans`

- Прочитать `docs/spec/task.md`, понять требования.
- Декомпозировать на User Stories (шаблон ниже), сохранить в `docs/spec/`.
- Согласовать дизайн с пользователем → `docs/spec/{feature}-design.md`.
- Создать ветку: `git checkout -b feature/{service}-{feature}`.

**Gate:** спецификация утверждена пользователем.

---

## Phase 1: Backend (BackendAgent)

Load: `dotnet-entity`, `dotnet-endpoint`, `fluent-validation`, `swagger-docs`, `aspnet-core`

```
Entities/{Name}.cs                         # наследует базовый Entity
Entities/Enums/{Name}Type.cs               # если нужен enum
Data/Configurations/{Name}Configuration.cs # ToTable, ValueGeneratedNever, HasMaxLength, HasConversion<string>, HasIndex, HasData
Data/DbConstraints.cs                      # CHECK constraints — сюда (идемпотентный PL/pgSQL), НЕ в EF Config
Dtos/{Action}{Name}Request.cs, {Name}Response.cs
Mappers/{Name}Mapper.cs                    # корень Mappers/, ручные extension-мапперы
Interfaces/I{Name}Service.cs               # корень Interfaces/
Services/{Name}Service.cs                  # primary constructor, Result<T>, AsNoTracking, FindAsync, CancellationToken
Controllers/{Name}Controller.cs            # CRUD, SwaggerOperation/ProducesResponseType на русском
Validators/{Name}RequestValidator.cs
SwaggerExamples/{Name}ResponseExample.cs
Extensions/ServiceCollectionExtensions.cs  # регистрация DI (НЕ Program.cs)
```

- Миграция: `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql`
- Обновить Postman: `docs/spec/CollegeLMS.postman_collection.json`.

**Gate:** `dotnet build CollegeLMS.slnx` проходит.

---

## Phase 2: Tests (TesterAgent)

Load: `dotnet-test`, `test-driven-development`

```
CollegeLMS.Tests/Unit/Services/{Name}ServiceTests.cs
CollegeLMS.Tests/Integration/Controllers/{Name}ControllerTests.cs
```

- Модульные (xUnit + Moq + Bogus) — happy + error case на каждый метод сервиса.
- Интеграционные (`WebApplicationFactory`, EF InMemory).
- Локальная проверка таргетно: `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}`.
- Полный прогон — в CI/CD (`quality.yml`).

**Gate:** таргетные тесты зелёные.

---

## Phase 3: Frontend (FrontendAgent)

Load: `impeccable`, `design-system`, `nextjs-page`

```
CollegeLMS.Next/app/{route}/page.tsx
CollegeLMS.Next/app/{route}/loading.tsx
CollegeLMS.Next/app/{route}/error.tsx
CollegeLMS.Next/components/{Name}*.tsx
CollegeLMS.Next/lib/{name}.ts
```

- Дизайн-бриф (shape) → реализация (craft) → polish → audit (`impeccable`).
- Токены и компоненты (`design-system`), интеграция API с типами.
- Mobile-first, touch 44×44px, адаптив 393px ↔ 1920px (DESIGN.md §6).

**Gate:** `npm run dev` — страница рендерится, User Story проверена визуально.

---

## Phase 4: E2E (TesterAgent)

Load: `playwright`, `playwright-interactive`

```
CollegeLMS.Next/e2e/{feature}.spec.ts
```

- Тесты изолированы: ответы API мокаются через `page.route("**/api/...")` — реальный backend/БД не нужны.
- Локально: `cd CollegeLMS.Next && npx playwright test {spec}` (только затронутый спек).

**Gate:** затронутый спек проходит.

---

## Phase 5: CI/CD (DevOpsAgent)

Load: `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`, `gh-fix-ci`

- ⚠️ Локальный Docker **не запускаем** — сборка стека в CI/CD.
- Статически проверить `Dockerfile`, `docker-compose.yml`, `.github/workflows/*`: переменные окружения, profiles, зависимости job'ов.

**Gate:** конфиги корректны; `deploy.yml` (`needs: quality`) подхватит `quality.yml` автоматически.

---

## Phase 6: Merge & Deploy (Architect)

Load: `verification-before-completion`, `requesting-code-review`, `yeet`

- `verification-before-completion`: свежая проверка (build/tests на актуальном состоянии, не по памяти).
- `requesting-code-review`: ревью всех изменений; замечания → исправить и перезапустить фазы.
- Слияние и пуш:
  ```
  git checkout master
  git merge feature/{service}-{feature}
  git push origin master
  ```
- Проверить, что GitHub Actions (quality → deploy) запустился и прошёл.

**Gate:** CI/CD зелёный, деплой на VPS выполнен.

---

## Гейты приёмки

| Гейт | Проверка | Кто | Фаза |
|------|----------|-----|------|
| **G1** | `dotnet build` | BackendAgent | 1 |
| **G2** | таргетные `dotnet test` | TesterAgent | 2 |
| **G3** | `npm run dev` рендерится | FrontendAgent | 3 |
| **G4** | затронутый Playwright-спек | TesterAgent | 4 |
| **G5** | CI (build + full test + E2E) зелёный | CI | 6 |

## Definition of Done

- [ ] `dotnet build CollegeLMS.slnx` проходит
- [ ] Таргетные тесты зелёные; полный прогон зелёный в CI
- [ ] Swagger UI показывает endpoint с русской документацией и SwaggerExamples
- [ ] Postman-коллекция обновлена
- [ ] Frontend работает (`npm run dev`), адаптив 393px ↔ 1920px
- [ ] Затронутый E2E-спек проходит
- [ ] Конфиги Docker/compose/workflow проверены (сборка стека — в CI/CD)
- [ ] Feature-ветка слита в `master`, CI/CD зелёный, деплой на VPS выполнен

---

## Fast-lane (hotfix)

Для критических багов в production — минуя полный цикл.

```
git checkout master
git checkout -b hotfix/{description}
# фикс без новых фич и рефакторинга; проверить dotnet build + npm run build
git add -A && git commit -m "hotfix: {description}"
git checkout master && git merge hotfix/{description}
git push origin master   # CD деплоит сразу
```

Правила: только критично (недоступность, потеря данных, безопасность); после hotfix — задача на полноценный фикс с тестами.

---

## Шаблон User Story

```markdown
## UC-N: {Роль} может {действие}

**Критерии приёмки:**
- [ ] {критерий 1}
- [ ] {критерий 2}

**API:**
- `{method} /api/{route}` — {описание}

**UI (если есть):**
- Страница `/{route}` с таблицей/формой
- Ошибки показаны пользователю (тост/сообщение)

**Зависимости:** {сервисы, от которых зависит}
```

## Ветки и коммиты

- `master` — стабильный код; `feature/{service}-{feature}` — фича; `hotfix/{description}` — срочный фикс.
- Префиксы: `feat:` / `fix:` / `docs:` / `test:` / `refactor:` / `chore:` / `hotfix:` / `merge:`.
- Коммиты фаз — `phase N: {feature} ...`; слияние — `merge:`.
