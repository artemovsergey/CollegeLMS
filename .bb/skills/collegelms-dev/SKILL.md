---
name: collegelms-dev
description: "Рабочий цикл CollegeLMS: ветки, сборка, тесты, дизайн-гейты, E2E, формат и деплой. Использовать перед любой правкой кода или конфигурации репозитория."
---

# CollegeLMS: разработка

## Когда применять

Любая задача в репозитории CollegeLMS: фича, баг, UI, миграция, конфигурация.
Скилл задаёт обязательный порядок и точные команды; доменные правила — в
`collegelms-schedule` и `docs/spec/`.

## Порядок

1. **Ветка до первого изменения.** `git checkout -b fix/{краткое-имя}` или
   `feat/{...}`. `master` — только проверенное и задеплоенное.
2. **Правки малыми шагами.** Сначала тест, если поведение воспроизводимо.
3. **Проверки** (минимум — затронутое):
   - `dotnet build CollegeLMS.slnx`
   - `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}`
   - `cd CollegeLMS.Next && npm run build`
   - `cd CollegeLMS.Next && npm run check:design-system` — при UI-правках
   - `cd CollegeLMS.Next && npx playwright test {spec}` — при изменении сценария
   - `dotnet csharpier check .`
4. **Коммит** с префиксом `feat:` / `fix:` / `docs:` / `test:` / `refactor:` /
   `chore:` / `hotfix:` / `merge:`.
5. **Merge и push без отдельного согласования:**
   `git checkout master && git merge {ветка} && git push origin master`.
   Push запускает `deploy.yml` (quality-гейты → сборка → деплой).
6. **Проверь CI** после push: `gh run list --limit 3` или
   `bb pull-request`.

## Команды

| Задача | Команда |
|---|---|
| Build backend | `dotnet build CollegeLMS.slnx` |
| Тесты таргетно | `dotnet test CollegeLMS.slnx --filter FullyQualifiedName~{Name}` |
| Тесты полностью (CI) | `dotnet test CollegeLMS.slnx` |
| Миграция | `dotnet ef migrations add Add{Name} --project CollegeLMS.API -- --provider Npgsql` |
| Frontend dev | `cd CollegeLMS.Next && npm run dev` |
| Frontend build | `cd CollegeLMS.Next && npm run build` |
| Дизайн-гейты | `cd CollegeLMS.Next && npm run check:design-system` |
| E2E | `cd CollegeLMS.Next && npx playwright test {spec}` |
| Формат | `dotnet csharpier format .` / `dotnet csharpier check .` |

## Подводные камни

- **Прод — тот же хост, что и рабочий каталог.** Прод-каталог
  `/home/user1/CollegeLMS`; `docker compose` только оттуда. Из рабочего
  checkout compose пересоздаст боевые контейнеры.
- **Playwright — только через loadbalancer:** `baseURL: http://localhost`.
  Напрямую `:3000` пути `/api/*` отдают 404, `next dev` на 3001 без CORS.
- **Место на диске.** Перед сборкой Next нужно ≥3 ГБ. Чаще всего место съедает
  journald: `sudo journalctl --disk-usage`, чистка
  `sudo journalctl --vacuum-size=300M`.
- **Миграции и env — согласовывать.** Миграции БД, смена переменных
  окружения, `docker-compose.yml` и workflow ломают деплой; их согласуют
  отдельно.
- **Секреты.** `.env` не коммитить; в worktree он копируется через
  `.worktreeinclude`.
- **Формат C#** — CSharpier, не стандартный `dotnet format`.

## Готово, когда

- Ветка смержена в `master`, push выполнен, CI зелёный.
- Для UI — дизайн-гейты без ошибок.
- Для бага — есть тест, воспроизводящий исходную проблему.
- В отчёте указаны коммит, ветка и результат проверок.
