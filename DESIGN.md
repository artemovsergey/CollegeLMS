---
name: CollegeLMS
description: Clean, modern educational management system
colors:
  neutral-bg: "#f8f6f3"
  neutral-fg: "#1a1a1a"
  neutral-muted: "#e8e4df"
  neutral-subtle: "#d4cfc9"
  primary: "#2962ff"
  primary-hover: "#1a45cc"
  primary-muted: "#e8eeff"
  destructive: "#dc3545"
  destructive-muted: "#fce8e8"
typography:
  body:
    fontFamily: "Inter, ui-sans-serif, system-ui, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: "Inter, ui-sans-serif, system-ui, sans-serif"
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
    backgroundColor: "#ffffff"
    rounded: "{rounded.lg}"
    padding: "{spacing.lg}"
  input:
    backgroundColor: "#ffffff"
    rounded: "{rounded.sm}"
    padding: "{spacing.sm} {spacing.md}"
    borderColor: "{colors.neutral-subtle}"
  table-header:
    backgroundColor: "{colors.neutral-muted}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
  badge-admin:
    backgroundColor: "{colors.primary-muted}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
  badge-teacher:
    backgroundColor: "{colors.neutral-muted}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
  badge-student:
    backgroundColor: "{colors.neutral-muted}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
---

# Design System: CollegeLMS

## 1. Overview

**Creative North Star: "The Clean Slate"**

CollegeLMS is a warm, calm administrative workspace — a tool that feels like a well-organized desk in a sunlit room, not a blinking dashboard in a server closet. Every screen prioritizes the task over the chrome. The interface recedes; the data steps forward.

The system rejects decorative clutter — no gradient text, no glassmorphism, no side-stripe borders. Hierarchy comes from generous whitespace, clear typographic scale, and a restrained accent color used precisely. The palette is warm-neutral with a single blue accent that signals interaction without demanding attention.

**Key Characteristics:**
- Warm, not cold. Tinted neutrals replace pure grays
- One accent color (blue), used on ≤15% of any screen. Its rarity is the point
- Cards are real containers with generous radius, not flat rectangles
- Tables are the default data display, not card grids
- Mobile-first: forms and tables survive 320px

## 2. Colors

Warm-neutral base with a crisp blue accent. The neutrals lean slightly toward sandy-warm (chroma ~0.01, hue ~60°) instead of pure gray — a subtle paper warmth.

### Primary
- **Signal Blue** (#2962ff / oklch(0.55 0.15 260)): Interactive elements — buttons, links, focus rings. Used sparingly so every blue element means something.

### Neutral
- **Warm Paper** (#f8f6f3 / oklch(0.97 0.008 70)): Page background. The room the furniture sits on.
- **Warm White** (#ffffff): Card, dialog, input backgrounds. Elevated surfaces.
- **Warm Border** (#d4cfc9 / oklch(0.85 0.008 70)): Borders, dividers, table row separators.
- **Warm Muted** (#e8e4df / oklch(0.90 0.008 70)): Muted backgrounds, table headers.
- **Soft Charcoal** (#1a1a1a / oklch(0.20 0.005 70)): Body text. Not pure black.

### Semantic
- **Signal Blue Muted** (#e8eeff): Selected/active table rows, admin badge backgrounds, info banners.
- **Rose Alert** (#dc3545): Destructive actions, errors.
- **Rose Muted** (#fce8e8): Error banners background.

### Named Rules
**The One Voice Rule.** The primary accent covers ≤15% of any screen. Its rarity is the point — when blue appears, it means something.

**The Warmth Rule.** All neutrals carry a subtle warm tint (chroma ≥ 0.005, hue toward 60°). Pure gray (`oklch(L 0 H)`) is forbidden. The warmth should be barely perceptible — noticeable only when compared side-by-side with a neutral gray.

## 3. Typography

**Body Font:** Inter, the system sans-serif stack

**Display Font:** Inter (same family, scaled with weight)

**Monospace Font:** JetBrains Mono for code

**Character:** Clean, readable, modern. Inter's open aperture and tall x-height keep body text legible at small sizes. No decorative typefaces — this is a productivity tool, not a magazine.

### Hierarchy
- **Headline** (600, 1.5rem/24px, 1.3): Page titles (`h1`). Top-level sections.
- **Title** (600, 1.125rem/18px, 1.4): Section headers (`h2`), dialog titles, card headers.
- **Body** (400, 0.9375rem/15px, 1.6): Primary reading text. Cap line length at 70ch.
- **Label** (500, 0.8125rem/13px, 1.25, letter-spacing 0.01em): Form labels, table headers, button text, metadata.
- **Caption** (400, 0.75rem/12px, 1.4): Helper text, timestamps, secondary metadata.

### Named Rules
**The Weight-Only Scale.** Hierarchy is expressed through weight (500 → 600) and size, not through font family switches or letter-spacing theatrics.

## 4. Elevation

Flat by default. Depth comes from tonal layering (background → card → border), not from shadows.

Cards, dialogs, and dropdowns sit on the surface without vertical lift. The card background (`#ffffff`) against the page background (`#f8f6f3`) provides enough separation. No box-shadows on cards.

**The only shadow** is on the Dialog overlay — a soft ambient shadow on the content panel and a dark overlay behind it.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest. The tonal contrast between page and card backgrounds provides depth. Shadows appear only for modals and dropdown menus.

## 5. Components

### Buttons
- **Shape:** Gently rounded corners (8px)
- **Primary:** Signal Blue background, white text, 10px 20px padding. Hover shifts to darker blue (`#1a45cc`). Active presses darker.
- **Ghost:** Transparent background, Soft Charcoal text. Hover gets a Warm Muted background.
- **Outline:** 1px Warm Border, transparent background. Hover fills with Warm Muted.
- **Destructive:** Rose Alert background.
- **States:** All buttons use a 150ms ease-out transition on background-color and box-shadow. Focus-visible ring uses Signal Blue at 3px offset.

### Cards
- **Shape:** Generous rounded corners (12px)
- **Background:** Warm White
- **Shadow:** None (see Flat-By-Default Rule)
- **Border:** None (tonal separation from page background is sufficient)
- **Internal Padding:** 24px (lg)

### Inputs & Fields
- **Style:** 1px Warm Border, Warm White background, 6px radius
- **Focus:** Border switches to Signal Blue, ring at 3px with 30% opacity Signal Blue
- **Error:** Border switches to Rose Alert
- **Disabled:** Warm Muted background, 50% opacity text
- **Label:** Above the field, Label type scale, 8px gap below

### Tables
- **Style:** Clean, minimal borders. Column headers use Label weight.
- **Header Row:** Warm Muted background, Label text.
- **Rows:** Alternating is not needed — zebra striping is visual noise. Use subtle 1px bottom border (Warm Border) between rows.
- **Hover Row:** Signal Blue Muted background at 50% opacity.
- **Radius:** 8px on the table container (via parent wrapper).

### Badges
- **Shape:** 6px radius, compact padding (4px 10px)
- **Admin:** Signal Blue Muted background, Signal Blue text
- **Teacher/Student:** Warm Muted background, Soft Charcoal text

### Navigation (Header)
- **Style:** Clean bar, no background fill. Just logo/title left, user info right.
- **Active state:** No underline or indicator needed — the page title is sufficient wayfinding.
- **Mobile:** Collapse user email to just the badge.

### Dialogs
- **Overlay:** `rgba(0,0,0,0.3)` dark backdrop
- **Content:** Warm White, 12px radius, 24px padding, soft ambient shadow
- **Title:** Title type scale
- **Actions:** Right-aligned, Cancel (ghost) then Save (primary)

## 6. Do's and Don'ts

### Do:
- **Do** use tinted neutrals (warm, chroma ≥ 0.005). Pure gray surfaces look unfinished.
- **Do** keep Signal Blue coverage under 15% per screen. Blue means interactive.
- **Do** use tables as the default data display — they scan faster than cards.
- **Do** wrap forms and tables in cards for container consistency.
- **Do** use generous whitespace between sections (32px+).
- **Do** keep buttons at consistent height (38–40px).
- **Do** show loading state as a centered spinner in the content area.

### Don't:
- **Don't** use `#000` or `#fff` anywhere. Tint every neutral.
- **Don't** add shadows to cards. Tonal separation is sufficient.
- **Don't** use gradient text, glassmorphism, or side-stripe borders.
- **Don't** put an accent border-left on cards. Use full borders or nothing.
- **Don't** use zebra-striped tables. Subtle row borders are cleaner.
- **Don't** stack identical card grids as a data display. Use tables.
- **Don't** wrap everything in a card. The page background is the primary surface.
