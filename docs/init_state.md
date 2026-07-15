# Исходное состояние проекта — старт с новой моделью

Два варианта:

- **Вариант A: Полный** — с OpenCode-инфраструктурой. Тестирует связку модель + обвязка.
- **Вариант B: Минимальный** — только спецификация и данные. Тестирует чистую модель.

---

## Вариант A: Полный (с обвязкой)

### Оставлено

```
docs/                   # ТЗ, User Stories, UML-диаграммы, дизайн-спеки
import/                 # WP dump, логотип, примеры расписания
scripts/                # Скрипты импорта WP + деплоя
AGENTS.md               # Инструкции для агента
PRODUCT.md              # Описание продукта
.env                    # Переменные окружения (БД, Redis, JWT, порты)

# OpenCode-инфраструктура
opencode.json           # Конфиг OpenCode (MCP, плагины)
.opencode/              # SDK OpenCode: навыки, команды
.agents/                # Навыки для фронтенд-дизайна
.superpowers/           # Плагин Superpowers
.mimocode/              # Зависимость notifier-плагина
.github/hooks/          # Хуки impeccable
.github/skills/         # Навыки impeccable

.git/                   # Git-репозиторий
```

### Удалено

```
CollegeLMS.API/         # C# backend
CollegeLMS.Tests/       # Тесты
CollegeLMS.Next/        # Next.js фронтенд
CollegeLMS.TelegramBot/  # Telegram-бот (бывший agentbridge)
uploads/                # Пустая папка
docker-compose.yml      # Инфраструктура стека
loadbalancer/            # Конфиг loadbalancer
.github/workflows/      # CI/CD
.config/                # dotnet-tools.json
.playwright-mcp/        # Логи сессий
.playwright-cli/        # Логи сессий
.opencode/plans/        # Старые планы
README.md               # Документация (устареет)
DESIGN.md               # Дизайн-документация (устареет)
CollegeLMS.slnx         # Файл решения
.env.example            # Шаблон окружения
.dockerignore           # Docker-игнор
.gitignore              # Git-игнор
.csharpier              # Форматтер
```

---

## Вариант B: Минимальный (чистая оценка модели)

Убрано всё, что может «подтаскивать» модель: AGENTS.md с workflow, навыки OpenCode, плагины, MCP-серверы, UML-диаграммы, готовые дизайн-спеки. Остаётся только постановка задачи и исходные данные.

### Оставлено

```
docs/spec/task.md       # ТЗ — основной документ
docs/spec/userstories.md  # User Stories — критерии приёмки
import/                 # WP dump, логотип, примеры расписания
scripts/                # Скрипты импорта WP
PRODUCT.md              # Краткое описание продукта
.env                    # Переменные окружения (БД, Redis, JWT, порты)
.git/                   # Git-репозиторий
```

### Удалено

```
CollegeLMS.API/         # C# backend
CollegeLMS.Tests/       # Тесты
CollegeLMS.Next/        # Next.js фронтенд
CollegeLMS.TelegramBot/  # Telegram-бот (бывший agentbridge)
uploads/                # Пустая папка
docs/diagrams/          # UML-диаграммы (подсказывают архитектуру)
docs/superpowers/specs/ # Готовые дизайн-спеки (подсказывают решения)
docs/superpowers/plans/ # Планы реализации
docs/network-recovery-plan.md
docs/skills-knowledge-base.md
AGENTS.md               # Инструкции для агента (скрывают слабости модели)
opencode.json           # MCP-серверы, плагины (внешняя помощь)
.opencode/              # Навыки OpenCode
.agents/                # Навыки фронтенд-дизайна
.superpowers/           # Плагин
.mimocode/              # Плагин
.github/                # Хуки, навыки, CI/CD
docker-compose.yml      # Инфраструктура стека
loadbalancer/            # Конфиг loadbalancer
.config/                # dotnet-tools.json
.playwright-mcp/        # Логи
.playwright-cli/        # Логи
README.md, DESIGN.md    # Документация
CollegeLMS.slnx, .env.example
.dockerignore, .gitignore, .csharpier
```

### Что проверять

| Критерий | Как замерить |
|----------|-------------|
| Качество архитектуры | Сама ли разбила на сервисы/модули? |
| Полнота реализации | Все ли 5 базовых сервисов реализованы? |
| Самостоятельность | Сколько итераций нужно на исправление ошибок? |
| Качество кода | Читаемость, тесты, обработка ошибок |
| Скорость | Время до первого `dotnet build` |

---

## Замечания

- `scripts/` оставлены в обоих вариантах — содержат утилиты импорта WordPress
- `.env` оставлен как референс — новая модель решит, какие переменные использовать
- В варианте B модель пишет всё с нуля: архитектуру, стек, код, тесты, инфраструктуру
- Рекомендация: сначала вариант B (чистая оценка), потом вариант A (с обвязкой) — разница покажет реальную ценность инфраструктуры
