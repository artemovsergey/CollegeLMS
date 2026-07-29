---
name: CollegeLMS
description: Clean, modern educational management system
colors:
  primary: "#24386a"
  primary-hover: "#1c2c54"
  primary-light: "#4a5a7a"
  primary-lighter: "#7a8aa5"
  secondary: "#5b6a90"
  accent-green: "#2f8733"
  cream: "#efead7"
  neutral-bg: "#f5f7fa"
  neutral-fg: "#111827"
  neutral-muted: "#4a5a7a"
  border: "#c9ceda"
  destructive: "#dc2626"
  destructive-muted: "#fef2f2"
  white: "#ffffff"
  muted: "#eef0f4"
  muted-fg: "#4a5a7a"
typography:
  body:
    fontFamily: '"Inter", ui-sans-serif, system-ui, sans-serif'
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: '"Inter", ui-sans-serif, system-ui, sans-serif'
    fontSize: "0.8125rem"
    fontWeight: 500
    lineHeight: 1.25
    letterSpacing: "0.01em"
rounded:
  sm: "6px"
  md: "8px"
  lg: "12px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "10px 20px"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
    rounded: "{rounded.md}"
    padding: "10px 20px"
  card:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.lg}"
    padding: "{spacing.lg}"
  input:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sm} {spacing.md}"
    borderColor: "{colors.border}"
  table-header:
    backgroundColor: "{colors.muted}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
  badge-admin:
    backgroundColor: "{colors.muted}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
  badge-teacher:
    backgroundColor: "{colors.secondary}"
    textColor: "#ffffff"
    rounded: "{rounded.sm}"
  badge-student:
    backgroundColor: "{colors.muted}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
---

# Design System: CollegeLMS

## 1. Overview

**Creative North Star: "Institutional Clarity"**

CollegeLMS — единая цифровая среда Ставропольского колледжа связи. Сайт сочетает официальный публичный портал (информация об ОО, специальности, приём) и закрытую LMS.

Интерфейс строится на **Deep Navy** (`#24386a`) как основном цвете бренда — он идёт из логотипа колледжа и задаёт тон официальности и надёжности. Акцентный College Blue (`#568edd`) используется в тёмной теме; в светлой теме акцент — сам Deep Navy. Палитра основана на fill-цветах SVG логотипа, никаких «подходящих оттенков» из головы.

Система отвергает декоративный мусор: градиентные тексты, glassmorphism, side-stripe границы. Иерархия строится на воздухе, типографской шкале и сдержанном использовании брендового цвета.

**Ключевые характеристики:**
- Primary Deep Navy (`#24386a`), extracted from the logo text. Используется для кнопок, ссылок, заголовков
- Акцентный цвет (`--accent`) совпадает с primary в светлой теме, меняется на College Blue (`#568edd`) в тёмной
- 5 цветовых пресетов: indigo, blue, sapphire, plum, green
- Body text: `#111827` (gray-900) — хороший контраст на белом
- Inter — шрифт для всего интерфейса
- Mobile-first: формы и таблицы работают на 320px

## 2. Colors

### Primary
- **Deep Navy** (#24386a / oklch(0.35 0.091 266)): Основной цвет бренда. Кнопки, ссылки, заголовки, футер, хедер. Извлечён из текста логотипа.
- **Deep Navy Hover** (#1c2c54): Ховеры и активные состояния.
- **Muted Blue** (#4a5a7a): Muted-foreground, второй уровень текста.
- **Lighter Blue** (#7a8aa5): Placeholder-текст, менее важные элементы.

### Secondary
- **Steel Blue** (#5b6a90): Secondary кнопки, неактивные badge.
- **Leaf Green** (#2f8733): Зелёный из лавровых ветвей логотипа. Только для success-состояний.
- **Warm Cream** (#efead7): Кремовый из тарелки антенны. Светлые баннеры, информационные callout.

### Neutral
- **White** (#ffffff): Фон страницы, карточки, диалоги.
- **Muted** (#eef0f4): Фон секций, table header, второстепенные поверхности.
- **Muted Foreground** (#4a5a7a): Secondary текст, метаданные.
- **Border** (#c9ceda): Границы элементов, разделители.
- **Foreground** (#111827 / gray-900): Основной текст. Не чисто-чёрный.

### Semantic
- **Destructive** (#dc2626): Ошибки, деструктивные действия.
- **Destructive Muted** (#fef2f2): Фон ошибок.

### Dark Mode
- **Dark BG** (#111827): Фон в тёмной теме
- **Dark Card** (#1f2937): Карточки в тёмной теме
- **Dark Accent** (#568edd): College Blue становится акцентным цветом
- **Dark Muted FG** (#929cb5): Muted текст в тёмной теме

### Цветовые пресеты
Пользователь может выбрать один из 5 пресетов, которые меняют `--accent`:
- **Indigo** (#24386a) — Deep Navy (по умолчанию)
- **Blue** (#1e4d8c) — Medium Blue
- **Sapphire** (#3b5998) — Sapphire
- **Plum** (#4a4e6b) — Plum
- **Green** (#2d5a4a) — Forest Green

### Named Rules
**The Deep Navy Rule.** Deep Navy — единственный цвет интерактивных элементов в светлой теме. ≤15% экрана.

**The Dark Accent Rule.** В тёмной теме акцент переключается на College Blue (`#568edd`) для лучшей читаемости на тёмном фоне.

**The Cold Steel Rule.** Нейтральные цвета — из металлических элементов логотипа (chroma ≤ 0.03, hue 131–291°). Чистый серый (#808080) запрещён.

## 3. Typography

**Body Font:** Inter, современный гуманистический sans-serif с отличной поддержкой кириллицы. Широкие apertures, чёткое различие похожих глифов.

**Display Font:** Inter (то же семейство, weight 500–700).

**Monospace Font:** JetBrains Mono для кода.

### Hierarchy
- **Headline** (600, 1.5rem/24px, 1.3): Page titles (`h1`). Top-level sections.
- **Title** (600, 1.125rem/18px, 1.4): Section headers (`h2`), dialog titles, card headers.
- **Body** (400, 1rem/16px, 1.6): Primary reading text. Cap line length at 70ch.
- **Label** (500, 0.8125rem/13px, 1.25, letter-spacing 0.01em): Form labels, table headers, button text, metadata.
- **Caption** (400, 0.75rem/12px, 1.4): Helper text, timestamps, secondary metadata.

### Named Rules
**The Weight-Only Scale.** Hierarchy is expressed through weight (400 → 500 → 600) and size, not through font family switches or letter-spacing theatrics. Inter provides clear weight contrast at 400/500/600/700.

## 4. Elevation

Flat by default. Depth comes from tonal layering (background → card → border), not from shadows.

Cards, dialogs, and dropdowns sit on the surface without vertical lift. The card background (`#ffffff`) against the page background provides enough separation. No box-shadows on cards.

**The only shadow** is on the Dialog overlay — a soft ambient shadow on the content panel and a dark backdrop at 30% opacity behind it.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest. The tonal contrast between page and card backgrounds provides depth. Shadows appear only for modals and dropdown menus.

## 5. Components

### Buttons
- **Shape:** Gently rounded corners (8px)
- **Primary:** Deep Navy (`#24386a`) background, white text, 10px 20px padding. Hover shifts to Deep Navy Hover (`#1c2c54`).
- **Ghost:** Transparent background, Deep Navy text. Hover gets Muted background.
- **Destructive:** Red Alert background.
- **States:** All buttons use a 150ms ease-out transition on background-color and box-shadow. Focus-visible ring uses Deep Navy at 3px offset.

### Cards
- **Shape:** Generous rounded corners (12px)
- **Background:** White
- **Shadow:** None (see Flat-By-Default Rule)
- **Border:** None (tonal separation from page background is sufficient)
- **Internal Padding:** 24px (lg)

### Inputs & Fields
- **Style:** 1px Cool Border, White background, 6px radius
- **Focus:** Border switches to College Blue, ring at 3px with 30% opacity College Blue
- **Error:** Border switches to Rose Alert
- **Disabled:** Blue Tint background, 50% opacity Slate text
- **Label:** Above the field, Label type scale, 8px gap below

### Tables
- **Style:** Clean, minimal borders. Column headers use Label weight.
- **Header Row:** Blue Tint background, Deep Navy Label text.
- **Rows:** Alternating is not needed — zebra striping is visual noise. Use subtle 1px bottom border (Cool Border) between rows.
- **Hover Row:** Blue Tint at 50% opacity.
- **Radius:** 8px on the table container (via parent wrapper).

### Badges
- **Shape:** 6px radius, compact padding (4px 10px)
- **Admin:** Blue Tint background, College Blue text
- **Teacher:** Crest Purple background, white text
- **Student:** Page Blue background, Deep Navy text

### Navigation (Header)
- **Style:** Clean bar, no background fill. Just logo/title left, user info right.
- **Active state:** No underline or indicator needed — the page title is sufficient wayfinding.
- **Mobile:** Collapse user email to just the badge.

### Dialogs
- **Overlay:** `rgba(36,56,106,0.35)` dark backdrop (Deep Navy tinted)
- **Content:** White, 12px radius, 24px padding, soft ambient shadow
- **Title:** Title type scale
- **Actions:** Right-aligned, Cancel (ghost) then Save (primary)

### Skeleton (Loading Placeholder)

- **Shape:** 6px border-radius, no border
- **Colors:** Blue Tint (`#e4edf8`) background; pulsing via a 1.5s ease-in-out opacity animation from 100% to 40%
- **Sizes:** Match the element they replace — card skeletons use card dimensions, text skeletons use `h-4`, avatar skeletons are circular
- **CSS:** `@keyframes skeleton-pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }`
- **Action:** Loading skeleton ONLY. Never use for empty states or error states — those use empty/error components instead

### Toast (Sonner)

- **Library:** Sonner (`sonner` npm package) — single source of truth for toasts
- **Position:** `position="bottom-right"` (default)
- **Duration:** Success/Info — 3s; Error — 5s
- **Colors:**
  - **Success:** Leaf Green (`#2f8733`) icon + title, Green Tint (`#e8f5e9`) background
  - **Error:** Rose Alert (`#c43e3e`) icon + title, Rose Tint (`#f8e8e8`) background
  - **Info:** College Blue (`#568edd`) icon, Blue Tint (`#e4edf8`) background
- **Content:** Title only for simple messages; title + description for detail messages
- **Dismiss:** Always dismissible by clicking. Auto-dismiss according to duration.
- **Action button:** Optional; use `action` prop for undo/retry (ghost style, small)

### Cards
- **Shape:** Generous rounded corners (12px)
- **Background:** White
- **Shadow:** None (see Flat-By-Default Rule)
- **Border:** 1px solid Border (`#c9ceda`)
- **Internal Padding:** 24px (lg)

### Inputs & Fields
- **Style:** 1px Border, White background, 6px radius
- **Focus:** Border switches to Deep Navy, ring at 3px with 30% opacity Deep Navy
- **Error:** Border switches to Destructive
- **Disabled:** Muted background, 50% opacity text
- **Label:** Above the field, Label type scale, 8px gap below

### Tables
- **Style:** Clean, minimal borders. Column headers use Label weight.
- **Header Row:** Muted background, Deep Navy Label text.
- **Rows:** Subtle 1px bottom border between rows.
- **Hover Row:** Muted at 50% opacity.
- **Radius:** 8px on the table container (via parent wrapper).

### Badges
- **Shape:** 6px radius, compact padding (4px 10px)
- **Admin:** Muted background, Deep Navy text
- **Teacher:** Steel Blue background, white text
- **Student:** Muted background, Foreground text

### Navigation (Header)
- **Style:** Deep Navy background, white text. Logo left, nav center, toggles right.
- **Mobile:** Hamburger menu on the right.

### Dialogs
- **Overlay:** `rgba(0,0,0,0.3)` dark backdrop
- **Content:** White, 12px radius, 24px padding, soft ambient shadow
- **Title:** Title type scale
- **Actions:** Right-aligned, Cancel (ghost) then Save (primary)

### Skeleton (Loading Placeholder)
- **Shape:** 6px border-radius, no border
- **Colors:** Muted background; pulsing via a 1.5s ease-in-out opacity animation from 100% to 40%
- **Sizes:** Match the element they replace
- **CSS:** `@keyframes skeleton-pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }`

### Toast (Sonner)
- **Library:** Sonner — single source of truth for toasts
- **Position:** `position="bottom-right"` (default)
- **Duration:** Success/Info — 3s; Error — 5s
- **Colors:**
  - **Success:** Leaf Green (`#2f8733`) icon + title
  - **Error:** Destructive (`#dc2626`) icon + title
  - **Info:** Deep Navy (`#24386a`) icon
- **Dismiss:** Always dismissible by clicking. Auto-dismiss according to duration.

## 6. Icons

**Library:** Lucide React — single source of truth. No icon fonts, no second library, no raw SVGs for common UI.

**Philosophy:** Icons are wayfinding aids, not decoration. Every icon earns its place by reducing cognitive load — a trash icon is faster to scan than "Удалить". If an icon doesn't speed up recognition, omit it.

### Feature → Icon Map

| Section / Action | Lucide Name | Notes |
|-----------------|-------------|-------|
| Dashboard | `LayoutDashboard` | |
| Schedule | `CalendarDays` | |
| Courses | `BookOpen` | |
| Users / People | `Users` | |
| Profile | `UserCircle` | |
| Materials | `FileText` | |
| Upload | `Upload` | |
| Tests / Exams | `ClipboardCheck` | |
| Journal / Grades | `NotebookPen` | |
| Notifications | `Bell` | |
| Messages | `MessageSquare` | |
| Reports | `BarChart3` | |
| Settings | `Settings` | |
| Search | `Search` | |
| Logout | `LogOut` | |
| Add / Create | `Plus` | |
| Edit | `Pencil` | |
| Delete | `Trash2` | |
| Filter | `Filter` | |
| Sort | `ArrowUpDown` | |
| Download | `Download` | |
| Print | `Printer` | |
| Close / Dismiss | `X` | |
| Chevron (expand) | `ChevronDown` | Collapse: `ChevronUp` |
| Arrow (back) | `ArrowLeft` | Forward: `ArrowRight` |
| Info | `Info` | |
| Warning | `TriangleAlert` | |
| Error | `CircleAlert` | |
| Success | `CircleCheck` | |
| Loading | `LoaderCircle` | `animate-spin` |
| Empty state | `Inbox` | |
| Drag handle | `GripVertical` | |
| External link | `ExternalLink` | |
| Menu (mobile) | `Menu` | |

### Sizing

| Context | Class | px |
|---------|-------|----|
| Inline with body text | `h-4 w-4` | 16 |
| Button icon (icon-only or icon+label) | `h-5 w-5` | 20 |
| Section/header icon | `h-6 w-6` | 24 |
| Empty state hero icon | `h-12 w-12` | 48 |

### Color

| Context | Color | Rule |
|---------|-------|------|
| Default | `currentColor` | Inherits text color — most icons |
| Interactive (hover) | College Blue | Icon-only buttons on hover/focus |
| Semantic status | Leaf Green / Rose Alert / Slate | Success, error, info/warning |
| Muted | Slate (`#5a6a8a`) | Secondary actions, metadata |
| Decorative | College Blue (60% opacity) | Empty state hero icons |

### Accessibility

- **Icon-only buttons** MUST have `aria-label` describing the action in Russian (e.g. `aria-label="Удалить файл"`)
- **Icons with adjacent visible text** MUST use `aria-hidden="true"` — the text is the accessible label
- **Loading spinner**: `aria-label="Загрузка"` + `role="status"`
- **Focus-visible ring** on icon buttons: `focus-visible:ring-2 focus-visible:ring-[--primary] focus-visible:ring-offset-2`

### Animation

| Situation | Class | Duration |
|-----------|-------|----------|
| Loading spinner | `animate-spin` | continuous |
| Chevron expand/collapse | `transition-transform duration-200 rotate-0/180` | 200ms |
| Button icon hover | `transition-transform duration-150 group-hover:scale-110` | 150ms |
| Status change | `transition-opacity duration-300` | 300ms |

## 7. Do's and Don'ts

### Do:
- **Do** use Deep Navy (`#24386a`) as the primary interactive color in light theme. Its rarity is the point.
- **Do** use metal-toned neutrals (`#4a5a7a`, `#c9ceda`). Pure gray looks unfinished.
- **Do** use Leaf Green (`#2f8733`) only for success states.
- **Do** use tables as the default data display — they scan faster than cards.
- **Do** wrap forms and tables in cards for container consistency.
- **Do** use generous whitespace between sections (32px+).
- **Do** keep buttons at consistent height (38–40px).
- **Do** use CSS custom properties for all colors (never hardcoded hex in components).

### Don't:
- **Don't** use hardcoded hex colors in components — always use CSS variables (`text-primary`, `text-muted-fg`, etc.)
- **Don't** add shadows to cards. Tonal separation is sufficient.
- **Don't** use gradient text, glassmorphism, or side-stripe borders.
- **Don't** put an accent border-left on cards or blockquotes. Use full borders or nothing.
- **Don't** use zebra-striped tables. Subtle row borders are cleaner.
- **Don't** stack identical card grids as a data display. Use tables.
- **Don't** wrap everything in a card. The page background is the primary surface.

## 8. Dashboard Components

### CourseCard (`components/CourseCard.tsx`)
Карточка курса для дашборда. Сетка: `grid gap-6 sm:grid-cols-2 lg:grid-cols-3`.

```
┌─────────────────────────┐
│  Название курса          │
│  ─────────────────────   │
│  Прогресс: ██████░░ 60%  │
│  3/5 заданий · 78% ср.   │
│  [Перейти]               │
└─────────────────────────┘
```

### ProgressBar (`components/ui/progress.tsx`)
Индикатор прогресса на Tailwind CSS 4. Цвет заполнения — `bg-accent`. Используется внутри CourseCard и на странице курса.

### Студенческий дашборд (`/my/dashboard`)
- Только карточки курсов с прогрессом
- Нет дедлайнов, нет оценок, нет счётчиков

### Преподавательский дашборд (`/teacher/dashboard`)
- Карточки курсов
- Портфолио (повышения квалификации, грамоты, категории, сертификаты)
- Нет сводки нагрузки, студентов, последних отправок


# Color Reference

## Фактические цвета CSS (из globals.css)

| Переменная          | HEX         | Назначение                    |
| ------------------- | ----------- | ----------------------------- |
| `--primary`         | **#24386a** | Кнопки, заголовки, ссылки     |
| `--accent`          | **#24386a** | Акцент (совпадает с primary)  |
| `--fg`              | **#111827** | Основной текст (gray-900)     |
| `--muted`           | **#eef0f4** | Фон секций, table header      |
| `--muted-fg`        | **#4a5a7a** | Secondary текст, метаданные   |
| `--border`          | **#c9ceda** | Границы элементов             |
| `--input`           | **#c9ceda** | Границы input-полей           |
| `--accent-light`    | **#4a5a7a** | Hover, light accent           |
| `--accent-lighter`  | **#7a8aa5** | Placeholder, disabled         |
| `--card`            | **#ffffff** | Фон карточек                  |
| `--card-foreground` | **#111827** | Текст на карточках            |
| `--secondary`       | **#5b6a90** | Secondary badge               |
| `--destructive`     | **#dc2626** | Ошибки, удаление              |
| `--ring`            | **#24386a** | Focus ring                    |

## Dark Mode

| Переменная          | HEX         | Назначение                    |
| ------------------- | ----------- | ----------------------------- |
| `--bg`              | **#111827** | Фон                           |
| `--fg`              | **#f3f4f6** | Текст                         |
| `--accent`          | **#568edd** | College Blue как акцент       |
| `--primary`         | **#568edd** | Primary в тёмной теме         |
| `--card`            | **#1f2937** | Карточки                      |
| `--muted`           | **#1f2937** | Muted поверхности             |
| `--muted-fg`        | **#929cb5** | Muted текст                   |
| `--border`          | **#374151** | Границы                       |

## Цветовые пресеты

| Пресет    | `--accent` | `--accent-hover` |
|-----------|------------|------------------|
| Indigo    | #24386a    | #1c2c54          |
| Blue      | #1e4d8c    | #163d73          |
| Sapphire  | #3b5998    | #2e477a          |
| Plum      | #4a4e6b    | #3a3e56          |
| Green     | #2d5a4a    | #23483b          |

## Цвета из логотипа

Палитра извлечена прямым анализом fill-атрибутов `import/logo.svg`:

| Цвет                         | HEX         | В SVG                          |
| ---------------------------- | ----------- | ------------------------------ |
| 🔷 Deep Navy (primary)       | **#24386a** | Надписи, градиентные стопы     |
| 🟣 Crest Purple              | **#b9b3e5** | Лента с названием              |
| 🟩 Leaf Green                | **#2f8733** | Лавровые ветви                 |
| ⚙️ Metal Gray                | **#545263** | Антенна, металлические элем.   |
| 🔲 Light Steel               | **#c9cdc6** | Светлые элементы антенны       |
| 🤍 Warm Cream                | **#efead7** | Тарелка антенны                |
| 🔵 College Blue              | **#568edd** | Глобус (используется в dark)   |
| 🔷 Medium Blue               | **#4d74b4** | Промежуточные градиенты шара   |

Эта комбинация соответствует тематике образовательного учреждения в сфере телекоммуникаций и радиоэлектроники.


# Example References

Референсы общих сайтов по дизайну:

- https://samara.lemanapro.ru/
- https://www.vsk.ru/
- https://stgau.ru/
- https://www.mtsbank.ru/
- https://www.tbank.ru/
- https://netology.ru/

Референсы сайтов учебных заведений для примера дизайна и контента:

- https://vkipo.ru/ - отличный пример сайта, пример оформления специальностей

- https://mti.moscow/college - красивый согласованный дизайн

- https://www.hse.ru/ - согласованный дизайн, планый переходы, прияно для глаз

- https://tpu.ru/ - красивый дизайн, топ 1 по дизайну в этой подборке

- https://mibiu.ru/ - единый стиль 