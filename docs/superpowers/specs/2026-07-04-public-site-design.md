# Public College Website — Design Spec

## 1. Overview

**Goal:** Build a new public-facing college website on the CollegeLMS stack (.NET 10 + Next.js 14 + Tailwind CSS v4).

**Scope:** Public site + authentication (internal services deferred).

**Philosophy:** Build the beautiful new site first (empty data), then migrate 3720 WordPress news articles later.

## 2. User Stories

| UC | Description | Status |
|----|-------------|--------|
| UC-1 | Visitor views homepage | ❌ |
| UC-2 | Visitor views news list with pagination | ❌ |
| UC-3 | Visitor views a single news article | ❌ |
| UC-4 | Admin manages news (CRUD) | ❌ |
| UC-7..11 | Auth (login, logout, profile, user management) | ✅ Done |

**Explicitly excluded:** UC-5 (Pages), UC-6 (WordPress import), UC-12+ (Schedule, Courses, etc.)

## 3. Architecture

### 3.1 Data Model

**News** entity (inherits `Entity` — Guid Id, CreatedAt, UpdatedAt):

| Column | Type | Constraints |
|--------|------|-------------|
| Title | string | Required, MaxLength(255) |
| Content | string | Required |
| ImageUrl | string? | Optional, MaxLength(2048) |
| CategoryId | Guid? | FK → NewsCategory |
| IsPublished | bool | Default false |
| PublishedAt | DateTime | Default UtcNow |
| IsDeleted | bool | Soft delete |
| CreatedById | Guid | FK → User |

**NewsCategory** entity:

| Column | Type | Constraints |
|--------|------|-------------|
| Name | string | Required, MaxLength(100) |
| Slug | string | Required, MaxLength(100), Unique |

### 3.2 API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/news` | Public | Paginated news list (sorted by PublishedAt desc) |
| `GET` | `/api/news/{id}` | Public | Single news detail |
| `POST` | `/api/news` | Admin | Create news |
| `PUT` | `/api/news/{id}` | Admin | Update news |
| `DELETE` | `/api/news/{id}` | Admin | Soft delete (IsDeleted = true) |
| `GET` | `/api/news/categories` | Public | Category list |

**Query params for GET /api/news:**
- `page` (default 1)
- `pageSize` (default 20, max 100)
- `categoryId` (optional filter)
- `search` (optional, title/content search)

### 3.3 Project Structure

**Backend (existing patterns):**
- `Entities/News.cs`, `Entities/NewsCategory.cs`
- `Entities/Enums/` — (none needed)
- `Data/Configurations/NewsConfiguration.cs`
- `Data/DbConstraints.cs` — CHECK constraints (if needed)
- `Dtos/NewsRequest.cs`, `Dtos/NewsResponse.cs`, `Dtos/NewsCategoryResponse.cs`
- `Mappers/NewsMapper.cs`
- `Interfaces/INewsService.cs`
- `Services/NewsService.cs`
- `Controllers/NewsController.cs`
- `Validators/NewsRequestValidator.cs`
- `SwaggerExamples/NewsResponseExample.cs`

**Frontend (route group `(public)`):**
- `app/(public)/layout.tsx` — public header + footer
- `app/(public)/page.tsx` — homepage
- `app/(public)/news/page.tsx` — news list
- `app/(public)/news/[id]/page.tsx` — news detail
- `app/(public)/login/page.tsx` — login (redirect from existing)

### 3.4 Homepage Content

Hardcoded in components (no entity/API):

- Hero section: college name, tagline, logo
- About section: brief college description
- News highlights: latest 3-6 news via `GET /api/news?pageSize=6`
- Contacts: address, phone, email
- Footer: full contact info, copyright, social links

## 4. Design Process

### Phase 0 — UX Research (Layers)

1. **layers-orient**: Audit public college website — identify users (abiturient, student, parent, teacher, admin), their tasks, bottleneck layer
2. **layers-user-needs**: Extract prioritized job stories from requirements
3. **layers-conceptual-model**: Map objects (News, Category, Navigation) and vocabulary
4. **layers-interaction-flow**: Detail edge cases, empty states, user flows for:
   - Browsing news (list → detail → back)
   - Navigating site (header → pages → footer)
   - Login flow
   - Admin CRUD (create → save → see in list)
5. **layers-surface**: Wireframe key pages (homepage, news list, news detail, login)

### Phase 1 — Visual Direction (frontend-design)

- Derive visual language from college logo (colors, crest, character)
- Typography, spacing, motion personality
- Commit to a direction before implementation

### Phase 2 — Implementation (design-system + nextjs-page)

- Build public layout (header with logo/nav, footer with contacts)
- Build homepage sections (hero, about, news, contacts)
- Build news list page (paginated card grid)
- Build news detail page (full article with image)
- Build admin news management (CRUD form, table)
- Follow DESIGN.md tokens strictly

### Phase 3 — Polish & Audit (refactor-ui + web-design-guidelines)

- Full design pass: hierarchy, typography, color, spacing, buttons, clutter, empty states, shadows, contrast, grouping
- Code-level audit: a11y, forms, focus, animation, performance

## 5. Existing Assets

- **DESIGN.md** — Full design system: colors (from logo), typography (Inter), components, icons (Lucide), elevation
- **design-references.md** — 7 Russian educational sites analyzed: lemanapro, VSK, VTB, STGAU, MTS Bank, T-Bank, Netology
- **Logo**: `http://stvcc.ru/wp-content/uploads/2017/02/logo.jpg` (800×531 px, permanent)
- **Old site**: WordPress at stvcc.ru, REST API live

## 6. Implementation Order

| Phase | Agent | Work | Est. Time |
|-------|-------|------|-----------|
| 1. Backend | BackendAgent | Entity → Config → DTO → Mapper → Service → Controller → Validators → Swagger → DI → Migration | ~1h |
| 2. Tests | TesterAgent | Unit + Integration tests | ~40m |
| 3. UX | FrontendAgent | Layers (orient → needs → model → flow → surface) | ~1h |
| 4. Visual | FrontendAgent | frontend-design concept | ~30m |
| 5. Frontend | FrontendAgent | Layout + pages + components | ~1.5h |
| 6. Polish | FrontendAgent | refactor-ui + web-design-guidelines | ~40m |
| 7. Docs | AnalystAgent | PlantUML diagrams | ~20m |
| 8. Merge | Architect | Verification → commit → push | ~10m |

## 7. Exclusions & Future

- **Not now:** Pages entity, WordPress import, Schedule, Courses, Testing, Journal
- **Not now:** UC-6 import — will be done as separate feature after site is built
- **Not now:** SiteSettings entity — homepage content is hardcoded
