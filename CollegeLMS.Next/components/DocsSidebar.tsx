"use client"

import { useState } from "react"
import Link from "next/link"
import { Menu, X } from "lucide-react"

interface SidebarSubsection {
  title: string
  slug: string
  href: string
}

interface DocsSidebarProps {
  sectionTitle: string
  sectionHref: string
  subsections: SidebarSubsection[]
  currentSlug?: string
}

export default function DocsSidebar({
  sectionTitle,
  sectionHref,
  subsections,
  currentSlug,
}: DocsSidebarProps) {
  const [mobileOpen, setMobileOpen] = useState(false)

  const sidebarContent = (
    <nav className="flex flex-col gap-0.5">
      <Link
        href={sectionHref}
        className="mb-2 block rounded-md px-3 py-2 text-sm font-semibold text-primary hover:bg-muted transition-colors"
      >
        {sectionTitle}
      </Link>
      {subsections.map((sub) => {
        const isActive = sub.slug === currentSlug
        return (
          <Link
            key={sub.slug}
            href={sub.href}
            onClick={() => setMobileOpen(false)}
            className={`block rounded-md px-3 py-1.5 text-sm transition-colors ${
              isActive
                ? "bg-accent/10 font-medium text-accent"
                : "text-muted-foreground hover:bg-muted hover:text-primary"
            }`}
          >
            {sub.title}
          </Link>
        )
      })}
    </nav>
  )

  return (
    <>
      <button
        onClick={() => setMobileOpen(!mobileOpen)}
        className="md:hidden fixed bottom-4 right-4 z-50 flex h-10 w-10 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg"
        aria-label={mobileOpen ? "Закрыть меню" : "Открыть меню"}
      >
        {mobileOpen ? <X size={18} /> : <Menu size={18} />}
      </button>

      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/30 md:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      <aside
        className={`${
          mobileOpen ? "fixed inset-y-0 left-0 z-50 w-64 border-r bg-background" : "hidden"
        } overflow-y-auto p-4 md:sticky md:top-20 md:block md:h-[calc(100vh-5rem)] md:w-56 md:shrink-0 md:border-r md:bg-transparent`}
      >
        {sidebarContent}
      </aside>
    </>
  )
}
