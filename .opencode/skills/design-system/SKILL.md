# Design System Skill

CollegeLMS design system — Tailwind CSS v4 + shadcn/ui + Lucide icons.

## When to use

- Creating new pages or components
- Adding new UI elements
- Refactoring existing UI
- Setting up design tokens
- Building form patterns

## Tech stack

- **Styling**: Tailwind CSS v4 (CSS-first config, no tailwind.config.js)
- **Components**: shadcn/ui (Radix UI primitives)
- **Icons**: Lucide React
- **Animation**: Framer Motion (optional)
- **Fonts**: Inter (sans), JetBrains Mono (mono)

## Design tokens

CSS custom properties defined in `tokens/`. Import in `globals.css`:

```css
@import "tailwindcss";
@import "./tokens/colors.css";
@import "./tokens/typography.css";
@import "./tokens/spacing.css";
```

## Component organization

```
frontend/
  components/
    ui/           # shadcn primitives (button, input, card, etc.)
    forms/        # Form-specific composed components
    layout/       # Layout components (sidebar, header, etc.)
  lib/
    utils.ts      # cn() helper from shadcn
```

- `components/ui/` — shadcn-generated, do not edit manually
- `components/` — project-specific composed components using ui/ primitives

## Tailwind conventions

- Use utility classes directly in JSX
- Responsive: `sm:`, `md:`, `lg:` prefixes (mobile-first)
- Colors: shadcn semantic tokens (`bg-primary`, `text-muted-foreground`)
- Spacing: consistent `gap-4`, `p-4`, `m-2` patterns
- Use `cn()` for conditional classes

## Accessibility

- Semantic HTML: `<button>`, `<nav>`, `<main>`, `<section>`
- ARIA labels on all interactive elements
- Keyboard navigation for modals and dropdowns
- Focus visible indicators (`focus-visible:ring-2`)
- Screen reader text with `sr-only`

## Icon system

```tsx
import { LucideIcon } from "lucide-react"
// Use: <Icon className="h-4 w-4" />
```

Preferred size: `h-4 w-4` (inline), `h-5 w-5` (buttons), `h-6 w-6` (headers)

## Responsive patterns

- Mobile-first: base styles for mobile, `md:` for tablet, `lg:` for desktop
- Touch targets: minimum 44x44px for interactive elements
- Grid: `grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4`
- Container: `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8`

## Form patterns

- Labels always visible (no placeholder-only labels)
- Error messages below input, red text
- Required fields marked with `*` or "обязательно"
- Loading state on submit button
- Client-side validation with server-side fallback

## Files

- `tokens/colors.css` — color palette
- `tokens/typography.css` — font scale
- `tokens/spacing.css` — spacing, radius, shadows
- `components/button.md` — button patterns
- `components/input.md` — input patterns
- `components/card.md` — card layouts
- `components/table.md` — table with sort/filter
- `components/modal.md` — dialog patterns
- `components/badge.md` — status badges
- `components/toast.md` — notifications
