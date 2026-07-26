# UI Fixes Implementation Plan

> **Goal:** Apply 9 UI fixes and improvements based on approved design spec.

**Architecture:** All changes are frontend-only — Next.js components, CSS, and data files.

**Tech Stack:** Next.js 14, TypeScript, Tailwind CSS 4, Lucide React

## Tasks

### Task 1: Increase base font size + fix accessibility mode icon visibility

- Modify: `CollegeLMS.Next/app/globals.css`

- [ ] Add `font-size: 17px` to `body` in `@layer base`
- [ ] Add `.accessibility-mode` override for theme/accessibility toggles

### Task 2: Change footer background color

- Modify: `CollegeLMS.Next/components/Footer.tsx`

- [ ] Change `bg-white` to `bg-muted` on footer element

### Task 3: Fix contacts map embed

- Modify: `CollegeLMS.Next/app/(public)/contacts/page.tsx`

- [ ] Change `q=45.0450,41.9808` to `q=ГБПОУ+Ставропольский+колледж+связи+имени+Петрова`

### Task 4: Add Сотруднику navigation section

- Modify: `CollegeLMS.Next/data/site-content.ts`

- [ ] Add `Сотруднику` section with `/employee` href

### Task 5: Create employee page

- Create: `CollegeLMS.Next/app/(public)/employee/page.tsx`

- [ ] Create a simple employee page with login redirect

### Task 6: Rewrite Header.tsx — move toggles, remove login, add admission link

- Modify: `CollegeLMS.Next/components/Header.tsx`

- [ ] Remove login button from Row 1
- [ ] Remove user profile popup from Row 2
- [ ] Move ThemeToggle + AccessibilityToggle from Row 2 to Row 1
- [ ] Add admission campaign link with amber accent
- [ ] Fix icon contrast in dark mode

### Task 7: Replace partner icons with SVG logos

- Modify: `CollegeLMS.Next/components/PartnersSection.tsx`

- [ ] Replace Lucide icons with inline SVG logos

### Task 8: Add video preview to MediaSection

- Modify: `CollegeLMS.Next/components/MediaSection.tsx`

- [ ] Fetch Rutube thumbnail via API call
- [ ] Show preview image + play button
- [ ] Modal with iframe player on click
