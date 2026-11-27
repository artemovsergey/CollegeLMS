# Очистка агентского пайплайна CollegeLMS — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Сократить обязательный инструментарий агентов и добавить независимое ревью в чистом контексте перед merge, не добавляя новых компонентов пайплайна.

**Architecture:** Правятся три существующих markdown-документа: `feature-workflow` (фазы 3 и 6, таблица гейтов), `collegelms-roles` (минимум скиллов), корневой `AGENTS.md` (ссылки вместо дублей). Код, workflows, compose, env, роли и модели не меняются.

**Tech Stack:** Markdown, git, существующие скиллы репозитория; проверки — `git diff --check` и при наличии времени `dotnet build CollegeLMS.slnx` / `npm run check:design-system`.

**Spec:** `docs/superpowers/specs/2026-11-27-agent-pipeline-cleanup-design.md`

## Global Constraints

- Ветка `refactor/agent-pipeline-cleanup` создана до правок; прямые коммиты в `master` запрещены.
- Не трогать чужие неотслеживаемые файлы: `docs/superpowers/specs/2026-11-16-group-exam-day-design.md`.
- Не менять код продукта, `.github/workflows`, compose и переменные окружения.
- Не менять роли, модели субагентов и порядок завершения работы (коммит → merge → push).
- Не добавлять новый «скилл пайплайна» или агента-оркестратора.
- Проверки остаются на границах фаз; полный прогон тестов — в CI.
- Стейджить только файлы задачи; не использовать `git add -A`.
- Коммит — в рабочую ветку; merge/push — по правилам проекта (в этой сессии без отдельного запроса не делать).

## Review Focus

- Секреты в diff: ревьюер получает только `git diff master...HEAD`, спецификацию и чек-лист — проверка закреплена в задаче 1, шаг ревью фазы 6.
- Риск миграций/деплоя: те же пять пунктов проверки ревьюера в задаче 1.
- Потеря скиллов при сокращении обязательного минимума: задача 2 переносит их в «по необходимости», не удаляя.
- Утрата фактов при сокращении `AGENTS.md`: задача 3 проверяет наличие фактов в целевых скиллах до удаления разделов.
- Непреднамеренное изменение workflows/compose/кода: финальная проверка `git diff --name-only` в задаче 3.

---

### Task 1: Ревью в чистом контексте и скиллы фазы 3 в `feature-workflow`

**Files:**
- Modify: `.opencode/skills/feature-workflow/SKILL.md` (фаза 3 — строки 90–106; фаза 6 — строки 136–150; таблица гейтов — строки 154–162).
- Commit: `docs/superpowers/specs/2026-11-27-agent-pipeline-cleanup-design.md` (уже создан, не закоммичен).

**Interfaces:**
- Consumes: `requesting-code-review` — источник чек-листа для reviewer-субагента; `quality.yml` — дизайн-гейты уже там.
- Produces: обновлённый скилл `feature-workflow`, гейт `G6`, закоммиченный дизайн-документ.

- [ ] **Step 1: В фазе 3 заменить список скиллов и убрать polish/audit**

В `.opencode/skills/feature-workflow/SKILL.md` заменить:

```markdown
Load: `impeccable`, `design-system`, `nextjs-page`
```

на:

```markdown
Load: `design-system`, `nextjs-page`, `modern-web-guidance`
```

Удалить строку `- Дизайн-бриф (shape) → реализация (craft) → polish → audit (`impeccable`).` и оставить остальные пункты фазы 3 без изменений.

- [ ] **Step 2: Добавить подшаг ревью в фазу 6**

После текущих пунктов `verification-before-completion` и `requesting-code-review`, до подшага «Слияние и пуш», вставить подшаг:

```markdown
- Независимое ревью в чистом контексте: один reviewer-субагент в **новой сессии**.
  Передать только `git diff master...HEAD`, спецификацию и чек-лист
  `requesting-code-review`; не передавать историю переписки разработки.
  Субагент только читает: проверяет соответствие спецификации, качество тестов,
  конвенции `AGENTS.md`, секреты в diff, риск миграций и деплоя; отдаёт отчёт
  по тяжести `blocker`/`major`/`minor` без правки файлов и без git-операций.
  При `blocker` или `major` архитектор исправляет замечания и повторяет ревью.
```

Оставить слияние, пуш и проверку CI без изменений.

- [ ] **Step 3: Добавить гейт G6 в таблицу приёмки**

В таблицу «Гейты приёмки» после строки `G5` добавить:

```markdown
| **G6** | ревью в чистом контексте: нет blocker/major | reviewer-субагент | 6 |
```

- [ ] **Step 4: Проверить, что список changed-файлов ограничен задачей**

Run: `git diff --name-only`
Expected: только `.opencode/skills/feature-workflow/SKILL.md` и дизайн-документ (чужой неотслеживаемый документ `2026-11-16-group-exam-day-design.md` в выводе `git diff` не появляется).

- [ ] **Step 5: Commit**

```bash
git add .opencode/skills/feature-workflow/SKILL.md docs/superpowers/specs/2026-11-27-agent-pipeline-cleanup-design.md
git commit -m "refactor: clean feature-workflow phases and design doc"
```

---

### Task 2: Минимум скиллов в `collegelms-roles`

**Files:**
- Modify: `.opencode/skills/collegelms-roles/SKILL.md` (раздел «Обязательные скиллы по ролям», строки 27–38).

**Interfaces:**
- Consumes: правило `modern-web-guidance` из корневого `AGENTS.md`; список скиллов из задачи 1.
- Produces: сокращённый обязательный минимум и пункт «по необходимости» без удаления скиллов.

- [ ] **Step 1: Заменить раздел обязательных скиллов**

Заменить весь раздел `## Обязательные скиллы по ролям` на:

```markdown
## Обязательные скиллы по ролям

- Backend: `dotnet-entity`, `dotnet-endpoint`, `fluent-validation`, `swagger-docs`.
- Frontend: `design-system`, `nextjs-page`; `modern-web-guidance` — по правилу
  `AGENTS.md` перед правками HTML/CSS/клиентского JS.
- Tester: `dotnet-test`, `test-driven-development`, `playwright`.
- DevOps: `docker-compose-dev`, `vps-deploy`, `cicd-pipeline`.
- Docs: по задаче (`course-materials` и скиллы экспорта — при необходимости).

По необходимости (не обязательно на старте, но доступны): `dotnet-webapi`,
`result-pattern`, `aspnet-core`, `efcore-patterns`, `seed-data`, `cors-security`,
`jwt-auth`, `impeccable`, `frontend-design`, `shadcn`,
`vercel-react-best-practices`, `web-design-guidelines`, `systematic-debugging`,
`playwright-interactive`, `gh-fix-ci`, `office-docx`, `office-export-pdf`,
`office-render`. Скиллы не удаляются: обязательный минимум сокращён, а не список.
```

- [ ] **Step 2: Проверить, что каждый исключённый скилл остался в документе**

Run: `grep -o 'dotnet-webapi\|impeccable\|gh-fix-ci\|aspnet-core' .opencode/skills/collegelms-roles/SKILL.md | sort -u`
Expected: все четыре имени присутствуют.

- [ ] **Step 3: Commit**

```bash
git add .opencode/skills/collegelms-roles/SKILL.md
git commit -m "refactor: trim mandatory skills in collegelms-roles"
```

---

### Task 3: Короткий корневой `AGENTS.md`

**Files:**
- Modify: `AGENTS.md` (разделы «Прод и стенд» и «Агенты и модели» со вложенным DSH — строки 48–71).

**Interfaces:**
- Consumes: факты из `.opencode/skills/vps-bb-status/SKILL.md`, `.opencode/skills/dsh-environment/SKILL.md`, `.bb/skills/collegelms-dev/SKILL.md` — проверены до удаления.
- Produces: `AGENTS.md` без дублирования деталей прод-каталога, MCP и Playwright.

- [ ] **Step 1: Подтвердить, что факты есть в целевых скиллах**

Run:

```bash
grep -n '3 ГБ\|journalctl\|/home/user1/CollegeLMS' .opencode/skills/vps-bb-status/SKILL.md
grep -n 'cordis.patch.yml\|77 инструмент' .opencode/skills/dsh-environment/SKILL.md
grep -n ':3000\|:3001\|baseURL' .bb/skills/collegelms-dev/SKILL.md
```

Expected: совпадения есть в каждом файле; если какого-то факта нет — оставить его в `AGENTS.md`.

- [ ] **Step 2: Заменить два раздела ссылками**

Удалить разделы `## Прод и стенд` и `## Агенты и модели` вместе с подразделами и вставить на их место:

```markdown
## Прод и стенд, агенты и среда

- Прод, docker-команды, порог диска и логи — `.opencode/skills/vps-bb-status/SKILL.md`.
- Playwright: `baseURL: http://localhost`, `:3000` отдаёт 404 на `/api/*`,
  `next dev` поднимается на 3001 — `.bb/skills/collegelms-dev/SKILL.md`.
- Модели, MCP, DSH-профиль и секреты агентской среды —
  `.opencode/skills/dsh-environment/SKILL.md` и
  `.opencode/skills/collegelms-roles/SKILL.md`.
```

Оставить без изменений разделы «Пути», «Секреты», «Ветки», «Команды», «Скиллы проекта», «Frontend», «Конвенции .NET / EF Core».

- [ ] **Step 3: Проверить границы правок и формат**

Run: `git diff --check && git diff --stat`
Expected: без ошибок; в diff только `AGENTS.md` (плюс уже закоммиченные файлы двух предыдущих задач не меняются).

- [ ] **Step 4: Commit**

```bash
git add AGENTS.md
git commit -m "docs: shorten root AGENTS.md to pointers"
```

---

### Финальная верификация (после всех трёх задач)

- [ ] `git diff --check` — без вывода.
- [ ] `git status --short` — только ожидаемые файлы; чужие неотслеживаемые файлы не задействованы.
- [ ] `git log --oneline -5` — три новых коммита в ветке `refactor/agent-pipeline-cleanup`.
- [ ] По правилам проекта при наличии времени: `dotnet build CollegeLMS.slnx` и `cd CollegeLMS.Next && npm run check:design-system` (markdown-правки не требуют, но подтверждают, что гейты не сломаны).
- [ ] Merge/push — только после отдельного запроса пользователя.

## Self-review

- Spec coverage: все три решённых блока дизайна (чистое ревью, минимум скиллов, короткий AGENTS.md) покрыты задачами 1–3; не-цели соблюдены проверкой changed-файлов.
- Step scan: каждый шаг — одно конкретное действие с проверяемым результатом; подшаг ревью записан дословно, чтобы не оставить выбора формулировок.
- Type consistency: имена скиллов и гейт `G6` используются одинаково в задаче 1 и в финальной верификации.
- Review Focus: пять пунктов закреплены в шагах задач 1 и 3.
- Proportion: план короче объёма правок за счёт дословных вставок только там, где формулировка фиксируется дизайном.
