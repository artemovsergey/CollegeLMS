# Design System Evolution — Logo-Driven Palette Refresh

## Rationale

Current design system was built with shadcn/ui defaults (navy blue primary, muted blue accent). The college logo at stvcc.ru contains a distinct palette that should drive all visual decisions. This spec maps logo colors → design tokens → implementation with minimal structural changes (Approach A).

## Color Palette

### Light mode

| Token | Current (OKLCH) | → Target (HEX) | Logo reference |
|-------|-----------------|----------------|----------------|
| `--primary` | `oklch(0.25 0.08 260)` | `#293553` | Тёмно-синий (надписи, контуры) |
| `--accent` | `oklch(0.58 0.12 250)` | `#578DD4` | Яркий голубой (глобус, внешнее кольцо) |
| `--muted-foreground` | `oklch(0.52 0.04 190)` | `#617D81` | Серо-графитовый (антенна) |
| `--secondary` | `oklch(0.91 0.01 80)` | `#E6E4DE` | Кремовый (тарелка антенны) |
| `--muted` | `oklch(0.95 0.005 260)` | `#E6E4DE` | Кремовый фон карточек |
| `--lilac` | `oklch(0.72 0.04 280)` | `#B0B0CE` | Лиловый (лента с названием) |
| `--success` | `oklch(0.52 0.15 145)` | `#348B38` | Зелёный (лавровые ветви) |
| `--border` | `oklch(0.83 0.01 260)` | → подстроить под `#578DD4` at low opacity | |
| `--ring` | `oklch(0.58 0.12 250)` | `#578DD4` | Фокус-кольцо = accent |

### Dark mode
- All tokens adapt proportionally (luminance shift)
- `--primary` becomes lighter `oklch(0.55 0.12 260)` stays
- `--accent` stays `#578DD4` equivalent `oklch(0.60 0.13 250)`
- `--secondary` becomes dark `oklch(0.25 0.015 260)`

### OKLCH conversion notes
All `#578DD4` → `oklch(0.60 0.09 255)` approximate
All `#293553` → `oklch(0.25 0.06 265)`

## Typography

| Role | Current | → Target | Reason |
|------|---------|----------|--------|
| Display (headings) | Inter | Golos Display | Characterful Russian grotesk, wider weight range |
| Body | Inter | Golos Text | Better Cyrillic readability than Inter |
| Mono | JetBrains Mono | (keep) | Already good |

Packages to add:
- `@fontsource/golos-text`
- `@fontsource/golos-display`

Remove Inter from font stack, keep as fallback.

## Component changes

### What stays
- Layout (max-w-7xl, grid, spacing)
- Header structure (sticky, nav, mobile menu)
- Footer
- Carousel
- Forms
- News cards
- All section components

### What changes
| Component | Change |
|-----------|--------|
| Header active nav link | `text-accent` now `#578DD4` (brighter) |
| All `bg-accent` buttons | Brighter blue `#578DD4` |
| All `text-accent` links | Brighter blue `#578DD4` |
| Card backgrounds | `--muted` → `#E6E4DE` (cream tint) |
| `--border` | Subtle blue-ish tint from `#578DD4` at low opacity |
| Input focus ring | `--ring` = `#578DD4` |
| Success states | `--success` = `#348B38` (laurel green) |

### Specialty cards (vkipo.ru pattern)
Already implemented as cards with icon + code + qualification + duration. Only color changes.

## Files to modify

1. `frontend/app/globals.css` — update CSS variable values
2. `frontend/app/layout.tsx` — add Golos font loading
3. `frontend/components/Header.tsx` — no changes needed (uses variables)
4. `frontend/components/Footer.tsx` — no changes needed (uses variables)
5. `frontend/components/SpecialtiesSection.tsx` — no changes needed (uses variables)
6. `frontend/package.json` — add @fontsource/golos-text, @fontsource/golos-display

## Scope

- **In scope**: CSS variable updates, font loading, visual verification
- **Not in scope**: structural layout changes, new components, dark mode overhaul, logo replacement, hero section redesign
