# Redesign — CollegeLMS Public Site & Admin

> **Date:** 2026-07-05
> **Status:** Design approved
> **Style:** Классический академический (референсы: tpu.ru, vkipo.ru, sechenov.ru)
> **Scope:** Полный редизайн — публичная часть + админка/дашборды

---

## 1. Design Principles

1. **Академическая сдержанность** — чисто, строго, без лишних украшений
2. **Белый воздух** — чистый фон, крупные отступы, чёткая иерархия
3. **Качественная типографика** — Inter, bold заголовки, хороший leading
4. **Фотографии как контент** — hero на всю ширину, качественные изображения кампуса
5. **Единый визуальный язык** — публичка и админка выглядят как один продукт

---

## 2. Color Palette

### Light theme (основная)

| Token | Значение | Описание |
|-------|----------|----------|
| `--background` | `#ffffff` | Чистый белый |
| `--foreground` | `oklch(0.15 0.03 260)` | Почти чёрный с синим подтоном |
| `--primary` | `oklch(0.55 0.18 260)` | Глубокий синий (~#1A5CFF) |
| `--primary-foreground` | `#ffffff` | Белый |
| `--secondary` | `oklch(0.75 0.12 80)` | Золотой акцент |
| `--secondary-foreground` | `oklch(0.15 0.03 260)` | Тёмный |
| `--card` | `#ffffff` | Белый |
| `--card-foreground` | `oklch(0.15 0.03 260)` | Тёмный |
| `--muted` | `oklch(0.96 0.005 260)` | Тонкий серо-голубой для секций |
| `--muted-foreground` | `oklch(0.55 0.02 260)` | Серый |
| `--accent` | `oklch(0.55 0.18 260)` | Как primary |
| `--accent-foreground` | `#ffffff` | Белый |
| `--destructive` | `oklch(0.55 0.20 30)` | Красный |
| `--border` | `oklch(0.92 0.005 260)` | Очень светлый |
| `--ring` | `oklch(0.55 0.18 260)` | Как primary |
| `--radius` | `0.75rem` | 12px |

### Dark theme

| Token | Значение |
|-------|----------|
| `--background` | `oklch(0.13 0.02 260)` |
| `--foreground` | `oklch(0.92 0.005 260)` |
| `--primary` | `oklch(0.65 0.15 260)` |
| `--card` | `oklch(0.16 0.02 260)` |
| `--muted` | `oklch(0.18 0.02 260)` |
| `--border` | `oklch(0.22 0.02 260)` |

### Footer theme

| Token | Значение |
|-------|----------|
| footer-bg | `#0D1B2A` |
| footer-text | `oklch(0.85 0.01 260)` |
| footer-heading | `#ffffff` |

---

## 3. Typography

| Свойство | Значение |
|----------|----------|
| Font (body) | `Inter`, system sans-serif |
| Font (headings) | `Inter`, bold weight |
| Body size | `0.938rem` (15px) |
| H1 | `2.5rem`–`3.5rem` bold (hero title) |
| H2 | `1.75rem` bold (section titles) |
| H3 | `1.25rem` semibold |
| Leading | `1.6` body, `1.2` headings |
| Letter-spacing | `-0.01em` для крупных заголовков |

---

## 4. Layout

- **Container:** `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8` (как сейчас)
- **Content pages:** `max-w-4xl mx-auto`
- **Admin pages:** `max-w-6xl mx-auto`
- **Отступы секций:** `py-16 sm:py-20 lg:py-24`
- **Сетка:** 12-column grid, gap-6

---

## 5. Components

### 5.1 Header

```
[Лого]  ГБПОУ «Ставропольский колледж связи им. В.А. Петрова»  │  Сведения  Колледж  Образование  Абитуриенту  Студенту  Выпускнику  │  🔍  Войти  ☰
```

- **Sticky**, белый фон, тень при скролле
- Лого 48×48, сохранение пропорций
- Полное имя колледжа
- 6 разделов с компактными подменю в 2 колонки
- Иконка поиска (визуально), кнопка "Войти"
- Mobile: slide-out drawer, не аккордеон
- Accessibility toggle **только в футере**

### 5.2 Hero (главная)

- Фото на всю ширину (100vw × 100vh min)
- Параллакс или фиксированный фон
- Градиентное наложение: `linear-gradient(180deg, #1A5CFF 0%, transparent 60%)`
- Белый текст: название колледжа (H1) + слоган
- 2 CTA: "Поступить" (white filled), "Специальности" (white outline)
- Плавный переход: белая волна или fade к следующему блоку

### 5.3 Footer

- **Тёмный фон** `#0D1B2A`
- **4 колонки навигации:** О колледже, Абитуриенту, Студенту, Мы в соцсетях
- **Подписка на новости:** email input + кнопка в 4-й колонке
- **Контакты** центрированы отдельной строкой
- **Соцсети:** VK (#0077FF), Telegram (#08C), Max (градиент #52C5FE→#3948EC→#9A40DA)
- Нижняя строка: копирайт + Политика конфиденциальности

### 5.4 News

- **List:** 3-column grid, карточки с фото (16:9), дата, заголовок, краткое описание
- **Detail:** featured image на всю ширину, заголовок, дата/категория, HTML контент в prose
- **Фильтры:** category buttons + search input

### 5.5 Section Pages (about/college/education/etc.)

- **Index:** сетка подразделов с иконками/изображениями
- **Detail:** breadcrumbs + заголовок + HTML контент в prose
- Сохранить существующую структуру SectionPage

### 5.6 Admin & Dashboards

- **Admin header:** CL logo + "CollegeLMS" + role-based nav tabs + user info + logout
- **Таблицы:** с hover, сортировка (пока визуально)
- **Формы:** компактные dialogs, label над полем
- **Статистика:** 3-4 большие цифры в карточках
- Сохранить существующую логику, обновить визуал

---

## 6. Animation & Interaction

- **Hover:** `transition-colors duration-200` на ссылках и кнопках
- **Hero parallax:** `background-attachment: fixed` (desktop only)
- **Header shadow:** появляется при скролле > 0
- **Mobile menu:** slide-out, с затемнением фона
- **Loading:** spinner (как сейчас)
- **Без излишней анимации** — академический стиль

---

## 7. Implementation Order

1. **Phase 1:** CSS tokens + globals.css — новая цветовая схема, шрифты
2. **Phase 2:** Header — новый дизайн, mobile menu
3. **Phase 3:** Hero — главная страница
4. **Phase 4:** Footer — новый дизайн
5. **Phase 5:** News pages — карточки, фильтры
6. **Phase 6:** Section pages — обновление
7. **Phase 7:** Admin + dashboards — визуальное обновление

---

*Design approved 2026-07-05. Next step: writing-plans → implementation.*
