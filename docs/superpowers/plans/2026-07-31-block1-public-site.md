# Блок 1: Публичный сайт — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Обновить публичный сайт: выпадающие меню с hover-анимацией, кнопка «Войти», раздел «Достижения», единый стиль контента, фиксы постера/поиска/контактов/футера/логина.

**Architecture:** Правки в компонентах Next.js App Router (`CollegeLMS.Next/`): Header, Footer, страницы. Контент разделов — статические данные (`data/site-content.ts`, `data/page-contents.json`) + `.docs-content` CSS. Редирект `/lms` по роли из JWT. Никаких изменений бекенда.

**Tech Stack:** Next.js 14 (App Router, "use client"), Tailwind CSS 4, TypeScript, lucide-react, sonner.

## Global Constraints

- Все данные и комментарии в коде на русском
- Компоненты: shadcn/ui примитивы из `components/ui/`, проектные — в `components/`
- Иконки — только lucide-react (SocialIcon svg — исключение, существует)
- Проверка: `npx tsc --noEmit` и `npm run build` должны проходить без ошибок
- Коммиты: `git add -A` + сообщения по AGENTS.md (`feat:` / `fix:` / `docs:`)
- Адаптивность: Toshiba A665 12k (ноутбук), Xiaomi Mi 9 SE (телефон), широкие экраны 1920+
- Контент WP-импорта: HTML в `page-contents.json` (UTF-8, ключ = slug подстраницы)

---

### Task 1: Выпадающие меню в хедере, кнопка «Войти», ссылка Max

**Files:**
- Modify: `CollegeLMS.Next/components/Header.tsx` (полная замена)
- Verify: `CollegeLMS.Next/data/site-content.ts` — не менять (subsections уже есть)

**Interfaces:**
- Consumes: `siteNavigation: Section[]` из `@/data/site-content` (`Section = { title, slug, href, subsections: Subsection[] }`, `Subsection = { title, slug, href, content }`)
- Produces: desktop nav с dropdown-панелями (opacity + translate-y, 150ms, close-задержка 120ms), мобильный аккордеон, пункт «Войти» в конце nav, `socialLinks` с актуальным Max

- [ ] **Step 1: Заменить содержимое `Header.tsx`**

```tsx
"use client"

import { useState, useRef, useEffect } from "react"
import Link from "next/link"
import { Menu, X, Search, ChevronDown, User } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"

const socialLinks = [
  { href: "https://vk.com/stvcc_stav", label: "ВКонтакте", icon: "vk" },
  { href: "https://t.me/stvcc", label: "Telegram", icon: "tg" },
  { href: "https://max.ru/id2634028465_gos", label: "Max", icon: "max" },
]

// SocialIcon: оставить РОВНО как в текущем файле (vk/tg/max SVG) — без изменений

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)
  const [openMenu, setOpenMenu] = useState<string | null>(null)
  const [openMobileSection, setOpenMobileSection] = useState<string | null>(null)
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    function onScroll() {
      setScrolled(window.scrollY > 0)
    }
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") setOpenMenu(null)
    }
    function onClickOutside(e: MouseEvent) {
      if ((e.target as HTMLElement).closest("[data-nav-item]")) return
      setOpenMenu(null)
    }
    window.addEventListener("keydown", onKeyDown)
    window.addEventListener("click", onClickOutside)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
      window.removeEventListener("click", onClickOutside)
    }
  }, [])

  const openMenuDelayed = (slug: string) => {
    if (closeTimer.current) clearTimeout(closeTimer.current)
    setOpenMenu(slug)
  }
  const closeMenuDelayed = () => {
    if (closeTimer.current) clearTimeout(closeTimer.current)
    closeTimer.current = setTimeout(() => setOpenMenu(null), 120)
  }

  return (
    <header className="sticky top-0 z-50 bg-accent">
      <div className="flex flex-col">
        {/* Row 1: Top bar — hides on scroll */}
        <div className={`overflow-hidden transition-all duration-300 ease-in-out ${scrolled ? "max-h-0 opacity-0 py-0 border-transparent" : "max-h-14 opacity-100"}`}>
          <div className="flex h-12 items-center justify-between px-4 lg:px-6">
            <div className="flex items-center gap-2">
              {socialLinks.map((link) => (
                <a
                  key={link.href}
                  href={link.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center h-7 w-7 rounded text-white/60 hover:text-white transition-colors"
                  aria-label={link.label}
                >
                  <SocialIcon icon={link.icon} className="h-4 w-4" />
                </a>
              ))}
              <span className="mx-1 text-white/20">|</span>
              <Link href="/schedule" className="text-xs text-white/70 hover:text-white transition-colors">Расписание</Link>
              <span className="mx-1 text-white/20">|</span>
              <Link href="/contacts" className="text-xs text-white/70 hover:text-white transition-colors">Контакты</Link>
              <span className="mx-1 text-white/20">|</span>
              <Link href="/news" className="text-xs text-white/70 hover:text-white transition-colors">Новости</Link>
              <span className="mx-1 text-white/20">|</span>
              <Link href="/admissions" className="text-xs font-semibold text-amber-300 hover:text-amber-200 transition-colors">Приёмная кампания 2026</Link>
            </div>
            <div className="flex items-center gap-1">
              <Link href="/search" className="flex items-center justify-center h-8 w-8 rounded-md text-white/80 hover:text-white hover:bg-white/10 transition-colors" aria-label="Поиск"><Search size={16} /></Link>
              <div className="flex items-center [&_button]:text-white/80 [&_button]:hover:text-white [&_button]:hover:bg-white/10 [&_button]:rounded-md [&_button]:p-1.5">
                <AccessibilityToggle />
                <ThemeToggle />
              </div>
            </div>
          </div>
        </div>

        {/* Row 2: Logo text + Navigation */}
        <div className="grid grid-cols-3 items-center border-b border-white/10 px-4 lg:px-6">
          <Link href="/" className="flex shrink-0 flex-col py-3 leading-tight">
            <span className="text-base sm:text-lg font-bold text-white">Ставропольский колледж связи</span>
            <span className="text-[10px] sm:text-xs text-white/60">имени Героя Советского Союза В.А. Петрова</span>
          </Link>

          <nav className="hidden lg:flex items-center justify-center gap-0.5" aria-label="Главное меню">
            {siteNavigation.map((section) => {
              const hasSubs = section.subsections.length > 0
              const isOpen = openMenu === section.slug
              return (
                <div
                  key={section.slug}
                  data-nav-item
                  className="relative"
                  onMouseEnter={() => hasSubs && openMenuDelayed(section.slug)}
                  onMouseLeave={() => hasSubs && closeMenuDelayed()}
                >
                  <Link
                    href={section.href}
                    onFocus={() => hasSubs && openMenuDelayed(section.slug)}
                    onBlur={() => hasSubs && closeMenuDelayed()}
                    aria-expanded={hasSubs ? isOpen : undefined}
                    aria-haspopup={hasSubs ? "true" : undefined}
                    className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-white/80 hover:text-white transition-colors rounded-md"
                  >
                    {section.title}
                    {hasSubs && (
                      <ChevronDown size={14} className={`transition-transform duration-200 ${isOpen ? "rotate-180" : ""}`} />
                    )}
                  </Link>

                  {hasSubs && (
                    <div
                      className={`absolute left-1/2 top-full z-50 w-64 -translate-x-1/2 pt-2 transition-all duration-150 ease-out ${
                        isOpen ? "visible translate-y-0 opacity-100" : "invisible -translate-y-1 opacity-0"
                      }`}
                    >
                      <div className="overflow-hidden rounded-lg border border-border bg-white shadow-xl">
                        {section.subsections.map((sub) => (
                          <Link
                            key={sub.slug}
                            href={sub.href}
                            className="block border-b border-border/50 px-4 py-2.5 text-sm text-fg transition-colors last:border-b-0 hover:bg-muted hover:text-primary"
                          >
                            {sub.title}
                          </Link>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )
            })}

            <Link
              href="/login"
              className="ml-2 flex items-center gap-1.5 rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white transition-all duration-200 hover:bg-white hover:text-accent"
            >
              <User size={16} />
              Войти
            </Link>
          </nav>

          <div className="flex items-center justify-end gap-2">
            <button onClick={() => setMobileOpen(!mobileOpen)} className="lg:hidden rounded-md p-2 text-white/80 hover:bg-white/10" aria-label="Меню">
              {mobileOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-b border-white/10 bg-accent px-4 pb-4 pt-2">
          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => {
              const hasSubs = section.subsections.length > 0
              const isOpen = openMobileSection === section.slug
              return (
                <div key={section.slug}>
                  <div className="flex items-center justify-between gap-2">
                    <Link
                      href={section.href}
                      className="block flex-1 px-3 py-2 text-sm font-medium text-white/80 rounded-md hover:bg-white/10"
                      onClick={() => setMobileOpen(false)}
                    >
                      {section.title}
                    </Link>
                    {hasSubs && (
                      <button
                        onClick={() => setOpenMobileSection(isOpen ? null : section.slug)}
                        className="rounded-md p-2 text-white/80 hover:bg-white/10"
                        aria-label={`Показать подпункты раздела ${section.title}`}
                      >
                        <ChevronDown size={16} className={`transition-transform duration-200 ${isOpen ? "rotate-180" : ""}`} />
                      </button>
                    )}
                  </div>
                  {hasSubs && (
                    <div className={`overflow-hidden transition-all duration-200 ease-out ${isOpen ? "max-h-96" : "max-h-0"}`}>
                      <div className="ml-4 border-l border-white/20 pl-3">
                        {section.subsections.map((sub) => (
                          <Link
                            key={sub.slug}
                            href={sub.href}
                            onClick={() => setMobileOpen(false)}
                            className="block px-3 py-1.5 text-sm text-white/70 rounded-md hover:bg-white/10 hover:text-white"
                          >
                            {sub.title}
                          </Link>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )
            })}

            <Link
              href="/login"
              onClick={() => setMobileOpen(false)}
              className="mt-2 flex items-center justify-center gap-1.5 rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-white hover:text-accent"
            >
              <User size={16} />
              Войти
            </Link>
          </nav>
        </div>
      )}
    </header>
  )
}
```

- [ ] **Step 2: Проверить типы и сборку**

Run: `npx tsc --noEmit` (в `CollegeLMS.Next/`) и `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "feat: dropdown-меню в хедере с hover-анимацией, кнопка Войти, ссылка Max"
```

### Task 2: Страница входа — логотип-ссылка, быстрый вход выше формы

**Files:**
- Modify: `CollegeLMS.Next/app/login/page.tsx`

**Interfaces:**
- Consumes: существующая форма входа (QUICK_LOGINS, handleSubmit) — сохранить
- Produces: десктоп-логотип обёрнут в `Link href="/"`, блок «Быстрый вход» — под заголовком «Личный кабинет», форма — ниже

- [ ] **Step 1: Обернуть десктоп-логотип в ссылку и перенести быстрый вход**

Заменить фрагмент (строки 49-64):

```tsx
      <div className="hidden lg:flex flex-col items-center justify-center bg-gradient-to-br from-[#24386a] to-[#3B7DD8] p-12">
        <div className="max-w-md">
          <Link href="/">
            <Image
              src="/logo.svg"
              alt="Ставропольский колледж связи"
              width={300}
              height={200}
              className="w-full h-auto drop-shadow-[0_4px_12px_rgba(0,0,0,0.3)]"
              unoptimized
            />
          </Link>
          <h2 className="mt-8 text-center text-xl font-semibold text-white/90">
            ГБПОУ — Ставропольский колледж связи<br />
            имени Героя Советского Союза В.А. Петрова
          </h2>
        </div>
      </div>
```

Заменить фрагмент (строки 83-151): заголовок → блок быстрого входа → форма. Новый порядок:

```tsx
          <h1 className="mb-4 text-2xl font-semibold text-primary text-center">Личный кабинет</h1>

          <div className="mb-6 rounded-md border border-border bg-muted/50 p-3">
            <p className="mb-1.5 text-xs text-accent-lighter">Быстрый вход (разработка)</p>
            <select
              onChange={(e) => {
                const account = QUICK_LOGINS.find(a => a.role === e.target.value)
                if (account) {
                  setLoginInput(account.login)
                  setPassword(account.password)
                }
              }}
              defaultValue=""
              className="w-full rounded-md border border-input bg-white px-3 py-1.5 text-xs text-fg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
            >
              <option value="" disabled>Выберите роль...</option>
              {QUICK_LOGINS.map(a => (
                <option key={a.role} value={a.role}>{a.label}</option>
              ))}
            </select>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-5">
            {/* ... существующие поля и кнопка — без изменений ... */}
          </form>
```

Затем удалить старый блок «Быстрый вход» внизу (строки 133-151, от `<div className="mt-6 pt-6 border-t border-border">` до `</div>`) — он перенесён выше.

- [ ] **Step 2: Проверить сборку**

Run: `npx tsc --noEmit` и `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "feat: логотип-ссылка на /login, быстрый вход выше формы"
```

### Task 3: Редирект `/lms` по роли

**Files:**
- Modify: `CollegeLMS.Next/app/lms/page.tsx` (полная замена)

**Interfaces:**
- Consumes: `useAuth()` из `@/lib/auth` (возвращает `{ token, user }`, `user.role: "Admin" | "Teacher" | "Student" | "Dispatcher"`)
- Produces: страница-редиректор; Student → `/my/dashboard`, Teacher → `/teacher/dashboard`, Admin → `/admin`, Dispatcher → `/schedule` (временный целевой путь до Блока 2, где появится `/dispatcher/dashboard`), без токена → `/login`

- [ ] **Step 1: Заменить содержимое `app/lms/page.tsx`**

```tsx
"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth"

export default function LmsRedirectPage() {
  const { token, user } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!token) {
      router.replace("/login")
      return
    }
    switch (user?.role) {
      case "Admin":
        router.replace("/admin")
        break
      case "Teacher":
        router.replace("/teacher/dashboard")
        break
      case "Dispatcher":
        router.replace("/schedule")
        break
      default:
        router.replace("/my/dashboard")
    }
  }, [token, user, router])

  return null
}
```

- [ ] **Step 2: Проверить сборку**

Run: `npx tsc --noEmit` и `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "feat: /lms редирект по роли пользователя"
```

### Task 4: Поиск — убрать дублирующий пустой блок, показать последние новости

**Files:**
- Modify: `CollegeLMS.Next/app/(public)/search/page.tsx` (блок `{!query && ...}`, строки 267-274)

**Interfaces:**
- Consumes: `api.get<Result<NewsResponse>>("/api/news", { params: { page: 1, pageSize: 3 } })` — публичный эндпоинт
- Produces: при пустом запросе — секция «Последние новости» (3 карточки: картинка, дата, заголовок) вместо текста «Введите поисковый запрос...»

- [ ] **Step 1: Добавить состояние и загрузку последних новостей**

В компоненте `SearchResults` добавить:

```tsx
  const [recentNews, setRecentNews] = useState<NewsResponse[]>([])

  useEffect(() => {
    if (query) return
    api
      .get<Result<NewsResponse[]>>("/api/news", { params: { page: 1, pageSize: 3 } })
      .then((res) => {
        const body = res.data
        if (body.isSuccess && body.data) {
          setRecentNews(Array.isArray(body.data) ? body.data : [])
        }
      })
      .catch(() => setRecentNews([]))
  }, [query])
```

- [ ] **Step 2: Заменить пустой блок**

Заменить (строки 267-274):

```tsx
      {!query && (
        <div className="rounded-lg border border-border bg-card p-8 text-center">
          <SearchIcon className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
          <p className="text-muted-foreground">
            Введите поисковый запрос для поиска по новостям и страницам сайта
          </p>
        </div>
      )}
```

на:

```tsx
      {!query && (
        <section className="mt-6">
          <h2 className="mb-4 text-lg font-semibold text-fg">Последние новости</h2>
          {recentNews.length === 0 ? (
            <div className="rounded-lg border border-border bg-card p-8 text-center">
              <p className="text-muted-foreground">Новостей пока нет</p>
            </div>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {recentNews.map((n) => (
                <Link
                  key={n.id}
                  href={`/news/${n.id}`}
                  className="group overflow-hidden rounded-lg border border-border bg-card transition-shadow hover:shadow-lg"
                >
                  {n.imageUrl && (
                    <div className="relative h-40 w-full overflow-hidden">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={n.imageUrl}
                        alt=""
                        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                      />
                    </div>
                  )}
                  <div className="p-4">
                    <p className="mb-1 text-xs text-muted-foreground">
                      {new Date(n.publishedAt).toLocaleDateString("ru-RU")}
                      {n.categoryName && ` · ${n.categoryName}`}
                    </p>
                    <p className="line-clamp-2 text-sm font-medium text-fg group-hover:text-primary">
                      {n.title}
                    </p>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </section>
      )}
```

- [ ] **Step 3: Проверить импорты** — `NewsResponse` должен быть импортирован из `@/types`; если нет — добавить в существующий import.

- [ ] **Step 4: Проверить сборку**

Run: `npx tsc --noEmit` и `npm run build`
Expected: без ошибок

- [ ] **Step 5: Коммит**

```bash
git add -A && git commit -m "feat: на поиске без запроса показываем последние новости"
```

### Task 5: Постер детальной новости — 1/3 описание на синем фоне + 2/3 картинка

**Files:**
- Modify: `CollegeLMS.Next/app/(public)/news/[id]/page.tsx`

**Interfaces:**
- Consumes: `news: NewsResponse` (imageUrl, title, publishedAt, categoryName, content)
- Produces: вёрстка `grid lg:grid-cols-3`: левая колонка `bg-primary` (дата, категория, заголовок, анонс из 2-3 строк), правая `lg:col-span-2` — картинка `object-cover` с кликом на лайтбокс

- [ ] **Step 1: Добавить хелпер-анонс (очистка HTML)**

В `SearchResults`-стиле — в `NewsDetailPage` добавить перед `return`:

```tsx
  const excerpt = useMemo(() => {
    if (!news) return ""
    const text = news.content
      .replace(/<[^>]+>/g, " ")
      .replace(/\s+/g, " ")
      .trim()
    return text.length > 200 ? `${text.slice(0, 200)}…` : text
  }, [news])
```

- [ ] **Step 2: Заменить блок «назад + постер + дата + заголовок»**

Заменить фрагмент от `<Button variant="ghost" ...>` до `</h1>` (строки 140-172) на:

```tsx
      <Button variant="ghost" size="sm" className="mb-6" asChild>
        <Link href="/news">← Все новости</Link>
      </Button>

      {allImages.length > 0 ? (
        <div className="mb-8 grid grid-cols-1 overflow-hidden rounded-xl border border-border shadow-sm lg:grid-cols-3">
          <div className="flex flex-col justify-center gap-3 bg-primary p-6 text-primary-foreground lg:p-8">
            <p className="text-sm text-primary-foreground/80">
              {new Date(news.publishedAt).toLocaleDateString("ru-RU", {
                year: "numeric",
                month: "long",
                day: "numeric",
              })}
              {news.categoryName && ` · ${news.categoryName}`}
            </p>
            <h1 className="text-xl font-bold leading-tight sm:text-2xl">{news.title}</h1>
            {excerpt && <p className="line-clamp-3 text-sm text-primary-foreground/90">{excerpt}</p>}
          </div>
          <button
            onClick={() => { setGalleryIndex(0); setGalleryOpen(true) }}
            className="group lg:col-span-2 lg:min-h-[320px] overflow-hidden rounded-r-xl text-left"
            aria-label="Открыть фотографию"
          >
            <Image
              src={allImages[0]}
              alt=""
              width={0}
              height={0}
              sizes="(min-width: 1024px) 66vw, 100vw"
              className="h-full max-h-[420px] w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
              style={{ width: "100%", height: "100%", maxHeight: "420px" }}
            />
          </button>
        </div>
      ) : (
        <div className="mb-8 rounded-xl bg-primary p-6 lg:p-8">
          <p className="mb-2 text-sm text-primary-foreground/80">
            {new Date(news.publishedAt).toLocaleDateString("ru-RU", {
              year: "numeric",
              month: "long",
              day: "numeric",
            })}
            {news.categoryName && ` · ${news.categoryName}`}
          </p>
          <h1 className="text-2xl font-bold leading-tight text-primary-foreground sm:text-3xl">
            {news.title}
          </h1>
        </div>
      )}
```

- [ ] **Step 3: Проверить сборку**

Run: `npx tsc --noEmit` и `npm run build`
Expected: без ошибок

- [ ] **Step 4: Коммит**

```bash
git add -A && git commit -m "feat: постер новости — описание на синем фоне 1/3 + картинка 2/3"
```

### Task 6: Отступы галереи в контенте новости

**Files:**
- Modify: `CollegeLMS.Next/app/globals.css` (после `.docs-content img` блока, ~строка 352)

**Interfaces:**
- Consumes: WP-разметка галерей `.gallery` / `dl.gallery-item` внутри HTML-контента (санитизируется ContentRenderer — inline style удаляется)
- Produces: `.docs-content .gallery` — grid с gap 1rem; картинки плиток — во всю ширину ячейки

- [ ] **Step 1: Добавить стили галереи**

```css
.docs-content .gallery {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
  margin: 1rem 0;
}

.docs-content .gallery-item {
  margin: 0;
  text-align: center;
}

.docs-content .gallery-item img {
  width: 100%;
  height: auto;
  object-fit: cover;
  margin: 0;
  border: 1px solid hsl(var(--border));
}

.docs-content .gallery-caption {
  margin-top: 0.375rem;
  font-size: 0.8125rem;
  color: hsl(var(--muted-fg));
}
```

- [ ] **Step 2: Проверить сборку**

Run: `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "fix: отступы между плитками галереи в контенте новостей"
```

### Task 7: Контакты — выровнять блоки контактов и карту

**Files:**
- Modify: `CollegeLMS.Next/app/(public)/contacts/page.tsx`

**Interfaces:**
- Consumes: текущая сетка `grid gap-8 lg:grid-cols-2` (левая колонка — контакты, правая — iframe карты)
- Produces: обе колонки одинаковой высоты; карта растягивается на всю высоту колонки

- [ ] **Step 1: Выровнять колонки**

Прочитать файл и изменить: обёртке карты добавить `h-full` и `min-h-[400px]`, iframe — `h-full`:

```tsx
<div className="h-full min-h-[400px] overflow-hidden rounded-lg border">
  <iframe
    src="https://www.google.com/maps?..."
    className="h-full w-full"
    style={{ minHeight: "400px", border: 0 }}
    loading="lazy"
    referrerPolicy="no-referrer-when-downgrade"
    allowFullScreen
  />
</div>
```

И на родительской сетке у левой колонки — `flex flex-col justify-between` (если блоки контактов раздельные) либо просто `items-start`. Ключевое требование: карта по высоте равна колонке контактов (или наоборот) — проверяется визуально на `lg`.

- [ ] **Step 2: Проверить сборку**

Run: `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "fix: контакты — выравнивание блока контактов и карты"
```

### Task 8: Футер — убрать дублирующую информацию

**Files:**
- Modify: `CollegeLMS.Next/components/Footer.tsx`

**Interfaces:**
- Produces: без мёртвого `infoLinks`, без дублей ссылок и названия колледжа; битые ссылки удалены/исправлены

- [ ] **Step 1: Исправить Footer.tsx**

- Удалить массив `infoLinks` (строки 5-12) — не используется
- В бренд-блоке убрать дублирование названия: оставить один `<span>` «ГБПОУ «Ставропольский колледж связи имени Героя Советского Союза В.А. Петрова»» вместо «ссылка + подпись»
- Колонки:
  - «Образование»: убрать «Профессии» (`/professions`) и «Доп. образование» (`/additional-education`) — страниц нет, ведут в 404. Оставить «Специальности» (`/specialties`) и добавить «Курсы» (`/education/kursyi`) + «Целевое обучение» (`/education/tselevoe-obuchenie`)
  - «Документы»: заменить ссылки на существующие страницы раздела «Колледж → Сведения об ОО» (`/about`) — например: «Устав и лицензия» (`/about/ustav-kolledzha`), «Сведения об ОО» (`/about`). Если страниц с точными slug нет — оставить «Сведения об ОО» (`/about`) как единственную ссылку колонки и удалить битые
  - Внизу: «Сведения об образовательной организации» (`/sveden` — 404) → заменить на `/about`; «Политика конфиденциальности» (`/privacy` — 404) → удалить
- «Реквизиты» (ИНН/КПП/ОГРН): оставить как текст (не ссылки `#`)

- [ ] **Step 2: Проверить сборку**

Run: `npx tsc --noEmit` и `npm run build`
Expected: без ошибок

- [ ] **Step 3: Коммит**

```bash
git add -A && git commit -m "fix: футер — убраны дубли и битые ссылки"
```

### Task 9: Раздел «Достижения» — Профессионалы и Мастер года

**Files:**
- Modify: `CollegeLMS.Next/data/site-content.ts` (добавить раздел)
- Modify: `CollegeLMS.Next/data/page-contents.json` (добавить 2 ключа)
- Create: `CollegeLMS.Next/app/(public)/achievements/[[...slug]]/page.tsx`

**Interfaces:**
- Consumes: `wp_data_full.json` (дамp WP: `posts[].slug`, `posts[].content.rendered` — UTF-8; для «Мастер года» slug = `master-goda`); раздел рендерится через существующий `SectionPage` (`sectionSlug="achievements"`)
- Produces: пункт «Достижения» в главном меню с подпунктами «Профессионалы» (`/achievements/professionaly`) и «Мастер года» (`/achievements/master-year`)

- [ ] **Step 1: Добавить раздел в siteNavigation**

После раздела «Студенту» (строка 73) вставить:

```tsx
  {
    title: "Достижения",
    slug: "achievements",
    href: "/achievements",
    subsections: [
      { title: "Профессионалы", slug: "professionaly", href: "/achievements/professionaly", content: "" },
      { title: "Мастер года", slug: "master-year", href: "/achievements/master-year", content: "" },
    ],
  },
```

- [ ] **Step 2: Создать route-страницу**

Создать `CollegeLMS.Next/app/(public)/achievements/[[...slug]]/page.tsx`:

```tsx
import SectionPage from "@/components/SectionPage"
import { type Metadata } from "next"

export const metadata: Metadata = {
  title: "Достижения",
}

export default function Page({ params }: { params: { slug?: string[] } }) {
  return <SectionPage sectionSlug="achievements" slug={params.slug} />
}
```

- [ ] **Step 3: Извлечь и подготовить контент из дампа**

Скрипт (временный, из `C:\Users\asv\AppData\Local\Temp\opencode\extract-achievements.ps1`):

```powershell
$s = Get-Content -Raw -Encoding UTF8 "import/wp_data_full.json"
$json = $s | ConvertFrom-Json

# «Мастер года» — пост master-goda
$master = $json.posts | Where-Object { $_.slug -eq "master-goda" } | Select-Object -First 1
$masterContent = [System.Net.WebUtility]::HtmlDecode(($master.content.rendered -replace '<[^>]+>', ' ' -replace '\s+', ' '))

# «Профессионалы» — сводка из ключевых постов чемпионата
$proSlugs = @("championat-professionaly", "professionaly-iz-kolledzha-svyazi", "regionalnyj-etap-chempionat-professionaly-2025-stavropolskij-kraj")
$proPosts = $json.posts | Where-Object { $proSlugs -contains $_.slug } | Select-Object -First 3

$proText = "Чемпионатное движение «Профессионалы» — это всероссийские соревнования по профессиональному мастерству среди студентов колледжей и техникумов. Студенты Ставропольского колледжа связи регулярно участвуют в чемпионате и занимают призовые места."
foreach ($p in $proPosts) {
  $t = $p.title.rendered
  $d = [System.Net.WebUtility]::HtmlDecode(($p.content.rendered -replace '<[^>]+>', ' ' -replace '\s+', ' '))
  if ($d.Length -gt 240) { $d = $d.Substring(0, 240) + "…" }
  $proText += "`n<h3>$t</h3>`n<p>$d</p>`n"
}

Set-Content -Path "C:\Users\asv\AppData\Local\Temp\opencode\achievements-content.json" -Value (@{ professionaly = $proText; masterYear = $masterContent } | ConvertTo-Json -Depth 2) -Encoding UTF8
Write-Output "OK — saved"
```

Выполнить и посмотреть результат (`Get-Content -Encoding UTF8 ...`). Если контент нечитаем в консоли (кодировка), читать через `read` tool из временного файла.

- [ ] **Step 4: Вставить контент в page-contents.json**

Прочитать `page-contents.json` (формат: `{ "slug": { "id": ..., "content": "...", "title": "..." }, ... }`), добавить ключи:

- `"professionaly"`: `{ "content": "<HTML из proText>", "title": "Профессионалы" }`
- `"master-year"`: `{ "content": "<HTML из masterContent>", "title": "Мастер года" }`

Оформить по единому стилю (см. Task 10): текст в `<p>`, списки в `<ul><li>`, подзаголовки в `<h3>`. Если итоговый текст слишком длинный (>600 слов) — сократить до сути (описание движения/конкурса + 2-3 результата).

- [ ] **Step 5: Проверить**

Run: `npm run dev` (или `npm run build`), открыть `/achievements/professionaly` и `/achievements/master-year`
Expected: страницы рендерятся с контентом, меню показывает подпункты

- [ ] **Step 6: Коммит**

```bash
git add -A && git commit -m "feat: раздел Достижения — Профессионалы и Мастер года"
```

### Task 10: Единый стиль контента всех разделов + аудит ссылок

**Files:**
- Modify: `CollegeLMS.Next/app/globals.css` (.docs-content — усилить)
- Modify: `CollegeLMS.Next/data/page-contents.json` (правки контента по результатам аудита)
- Temp: скрипт аудита ссылок

**Interfaces:**
- Produces: контент всех подстраниц 5 разделов в едином виде: заголовки h2/h3, маркированные списки, таблицы, цитаты, картинки; ссылки проверены — битые и нерабочие удалены

- [ ] **Step 1: Усилить .docs-content (типографика и компоненты)**

Добавить в `globals.css` после существующих `.docs-content` правил:

```css
.docs-content {
  font-size: 1rem;
  color: hsl(var(--fg));
}

.docs-content h2 {
  font-size: 1.375rem;
}

.docs-content h4 {
  font-size: 1.0625rem;
  font-weight: 600;
  margin-top: 1rem;
  margin-bottom: 0.5rem;
  color: hsl(var(--fg));
}

.docs-content p {
  font-size: 1rem;
}

.docs-content ul {
  list-style: none;
  padding-left: 0;
}

.docs-content ul > li {
  position: relative;
  padding-left: 1.5rem;
  margin-bottom: 0.5rem;
}

.docs-content ul > li::before {
  content: "";
  position: absolute;
  left: 0;
  top: 0.65rem;
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 9999px;
  background: hsl(var(--accent));
}

.docs-content a {
  text-decoration: none;
  border-bottom: 1px solid hsl(var(--accent) / 0.4);
  transition: border-color 0.15s ease, color 0.15s ease;
}

.docs-content a:hover {
  border-bottom-color: hsl(var(--accent));
  opacity: 1;
}

.docs-content .wp-caption,
.docs-content figure {
  margin: 1rem 0;
}

.docs-content .wp-caption-text,
.docs-content figcaption {
  margin-top: 0.5rem;
  text-align: center;
  font-size: 0.8125rem;
  color: hsl(var(--muted-fg));
}

.docs-content iframe {
  max-width: 100%;
  border-radius: 0.5rem;
}

.docs-content .docs-button {
  display: inline-block;
  margin: 0.25rem 0.5rem 0.25rem 0;
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-weight: 600;
  transition: opacity 0.15s ease;
}

.docs-content .docs-button:hover {
  opacity: 0.85;
  border-bottom: none;
}
```

- [ ] **Step 2: Написать скрипт аудита ссылок**

Создать `scripts/check-content-links.ps1`:

```powershell
# Аудит ссылок в page-contents.json
$jsonPath = "CollegeLMS.Next\data\page-contents.json"
$json = Get-Content -Raw -Encoding UTF8 $jsonPath | ConvertFrom-Json
$report = @()

foreach ($prop in $json.PSObject.Properties) {
  $entry = $prop.Value
  if (-not $entry -or -not $entry.content) { continue }
  $matches = [regex]::Matches($entry.content, 'href="([^"]+)"')
  foreach ($m in $matches) {
    $href = $m.Groups[1].Value
    if ($href -match '^(mailto:|tel:|#)') { continue }
    if ($href -match '^/(wp-content|wp-includes)') { $href = "https://stvcc.ru$href" }
    elseif ($href -match '^/') { $href = "https://stvcc.ru$href" }
    $report += [pscustomobject]@{ Page = $prop.Name; Url = $href }
  }
}

$report | ConvertTo-Json -Depth 2 | Set-Content "scripts\content-links-report.json" -Encoding UTF8
Write-Output "Links found: $($report.Count)"
```

- [ ] **Step 3: Проверить каждую ссылку**

Run: `powershell -File scripts/check-content-links.ps1`, затем проверить статусы:

```powershell
$links = Get-Content -Raw "scripts\content-links-report.json" | ConvertFrom-Json
foreach ($l in $links) {
  try {
    $r = Invoke-WebRequest -Uri $l.Url -Method Head -TimeoutSec 15 -UseBasicParsing
    "$($r.StatusCode) | $($l.Page) | $($l.Url)"
  } catch {
    "ERR | $($l.Page) | $($l.Url) | $($_.Exception.Message)"
  }
}
```

Обработка результата:
- 200 → оставить
- 301/302 → обновить на финальный URL (из `$r.BaseResponse.RequestMessage.RequestUri`)
- 404/ERR → проверить на stvcc.ru вручную (если сайт доступен); если нерабочая → удалить ссылку/абзац с ней из контента
- stvcc.ru недоступен → пометить, что контент со ссылками на stvcc.ru оставить, но добавить TODO на перепроверку (в отчёт не входит)

- [ ] **Step 4: Вычистить контент (единый вид)**

Пройтись по `page-contents.json` — каждый раздел по чек-листу:
- Убрать `<script>`, `<style>`, пустые `<span style="...">` (если остались) — ContentRenderer уже санитизирует, но очистка улучшает рендер
- `&nbsp;` в начале/конце абзацев — убрать
- Заголовки: `<strong>` в начале абзаца → `<h3>` (если это подзаголовок)
- Ссылки-кнопки на PDF (Устав, Лицензия и т.п.) → обернуть в `<a class="docs-button" href="...">`
- Таблицы оставить (стилизованы `.docs-content table`)
- Фотографии с подписями → `<figure><img><figcaption>`

Конкретные правки вносить по результату просмотра каждой подстраницы через `npm run dev` (список подстраниц — из `siteNavigation`). ВАЖНО: править только оформление, не смысл текста.

- [ ] **Step 5: Проверить и закоммитить**

Run: `npm run build`; открыть 5-6 страниц разделов в браузере (Playwright, если нужно) — визуально единый стиль
```bash
git add -A && git commit -m "feat: единый стиль контента разделов + аудит ссылок"
```

### Task 11: AGENTS.md — устройства для проверки адаптивности

**Files:**
- Modify: `AGENTS.md`

- [ ] **Step 1: Добавить абзац в раздел «Соглашения по фронтенду»**

После строки про контейнер `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8` добавить:

```markdown
- **Адаптивность**: проверять на ноутбуке Toshiba A665 12k (1366×768) и телефоне Xiaomi Mi 9 SE (2340×1080, viewport ~393px), а также на широких экранах (1920+). Все страницы должны корректно отображаться на всех перечисленных устройствах
```

- [ ] **Step 2: Коммит**

```bash
git add -A && git commit -m "docs: AGENTS.md — устройства для проверки адаптивности"
```

---

## Self-Review (Block 1)

| Задача пользователя | Task |
|---|---|
| Логотип → ссылка на корень | Task 2 (плюс уже есть в Header) |
| Выпадающие меню + анимация | Task 1 |
| Канал Max | Task 1 |
| Контент единым стилем + проверка ссылок | Task 10 |
| Дублирующий блок на поиске | Task 4 |
| Постер новости 1/3 + 2/3 | Task 5 |
| Достижения → Профессионалы, Мастер года | Task 9 |
| Кнопка «Войти» | Task 1 |
| Контакты — выравнивание | Task 7 |
| Футер — дубли | Task 8 |
| Быстрый вход выше формы | Task 2 |
| Галерея — отступы | Task 6 |
| AGENTS.md — устройства | Task 11 |



