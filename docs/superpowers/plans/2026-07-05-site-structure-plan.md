# Site Structure Implementation Plan

> **For agentic workers:** Execute tasks sequentially.

**Goal:** Build the full public college website with all sections matching the old WordPress site.

**Approach:** Hybrid — static content from JSON dump for sections, API for news.

**Tech Stack:** Next.js 14, Tailwind CSS v4, TypeScript, next-themes, embla-carousel

## Global Constraints
- Use Tailwind CSS v4 syntax
- All components in `components/` directory (not `components/ui/`)
- All content data in `data/site-content.ts`
- Pages in `app/(public)/` directory
- Logo in `public/logo.png`, favicon in `public/favicon.ico`

---

### Task 1: Download logo + generate favicon

**Files:**
- Create: `frontend/public/logo.png`
- Create: `frontend/public/favicon.ico`
- Create: `frontend/public/logo-white.png`

- [ ] Download logo from stvcc.ru and save to public/
- [ ] Generate favicon.ico from logo (use ImageMagick or ffmpeg)
- [ ] Commit

### Task 2: Create site-content.ts with all page data

**Files:**
- Create: `frontend/data/site-content.ts`

- [ ] Extract all top-level pages and their children from wp_data_full.json
- [ ] Type and export as structured data
- [ ] Commit

### Task 3: Install carousel dependency + create globals.css

**Files:**
- Modify: `frontend/package.json`
- Modify: `frontend/app/globals.css`

- [ ] Install embla-carousel-react
- [ ] Add accessibility CSS variables (font-size, contrast) to globals.css
- [ ] Commit

### Task 4: Create AccessibilityToggle + ThemeToggle components

**Files:**
- Create: `frontend/components/AccessibilityToggle.tsx`
- Create: `frontend/components/ThemeToggle.tsx`

- [ ] AccessibilityToggle: toggles class on `<html>`, stores in localStorage, applies font-size: 18px, high contrast, disables animations
- [ ] ThemeToggle: uses next-themes for dark/light
- [ ] Commit

### Task 5: Create Breadcrumbs component

**Files:**
- Create: `frontend/components/Breadcrumbs.tsx`

- [ ] Renders: Главная / Раздел / Подраздел
- [ ] Accepts items array of {label, href}
- [ ] Commit

### Task 6: Create Carousel component

**Files:**
- Create: `frontend/components/Carousel.tsx`

- [ ] Uses embla-carousel-react
- [ ] Auto-sliding with 5s interval
- [ ] Shows news title, excerpt, image
- [ ] Manual prev/next arrows + dots
- [ ] Commit

### Task 7: Create Header component with dropdown menus

**Files:**
- Create: `frontend/components/Header.tsx`

- [ ] Logo + "ГБПОУ СКС" on left
- [ ] Horizontal dropdown menus in center:
  - Сведения об ОО, Колледж, Образование, Абитуриенту, Студенту, Выпускнику
- [ ] AccessibilityToggle + ThemeToggle on right
- [ ] "Войти" button
- [ ] Mobile hamburger menu
- [ ] Commit

### Task 8: Create Footer component

**Files:**
- Create: `frontend/components/Footer.tsx`

- [ ] Contact info: пр-д Черняховского, 3 | +7(8652)24-25-27 | college@stvcc.ru
- [ ] Section links
- [ ] Учредитель info, VK link, copyright
- [ ] Commit

### Task 9: Create all section pages

**Files:**
- Create: `frontend/app/(public)/about/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/college/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/education/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/admissions/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/student-life/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/graduates/[[...slug]]/page.tsx`
- Create: `frontend/app/(public)/partners/page.tsx`

- [ ] Each page reads from site-content.ts
- [ ] `[[...slug]]` catch-all route resolves to sub-section
- [ ] Breadcrumbs shown on each page
- [ ] 404 for unknown slugs
- [ ] Commit

### Task 10: Update homepage with carousel + new Header/Footer

**Files:**
- Modify: `frontend/app/(public)/page.tsx`
- Modify: `frontend/app/(public)/layout.tsx`

- [ ] Replace old header/footer with new Header/Footer components
- [ ] Add Carousel to homepage (5 latest news with images)
- [ ] Commit

### Task 11: Verify build

- [ ] Run `npm run dev` and verify pages render
- [ ] Fix any issues
