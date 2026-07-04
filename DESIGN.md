---
name: CollegeLMS
description: Clean, modern educational management system
colors:
  primary: "#568cd6"
  primary-hover: "#3b6ea8"
  primary-light: "#e4edf8"
  secondary: "#b9b1e6"
  accent-green: "#2d872d"
  cream: "#f0e8d1"
  neutral-bg: "#f5f7fa"
  neutral-fg: "#152851"
  neutral-muted: "#5a6a8a"
  border: "#d4d9e3"
  destructive: "#c43e3e"
  destructive-muted: "#f8e8e8"
  white: "#ffffff"
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
    backgroundColor: "{colors.white}"
    rounded: "{rounded.lg}"
    padding: "{spacing.lg}"
  input:
    backgroundColor: "{colors.white}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sm} {spacing.md}"
    borderColor: "{colors.border}"
  table-header:
    backgroundColor: "{colors.primary-light}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
  badge-admin:
    backgroundColor: "{colors.primary-light}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
  badge-teacher:
    backgroundColor: "{colors.secondary}"
    textColor: "#ffffff"
    rounded: "{rounded.sm}"
  badge-student:
    backgroundColor: "{colors.neutral-bg}"
    textColor: "{colors.neutral-fg}"
    rounded: "{rounded.sm}"
---

# Design System: CollegeLMS

## 1. Overview

**Creative North Star: "The Clean Slate"**

CollegeLMS is a calm, authoritative academic workspace — a tool that feels like a well-organized dean's office in natural light, not a blinking dashboard in a server closet. Every screen prioritizes the task over the chrome. The interface recedes; the data steps forward.

The system rejects decorative clutter — no gradient text, no glassmorphism, no side-stripe borders. Hierarchy comes from generous whitespace, clear typographic scale, and a restrained brand color used precisely. The palette is derived directly from the college logo: confident navy-blues, a heraldic purple from the crest, and warm cream accents.

**Key Characteristics:**
- Clean blue primary, drawn from the logo. Cool neutrals replace pure grays
- One accent color (College Blue `#568cd6`), used on ≤15% of any screen. Its rarity is the point
- The logo's purple (`#b9b1e6`) appears only for special badges and the Teacher role
- Cards are real containers with generous radius, not flat rectangles
- Tables are the default data display, not card grids
- Mobile-first: forms and tables survive 320px

## 2. Colors

All colors are extracted from the college logo. The palette balances a cool blue primary with warm cream accents from the crest, anchored by a deep navy text color.

### Primary
- **College Blue** (#568cd6 / oklch(0.62 0.11 255)): Interactive elements — buttons, links, focus rings. Used sparingly so every blue element means something.
- **College Blue Dark** (#3b6ea8 / oklch(0.49 0.11 260)): Hover and active states. The pressed-down version of primary.
- **College Blue Light** (#e4edf8 / oklch(0.93 0.02 260)): Muted backgrounds, table headers, admin badges, selected rows.

### Secondary
- **Crest Purple** (#b9b1e6 / oklch(0.68 0.09 290)): The heraldic purple from the college crest. Used for Teacher badges and secondary decorative elements only.
- **Leaf Green** (#2d872d / oklch(0.58 0.17 145)): The green accent from the logo's leaf. Used exclusively for success states and the campus nature connection.
- **Cream** (#f0e8d1 / oklch(0.93 0.03 90)): The warm parchment tone from the crest. Light banners, informational callouts, achievement highlights.

### Neutral
- **Page Blue** (#f5f7fa / oklch(0.97 0.006 260)): Page background. A barely-there cool tint that complements the blue logo. The room the furniture sits on.
- **White** (#ffffff): Card, dialog, input backgrounds. Elevated surfaces.
- **Cool Border** (#d4d9e3 / oklch(0.87 0.01 260)): Borders, dividers, table row separators. A subtle blue undertone that ties back to the logo.
- **Slate** (#5a6a8a / oklch(0.50 0.04 260)): Muted secondary text, placeholder text, metadata.
- **Deep Navy** (#152851 / oklch(0.23 0.04 260)): Body text. The darkest tone from the logo text. Not pure black.

### Semantic
- **Blue Tint** (#e4edf8): Selected/active table rows, admin badge backgrounds, info banners.
- **Rose Alert** (#c43e3e): Destructive actions, errors.
- **Rose Tint** (#f8e8e8): Error banners background.

### Named Rules
**The One Voice Rule.** College Blue covers ≤15% of any screen. Its rarity is the point — when blue appears, it means something.

**The Crest Rule.** Purple is reserved for Teacher role identification and the college crest in headers. Never use purple for interactive elements.

**The Cool Neutral Rule.** All neutrals carry a subtle blue-cool undertone (chroma ≤ 0.01, hue toward 260°). Pure gray is forbidden. The coolness should be barely perceptible — a quiet nod to the navy in the logo.

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

Cards, dialogs, and dropdowns sit on the surface without vertical lift. The card background (`#ffffff`) against the page background (`#f5f7fa`) provides enough separation. No box-shadows on cards.

**The only shadow** is on the Dialog overlay — a soft ambient shadow on the content panel and a dark overlay behind it.

### Named Rules
**The Flat-By-Default Rule.** Surfaces are flat at rest. The tonal contrast between page and card backgrounds provides depth. Shadows appear only for modals and dropdown menus.

## 5. Components

### Buttons
- **Shape:** Gently rounded corners (8px)
- **Primary:** College Blue background, white text, 10px 20px padding. Hover shifts to College Blue Dark (`#3b6ea8`). Active presses darker.
- **Ghost:** Transparent background, Deep Navy text. Hover gets a Blue Tint background.
- **Outline:** 1px Cool Border, transparent background. Hover fills with Blue Tint.
- **Destructive:** Rose Alert background.
- **States:** All buttons use a 150ms ease-out transition on background-color and box-shadow. Focus-visible ring uses College Blue at 3px offset.

### Cards
- **Shape:** Generous rounded corners (12px)
- **Background:** White
- **Shadow:** None (see Flat-By-Default Rule)
- **Border:** None (tonal separation from page background is sufficient)
- **Internal Padding:** 24px (lg)

### Inputs & Fields
- **Style:** 1px Cool Border, White background, 6px radius
- **Focus:** Border switches to College Blue, ring at 3px with 30% opacity College Blue
- **Error:** Border switches to Rose Alert
- **Disabled:** Blue Tint background, 50% opacity Slate text
- **Label:** Above the field, Label type scale, 8px gap below

### Tables
- **Style:** Clean, minimal borders. Column headers use Label weight.
- **Header Row:** Blue Tint background, Deep Navy Label text.
- **Rows:** Alternating is not needed — zebra striping is visual noise. Use subtle 1px bottom border (Cool Border) between rows.
- **Hover Row:** Blue Tint at 50% opacity.
- **Radius:** 8px on the table container (via parent wrapper).

### Badges
- **Shape:** 6px radius, compact padding (4px 10px)
- **Admin:** Blue Tint background, College Blue text
- **Teacher:** Crest Purple background, white text
- **Student:** Page Blue background, Deep Navy text

### Navigation (Header)
- **Style:** Clean bar, no background fill. Just logo/title left, user info right.
- **Active state:** No underline or indicator needed — the page title is sufficient wayfinding.
- **Mobile:** Collapse user email to just the badge.

### Dialogs
- **Overlay:** `rgba(21,40,81,0.35)` dark backdrop (Deep Navy tinted)
- **Content:** White, 12px radius, 24px padding, soft ambient shadow
- **Title:** Title type scale
- **Actions:** Right-aligned, Cancel (ghost) then Save (primary)

## 6. Icons

**Library:** Lucide React — single source of truth. No icon fonts, no second library, no raw SVGs for common UI.

**Philosophy:** Icons are wayfinding aids, not decoration. Every icon earns its place by reducing cognitive load — a trash icon is faster to scan than "Удалить". If an icon doesn't speed up recognition, omit it.

### Feature → Icon Map

| Section / Action | Lucide Name | Notes |
|-----------------|-------------|-------|
| Dashboard | `LayoutDashboard` | |
| Schedule | `CalendarDays` | |
| Courses | `BookOpen` | |
| Users / People | `Users` | |
| Profile | `UserCircle` | |
| Materials | `FileText` | |
| Upload | `Upload` | |
| Tests / Exams | `ClipboardCheck` | |
| Journal / Grades | `NotebookPen` | |
| Notifications | `Bell` | |
| Messages | `MessageSquare` | |
| Reports | `BarChart3` | |
| Settings | `Settings` | |
| Search | `Search` | |
| Logout | `LogOut` | |
| Add / Create | `Plus` | |
| Edit | `Pencil` | |
| Delete | `Trash2` | |
| Filter | `Filter` | |
| Sort | `ArrowUpDown` | |
| Download | `Download` | |
| Print | `Printer` | |
| Close / Dismiss | `X` | |
| Chevron (expand) | `ChevronDown` | Collapse: `ChevronUp` |
| Arrow (back) | `ArrowLeft` | Forward: `ArrowRight` |
| Info | `Info` | |
| Warning | `TriangleAlert` | |
| Error | `CircleAlert` | |
| Success | `CircleCheck` | |
| Loading | `LoaderCircle` | `animate-spin` |
| Empty state | `Inbox` | |
| Drag handle | `GripVertical` | |
| External link | `ExternalLink` | |
| Menu (mobile) | `Menu` | |

### Sizing

| Context | Class | px |
|---------|-------|----|
| Inline with body text | `h-4 w-4` | 16 |
| Button icon (icon-only or icon+label) | `h-5 w-5` | 20 |
| Section/header icon | `h-6 w-6` | 24 |
| Empty state hero icon | `h-12 w-12` | 48 |

### Color

| Context | Color | Rule |
|---------|-------|------|
| Default | `currentColor` | Inherits text color — most icons |
| Interactive (hover) | College Blue | Icon-only buttons on hover/focus |
| Semantic status | Leaf Green / Rose Alert / Slate | Success, error, info/warning |
| Muted | Slate (`#5a6a8a`) | Secondary actions, metadata |
| Decorative | College Blue (60% opacity) | Empty state hero icons |

### Accessibility

- **Icon-only buttons** MUST have `aria-label` describing the action in Russian (e.g. `aria-label="Удалить файл"`)
- **Icons with adjacent visible text** MUST use `aria-hidden="true"` — the text is the accessible label
- **Loading spinner**: `aria-label="Загрузка"` + `role="status"`
- **Focus-visible ring** on icon buttons: `focus-visible:ring-2 focus-visible:ring-[--primary] focus-visible:ring-offset-2`

### Animation

| Situation | Class | Duration |
|-----------|-------|----------|
| Loading spinner | `animate-spin` | continuous |
| Chevron expand/collapse | `transition-transform duration-200 rotate-0/180` | 200ms |
| Button icon hover | `transition-transform duration-150 group-hover:scale-110` | 150ms |
| Status change | `transition-opacity duration-300` | 300ms |

## 7. Do's and Don'ts

### Do:
- **Do** use College Blue sparingly (<15% per screen). Blue means interactive.
- **Do** use cool-tinted neutrals (blue undertone, chroma ≤ 0.01). Pure gray looks unfinished.
- **Do** use Crest Purple exclusively for Teacher badges — never for buttons or links.
- **Do** use Leaf Green only for success states and the campus leaf identity.
- **Do** use tables as the default data display — they scan faster than cards.
- **Do** wrap forms and tables in cards for container consistency.
- **Do** use generous whitespace between sections (32px+).
- **Do** keep buttons at consistent height (38–40px).
- **Do** show loading state as a centered spinner in the content area.

### Don't:
- **Don't** use `#000` or pure `#fff` as text. Deep Navy for body, White for surfaces.
- **Don't** add shadows to cards. Tonal separation is sufficient.
- **Don't** use gradient text, glassmorphism, or side-stripe borders.
- **Don't** put an accent border-left on cards. Use full borders or nothing.
- **Don't** use zebra-striped tables. Subtle row borders are cleaner.
- **Don't** stack identical card grids as a data display. Use tables.
- **Don't** wrap everything in a card. The page background is the primary surface.
- **Don't** use the Crest Purple for interactive elements — it's a heraldic identifier, not a call to action.
