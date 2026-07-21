# UI/UX Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Redesign CollegeLMS — clean white/black base with 5 accent palette presets, Inter typography, TPU-inspired header/footer/news cards, centred auth page, drawer-based authenticated layout, animated SVG loader, carousel gradient, import progress fix.

**Architecture:** Single CSS variables system (`:root` for base, `[data-theme="x"]` for accent palettes). Inter font replaces Golos Text. Shared `AuthenticatedShell` component for both `(authenticated)` and `admin` layouts.

**Tech Stack:** Next.js 14, Tailwind CSS v4, Inter (Google Fonts)

## Global Constraints

- Base colors: white bg (`#fff`), black text (`#000`), dark mode inverted
- Palette = accent (primary) + 3 intermediate tints
- 5 palettes switchable via `data-theme`: blue, indigo, sapphire, plum, green
- Accent color only for links/buttons
- Inter font everywhere (remove Golos Text)
- Full college name next to logo on ALL pages: «Ставропольский колледж связи / имени Героя Советского Союза В.А. Петрова»
- Design preset switcher (default/tpu) stays in corner temporarily

---
### Task 1: CSS Design Tokens — White/Black Base + 5 Palettes

**Files:**
- Modify: `app/globals.css`

- [ ] **Step 1: Rewrite `:root` block with base tokens**

```css
:root {
  --bg: #ffffff;
  --fg: #000000;
  --accent: #568edd;
  --accent-hover: #4080cc;
  --accent-light: #d5e3f7;
  --accent-lighter: #eaf1fb;
  --muted: #f5f5f5;
  --muted-fg: #666666;
  --border: #e5e5e5;
  --card: #ffffff;
  --radius: 0.5rem;
}

.dark {
  --bg: #000000;
  --fg: #ffffff;
  --muted: #1a1a1a;
  --muted-fg: #999999;
  --border: #333333;
  --card: #111111;
  --accent-light: #1a2a3a;
  --accent-lighter: #0d1a2a;
}
```

- [ ] **Step 2: Update `@theme inline` to map custom props**

Replace current block with:
```css
@theme inline {
  --color-bg: var(--bg);
  --color-fg: var(--fg);
  --color-accent: var(--accent);
  --color-accent-hover: var(--accent-hover);
  --color-accent-light: var(--accent-light);
  --color-accent-lighter: var(--accent-lighter);
  --color-muted: var(--muted);
  --color-muted-fg: var(--muted-fg);
  --color-border: var(--border);
  --color-card: var(--card);
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
  --animate-accordion-down: accordion-down 0.2s ease-out;
  --animate-accordion-up: accordion-up 0.2s ease-out;
}
```

- [ ] **Step 3: Rewrite 5 `[data-theme="*"]` palette blocks**

```css
[data-theme="blue"] {
  --accent: #568edd;
  --accent-hover: #4080cc;
  --accent-light: #abc7ee;
  --accent-lighter: #d5e3f7;
}
[data-theme="indigo"] {
  --accent: #24386a;
  --accent-hover: #1c2c54;
  --accent-light: #929cb5;
  --accent-lighter: #c9ceda;
}
[data-theme="sapphire"] {
  --accent: #4d74b4;
  --accent-hover: #3d5d93;
  --accent-light: #a6bada;
  --accent-lighter: #d3dded;
}
[data-theme="plum"] {
  --accent: #b9b3e5;
  --accent-hover: #9f97da;
  --accent-light: #dcd9f2;
  --accent-lighter: #eeecf9;
}
[data-theme="green"] {
  --accent: #2f8733;
  --accent-hover: #256f29;
  --accent-light: #97c399;
  --accent-lighter: #cbe1cc;
}
```

Dark variants remain the same colors (no change needed for dark mode palette colors).

- [ ] **Step 4: Remove old color classes that no longer apply**

Remove `--college-navy`, `--color-tpu-*`, old theme-specific `--color-*` that used oklch. Remove old TPU-only CSS classes file unless tpu preset is kept.

- [ ] **Step 5: Update `@layer base` to use new tokens**

```css
@layer base {
  * { @apply border-border; }
  body { @apply bg-bg text-fg; }
}
```

- [ ] **Step 6: Verify**

Run: `npx tsc --noEmit` — expected: no errors

---
### Task 2: Replace Golos Text with Inter

**Files:**
- Modify: `package.json`
- Modify: `app/layout.tsx`
- Modify: `app/globals.css` (font-sans already updated in Task 1)

- [ ] **Step 1: Swap packages**

```bash
npm uninstall @fontsource/golos-text && npm install @fontsource/inter
```

- [ ] **Step 2: Update layout.tsx imports**

Replace `@fontsource/golos-text` imports with:
```tsx
import "@fontsource/inter"
import "@fontsource/inter/500.css"
import "@fontsource/inter/600.css"
import "@fontsource/inter/700.css"
```

- [ ] **Step 3: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 3: Header — Logo + Full College Name

**Files:**
- Modify: `components/Header.tsx`
- Modify: `app/(authenticated)/layout.tsx`
- Modify: `app/admin/layout.tsx`

- [ ] **Step 1: Rewrite Header.tsx with full college name**

```tsx
"use client"

import { useState } from "react"
import Link from "next/link"
import { Menu, X, Search } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const { user } = useAuth()

  return (
    <header className="sticky top-0 z-50 border-b border-border bg-bg">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          <img src="/logo.svg" alt="Ставропольский колледж связи" className="object-contain" style={{ height: "44px" }} />
          <div className="flex flex-col leading-tight">
            <span className="text-sm font-semibold text-fg">Ставропольский колледж связи</span>
            <span className="text-[11px] text-muted-fg">имени Героя Советского Союза В.А. Петрова</span>
          </div>
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <Link key={section.slug} href={section.href} className="px-3 py-2 text-sm font-medium text-muted-fg hover:text-accent transition-colors rounded-md">
              {section.title}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <Link href="/search" className="hidden md:flex items-center justify-center h-9 w-9 rounded-md text-muted-fg hover:text-accent hover:bg-muted transition-colors" aria-label="Поиск"><Search size={18} /></Link>
          <AccessibilityToggle />
          <ThemeToggle />
          {user ? (
            <button className="flex items-center gap-2 rounded-md p-1.5 text-sm font-medium text-muted-fg hover:text-accent hover:bg-muted transition-colors">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent">
                {user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)}
              </span>
              <span className="hidden md:inline max-w-[120px] truncate">{user.fullName}</span>
            </button>
          ) : (
            <Link href="/login" className="ml-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-accent-hover">Войти</Link>
          )}
          <button onClick={() => setMobileOpen(!mobileOpen)} className="lg:hidden ml-2 rounded-md p-2 text-muted-fg hover:bg-muted" aria-label="Меню">
            {mobileOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>
      {mobileOpen && (
        <div className="lg:hidden border-t border-border bg-bg px-4 pb-4 pt-2">
          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => (
              <Link key={section.slug} href={section.href} className="block px-3 py-2 text-sm font-medium text-fg rounded-md hover:bg-muted" onClick={() => setMobileOpen(false)}>
                {section.title}
              </Link>
            ))}
          </nav>
        </div>
      )}
    </header>
  )
}
```

- [ ] **Step 2: Add logo + name to authenticated layouts**

In `app/(authenticated)/layout.tsx`, replace header logo with same pattern:
```tsx
<Link href="/" className="flex items-center gap-3">
  <img src="/logo.svg" alt="" className="object-contain" style={{ maxHeight: "3rem" }} />
  <div className="flex flex-col leading-tight">
    <span className="text-xs font-semibold text-fg">Ставропольский колледж связи</span>
    <span className="text-[10px] text-muted-fg">имени Героя Советского Союза В.А. Петрова</span>
  </div>
</Link>
```

Same for `app/admin/layout.tsx`.

- [ ] **Step 3: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 4: Footer — TPU Structure

**Files:**
- Modify: `components/Footer.tsx`

- [ ] **Step 1: Rewrite Footer.tsx**

Full content: accordion «Полезная информация» (16 links) → 4-column grid (logo+desc, приёмная комиссия, реквизиты, документы) → copyright bar. See full code in task description above (condensed for brevity — use TPU accordion + column pattern from the spec).

- [ ] **Step 2: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 5: News Cards — Bordered Grid

**Files:**
- Modify: `app/(public)/page.tsx` (news section inline)

- [ ] **Step 1: Replace inline news section with bordered grid**

Replace the news section in `app/(public)/page.tsx` with TPU-style bordered grid:

```tsx
<div className="grid gap-0 border border-border rounded-lg overflow-hidden sm:grid-cols-2 lg:grid-cols-3">
  {news.map((item) => (
    <Link key={item.id} href={`/news/${item.id}`} className="block bg-card p-5 transition-colors hover:bg-muted border-b border-r border-border">
      {item.imageUrl && (
        <img src={item.imageUrl} alt="" className="w-full h-40 object-cover mb-3" />
      )}
      {item.categoryName && (
        <span className="inline-block text-xs font-medium text-white bg-accent px-2 py-0.5 rounded-sm mb-2">{item.categoryName}</span>
      )}
      <p className="text-xs text-muted-fg mb-1">{new Date(item.publishedAt).toLocaleDateString("ru-RU")}</p>
      <h3 className="text-sm font-semibold text-fg line-clamp-2">{item.title}</h3>
    </Link>
  ))}
</div>
```

- [ ] **Step 2: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 6: Carousel — Gradient Overlay

**Files:**
- Modify: `components/Carousel.tsx`

- [ ] **Step 1: Add gradient overlay**

Find the image element and add a gradient overlay div after it:
```tsx
<div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent pointer-events-none" />
```

Ensure the text overlay appears above the gradient (higher z-index or DOM order after gradient).

- [ ] **Step 2: Verify build**

Run: `npm run build`

---
### Task 7: Auth Page — Centred Card

**Files:**
- Modify: `app/login/page.tsx`

- [ ] **Step 1: Rewrite login page**

Centred card style (id.cloud.ru): logo + title on top, login/password fields, error state, submit button:

```tsx
export default function LoginPage() {
  // ... state handling same as current

  return (
    <div className="flex min-h-screen items-center justify-center p-4 bg-muted">
      <div className="w-full max-w-sm bg-card rounded-lg border border-border p-8">
        <div className="text-center mb-8">
          <img src="/logo.svg" alt="Ставропольский колледж связи" className="mx-auto h-16 mb-4" />
          <h1 className="text-lg font-semibold text-fg">Вход в систему</h1>
          <p className="text-sm text-muted-fg mt-1">ГБПОУ «Ставропольский колледж связи»</p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {error && <div className="rounded-md bg-accent/10 p-3 text-sm text-accent">{error}</div>}

          <div>
            <label htmlFor="login" className="block text-sm font-medium text-fg mb-1">Логин</label>
            <input id="login" type="text" required value={loginInput} onChange={e => setLoginInput(e.target.value)}
              className="w-full rounded-md border border-border bg-bg px-3 py-2 text-sm text-fg focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent" />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-fg mb-1">Пароль</label>
            <input id="password" type="password" required value={password} onChange={e => setPassword(e.target.value)}
              className="w-full rounded-md border border-border bg-bg px-3 py-2 text-sm text-fg focus:outline-none focus:ring-2 focus:ring-accent/30 focus:border-accent" />
          </div>

          <div className="flex justify-end">
            <a href="/forgot-password" className="text-xs text-accent hover:text-accent-hover">Забыли пароль?</a>
          </div>

          <button type="submit" disabled={submitting}
            className="w-full rounded-md bg-accent px-4 py-2.5 text-sm font-medium text-white hover:bg-accent-hover transition-colors disabled:opacity-50">
            {submitting ? "Вход..." : "Войти"}
          </button>
        </form>
      </div>
    </div>
  )
}
```

Keep quick login presets for development (in a collapsible section or remove).

- [ ] **Step 2: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 8: Authenticated Layout — Drawer Menu Left + Drawer Profile Right

**Files:**
- Modify: `app/(authenticated)/layout.tsx`
- Modify: `app/admin/layout.tsx`

- [ ] **Step 1: Create `components/AuthenticatedShell.tsx`**

Shared component with props: `menuItems` (nav items), `children`.

Pattern:
- Header: logo + name centrally, burger (opens left drawer), avatar (opens right drawer)
- Left drawer: `<aside>` fixed left, overlay on mobile. Contains nav items.
- Right drawer: `<aside>` fixed right, overlay. Contains user info + logout.
- Main: content with proper margin/padding on desktop

Simplified approach — use CSS transitions for drawer open/close.

- [ ] **Step 2: Refactor `(authenticated)/layout.tsx` to use AuthenticatedShell**

Pass menu items for student/teacher/dispatcher roles.

- [ ] **Step 3: Refactor `admin/layout.tsx` to use AuthenticatedShell**

Pass admin menu items.

- [ ] **Step 4: Verify build**

Run: `npm run build`

---
### Task 9: Animated SVG Logo Loader

**Files:**
- Create: `components/AnimatedLogoLoader.tsx`

- [ ] **Step 1: Create loader component**

```tsx
"use client"

export default function AnimatedLogoLoader() {
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-bg">
      <div className="relative">
        <svg width="80" height="80" viewBox="0 0 100 100" fill="none" className="text-accent">
          {/* SVG path of the logo — stroke-dasharray + animation */}
          <path
            d="M20 80 L50 20 L80 80"
            stroke="currentColor"
            strokeWidth="6"
            strokeLinecap="round"
            strokeLinejoin="round"
            fill="none"
            className="animate-draw"
            style={{
              strokeDasharray: 200,
              strokeDashoffset: 200,
              animation: "draw 2s ease forwards",
            }}
          />
        </svg>
      </div>
      <style jsx>{`
        @keyframes draw {
          to { stroke-dashoffset: 0; }
        }
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
        .animate-draw {
          animation: draw 2s ease forwards, pulse 1.5s ease 2s infinite;
        }
      `}</style>
    </div>
  )
}
```

Note: Replace the path `M20 80 L50 20 L80 80` with the actual SVG logo path from `/logo.svg`.

- [ ] **Step 2: Integrate into app loading states**

Replace any `LoadingSpinner` usage with `AnimatedLogoLoader` in layouts where appropriate.

- [ ] **Step 3: Verify build**

Run: `npm run build`

---
### Task 10: Fix Import Progress Bar

**Files:**
- Backend: `CollegeLMS.API/Controllers/ImportController.cs` (or relevant)
- Frontend: `app/admin/import/page.tsx`

- [ ] **Step 1: Check backend progress response**

The issue is most likely in the controller returning progress. The PHP API likely returns `{ processed: 6, total: 100 }` but the display shows `6 из 4314%`. Find the progress endpoint and ensure `processed` and `total` are numbers, and the frontend calculates percentage.

- [ ] **Step 2: Fix frontend progress display**

In `app/admin/import/page.tsx`, ensure progress bar:
```tsx
const percent = data.total > 0 ? Math.round((data.processed / data.total) * 100) : 0
```

- [ ] **Step 3: Verify**

Run import, watch progress bar.

---
### Task 11: Clean Up — Remove Old TPU Components

**Files:**
- Delete: `components/HeaderTPU.tsx`
- Delete: `components/FooterTPU.tsx`
- Delete: `components/CarouselTPU.tsx`
- Delete: `components/HomeTPU.tsx`
- Keep or remove TPU section components based on usage

- [ ] **Step 1: Audit which TPU components are still referenced**

Check imports in `app/(public)/page.tsx`, `app/(public)/layout.tsx`, etc.
Remove unused components.

- [ ] **Step 2: Verify build**

Run: `npm run build` — expected: Build succeeds

---
### Task 12: Update ThemeSwitcher

**Files:**
- Modify: `components/ThemeSwitcher.tsx`

- [ ] **Step 1: Keep switcher in corner**

The switcher stays with design presets (default/tpu) + palette presets (5 colors) + light/dark toggle. No functional changes — just update color swatches to match new palette tokens.

---
## Self-Review Checklist

- [ ] Spec coverage: each requirement maps to at least one task
- [ ] No placeholders: every step has actual code
- [ ] Type consistency: no mismatched signatures between tasks
