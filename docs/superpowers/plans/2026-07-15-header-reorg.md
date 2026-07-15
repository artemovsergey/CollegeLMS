# Header Reorganization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Уместить строку поиска и навигацию в `max-w-7xl` без горизонтального скролла

**Architecture:** Перегруппировка 6 разделов навигации в 4 + компактный поиск-иконка

**Tech Stack:** Next.js 14, TypeScript, Tailwind CSS 4, Lucide React

## Global Constraints

- Use `data/site-content.ts` as the single source of truth for navigation
- All existing slugs must remain valid (pages already exist)
- Search icon navigates to `/search` via `next/link`
- Header container: `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8`

---

### Task 1: Перегруппировать данные навигации

**Files:**
- Modify: `CollegeLMS.Next/data/site-content.ts`

**Interfaces:**
- Consumes: existing `Section`, `Subsection` types (unchanged)
- Produces: `siteNavigation` array with 4 sections instead of 6

- [ ] **Step 1: Заменить `siteNavigation` на 4 раздела**

Заменить весь массив `siteNavigation` в `CollegeLMS.Next/data/site-content.ts`:

```ts
export const siteNavigation: Section[] = [
  {
    title: "Колледж",
    slug: "college",
    href: "/college",
    subsections: [
      { title: "О колледже", slug: "o-kolledzhe", href: "/college/o-kolledzhe", content: "" },
      { title: "История создания", slug: "istoriya-sozdaniya-kolledzha", href: "/college/istoriya-sozdaniya-kolledzha", content: "" },
      { title: "Воспитательная работа", slug: "vospitatelnaya-rabota", href: "/college/vospitatelnaya-rabota", content: "" },
      { title: "Наставничество", slug: "nastavnichestvo", href: "/college/nastavnichestvo", content: "" },
      { title: "Общежитие", slug: "obshhezhitie", href: "/college/obshhezhitie", content: "" },
      { title: "Профсоюзная организация", slug: "pervichnaya-profsoyuznaya-organizatsiya", href: "/college/pervichnaya-profsoyuznaya-organizatsiya", content: "" },
      { title: "Независимая оценка качества", slug: "nezavisimaya-otsenka-kachestva-uslovij-osushhestvleniya-obrazovatelnoj-deyatelnosti", href: "/college/nezavisimaya-otsenka-kachestva-uslovij-osushhestvleniya-obrazovatelnoj-deyatelnosti", content: "" },
      { title: "План работы", slug: "plan-raboty-gbpou-sks-na-tekushhij-uchebnyj-god", href: "/college/plan-raboty-gbpou-sks-na-tekushhij-uchebnyj-god", content: "" },
      { title: "Контакты", slug: "polnaya-kontaktnaya-informatsiya", href: "/college/polnaya-kontaktnaya-informatsiya", content: "" },
      { title: "Сведения об ОО", slug: "svedeniya-oo", href: "/about", content: "" },
    ],
  },
  {
    title: "Обучение",
    slug: "education",
    href: "/education",
    subsections: [
      { title: "Специальности", slug: "perechen-spetsialnostey", href: "/education/perechen-spetsialnostey", content: "" },
      { title: "Курсы", slug: "kursyi", href: "/education/kursyi", content: "" },
      { title: "Целевое обучение", slug: "tselevoe-obuchenie", href: "/education/tselevoe-obuchenie", content: "" },
      { title: "Оплата услуг", slug: "oplata-uslug", href: "/education/oplata-uslug", content: "" },
      { title: "Образовательный кредит", slug: "obrazovatelnyj-kredit", href: "/education/obrazovatelnyj-kredit", content: "" },
    ],
  },
  {
    title: "Поступление",
    slug: "admissions",
    href: "/admissions",
    subsections: [
      { title: "Приемная комиссия", slug: "priemnaya-komissiya", href: "/admissions/priemnaya-komissiya", content: "" },
      { title: "Специальности", slug: "perechen-spetsialnostey", href: "/admissions/perechen-spetsialnostey", content: "" },
      { title: "Дни открытых дверей", slug: "den-otkrytyh-dverej-2026", href: "/admissions/den-otkrytyh-dverej-2026", content: "" },
      { title: "Приказы о зачислении", slug: "prikazy-na-zachislenie-2025", href: "/admissions/prikazy-na-zachislenie-2025", content: "" },
      { title: "Документы для приема", slug: "kakie-dokumentyi-neobhodimo-prinesti", href: "/admissions/kakie-dokumentyi-neobhodimo-prinesti", content: "" },
      { title: "Общежитие", slug: "obshhezhitie", href: "/admissions/obshhezhitie", content: "" },
      { title: "Количество заявлений", slug: "kolichestvo-podannyih-zayavleniy", href: "/admissions/kolichestvo-podannyih-zayavleniy", content: "" },
      { title: "Задать вопрос", slug: "8008-2", href: "/admissions/8008-2", content: "" },
    ],
  },
  {
    title: "Студентам",
    slug: "student-life",
    href: "/student-life",
    subsections: [
      { title: "Расписание занятий", slug: "raspisanie-zanyatij-po-ochnoj-forme-obucheniya", href: "/student-life/raspisanie-zanyatij-po-ochnoj-forme-obucheniya", content: "" },
      { title: "Расписание экзаменов", slug: "raspisanie-ekzamenov", href: "/student-life/raspisanie-ekzamenov", content: "" },
      { title: "Государственная итоговая аттестация", slug: "raspisanie-gosudarstvennoj-itogovoj-attestatsii", href: "/student-life/raspisanie-gosudarstvennoj-itogovoj-attestatsii", content: "" },
      { title: "Задолженности", slug: "raspisanie-likvidatsii-akademicheskih-zadolzhennostej", href: "/student-life/raspisanie-likvidatsii-akademicheskih-zadolzhennostej", content: "" },
      { title: "Библиотека", slug: "biblioteka", href: "/student-life/biblioteka", content: "" },
      { title: "Дистанционное обучение", slug: "distancionnoeobuch", href: "/student-life/distancionnoeobuch", content: "" },
      { title: "Трудоустройство и карьера", slug: "trudoustroystvo-i-karera", href: "/graduates/trudoustroystvo-i-karera", content: "" },
      { title: "Центр содействия трудоустройству", slug: "tsentr-sodejstviya-trudoustrojstvu-vypusknikov", href: "/graduates/tsentr-sodejstviya-trudoustrojstvu-vypusknikov", content: "" },
      { title: "Актуальные вакансии", slug: "aktualnyie-vakansii", href: "/graduates/aktualnyie-vakansii", content: "" },
      { title: "Оставить резюме", slug: "ostavit-rezyume-dlya-poiska-rabotyi", href: "/graduates/ostavit-rezyume-dlya-poiska-rabotyi", content: "" },
      { title: "Полезные ссылки", slug: "poleznyie-ssyilki", href: "/graduates/poleznyie-ssyilki", content: "" },
    ],
  },
]
```

- [ ] **Step 2: Проверить сборку**

```powershell
cd CollegeLMS.Next && npm run build
```

- [ ] **Step 3: Commit**

```bash
git add CollegeLMS.Next/data/site-content.ts
git commit -m "fix: regroup navigation from 6 to 4 sections for compact header"
```

---

### Task 2: Заменить поиск на иконку + убрать мобильный поиск

**Files:**
- Modify: `CollegeLMS.Next/components/Header.tsx`

- [ ] **Step 1: Заменить форму поиска на иконку-ссылку**

В `CollegeLMS.Next/components/Header.tsx` заменить строки 73-84 (десктопный поиск):

```tsx
<Link
  href="/search"
  className="hidden md:flex items-center justify-center h-9 w-9 rounded-md text-muted-foreground hover:text-accent hover:bg-muted transition-colors"
  aria-label="Поиск"
>
  <Search size={18} />
</Link>
```

- [ ] **Step 2: Убрать форму поиска из мобильного меню**

Удалить блок со строками 105-116 (мобильный поиск `<form className="mb-3 md:hidden">...`)

- [ ] **Step 3: Проверить сборку**

```powershell
cd CollegeLMS.Next && npm run build
```

- [ ] **Step 4: Commit**

```bash
git add CollegeLMS.Next/components/Header.tsx
git commit -m "fix: replace search form with search icon in header"
```
