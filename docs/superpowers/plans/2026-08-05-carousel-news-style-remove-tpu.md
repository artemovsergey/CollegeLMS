# Карусель в стиле новости + удаление TPU — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Переоформить слайды карусели главной в стиль постера новости (синий блок + фото + логотип) и полностью удалить дизайн-пресет TPU.

**Architecture:** Все изменения только в `CollegeLMS.Next/`. Слайд карусели перестраивается по образцу шапки новости `news/[id]/page.tsx:154-182` (grid 1:2, синий `bg-primary` блок слева, фото справа, логотип в углу). Удаляются 9 TPU-компонентов, пресет `"tpu"` из `design-provider.tsx` и все ветки `design === "tpu"` в `page.tsx`, `SectionPage.tsx`, `Carousel.tsx`.

**Tech Stack:** Next.js 14 App Router, TypeScript, Tailwind CSS v4, embla-carousel-react, lucide-react.

## Global Constraints

- Все комментарии в коде на русском
- Без новых зависимостей (используем уже установленные: `embla-carousel-react`, `next/image`, `lucide-react`)
- Верстка адаптивная mobile-first: брейкпоинты `sm:`, `md:`, `lg:`
- Данные слайда: `NewsResponse` из `/api/news?page=1&pageSize=50`, слайды = `items.filter(n => n.imageUrl).slice(0, 5)`
- Высота карусели не меняется: `h-[400px] md:h-[550px]`
- Статус: `npm run build` и `npm run lint` без ошибок после каждого таска
- Коммиты: `git add -A` + сообщения на русском

---

### Task 1: Удалить 9 TPU-компонентов

**Files:**
- Delete: `CollegeLMS.Next/components/HomeTPU.tsx`
- Delete: `CollegeLMS.Next/components/CarouselTPU.tsx`
- Delete: `CollegeLMS.Next/components/SpecialtiesSectionTPU.tsx`
- Delete: `CollegeLMS.Next/components/AdmissionSectionTPU.tsx`
- Delete: `CollegeLMS.Next/components/NewsSectionTPU.tsx`
- Delete: `CollegeLMS.Next/components/StatisticsSectionTPU.tsx`
- Delete: `CollegeLMS.Next/components/FAQSectionTPU.tsx`
- Delete: `CollegeLMS.Next/components/FeedbackFormTPU.tsx`
- Delete: `CollegeLMS.Next/components/BreadcrumbsTPU.tsx`

**Interfaces:**
- Consumes: ничего
- Produces: файлы больше не существуют (сборка сломается до Task 2-4 — это ожидаемо, коммит не делаем до восстановления)

- [ ] **Step 1: Удалить файлы**

```bash
Remove-Item CollegeLMS.Next/components/HomeTPU.tsx, CollegeLMS.Next/components/CarouselTPU.tsx, CollegeLMS.Next/components/SpecialtiesSectionTPU.tsx, CollegeLMS.Next/components/AdmissionSectionTPU.tsx, CollegeLMS.Next/components/NewsSectionTPU.tsx, CollegeLMS.Next/components/StatisticsSectionTPU.tsx, CollegeLMS.Next/components/FAQSectionTPU.tsx, CollegeLMS.Next/components/FeedbackFormTPU.tsx, CollegeLMS.Next/components/BreadcrumbsTPU.tsx
```

- [ ] **Step 2: Убедиться, что файлов нет**

```bash
Get-ChildItem CollegeLMS.Next/components -Filter "*TPU*"
```
Expected: пустой вывод

- [ ] **Step 3: Коммит НЕ делать — сборка сломана (импорты), чиним в Task 2-4**

---

### Task 2: Упростить design-provider — только пресет default

**Files:**
- Modify: `CollegeLMS.Next/lib/design-provider.tsx`

**Interfaces:**
- Consumes: файл из Task 1 удалён
- Produces: `DesignPreset = "default"`, `DESIGN_PRESETS = ["default"]`, `DESIGN_LABELS = { default: "Стандартный" }`, `DEFAULT_DESIGN = "default"` — всё, что остаётся от контекста (API без изменений)

- [ ] **Step 1: Заменить `DesignPreset` и константы**

В `CollegeLMS.Next/lib/design-provider.tsx` заменить строки 5-14:

```tsx
export type DesignPreset = "default"

const DESIGN_PRESETS: DesignPreset[] = ["default"]

const DESIGN_LABELS: Record<DesignPreset, string> = {
  default: "Стандартный",
}

const DEFAULT_DESIGN: DesignPreset = "default"
```

- [ ] **Step 2: Проверить, что остальной код файла не меняется**

`localStorage.getItem("design-preset")` теперь может вернуть `"tpu"` от старых сессий — `DESIGN_PRESETS.includes("tpu")` вернёт `false`, и дизайн останется `default`. Это желаемое поведение, ничего дописывать не нужно.

- [ ] **Step 3: Проверка**

```bash
cd CollegeLMS.Next && npm run lint
```
Expected: PASS (design-provider не имеет ссылок на tpu после правки; другие файлы временно падают — пропускаем их ошибки)

---

### Task 3: Убрать ветку HomeTPU из главной

**Files:**
- Modify: `CollegeLMS.Next/app/(public)/page.tsx:19-20,132-136`

**Interfaces:**
- Consumes: Task 2 (тип `DesignPreset` без "tpu")
- Produces: `HomePage()` без переключений — обычный компонент

- [ ] **Step 1: Убрать импорт HomeTPU и useDesign**

В `CollegeLMS.Next/app/(public)/page.tsx` удалить строку 19 (`import { useDesign } from "@/lib/design-provider"`) и строку 20 (`import HomeTPU from "@/components/HomeTPU"`).

- [ ] **Step 2: Заменить переключатель на прямой рендер**

Заменить строки 132-136:

```tsx
export default function HomePage() {
  return <HomePageContent />
}
```

- [ ] **Step 3: Проверка**

```bash
cd CollegeLMS.Next && npm run lint
```
Expected: PASS

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "refactor: удалить TPU-компоненты и пресет дизайна TPU"
```

---

### Task 4: Убрать TPU-ветки из SectionPage

**Files:**
- Modify: `CollegeLMS.Next/components/SectionPage.tsx:6,11,28,55-65,76,82,87`

**Interfaces:**
- Consumes: Task 2
- Produces: `SectionPage` без `useDesign`, без `BreadcrumbsTPU`, с постоянными default-классами

- [ ] **Step 1: Убрать импорты**

В `CollegeLMS.Next/components/SectionPage.tsx` удалить:
- строку 6: `import BreadcrumbsTPU from "@/components/BreadcrumbsTPU"`
- строку 11: `import { useDesign } from "@/lib/design-provider"`

- [ ] **Step 2: Убрать useDesign и ветки классов**

Заменить строку 28 `const { design } = useDesign()` — удалить (переменная больше не нужна).

Заменить строки 55-65 на:

```tsx
  const titleClass = "text-primary"
  const boxClass = "rounded-lg border border-border bg-card p-8 text-center"
  const emptyClass = "text-muted-foreground"
```

И строку 76 заменить:

```tsx
        <Breadcrumbs
          items={[
            { label: section.title, href: section.href },
            { label: subsection.title },
          ]}
        />
```

- [ ] **Step 3: Проверка**

```bash
cd CollegeLMS.Next && npm run lint
```
Expected: PASS

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "refactor: убрать TPU-ветки из SectionPage"
```

---

### Task 5: Переоформить слайд карусели в стиль новости

**Files:**
- Modify: `CollegeLMS.Next/components/Carousel.tsx` (весь файл — упрощение и новый слайд)

**Interfaces:**
- Consumes: Task 2 (тип дизайна), Task 3 (паттерн удаления переключателя)
- Produces: `Carousel` — единственный компонент без `CarouselTPU`-переключателя; слайд: grid 2 колонки (синий блок + фото) с логотипом

- [ ] **Step 1: Убрать импорты и переключатель**

В `CollegeLMS.Next/components/Carousel.tsx`:
- удалить строку 10: `import { useDesign } from "@/lib/design-provider"`
- удалить строку 11: `import CarouselTPU from "./CarouselTPU"`
- переименовать функцию `CarouselDefault` → `Carousel`
- удалить строки 170-174 (переключатель) — в конце файла

- [ ] **Step 2: Переоформить слайд (строки 79-130)**

Заменить внутренность `.map(...)` (строки 80-129) на:

```tsx
              <Link
                key={item.id}
                href={`/news/${item.id}`}
                className="relative flex min-h-0 flex-[0_0_100%] flex-col overflow-hidden rounded-lg bg-primary h-[400px] md:h-[550px] lg:grid lg:grid-cols-3"
              >
                <div className="relative flex flex-1 flex-col justify-center gap-3 p-5 text-primary-foreground sm:p-6 lg:col-span-1 lg:h-full lg:p-10">
                  <div className="absolute left-4 top-4 z-10 sm:left-6 sm:top-6">
                    <Image
                      src="/logo.svg"
                      alt="Ставропольский колледж связи"
                      width={0}
                      height={0}
                      sizes="100vw"
                      className="object-contain h-auto"
                      style={{ width: "auto", height: "100%", maxHeight: "64px" }}
                      unoptimized
                    />
                  </div>
                  <p className="text-sm text-primary-foreground/80">
                    {new Date(item.publishedAt).toLocaleDateString("ru-RU", {
                      year: "numeric",
                      month: "long",
                      day: "numeric",
                    })}
                    {item.categoryName && ` · ${item.categoryName}`}
                  </p>
                  <h2 className="line-clamp-2 text-xl font-bold leading-tight sm:text-2xl md:text-3xl">
                    {item.title}
                  </h2>
                  <p className="line-clamp-3 text-sm text-primary-foreground/90">
                    {item.content.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim()}
                  </p>
                  <div className="mt-2">
                    <span className="inline-block rounded-full bg-white/20 px-6 py-2 text-sm font-medium text-white backdrop-blur-sm transition-colors hover:bg-white/30">
                      Подробнее
                    </span>
                  </div>
                </div>
                <div className="relative h-48 sm:h-56 lg:col-span-2 lg:h-full">
                  {item.imageUrl ? (
                    <Image
                      src={item.imageUrl}
                      alt=""
                      fill
                      sizes="(min-width: 1024px) 66vw, 100vw"
                      className="object-cover"
                    />
                  ) : (
                    <div className="absolute inset-0 bg-gradient-to-br from-lilac/80 via-primary/60 to-blue-900/80" />
                  )}
                </div>
              </Link>
```

- [ ] **Step 3: Логика мобильной/десктопной раскладки (проверить разметкой)**

- Мобильные (`< lg`): `flex flex-col`, контейнер `h-[400px]` — текст-блок `flex-1` занимает оставшуюся высоту, фото-блок фиксированный `h-48 sm:h-56` внизу
- Десктоп (`lg`): `grid grid-cols-3` — текст-блок `col-span-1` (слева, синий), фото `col-span-2` справа на всю высоту (`lg:h-full`)
- Логотип в обоих случаях в углу левого блока (`absolute left-4 top-4`)
- Гораздо: строки 97-98 (старый градиент на весь слайд) удалены — фон блока теперь `bg-primary`, фото с `rounded-lg` на контейнере слайда (скругление углов сохраняется)

- [ ] **Step 4: Сборка и lint**

```bash
cd CollegeLMS.Next && npm run build && npm run lint
```
Expected: PASS оба

- [ ] **Step 5: Визуальная проверка в Playwright**

```bash
cd CollegeLMS.Next && npx playwright test --ui  # либо скриншот главной вручную
```
Проверить: слайд в стиле новости (синий блок слева с датой/заголовком/анонсом/«Подробнее», логотип в углу, фото справа), на мобильном вьюпорте (375px) фото сверху + блок снизу.

- [ ] **Step 6: Коммит**

```bash
git add -A
git commit -m "feat: слайд карусели в стиле новости — синий блок, фото, логотип"
```

---

### Task 6: Финальная проверка отсутствия TPU

**Files:**
- Read: вся папка `CollegeLMS.Next`

**Interfaces:**
- Consumes: Task 1-5

- [ ] **Step 1: Поиск упоминаний tpu**

```bash
rg -ri "tpu" CollegeLMS.Next/app CollegeLMS.Next/components CollegeLMS.Next/lib
```
Expected: пусто (файл `contacts/page.tsx` содержит "tpu" только в URL карты — это не дизайн, оставить)

- [ ] **Step 2: Поиск в data и остальном**

```bash
rg -ri "tpu|TPU" CollegeLMS.Next --glob "!node_modules/**" --glob "!.next/**"
```
Expected: только `contacts/page.tsx` (URL карты) и `package-lock.json`/`package.json` (если есть совпадения в зависимостях)

- [ ] **Step 3: Полная проверка**

```bash
cd CollegeLMS.Next && npm run build && npm run lint
```
Expected: PASS

- [ ] **Step 4: Коммит при необходимости**

Если были правки в Task 5-6 — коммит уже сделан; если нашлись хвосты tpu — исправить и закоммитить `git add -A && git commit -m "refactor: убрать остатки TPU"`

---

### Task 7: Обновить Postman/Swagger НЕ требуется + спека закрыта

- Backend не менялся — Postman-коллекция и Swagger без изменений
- Обновить статус дизайн-спеки: `docs/superpowers/specs/2026-08-05-carousel-news-style-remove-tpu-design.md` — статус «реализовано» (заголовок `Статус: утверждено пользователем` → `Статус: реализовано`)

- [ ] **Step 1: Обновить статус спеки**

```bash
git add -A && git commit -m "docs: спека карусель/TPU — статус реализовано"
```
