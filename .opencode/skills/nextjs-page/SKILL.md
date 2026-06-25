---
name: nextjs-page
description: Create a Next.js page using App Router with Tailwind CSS v4, TypeScript, loading/error states, and API integration
---

# nextjs-page

Create a Next.js page (App Router) with Tailwind CSS v4, TypeScript types, loading/error boundaries, and server action patterns.

## Workflow

### 1. Decide component type

- **Server component** (default `page.tsx`) — fetches data on server, renders HTML. No browser APIs.
- **Client component** — add `'use client'` at top for interactive state, forms, `useEffect`.

### 2. Create page

Path: `app/{route}/page.tsx`

**Server component — data fetching:**
```tsx
import type { PageProps } from "@/types"

interface MyPageProps extends PageProps {
  // params if dynamic route
}

export default async function MyPage({ params, searchParams }: MyPageProps) {
  const res = await fetch(
    `${process.env.NEXT_PUBLIC_API_URL}/api/endpoint`,
    { cache: "no-store" }
  )
  const data = await res.json()

  return (
    <div className="flex flex-col gap-4 p-6">
      <h1 className="text-2xl font-bold">Title</h1>
      {/* render data */}
    </div>
  )
}
```

**Client component — interactive:**
```tsx
"use client"

import { useState, useEffect } from "react"
import type { Item } from "@/types"

export default function MyPage() {
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/endpoint`)
      .then(r => r.json())
      .then(data => { setItems(data); setLoading(false) })
  }, [])

  if (loading) return <div>Loading...</div>

  return (
    <div className="flex flex-col gap-4 p-6">
      {/* interactive UI */}
    </div>
  )
}
```

### 3. Create data types

Path: `types/{name}.ts`

```ts
export interface Item {
  id: string
  name: string
  createdAt: string
}
```

For paginated API responses:
```ts
export interface ApiResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  hasNext: boolean
  hasPrevious: boolean
}
```

### 4. Add loading state

Path: `app/{route}/loading.tsx`

```tsx
export default function Loading() {
  return (
    <div className="flex items-center justify-center p-12">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-300 border-t-blue-600" />
    </div>
  )
}
```

### 5. Add error state

Path: `app/{route}/error.tsx`

```tsx
"use client"

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="flex flex-col items-center gap-4 p-12">
      <h2 className="text-xl font-semibold">Something went wrong</h2>
      <p className="text-gray-500">{error.message}</p>
      <button
        onClick={reset}
        className="rounded bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
      >
        Try again
      </button>
    </div>
  )
}
```

### 6. API call patterns

**Server-side fetch (SSR/SSG):**
```ts
const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/users`, {
  next: { revalidate: 60 }, // ISR — revalidate every 60s
})
const result: ApiResult<User> = await res.json()
```

**Client fetch with SWR (if needed):**
```tsx
import useSWR from "swr"

const { data, error, isLoading } = useSWR<Item[]>(
  `${process.env.NEXT_PUBLIC_API_URL}/api/items`
)
```

### 7. Form handling with Server Actions

```tsx
async function submitAction(formData: FormData) {
  "use server"
  const title = formData.get("title") as string
  // validate, call API, revalidate
}

// In component:
<form action={submitAction} className="flex flex-col gap-3">
  <input
    name="title"
    required
    className="rounded border border-gray-300 p-2"
  />
  <button
    type="submit"
    className="rounded bg-blue-600 px-4 py-2 text-white"
  >
    Submit
  </button>
</form>
```

### 8. Tailwind CSS v4 conventions

- Utility classes only — no custom CSS files
- Colors: `bg-blue-*`, `text-gray-*` for college branding
- Layout: `flex`, `grid`, `gap-*`, `p-*`, `m-*`
- Responsive: `sm:`, `md:`, `lg:` prefixes
- Forms: `rounded border border-gray-300 p-2` for inputs
- Buttons: `rounded px-4 py-2 text-white` with hover state

### 9. Design reference (from OmniFoodApp patterns)

- Font size: 16px–32px, weight ≥400
- Line height: 1.5–2 (larger for bigger text)
- Text left-aligned
- Short headings in caps
- Images from Unsplash/Pexels at 2× display size

## Convention rules

- Server components by default — `'use client'` only when necessary
- TypeScript interfaces for all data shapes
- `loading.tsx` and `error.tsx` for every route segment
- API base URL from `NEXT_PUBLIC_API_URL` env var
- Snake_case JSON from API → camelCase in TypeScript

## Dockerfile for Next.js (production)

```dockerfile
FROM node:22-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs
COPY --from=builder /app/public ./public
RUN mkdir .next && chown nextjs:nodejs .next
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
```

## Verification

- `npm run dev` starts without errors
- Page loads and renders data correctly
- Loading state shows during fetch
- Error state handles failures gracefully
- `loading.tsx` and `error.tsx` work for route segment
