# Site Structure — ГБПОУ СКС

## Goal
Build the full public-facing college website with all sections matching the old WordPress site (`stvcc.ru`).

## Stack
- Next.js 14 App Router, Tailwind CSS v4, TypeScript
- `next-themes` for dark/light mode and accessibility mode
- Static content from `import/wp_data_full.json`
- News via existing `/api/news` API

## Logo
- Source: `http://stvcc.ru/wp-content/uploads/2017/02/logo.jpg`
- Saved to `public/logo.png` (PNG converted)
- Favicon generated from logo → `public/favicon.ico`

## Routes

| Route | Description |
|-------|-------------|
| `/` | Home — hero, carousel (5 latest news w/ images), about, contacts |
| `/news` | All news (exists) |
| `/news/[id]` | Single news (exists) |
| `/about/[...slug]` | "Сведения об ОО" — 14 subsections |
| `/college/[...slug]` | "Колледж" — 9 subsections |
| `/education/[...slug]` | "Образование" — 4 subsections |
| `/admissions/[...slug]` | "Абитуриенту" — 8 subsections |
| `/student-life/[...slug]` | "Студенту" — 6 subsections |
| `/graduates/[...slug]` | "Выпускнику" — 5 subsections |
| `/partners` | "Партнёры" |

## Components

### Header
- Logo + "ГБПОУ СКС"
- Horizontal dropdown menus for each section (one level deep)
- Accessibility toggle (👁) — white bg + black text, large font 18-24px, no animations/images
- Theme toggle (🌙/☀️) — dark/light
- Login button

### Footer
- Contact info (пр-д Черняховского, 3 | +7(8652)24-25-27 | college@stvcc.ru)
- Section links
- учредитель info
- VK social link
- Copyright © ГБПОУ СКС

### Carousel (homepage)
- Shows 5 latest news items with images
- Auto-slide + manual navigation arrows/dots

### Breadcrumbs
- Shows: Главная / Раздел / Подраздел

### AccessibilityToggle
- Applies `font-size: 18px`, high contrast (white/black), disables images/animations
- Stored in localStorage

## Content
All section text extracted from `import/wp_data_full.json` (pages data).

## Design tokens
From DESIGN.md — Deep Navy #152851, College Blue #568cd6, white bg, clean cards without shadows.
