---
name: frontend-scaffold
description: Scaffold the Next.js project with Tailwind CSS v4 and TypeScript
---

# frontend-scaffold

Scaffold the Next.js frontend project for CollegeLMS.

## Setup (first time only)

```bash
npx create-next-app@14 frontend --typescript --tailwind --eslint --app --src-dir
```

## Structure

```
frontend/
  app/                    # App Router pages
    page.tsx              # Home page (users list)
    layout.tsx            # Root layout
    loading.tsx           # Loading state
    error.tsx             # Error boundary
    globals.css           # Tailwind imports
  components/             # Shared components
  types/                  # TypeScript type definitions
  public/                 # Static assets
  next.config.js          # Next.js config (output: 'standalone')
  tailwind.config.ts      # Tailwind CSS config
  tsconfig.json           # TypeScript config
  package.json
```

## Configuration

### next.config.js

```js
/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
}
module.exports = nextConfig
```

### TypeScript types (`frontend/types/index.ts`)

```ts
export interface Result<T> {
  isSuccess: boolean
  data?: T
  errorMessage?: string
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}
```

## Convention rules

- Use App Router (not Pages Router)
- Server components by default, `"use client"` only when needed
- Tailwind CSS v4 for styling
- TypeScript for all files
- Loading/error boundaries for every page
- API calls use `NEXT_PUBLIC_API_URL` env var
