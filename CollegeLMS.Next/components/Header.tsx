"use client"

import { useState } from "react"
import Link from "next/link"
import { Menu, X, Search } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const { user } = useAuth()

  return (
    <header className="sticky top-0 z-50 border-b border-border bg-bg">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          <img src="/logo.svg" alt="Ставропольский колледж связи" className="object-contain" style={{ height: "44px" }} />
          <div className="flex flex-col leading-tight">
            <span className="text-sm font-semibold text-fg">Ставропольский колледж связи</span>
            <span className="text-[11px] text-muted-fg">имени Героя Советского Союза В.А. Петрова</span>
          </div>
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <Link key={section.slug} href={section.href} className="px-3 py-2 text-sm font-medium text-muted-fg hover:text-accent transition-colors rounded-md">
              {section.title}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <Link href="/search" className="hidden md:flex items-center justify-center h-9 w-9 rounded-md text-muted-fg hover:text-accent hover:bg-muted transition-colors" aria-label="Поиск"><Search size={18} /></Link>
          <AccessibilityToggle />
          <ThemeToggle />
          {user ? (
            <button className="flex items-center gap-2 rounded-md p-1.5 text-sm font-medium text-muted-fg hover:text-accent hover:bg-muted transition-colors">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent">
                {user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)}
              </span>
              <span className="hidden md:inline max-w-[120px] truncate">{user.fullName}</span>
            </button>
          ) : (
            <Link href="/login" className="ml-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-accent-hover">Войти</Link>
          )}
          <button onClick={() => setMobileOpen(!mobileOpen)} className="lg:hidden ml-2 rounded-md p-2 text-muted-fg hover:bg-muted" aria-label="Меню">
            {mobileOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>
      {mobileOpen && (
        <div className="lg:hidden border-t border-border bg-bg px-4 pb-4 pt-2">
          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => (
              <Link key={section.slug} href={section.href} className="block px-3 py-2 text-sm font-medium text-fg rounded-md hover:bg-muted" onClick={() => setMobileOpen(false)}>
                {section.title}
              </Link>
            ))}
          </nav>
        </div>
      )}
    </header>
  )
}
