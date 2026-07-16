---
name: CollegeLMS
description: Clean, modern educational management system
colors:
  primary: "#568edd"
  primary-hover: "#4d74b4"
  primary-light: "#e4edf8"
  secondary: "#b9b3e5"
  accent-green: "#2f8733"
  cream: "#efead7"
  neutral-bg: "#f5f7fa"
  neutral-fg: "#24386a"
  neutral-muted: "#545263"
  border: "#c9cdc6"
  destructive: "#c43e3e"
  destructive-muted: "#f8e8e8"
  white: "#ffffff"
typography:
  body:
    fontFamily: '"Golos Text", ui-sans-serif, system-ui, sans-serif'
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: '"Golos Text", ui-sans-serif, system-ui, sans-serif'
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
    textColor: "{colors.neutral-fg}"
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
    backgroundColor: "{colors.primary-light}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
  badge-admin:
    backgroundColor: "{colors.primary-light}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
  badge-teacher:
    backgroundColor: "{colors.secondary}"
    textColor: "#ffffff"
    rounded: "{rounded.sm}"
  badge-student:
    backgroundColor: "{colors.neutral-bg}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
---

# Design System: CollegeLMS

## 1. Overview

**Creative North Star: "The Clean Slate"**

CollegeLMS is a calm, authoritative academic workspace — a tool that feels like a well-organized dean's office in natural light, not a blinking dashboard in a server closet. Every screen prioritizes the task over the chrome. The interface recedes; the data steps forward.

The system rejects decorative clutter — no gradient text, no glassmorphism, no side-stripe borders. Hierarchy comes from generous whitespace, clear typographic scale, and a restrained brand color used precisely. The palette is derived directly from the college logo SVG (`import/logo.svg`): confident navy-blues from the globe and text, a heraldic purple from the crest ribbon, warm cream from the antenna dish, and laurel green for success states.

**Key Characteristics:**
- Clean blue primary (`#568edd`), drawn from the logo's globe. Cool neutrals replace pure grays
- One accent color (College Blue `#568edd`), used on ≤15% of any screen. Its rarity is the point
- The logo's purple (`#b9b3e5`) appears only for special badges and the Teacher role
- Body text uses the logo's deep navy (`#24386a`), not pure black
- Cards are real containers with generous radius, not flat rectangles
- Tables are the default data display, not card grids
- Mobile-first: forms and tables survive 320px

## 2. Colors

All colors are extracted directly from the college logo SVG (`import/logo.svg`). The palette uses the exact fill colors from the logo's globe, crest, laurel branches, and metal elements — no approximations, no guesswork.

### Primary
- **College Blue** (#568edd / oklch(0.64 0.133 257)): Interactive elements — buttons, links, focus rings. Extracted from the globe outer ring. Used sparingly so every blue element means something.
- **College Blue Dark** (#4d74b4 / oklch(0.56 0.109 260)): Hover and active states. Medium blue from the logo's gradient stops.
- **College Blue Light** (#e4edf8 / oklch(0.94 0.018 253)): Muted backgrounds, table headers, admin badges, selected rows. Derived at ~15% opacity from primary.

### Secondary
- **Crest Purple** (#b9b3e5 / oklch(0.79 0.071 290)): The heraldic purple from the college crest ribbon. Used for Teacher badges and secondary decorative elements only.
- **Leaf Green** (#2f8733 / oklch(0.55 0.147 144)): The green accent from the logo's laurel branches. Used exclusively for success states.
- **Warm Cream** (#efead7 / oklch(0.94 0.026 95)): The cream tone from the antenna dish and light elements. Light banners, informational callouts, achievement highlights.

### Neutral
- **Page Blue** (#f5f7fa / oklch(0.98 0.005 258)): Page background. A barely-there cool tint that complements the blue logo.
- **White** (#ffffff / oklch(1 0 0)): Card, dialog, input backgrounds. Elevated surfaces.
- **Light Steel** (#c9cdc6 / oklch(0.84 0.011 131)): Borders, dividers, table row separators. A neutral warm-light tone from the logo's antenna elements.
- **Metal Gray** (#545263 / oklch(0.45 0.028 291)): Muted secondary text, placeholder text, metadata. The gray-blue from the logo's metal/antenna parts.
- **Deep Navy** (#24386a / oklch(0.35 0.091 266)): Body text and headings. Extracted from the logo text and dark gradient stops. Not pure black.

### Semantic
- **Blue Tint** (#e4edf8): Selected/active table rows, admin badge backgrounds, info banners.
- **Rose Alert** (#c43e3e / oklch(0.56 0.171 25)): Destructive actions, errors. Not from logo.
- **Rose Tint** (#f8e8e8): Error banners background.

### Named Rules
**The One Voice Rule.** College Blue covers ≤15% of any screen. Its rarity is the point — when blue appears, it means something.

**The Crest Rule.** Purple is reserved for Teacher role identification and the college crest in headers. Never use purple for interactive elements.

**The Cold Steel Rule.** Neutrals are drawn from the logo's metal elements — a slight warm-gray-blue cast (chroma ≤ 0.03, hue 131–291°). Pure gray (#808080) is forbidden.

## 3. Typography

**Body Font:** Golos Text, a modern humanist sans-serif with wide apertures and clear punctuation for Russian text

**Display Font:** Golos Text (same family, scaled with weight 500–700)

**Monospace Font:** JetBrains Mono for code

**Character:** Clean, readable, modern. Golos Text was designed for multilingual support with excellent Cyrillic legibility — wide apertures, tall x-height, clear distinction between similar glyphs. No decorative typefaces — this is an educational tool, not a magazine.

### Hierarchy
- **Headline** (600, 1.5rem/24px, 1.3): Page titles (`h1`). Top-level sections.
- **Title** (600, 1.125rem/18px, 1.4): Section headers (`h2`), dialog titles, card headers.
- **Body** (400, 0.9375rem/15px, 1.6): Primary reading text. Cap line length at 70ch.
- **Label** (500, 0.8125rem/13px, 1.25, letter-spacing 0.01em): Form labels, table headers, button text, metadata.
- **Caption** (400, 0.75rem/12px, 1.4): Helper text, timestamps, secondary metadata.

### Named Rules
**The Weight-Only Scale.** Hierarchy is expressed through weight (400 → 500 → 600) and size, not through font family switches or letter-spacing theatrics. Golos Text provides clear weight contrast at 400/500/600/700.

## 4. Elevation

Flat by default. Depth comes from tonal layering (background → card → border), not from shadows.

Cards, dialogs, and dropdowns sit on the surface without vertical lift. The card background (`#ffffff`) against the page background (`#f5f7fa`) provides enough separation. No box-shadows on cards.

**The only shadow** is on the Dialog overlay — a soft ambient shadow on the content panel and a Deep Navy (`#24386a`) overlay at 35% opacity behind it.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest. The tonal contrast between page and card backgrounds provides depth. Shadows appear only for modals and dropdown menus.

## 5. Components

### Buttons
- **Shape:** Gently rounded corners (8px)
- **Primary:** College Blue (`#568edd`) background, white text, 10px 20px padding. Hover shifts to College Blue Dark (`#4d74b4`). Active presses deeper navy.
- **Ghost:** Transparent background, Deep Navy (`#24386a`) text. Hover gets a Blue Tint background.
- **Outline:** 1px Cool Border, transparent background. Hover fills with Blue Tint.
- **Destructive:** Rose Alert background.
- **States:** All buttons use a 150ms ease-out transition on background-color and box-shadow. Focus-visible ring uses College Blue at 3px offset.

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
- **Do** use College Blue (`#568edd`) sparingly (<15% per screen). Blue means interactive.
- **Do** use metal-toned neutrals from the logo (`#545263`, `#c9cdc6`). Pure gray looks unfinished.
- **Do** use Crest Purple (`#b9b3e5`) exclusively for Teacher badges — never for buttons or links.
- **Do** use Leaf Green (`#2f8733`) only for success states.
- **Do** use tables as the default data display — they scan faster than cards.
- **Do** wrap forms and tables in cards for container consistency.
- **Do** use generous whitespace between sections (32px+).
- **Do** keep buttons at consistent height (38–40px).
- **Do** show loading state as a centered spinner in the content area.

### Don't:
- **Don't** use `#000` or pure `#fff` as text. Deep Navy (`#24386a`) for body, White for surfaces.
- **Don't** add shadows to cards. Tonal separation is sufficient.
- **Don't** use gradient text, glassmorphism, or side-stripe borders.
- **Don't** put an accent border-left on cards. Use full borders or nothing.
- **Don't** use zebra-striped tables. Subtle row borders are cleaner.
- **Don't** stack identical card grids as a data display. Use tables.
- **Don't** wrap everything in a card. The page background is the primary surface.
- **Don't** use the Crest Purple for interactive elements — it's a heraldic identifier, not a call to action.


# Color from Logo (actual SVG fills)

Палитра извлечена прямым анализом fill-атрибутов `import/logo.svg`. Цвета в дизайн-системе используют именно эти значения.

| Цвет                         | HEX         | OKLCH                         | В SVG                          |
| ---------------------------- | ----------- | ----------------------------- | ------------------------------ |
| 🔵 College Blue (primary)    | **#568edd** | `oklch(0.64 0.133 257)`       | Глобус, внешнее кольцо         |
| 🔷 Deep Navy (текст)         | **#24386a** | `oklch(0.35 0.091 266)`       | Надписи, градиентные стопы     |
| 🔷 Medium Blue (hover)       | **#4d74b4** | `oklch(0.56 0.109 260)`       | Промежуточные градиенты шара   |
| 🟣 Crest Purple              | **#b9b3e5** | `oklch(0.79 0.071 290)`       | Лента с названием              |
| 🟩 Leaf Green                | **#2f8733** | `oklch(0.55 0.147 144)`       | Лавровые ветви                 |
| ⚙️ Metal Gray (muted text)   | **#545263** | `oklch(0.45 0.028 291)`       | Антенна, металлические элем.   |
| 🔲 Light Steel (border)      | **#c9cdc6** | `oklch(0.84 0.011 131)`       | Светлые элементы антенны       |
| 🤍 Warm Cream                | **#efead7** | `oklch(0.94 0.026 95)`        | Тарелка антенны                |
| ⚫ Near Black                 | **#0e0a09** | `oklch(0.15 0.007 35)`        | Тёмные контуры                 |

### Дополнительные цвета

* **White** — `#FFFFFF` — карточки, фоны поверхностей
* **Page Blue** — `#f5f7fa` — фон страницы (не из логотипа)
* **Rose Alert** — `#c43e3e` — ошибки, деструктивные действия (не из логотипа)

### Общая характеристика палитры

Палитра сочетает:

* холодные **синие** оттенки (`#568edd` → `#24386a`) — технологии, связь, надёжность;
* **зелёный** (`#2f8733`) — рост, развитие, лавровая ветвь;
* **лиловый** (`#b9b3e5`) — официальность и торжественность;
* нейтральные **серо-кремовые** тона (`#545263`, `#c9cdc6`, `#efead7`) — металл и конструктив.

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