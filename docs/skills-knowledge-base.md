# База знаний: Skills & Инструменты

## Установленные в CollegeLMS

- **Impeccable v3.2.0** — анти-slop дизайн-язык, 23 команды (`polish`, `audit`, `bolder`, `quieter`...), brand/product регистры, 7 референс-файлов. [impeccable.style](https://impeccable.style)
- **Superpowers** — process skills (brainstorming, systematic-debugging, writing-plans, executing-plans, verification-before-completion, subagent-driven-development, dispatching-parallel-agents, yeet...)

---

## UI/UX Design Skills

### Anthropic Frontend Design (официальный)
- **277k+ установок**, официальный скилл Anthropic
- Запрещает Inter, Roboto, Arial, Space Grotesk («overused by AI»)
- Производит: смелую типографику, асимметричные макеты, намеренные анимации, визуальные детали (градиенты, текстуры, паттерны)
- Установка: `claude plugin add anthropic/frontend-design`

### Taste Skill (Leonxlnx) — 37.4k ★
- 3 регулируемых параметра: DESIGN_VARIANCE (1-10), MOTION_INTENSITY (1-10), VISUAL_DENSITY (1-10)
- 9 вариантов: taste-skill, gpt-taste, image-to-code, redesign, soft, minimalist, brutalist, stitch, output
- +3 imagegen скилла: imagegen-frontend-web, imagegen-frontend-mobile, brandkit
- Совместимость: Claude Code, Cursor, Codex, Windsurf, Antigravity, Copilot
- Сайт: [tasteskill.dev](https://tasteskill.dev)
- Установка: `npx skills add https://github.com/Leonxlnx/taste-skill`

### UI/UX Pro Max (nextlevelbuilder) — 88.7k ★
- Движок рассуждений, генерирует полную дизайн-систему под проект
- 67 UI-стилей (glassmorphism, brutalism, neumorphism, swiss...), 161 цветовая палитра, 57 комбинаций шрифтов, 99 UX-гайдлайнов
- Design System Generator: «fintech dashboard» → авто-подбор стиля/цветов/типографики
- Платформы: React, Next.js, Vue, Nuxt, Svelte, Flutter, SwiftUI, React Native
- Установка: `/plugin marketplace add nextlevelbuilder/ui-ux-pro-max-skill`

### Interface Design (Dammyjay93) — 5k ★
- Сохраняет дизайн-решения в `.interface-design/system.md` для согласованности между сессиями
- Нет стилистического дрейфа — кнопки, отступы, цвета остаются согласованными
- Предопределённые design directions: «Precision & Density», «Warmth & Approachability»
- Установка: `/plugin marketplace add Dammyjay93/interface-design`

### Designer Skills (Owl-Listener) — 1.5k ★
- 63 скилла, 27 команд, 8 плагинов, покрывает весь цикл дизайна:
  - design-research (10): персона, empathy map, journey map
  - design-systems (8): токены, компоненты, a11y, темизация
  - ux-strategy (8): конкурентный анализ, метрики
  - ui-design (9): сетки, цвета, типографика, data viz
  - interaction-design (7): микроанимации, state machines
  - prototyping-testing (8): юзабилити-тестирование, A/B
  - design-ops (7): critique, handoff, спринты
  - designer-toolkit (6): дизайн-рационал, кейсы, UX-writing
- Установка: `/plugin marketplace add Owl-Listener/designer-skills`

### Bencium UX Designer (bencium) — 273 ★
- Два варианта: **Innovative** (смелые творческие выборы) vs **Controlled** (согласованность и стандарты)
- 28k+ символов UX-материала: ACCESSIBILITY.md (WCAG 2.1/2.2), RESPONSIVE-DESIGN.md, MOTION-SPEC.md, DESIGN-SYSTEM-TEMPLATE.md
- Установка: `npx skills add bencium/bencium-marketplace -g --skill bencium-controlled-ux-designer`

### Frontend Design Pro Demo (claudekit) — 232 ★
- Витрина 11 дизайн-стилей с мастер-промптами и live-демо:
  1. Swiss Minimalism / 2. Neumorphism / 3. Glassmorphism / 4. Brutalism / 5. Claymorphism
  6. Aurora / Mesh Gradient / 7. Retro-Futurism Cyberpunk / 8. 3D Hyperrealism
  9. Vibrant Block Maximalist / 10. Dark OLED Luxury / 11. Organic Biomorphic
- Live-демо: [claudekit.github.io/frontend-design-pro-demo](https://claudekit.github.io/frontend-design-pro-demo)
- Установка: `claude plugin add claudekit/frontend-design-pro-demo`

### Figma to Code (Anthropic official)
- Дизайны Figma → production-ready код с верностью 1:1
- Установка: `npx skills add https://github.com/anthropics/skills --skill figma`

---

## Архитектура / Code Quality

### Vercel Agent Skills — 27.7k ★
- **Web Design Guidelines** — аудит против 100+ правил a11y, производительности и UX
- **React Best Practices** — 57 правил в 8 категориях для оптимального React
- **Composition Patterns** — compound components вместо boolean prop hell
- **React Native** — FlashList, GPU-ускоренные анимации
- Установка: `/plugin marketplace add vercel-labs/agent-skills`

---

## Аудит и Рецензирование

### Refactoring UI (LovroPodobnik) — 24 ★
- Аудит визуальной иерархии, отступов, теней, цветов
- Основан на системе Refactoring UI (Adam Wathan + Steve Schoger)
- Триггеры: «my UI looks off» / «fix the design» / «visual hierarchy»
- Репозиторий: [github.com/LovroPodobnik/refactoring-ui-skill](https://github.com/LovroPodobnik/refactoring-ui-skill)

### UX Heuristics (wondelai) — в составе 1.2k ★
- 10 эвристик Нильсена + принципы «Don't Make Me Think» Стива Круга
- Severity score для каждой проблемы
- Подробный отчёт: что сломано, почему, как исправить
- Триггеры: «audit this for usability» / «heuristic review» / «UX issues»

### Hooked UX (wondelai) — в составе 1.2k ★
- Hook Model Нира Эяля: Trigger → Action → Variable Reward → Investment
- Диагностика: где цикл удержания прерывается
- Триггеры: «users aren't coming back» / «improve retention» / «engagement»

### Design Sprint (wondelai) — в составе 1.2k ★
- Google Ventures Design Sprint: 5 фаз в одном потоке
  - День 1 — Understand / День 2 — Sketch / День 3 — Decide
  - День 4 — Prototype / День 5 — Test
- Триггеры: «run a design sprint» / «validate this idea»

### Внимание к безопасности (ToxicSkills от Snyk)
- 36% протестированных скиллов содержат prompt injection
- 1467 вредоносных пейлоадов найдено в экосистеме
- Всегда просматривать SKILL.md перед установкой из непроверенных источников

---

## Mobile

### iOS HIG Design (rshankras) — 395 ★
- Apple Human Interface Guidelines: safe areas, Dynamic Island, tab bars, modals
- Dark Mode с semantic colors, Dynamic Type, VoiceOver
- Триггеры: «iOS app» / «iPhone interface» / «HIG compliance»
- Репозиторий: [github.com/rshankras/claude-code-apple-skills](https://github.com/rshankras/claude-code-apple-skills)

---

## SEO / GEO

### seo-geo-claude-skills (aaron-he-zhu) — 2.1k ★
- 20 скиллов, 5 команд, фреймворки CORE-EEAT (80 критериев) + CITE (40 критериев)
- Категории:
  - **Research**: keyword-research, competitor-analysis, serp-analysis, content-gap-analysis
  - **Build**: seo-content-writer, geo-content-optimizer (AI-цитирование), meta-tags-optimizer, schema-markup-generator
  - **Optimize**: on-page-seo-auditor, technical-seo-checker, internal-linking-optimizer, content-refresher
  - **Monitor**: rank-tracker, backlink-analyzer, performance-reporter, alert-manager
  - **Cross-cutting**: content-quality-auditor, domain-authority-auditor, entity-optimizer, memory-management
- Команды:
  - `/aaron-seo-geo:auto` — авто-инференс цели
  - `/aaron-seo-geo:research` — keyword demand, SERP, конкуренты
  - `/aaron-seo-geo:create` — brief, write, series, refresh, publish
  - `/aaron-seo-geo:audit` — on-page + EEAT + tech + AI visibility
  - `/aaron-seo-geo:track` — rankings, alerts, reports
- Установка: `/plugin marketplace add aaron-he-zhu/seo-geo-claude-skills`

---

## Инфраструктура Claude Code

### gstack (garrytan) — 113k ★
- Коллекция 23 скиллов, превращающая Claude Code в виртуальную инженерную команду
- Цикл: Think → Plan → Build → Review → Test → Ship → Reflect
- Ключевые скиллы:

| Скилл | Роль | Что делает |
|-------|------|-----------|
| `/office-hours` | YC Office Hours | 6 forcing questions до написания кода |
| `/plan-ceo-review` | CEO | Scope review (expansion/selective/hold/reduction) |
| `/plan-eng-review` | Eng Manager | Архитектурный ревью |
| `/plan-design-review` | Senior Designer | Оценка дизайна 0-10, анти-AI-slop |
| `/design-review` | Designer Who Codes | Аудит + атомарные фиксы с before/after |
| `/design-consultation` | Design Explorer | Множественные направления, итерации |
| `/design-html` | Design Engineer | Production HTML из mockup'ов |
| `/review` | Code Reviewer | Code review |
| `/qa` | QA Lead | Браузерное тестирование |
| `/cso` | Chief Security Officer | OWASP Top 10 + STRIDE |
| `/ship` | Release Engineer | PR + деплой |

- Установка:
  ```
  git clone --single-branch --depth 1 https://github.com/garrytan/gstack.git ~/.claude/skills/gstack
  cd ~/.claude/skills/gstack && ./setup
  ```

### Caveman (JuliusBrussee)
- Сжимает ответы агента, убирая «воду», сохраняя код/команды/ошибки
- Экономия токенов: ~65% в среднем
- Режимы: `lite`, `full`, `ultra`, `wenyan`
- Команды:
  - `/caveman-commit` — conventional commit, ≤50 символов
  - `/caveman-review` — однострочные PR-комментарии
  - `/caveman-stats` — статистика использования токенов
  - `/caveman-compress` — сжатие CLAUDE.md на ~46%
- Репозиторий: [github.com/JuliusBrussee/caveman](https://github.com/JuliusBrussee/caveman)

### Canvas Design (Anthropic official)
- Визуальный canvas в браузере: диаграммы, вайрфреймы, схемы
- Компактный DSL (3-5x меньше токенов чем JSON)
- Авто-лейаут через row/stack контейнеры
- Экспорт PNG/PDF — для постеров и графики
- Установка: `npx skills add https://github.com/anthropics/skills --skill canvas-design`

### Context Mode
- Фильтрует shell-шум перед попаданием в контекст
- Ведёт лог изменений, задач, промптов
- Восстанавливает состояние сессии после сброса контекста

### Emil Kowalski Design (анимации)
- Продвинутый motion design: переходы страниц, микроинтеракции
- Основан на технических статьях emilkowal.ski
- Установка: `npx skills add emilkowalski/skill`

### App Store Screenshots (ParthJadhav)
- Скриншоты для App Store: device-mockups, экспорт во всех разрешениях Apple
- Установка: `npx skills add ParthJadhav/app-store-screenshots`

### Skill Creator (Anthropic official)
- Мета-скилл: создаёт персонализированные скиллы под проект
- Установка: `npx skills add https://github.com/anthropics/skills --skill skill-creator`

### Theme Factory (Anthropic official) — 10 тем
- 10 курируемых тем с цветовыми палитрами и шрифтами
- Каждая тема: CSS-переменные, типографические шкалы, a11y-протестировано
- Установка: `npx skills add https://github.com/anthropics/skills --skill theme-factory`

### Brand Guidelines (Anthropic official)
- Автоматическое применение цветов, шрифтов, отступов и тона бренда
- Установка: `npx skills add https://github.com/anthropics/skills --skill brand-guidelines`

---

## Ресурсы и Маркетплейсы

### Сайты
- [impeccable.style](https://impeccable.style) — дизайн-язык (уже используем)
- [tasteskill.dev](https://tasteskill.dev) — Taste Skill
- [layers.jamiemill.com](https://layers.jamiemill.com) — визуальные паттерны
- [app.superdesign.dev](https://app.superdesign.dev) — визуальные паттерны
- [refactoring-ui-plugin](https://github.com/gnurio/refactoring-ui-plugin) — визуальный аудит

### Маркетплейсы скиллов
- [github.com/wondelai/skills](https://github.com/wondelai/skills) — UX Heuristics, Hooked UX, Design Sprint
- [github.com/anthropics/skills](https://github.com/anthropics/skills) — официальные скиллы Anthropic
- [smithery.ai](https://smithery.ai) — поиск и фильтрация скиллов
- [BehiSecc/awesome-claude-skills](https://github.com/BehiSecc/awesome-claude-skills) — коллекция сообщества

### Статья
- [18 лучших скиллов Claude Code для UI/UX дизайна](https://pasqualepillitteri.it/ru/news/888/claude-code-18-luchshikh-skill-dlya-ui-ux-dizayna) — Pasquale Pillitteri, апрель 2026

---

## Приоритет для CollegeLMS

| Этап | Что установить | Зачем |
|------|---------------|-------|
| **Уже есть** | Impeccable, Superpowers | Дизайн-язык + процессные скиллы |
| **Сейчас** | — | Текущий дизайн устраивает |
| **При разработке новых страниц** | Anthropic Frontend Design | Базовая эстетика |
| **При ревью** | gstack (`/plan-design-review`, `/design-review`) | План-ревью + аудит живого UI |
| **Перед деплоем** | Vercel Web Design Guidelines | A11y + производительность |
| **Публичный запуск** | seo-geo-claude-skills | SEO + GEO для контента |
| **Для экономии** | Caveman | -65% токенов |
| **Для диаграмм/документации** | Canvas Design | Визуальные схемы |
