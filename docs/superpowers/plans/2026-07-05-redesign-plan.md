# Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Полный редизайн CollegeLMS — публичная часть + админка в классическом академическом стиле

**Architecture:** Эволюционный редизайн — меняем CSS-токены и компоненты, сохраняя структуру маршрутов и логику. Сначала глобальные стили, потом крупные компоненты (Header, Hero, Footer), затем страницы-списки (news, sections), затем админка.

**Tech Stack:** Next.js 14, Tailwind CSS v4, shadcn/ui, CSS custom properties в OKLCH

## Global Constraints

- Все цвета в OKLCH (кроме footer-bg и соцсетей)
- Белый фон вместо светло-серого
- Все изменения только визуальные — логика и маршруты не меняются
- Сохранить существующие data-файлы (site-content.ts, page-contents.json)
- Форматирование: Prettier
- Коммиты после каждой фазы

---

### Task 1: CSS tokens — новая цветовая схема

**Files:**
- Modify: `frontend/app/globals.css`

- [ ] **Step 1: Обновить CSS-переменные в `:root`**

```css
/* В globals.css, заменить секцию :root */
:root {
  --background: oklch(1 0 0);
  --foreground: oklch(0.15 0.03 260);
  --card: oklch(1 0 0);
  --card-foreground: oklch(0.15 0.03 260);
  --primary: oklch(0.55 0.18 260);
  --primary-foreground: oklch(1 0 0);
  --secondary: oklch(0.75 0.12 80);
  --secondary-foreground: oklch(0.15 0.03 260);
  --muted: oklch(0.96 0.005 260);
  --muted-foreground: oklch(0.55 0.02 260);
  --accent: oklch(0.55 0.18 260);
  --accent-foreground: oklch(1 0 0);
  --destructive: oklch(0.55 0.2 30);
  --destructive-foreground: oklch(1 0 0);
  --border: oklch(0.92 0.005 260);
  --input: oklch(0.92 0.005 260);
  --ring: oklch(0.55 0.18 260);
  --radius: 0.75rem;
}
```

- [ ] **Step 2: Обновить `.dark` секцию**

```css
.dark {
  --background: oklch(0.13 0.02 260);
  --foreground: oklch(0.92 0.005 260);
  --card: oklch(0.16 0.02 260);
  --card-foreground: oklch(0.92 0.005 260);
  --primary: oklch(0.65 0.15 260);
  --primary-foreground: oklch(1 0 0);
  --secondary: oklch(0.65 0.12 80);
  --secondary-foreground: oklch(1 0 0);
  --muted: oklch(0.18 0.02 260);
  --muted-foreground: oklch(0.65 0.02 260);
  --accent: oklch(0.65 0.15 260);
  --accent-foreground: oklch(1 0 0);
  --destructive: oklch(0.55 0.2 30);
  --border: oklch(0.22 0.02 260);
  --input: oklch(0.22 0.02 260);
  --ring: oklch(0.65 0.15 260);
}
```

- [ ] **Step 3: Обновить `@theme inline` секцию** — map новых переменных

```css
@theme inline {
  --color-background: var(--background);
  --color-foreground: var(--foreground);
  --color-card: var(--card);
  --color-card-foreground: var(--card-foreground);
  --color-primary: var(--primary);
  --color-primary-foreground: var(--primary-foreground);
  --color-secondary: var(--secondary);
  --color-secondary-foreground: var(--secondary-foreground);
  --color-muted: var(--muted);
  --color-muted-foreground: var(--muted-foreground);
  --color-accent: var(--accent);
  --color-accent-foreground: var(--accent-foreground);
  --color-destructive: var(--destructive);
  --color-destructive-foreground: var(--destructive-foreground);
  --color-border: var(--border);
  --color-input: var(--input);
  --color-ring: var(--ring);
  --radius: var(--radius);
}
```

- [ ] **Step 4: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "phase 1: обновить CSS-токены — белый фон, новый синий, золотой акцент"
```

---

### Task 2: Header — новый дизайн

**Files:**
- Modify: `frontend/components/Header.tsx`
- The rest of the header component and mobile menu

**Design:**
- Sticky header, белый фон, тень при скролле (через `useScroll` или IntersectionObserver)
- Лого 48px (сохранить пропорции)
- Полное имя колледжа
- 6 навигационных разделов с подменю в 2 колонки
- Иконка поиска (только визуально, без функционала)
- Кнопка "Войти"
- Mobile: slide-out drawer (не аккордеон)
- Accessibility toggle убрать из хедера

- [ ] **Step 1: Создать хук `useScrollPosition` или использовать `use effect` для тени**

Добавить `useEffect` в Header.tsx для отслеживания scrollY и добавления `shadow-sm` на header при скролле > 0.

- [ ] **Step 2: Обновить структуру Header.tsx**

- Уменьшить лого до 48×48 с сохранением aspect ratio
- Полное имя колледжа (взять из site-content.ts)
- 6 разделов с dropdown меню в 2 колонки
- Иконка поиска (SVG лупа)
- Кнопка "Войти"
- Убрать AccessibilityToggle, оставить ThemeToggle

- [ ] **Step 3: Mobile menu — slide-out drawer**

Заменить аккордеон на slide-out панель с затемнением:
- Кнопка гамбургер справа
- Slide-out слева с навигацией (все 6 разделов + подразделы)
- Затемнение фона
- Кнопка закрытия

- [ ] **Step 4: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "phase 2: переработать Header — тень при скролле, подменю 2 колонки, slide-out mobile"
```

---

### Task 3: Hero — главная страница

**Files:**
- Modify: `frontend/app/(public)/page.tsx`
- Modify: `frontend/components/Carousel.tsx` (заменить или убрать)

**Design:**
- Full-width фото на 100vh (изображение колледжа/студентов)
- Градиентное наложение: `linear-gradient(180deg, #1A5CFF 0%, transparent 60%)`
- Белый текст: название колледжа (H1) + слоган + 2 CTA кнопки
- Параллакс эффект (desktop only)
- Плавный переход к следующему блоку
- Ниже: секция "О колледже" (кратко) + секция "Последние новости" (сетка 3 карточки)

- [ ] **Step 1: Обновить `app/(public)/page.tsx`**

Заменить существующий контент:
```tsx
// hero секция
<section className="relative h-screen flex items-center justify-center overflow-hidden">
  <div className="absolute inset-0">
    <Image src="/hero-bg.jpg" alt="" fill className="object-cover" priority />
    <div className="absolute inset-0 bg-gradient-to-b from-[#1A5CFF] to-transparent opacity-80" />
  </div>
  <div className="relative z-10 text-center text-white max-w-4xl mx-auto px-4">
    <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold leading-tight mb-4">
      ГБПОУ «Ставропольский колледж связи имени В.А. Петрова»
    </h1>
    <p className="text-lg md:text-xl text-white/90 mb-8">
      Качество. Традиции. Будущее.
    </p>
    <div className="flex gap-4 justify-center">
      <Link href="/admissions" className="inline-flex h-12 items-center justify-center rounded-xl bg-white px-8 text-base font-semibold text-[#1A5CFF] shadow-sm transition-colors hover:bg-white/90">
        Поступить
      </Link>
      <Link href="/education" className="inline-flex h-12 items-center justify-center rounded-xl border-2 border-white px-8 text-base font-semibold text-white transition-colors hover:bg-white/10">
        Специальности
      </Link>
    </div>
  </div>
</section>
```

- [ ] **Step 2: Секция "О колледже" (краткая) + "Новости"**

```tsx
// секция "О колледже"
<section className="py-20 bg-muted">
  <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div className="max-w-3xl mx-auto text-center">
      <h2 className="text-3xl font-bold mb-6">О колледже</h2>
      <p className="text-lg text-muted-foreground leading-relaxed">
        Государственное бюджетное профессиональное образовательное учреждение
        «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
        — одно из ведущих учебных заведений Ставропольского края в сфере
        информационных технологий и связи.
      </p>
      <Link href="/about" className="mt-8 inline-flex h-11 items-center justify-center rounded-xl border border-border px-6 text-sm font-medium transition-colors hover:bg-muted">
        Подробнее о колледже →
      </Link>
    </div>
  </div>
</section>

// секция "Новости"
<section className="py-20">
  <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div className="flex items-center justify-between mb-10">
      <h2 className="text-3xl font-bold">Последние новости</h2>
      <Link href="/news" className="text-sm font-medium text-primary hover:underline">
        Все новости →
      </Link>
    </div>
    <div className="grid gap-6 md:grid-cols-3">
      {/* NewsCard × 3 — fetch from API, same as current */}
    </div>
  </div>
</section>
```

- [ ] **Step 3: Обновить или удалить Carousel.tsx**

Carousel больше не используется на главной. Либо удалить, либо оставить для других целей.

- [ ] **Step 4: Добавить hero-изображение**

Проверить, есть ли файл `public/hero-bg.jpg` или другое подходящее изображение. Если нет — использовать placeholder или существующее изображение.

- [ ] **Step 5: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "phase 3: hero с фото на главной, секции о колледже и новости"
```

---

### Task 4: Footer — новый дизайн

**Files:**
- Modify: `frontend/components/Footer.tsx`

**Design:**
- Тёмный фон `bg-[#0D1B2A]`
- 4 колонки навигации
- Подписка на новости (email input)
- Контакты центрированы
- Соцсети с официальными цветами на тёмном фоне
- Копирайт + Политика конфиденциальности
- Accessibility toggle

- [ ] **Step 1: Обновить Footer.tsx**

Заменить содержимое:

```tsx
import Link from "next/link"
import { siteNavigation } from "@/data/site-content"

export default function Footer() {
  return (
    <footer className="bg-[#0D1B2A] text-[oklch(0.85_0.01_260)]">
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
        <div className="grid gap-12 sm:grid-cols-2 lg:grid-cols-4">
          {/* Column 1: О колледже */}
          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">О колледже</h4>
            <ul className="space-y-2 text-sm">
              <li><Link href="/about" className="hover:text-white transition-colors">История</Link></li>
              <li><Link href="/about/rukovodstvo" className="hover:text-white transition-colors">Руководство</Link></li>
              <li><Link href="/about/license" className="hover:text-white transition-colors">Лицензия</Link></li>
              <li><Link href="/about/vacancies" className="hover:text-white transition-colors">Вакансии</Link></li>
            </ul>
          </div>
          {/* Column 2: Абитуриенту */}
          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Абитуриенту</h4>
            <ul className="space-y-2 text-sm">
              <li><Link href="/admissions" className="hover:text-white transition-colors">Правила приёма</Link></li>
              <li><Link href="/education" className="hover:text-white transition-colors">Специальности</Link></li>
              <li><Link href="/admissions/documents" className="hover:text-white transition-colors">Документы</Link></li>
            </ul>
          </div>
          {/* Column 3: Студенту */}
          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Студенту</h4>
            <ul className="space-y-2 text-sm">
              <li><Link href="/student-life" className="hover:text-white transition-colors">Расписание</Link></li>
              <li><Link href="/student-life" className="hover:text-white transition-colors">ЭИОС</Link></li>
              <li><Link href="/student-life" className="hover:text-white transition-colors">Библиотека</Link></li>
              <li><Link href="/student-life" className="hover:text-white transition-colors">Спорт</Link></li>
            </ul>
          </div>
          {/* Column 4: Соцсети + подписка */}
          <div>
            <h4 className="mb-4 text-sm font-semibold text-white">Мы в соцсетях</h4>
            <div className="flex gap-3 mb-6">
              {/* VK */}
              <a href="https://vk.com/stvcc_official" target="_blank" rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#0077FF] hover:text-white" aria-label="ВКонтакте">
                <svg width="20" height="20" viewBox="0 0 35 37" fill="currentColor">...</svg>
              </a>
              {/* Telegram */}
              <a href="https://t.me/stvcc_official" target="_blank" rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#08C] hover:text-white" aria-label="Telegram">
                <svg width="20" height="20" viewBox="0 0 35 37" fill="currentColor">...</svg>
              </a>
              {/* Max — обновлённый с официальным SVG */}
              <a href="https://web.max.ru/" target="_blank" rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white/70 transition-colors hover:bg-[#471AFF] hover:text-white" aria-label="Max">
                {/* official Max logo SVG */}
              </a>
            </div>
            <h4 className="mb-3 text-sm font-semibold text-white">Подписаться на новости</h4>
            <form className="flex gap-2" onSubmit={/* handle submit */}>
              <input type="email" placeholder="Ваш email"
                className="flex-1 rounded-lg bg-white/10 px-3 py-2 text-sm text-white placeholder:text-white/40 border border-white/20 focus:outline-none focus:ring-2 focus:ring-white/30" />
              <button type="submit"
                className="rounded-lg bg-white px-4 py-2 text-sm font-medium text-[#0D1B2A] hover:bg-white/90 transition-colors">
                Подписаться
              </button>
            </form>
          </div>
        </div>
        {/* Контакты */}
        <div className="mt-12 border-t border-white/10 pt-8 text-center text-sm">
          <p className="mb-1">Ставропольский край, г. Ставрополь, пр-д Черняховского, 3</p>
          <p className="mb-1">+7 (8652) 24-25-27 | <a href="mailto:college@stvcc.ru" className="hover:text-white transition-colors">college@stvcc.ru</a></p>
          <p className="mt-4 text-xs text-white/50">
            ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»
          </p>
          <p className="mt-2 text-xs text-white/40">
            &copy; {new Date().getFullYear()} Все права защищены | <Link href="/privacy" className="hover:text-white transition-colors">Политика конфиденциальности</Link>
          </p>
        </div>
      </div>
    </footer>
  )
}
```

- [ ] **Step 2: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "phase 4: новый Footer — тёмный фон, 4 колонки, подписка на новости"
```

---

### Task 5: News pages — обновление дизайна

**Files:**
- Modify: `frontend/app/(public)/news/page.tsx`
- Modify: `frontend/app/(public)/news/[id]/page.tsx`

- [ ] **Step 1: Обновить news list**

- Карточки с фото 16:9, дата, заголовок, краткое описание
- 3-column grid
- Category filter buttons (pill style)
- Search input
- Белый фон карточек, subtle border

- [ ] **Step 2: Обновить news detail**

- Featured image на всю ширину (max-w-4xl)
- Заголовок H1
- Дата + категория под заголовком
- HTML контент в prose-стиле
- Сохранить существующую логику

- [ ] **Step 3: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "phase 5: обновить дизайн страниц новостей"
```

---

### Task 6: Section pages — обновление

**Files:**
- Modify: `frontend/components/SectionPage.tsx`
- Modify: `frontend/components/Breadcrumbs.tsx`

- [ ] **Step 1: Обновить SectionPage**

- Сетка подразделов с иконками/изображениями
- Обновить отступы и типографику под новую цветовую схему

- [ ] **Step 2: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "phase 6: обновить дизайн секционных страниц"
```

---

### Task 7: Admin + dashboards — визуальное обновление

**Files:**
- Modify: `frontend/app/admin/layout.tsx`
- Modify: `frontend/app/admin/page.tsx`
- Modify: `frontend/app/admin/news/page.tsx`
- Modify: `frontend/app/teacher/dashboard/page.tsx`
- Modify: `frontend/app/my/dashboard/page.tsx`
- Modify: `frontend/app/courses/page.tsx`

- [ ] **Step 1: Обновить admin layout — header**

- CL logo + "CollegeLMS" в новой цветовой схеме
- Role-based навигация (Пользователи, Новости)
- User info + badge + logout
- Белый фон, border-bottom

- [ ] **Step 2: Обновить таблицы — шрифты, отступы, hover**

- Использовать обновлённые CSS-токены
- Увеличить отступы в ячейках

- [ ] **Step 3: Обновить dashboard cards**

- 3-4 stat cards с крупными цифрами (как в vtb/references)
- Обновить шрифты, тени, отступы

- [ ] **Step 4: Проверить сборку**

```bash
cd frontend && npx tsc --noEmit --pretty
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "phase 7: обновить дизайн админки и дашбордов"
```

---

### Task 8: Проверка после всех изменений

- [ ] **Step 1: Полная сборка frontend**

```bash
cd frontend && npx tsc --noEmit --pretty && npm run build
```

- [ ] **Step 2: Проверить в Docker**

```bash
docker compose up -d --build
```

- [ ] **Step 3: Проверить страницы**

- `http://localhost/` — hero, о колледже, новости
- `http://localhost/news` — список новостей
- `http://localhost/news/[id]` — деталь новости
- `http://localhost/about` — секционная страница
- `http://localhost/login` — форма входа
- `http://localhost/admin` — админка (после логина)
