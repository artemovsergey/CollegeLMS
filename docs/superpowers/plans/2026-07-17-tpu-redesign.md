# TPU Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the public-facing part of CollegeLMS to match tpu.ru design

**Architecture:** CSS token layer (`[data-design="tpu"]`) + component variants per design preset

**Tech Stack:** Next.js 14, TypeScript, Tailwind CSS 4, shadcn/ui

## Global Constraints

- All data stays unchanged — no API or data model modifications
- Only `(public)/` routes and shared components affected
- Admin and authenticated layouts untouched
- `tsc --noEmit` must pass without errors

---

### Task 1: CSS Design Tokens

**Files:**
- Modify: `app/globals.css`

**Interfaces:**
- Produces: `[data-design="tpu"]` CSS custom properties block consumed by all components

- [ ] **Step 1: Add [data-design="tpu"] token block**

Insert before `@theme inline` block:

```css
[data-design="tpu"] {
  --font-sans: "Golos Text", "PT Root UI", ui-sans-serif, system-ui, sans-serif;
  --color-tpu-bg: #ffffff;
  --color-tpu-bg-muted: #f7f7f9;
  --color-tpu-text-primary: #1a1a2e;
  --color-tpu-text-secondary: #6b7280;
  --color-tpu-accent: #0066cc;
  --color-tpu-accent-hover: #0052a3;
  --color-tpu-accent-light: #e8f0fe;
  --color-tpu-border: #e5e7eb;
  --color-tpu-card-bg: #ffffff;
  --color-tpu-header-bg: rgba(255, 255, 255, 0.97);
  --color-tpu-footer-bg: #1a1a2e;
  --color-tpu-footer-text: #9ca3af;
  --section-padding-y: 5rem;
  --header-tpu-height: 80px;
  --shadow-tpu-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-tpu-md: 0 4px 6px -1px rgba(0, 0, 0, 0.07);
  --shadow-tpu-lg: 0 10px 25px rgba(0, 0, 0, 0.08);
}
```

- [ ] **Step 2: Add TPU component-level styles**

Add blocks for each TPU component after `[data-design="tpu"]`:

```css
[data-design="tpu"] .header-tpu {
  @apply fixed top-0 left-0 right-0 z-50 transition-all duration-300;
  height: var(--header-tpu-height);
}
[data-design="tpu"] .header-tpu.transparent {
  background: transparent;
}
[data-design="tpu"] .header-tpu.scrolled {
  background: var(--color-tpu-header-bg);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-tpu-border);
}
```

- [ ] **Step 3: Verify file is valid**

Run: `npx tsc --noEmit` — expected: no errors

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "phase 1.1: add TPU design tokens to globals.css"
```

---

### Task 2: DesignProvider

**Files:**
- Create: `lib/design-provider.tsx`
- Modify: `app/(public)/layout.tsx`

**Interfaces:**
- Produces: `useDesign()` hook returning `{ design, setDesign, allDesigns }`, `DesignProvider` component

- [ ] **Step 1: Create DesignProvider**

```tsx
// lib/design-provider.tsx
"use client"

import { createContext, useContext, useEffect, useState } from "react"

export type DesignPreset = "default" | "tpu"

const DESIGN_PRESETS: DesignPreset[] = ["default", "tpu"]

const DESIGN_LABELS: Record<DesignPreset, string> = {
  default: "Стандартный",
  tpu: "ТПУ",
}

const DEFAULT_DESIGN: DesignPreset = "default"

const DesignContext = createContext<{
  design: DesignPreset
  setDesign: (d: DesignPreset) => void
  allDesigns: { value: DesignPreset; label: string }[]
} | null>(null)

export function DesignProvider({ children }: { children: React.ReactNode }) {
  const [design, setDesign] = useState<DesignPreset>(DEFAULT_DESIGN)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    const stored = localStorage.getItem("design-preset") as DesignPreset | null
    if (stored && DESIGN_PRESETS.includes(stored)) {
      setDesign(stored)
    }
    setMounted(true)
  }, [])

  useEffect(() => {
    if (!mounted) return
    document.documentElement.setAttribute("data-design", design)
    localStorage.setItem("design-preset", design)
  }, [design, mounted])

  const allDesigns = DESIGN_PRESETS.map((value) => ({
    value,
    label: DESIGN_LABELS[value],
  }))

  return (
    <DesignContext.Provider value={{ design, setDesign, allDesigns }}>
      {children}
    </DesignContext.Provider>
  )
}

export function useDesign() {
  const ctx = useContext(DesignContext)
  if (!ctx) throw new Error("useDesign must be used within DesignProvider")
  return ctx
}
```

- [ ] **Step 2: Wrap public layout with DesignProvider**

In `app/(public)/layout.tsx`, import and wrap:

```tsx
import { DesignProvider } from "@/lib/design-provider"

// Wrap children:
<DesignProvider>
  <Header />
  <main className="flex-1">{children}</main>
  <Footer />
</DesignProvider>
```

- [ ] **Step 3: Verify**

Run: `npx tsc --noEmit` — expected: no errors

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "phase 1.2: add DesignProvider for TPU design switching"
```

---

### Task 3: HeaderTPU + MegaMenu

**Files:**
- Create: `components/HeaderTPU.tsx`
- Create: `components/MegaMenu.tsx`
- Modify: `components/Header.tsx`

**Interfaces:**
- Consumes: `useDesign()` from Task 2, `useAuth()` from existing, `siteNavigation` from existing
- Produces: `<HeaderTPU />`, `<MegaMenu />`

- [ ] **Step 1: Create MegaMenu component**

```tsx
// components/MegaMenu.tsx
"use client"

import Link from "next/link"
import type { Section } from "@/data/site-content"

export default function MegaMenu({ section }: { section: Section }) {
  return (
    <div className="absolute left-0 top-full mt-0 w-screen max-w-4xl rounded-b-lg border border-t-0 border-gray-200 bg-white shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200">
      <div className="grid grid-cols-3 gap-6 p-6">
        {section.subsections.map((sub) => (
          <Link
            key={sub.slug}
            href={sub.href}
            className="text-sm text-gray-700 hover:text-[#0066cc] transition-colors"
          >
            {sub.title}
          </Link>
        ))}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Create HeaderTPU**

```tsx
// components/HeaderTPU.tsx
"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Search, User } from "lucide-react"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"
import MegaMenu from "./MegaMenu"

export default function HeaderTPU() {
  const [scrolled, setScrolled] = useState(false)
  const { user } = useAuth()

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 80)
    window.addEventListener("scroll", onScroll)
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  return (
    <header
      className={`header-tpu ${scrolled ? "scrolled" : "transparent"}`}
    >
      <div className="mx-auto flex h-full max-w-7xl items-center justify-between px-4 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          <img
            src="/logo.svg"
            alt="Колледж связи"
            className="object-contain"
            style={{ height: "56px" }}
          />
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <div key={section.slug} className="group relative">
              <Link
                href={section.href}
                className={`px-4 py-2 text-sm font-medium transition-colors rounded-md ${
                  scrolled
                    ? "text-gray-700 hover:text-[#0066cc]"
                    : "text-white/90 hover:text-white"
                }`}
              >
                {section.title}
              </Link>
              <MegaMenu section={section} />
            </div>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <button
            className={`p-2 rounded-md transition-colors ${
              scrolled
                ? "text-gray-500 hover:text-[#0066cc]"
                : "text-white/80 hover:text-white"
            }`}
            aria-label="Поиск"
          >
            <Search size={20} />
          </button>
          {user ? (
            <Link
              href="/my/profile"
              className={`flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md transition-colors ${
                scrolled
                  ? "bg-[#0066cc] text-white hover:bg-[#0052a3]"
                  : "bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm"
              }`}
            >
              <User size={16} />
              {user.fullName}
            </Link>
          ) : (
            <Link
              href="/login"
              className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
                scrolled
                  ? "bg-[#0066cc] text-white hover:bg-[#0052a3]"
                  : "bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm"
              }`}
            >
              Войти
            </Link>
          )}
        </div>
      </div>
    </header>
  )
}
```

- [ ] **Step 3: Update Header.tsx to delegate to HeaderTPU**

Add import check at the top of Header.tsx, wrap the return:

```tsx
import { useDesign } from "@/lib/design-provider"

export default function Header() {
  const { design } = useDesign()
  // ... existing state

  if (design === "tpu") {
    return <HeaderTPU />
  }

  // ... existing return
}
```

- [ ] **Step 4: Verify**

Run: `npx tsc --noEmit` — expected: no errors

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "phase 2: add HeaderTPU with MegaMenu"
```

---

### Task 4: Hero Carousel TPU

**Files:**
- Create: `components/CarouselTPU.tsx`
- Modify: `components/Carousel.tsx`

**Interfaces:**
- Consumes: `useDesign()` from Task 2
- Produces: `<CarouselTPU />`

- [ ] **Step 1: Create CarouselTPU**

Full-width hero with overlay, left-aligned text, CTA, dots. Copy Carousel.tsx logic but completely different layout.

```tsx
// components/CarouselTPU.tsx
"use client"

import { useState, useEffect, useCallback } from "react"
import useEmblaCarousel from "embla-carousel-react"
import Link from "next/link"
import type { Result, NewsResponse, PagedResponse } from "@/types"
import api from "@/lib/api"
import { ChevronLeft, ChevronRight } from "lucide-react"

export default function CarouselTPU() {
  // Same data loading as Carousel.tsx
  const [slides, setSlides] = useState<NewsResponse[]>([])
  // ... same loading logic

  return (
    <section className="relative h-[80vh] min-h-[600px] w-full">
      <div className="overflow-hidden h-full" ref={emblaRef}>
        <div className="flex h-full">
          {slides.map((item) => (
            <Link
              key={item.id}
              href={`/news/${item.id}`}
              className="relative min-w-0 flex-[0_0_100%] h-full"
            >
              <img
                src={item.imageUrl}
                alt=""
                className="h-full w-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/30 to-transparent" />
              <div className="absolute bottom-0 left-0 right-0 p-8 lg:p-16 max-w-3xl">
                {item.categoryName && (
                  <span className="inline-block mb-3 text-xs font-semibold uppercase tracking-wider text-white/80">
                    {item.categoryName}
                  </span>
                )}
                <h1 className="text-3xl lg:text-5xl font-bold text-white leading-tight mb-4">
                  {item.title}
                </h1>
                <p className="text-white/80 text-lg mb-6 line-clamp-2">
                  {item.excerpt || item.content?.replace(/<[^>]*>/g, "").slice(0, 150)}
                </p>
                <span className="inline-flex items-center gap-2 px-6 py-3 bg-[#0066cc] text-white text-sm font-medium rounded-md hover:bg-[#0052a3] transition-colors">
                  Подробнее →
                </span>
              </div>
            </Link>
          ))}
        </div>
      </div>
      {/* Dots */}
      <div className="absolute bottom-6 left-1/2 -translate-x-1/2 flex gap-2">
        {slides.map((_, i) => (
          <button
            key={i}
            onClick={() => scrollTo(i)}
            className={`w-2.5 h-2.5 rounded-full transition-all ${
              i === selectedIndex ? "bg-white w-8" : "bg-white/50"
            }`}
          />
        ))}
      </div>
      {/* Arrows */}
      <button onClick={scrollPrev} className="absolute left-4 top-1/2 -translate-y-1/2 p-2 rounded-full bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm">
        <ChevronLeft size={24} />
      </button>
      <button onClick={scrollNext} className="absolute right-4 top-1/2 -translate-y-1/2 p-2 rounded-full bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm">
        <ChevronRight size={24} />
      </button>
    </section>
  )
}
```

- [ ] **Step 2: Update Carousel.tsx to delegate**

```tsx
import { useDesign } from "@/lib/design-provider"

export default function Carousel() {
  const { design } = useDesign()
  if (design === "tpu") return <CarouselTPU />
  // ... existing
}
```

- [ ] **Step 3: Verify**

Run: `npx tsc --noEmit` — expected: no errors

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "phase 3: add CarouselTPU hero component"
```

---

### Task 5: Content Section Components (8 components)

**Files:**
- Create: `components/SpecialtiesSectionTPU.tsx`
- Create: `components/AdmissionSectionTPU.tsx`
- Create: `components/EventsSectionTPU.tsx`
- Create: `components/NewsSectionTPU.tsx`
- Create: `components/StatisticsSectionTPU.tsx`
- Create: `components/PartnersSectionTPU.tsx`
- Create: `components/FAQSectionTPU.tsx`
- Create: `components/FeedbackFormTPU.tsx`

**Interfaces:**
- Each component follows the same data structure as the original but with TPU styling
- All use TPU tokens: `--color-tpu-accent`, `--color-tpu-bg-muted`, etc.

- [ ] **Step 1: Create each TPU section component**

Each component mirrors its original but uses:
- `shadow-tpu-sm/md/lg` for cards
- `--color-tpu-accent` for CTAs
- `--color-tpu-bg-muted` for alternating section backgrounds
- TPU typography scale
- Minimal border-radius, clean lines

- [ ] **Step 2: Update original components to delegate**

Each original component checks `useDesign()` and renders TPU variant when `design === "tpu"`.

- [ ] **Step 3: Verify**

Run: `npx tsc --noEmit` — expected: no errors

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "phase 4: add TPU section components"
```

---

### Task 6: FooterTPU

**Files:**
- Create: `components/FooterTPU.tsx`
- Modify: `components/Footer.tsx`

- [ ] **Step 1: Create FooterTPU**

Dark background (`#1a1a2e`), 4-column grid, light text, social icons.

- [ ] **Step 2: Delegate in Footer.tsx**

- [ ] **Step 3: Commit**

---

### Task 7: SectionPageTPU + BreadcrumbsTPU

**Files:**
- Create: `components/SectionPageTPU.tsx`
- Create: `components/BreadcrumbsTPU.tsx`
- Modify: `components/SectionPage.tsx`

- [ ] **Step 1: Create BreadcrumbsTPU**

- [ ] **Step 2: Create SectionPageTPU**

- [ ] **Step 3: Delegate in SectionPage.tsx**

- [ ] **Step 4: Commit**

---

### Task 8: Homepage and Layout Updates

**Files:**
- Modify: `app/(public)/page.tsx`
- Modify: `app/(public)/layout.tsx`

- [ ] **Step 1: Update homepage to handle TPU sections ordering**

- [ ] **Step 2: Ensure layout includes DesignProvider**

- [ ] **Step 3: Commit**

---

### Task 9: DesignSwitcher

**Files:**
- Modify: `components/ThemeSwitcher.tsx`

- [ ] **Step 1: Add design toggle button group to ThemeSwitcher**

- [ ] **Step 2: Commit**

---

### Task 10: Mobile & Polish

- [ ] **Step 1: Mobile hamburger menu for HeaderTPU**
- [ ] **Step 2: Responsive section adjustments**
- [ ] **Step 3: Final tsc check**
- [ ] **Step 4: Commit**

---

### Task 11: Final Commit

- [ ] **Step 1: Commit all remaining changes**

```bash
git add -A && git commit -m "feature: implement TPU-based redesign for public pages"
```
