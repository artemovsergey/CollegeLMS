"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Menu, X, Search } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import { siteNavigation } from "@/data/site-content"

function splitIntoTwo<T>(items: T[]): [T[], T[]] {
  const mid = Math.ceil(items.length / 2)
  return [items.slice(0, mid), items.slice(mid)]
}

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 0)
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  return (
    <>
      <header
        className={`sticky top-0 z-50 bg-background transition-shadow duration-200 ${
          scrolled ? "shadow-sm" : "shadow-none"
        }`}
      >
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <Link href="/" className="flex items-center gap-3 shrink-0">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src="/logo.png"
              alt="Ставропольский колледж связи"
              className="h-12 w-12 rounded object-contain"
            />
            <div className="hidden sm:flex flex-col">
              <span className="text-sm font-bold leading-tight text-foreground max-w-64">
                ГБПОУ «Ставропольский колледж связи им. В.А. Петрова»
              </span>
            </div>
          </Link>

          <nav className="hidden lg:flex items-center gap-0.5">
            {siteNavigation.map((section) => (
              <div key={section.slug} className="group relative">
                <Link
                  href={section.href}
                  className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-primary rounded-md hover:bg-muted"
                >
                  {section.title}
                  <svg width="10" height="10" viewBox="0 0 10 10" fill="none" className="mt-0.5">
                    <path d="M2.5 3.75L5 6.25L7.5 3.75" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </Link>
                <div className="invisible group-hover:visible opacity-0 group-hover:opacity-100 absolute top-full left-0 mt-0 rounded-lg border border-border bg-card shadow-lg transition-all duration-200 z-50 min-w-[400px]">
                  {(() => {
                    const [leftCol, rightCol] = splitIntoTwo(section.subsections)
                    return (
                      <div className="grid grid-cols-2 gap-0 p-1.5">
                        <div>
                          {leftCol.map((sub) => (
                            <Link
                              key={sub.slug}
                              href={sub.href}
                              className="block px-3 py-1.5 text-sm text-muted-foreground hover:text-primary hover:bg-muted rounded-md transition-colors"
                            >
                              {sub.title}
                            </Link>
                          ))}
                        </div>
                        <div>
                          {rightCol.map((sub) => (
                            <Link
                              key={sub.slug}
                              href={sub.href}
                              className="block px-3 py-1.5 text-sm text-muted-foreground hover:text-primary hover:bg-muted rounded-md transition-colors"
                            >
                              {sub.title}
                            </Link>
                          ))}
                        </div>
                      </div>
                    )
                  })()}
                </div>
              </div>
            ))}
            <Link
              href="/news"
              className="px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-primary rounded-md hover:bg-muted"
            >
              Новости
            </Link>
          </nav>

          <div className="flex items-center gap-1">
            <button
              className="hidden sm:inline-flex items-center justify-center rounded-md p-2 text-muted-foreground hover:bg-muted transition-colors"
              aria-label="Поиск"
            >
              <Search size={18} />
            </button>
            <ThemeToggle />
            <Link
              href="/login"
              className="ml-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 hidden sm:inline-block"
            >
              Войти
            </Link>
            <button
              onClick={() => setMobileOpen(true)}
              className="lg:hidden ml-2 rounded-md p-2 text-muted-foreground hover:bg-muted"
              aria-label="Меню"
            >
              <Menu size={20} />
            </button>
          </div>
        </div>
      </header>

      {/* Mobile slide-out drawer */}
      {mobileOpen && (
        <div className="fixed inset-0 z-[60] lg:hidden">
          <div
            className="absolute inset-0 bg-black/40 transition-opacity"
            onClick={() => setMobileOpen(false)}
          />
          <div className="absolute right-0 top-0 h-full w-80 max-w-[85vw] bg-background shadow-xl overflow-y-auto">
            <div className="flex items-center justify-between px-4 h-16 border-b border-border">
              <span className="text-sm font-bold text-foreground">Меню</span>
              <button
                onClick={() => setMobileOpen(false)}
                className="rounded-md p-2 text-muted-foreground hover:bg-muted"
                aria-label="Закрыть"
              >
                <X size={20} />
              </button>
            </div>
            <nav className="flex flex-col gap-0 px-4 py-4">
              {siteNavigation.map((section) => (
                <div key={section.slug} className="border-b border-border last:border-b-0">
                  <Link
                    href={section.href}
                    className="block px-3 py-3 text-sm font-semibold text-foreground"
                    onClick={() => setMobileOpen(false)}
                  >
                    {section.title}
                  </Link>
                  <div className="pb-2">
                    {section.subsections.map((sub) => (
                      <Link
                        key={sub.slug}
                        href={sub.href}
                        className="block pl-6 pr-3 py-1.5 text-sm text-muted-foreground rounded-md hover:bg-muted"
                        onClick={() => setMobileOpen(false)}
                      >
                        {sub.title}
                      </Link>
                    ))}
                  </div>
                </div>
              ))}
              <Link
                href="/news"
                className="block px-3 py-3 text-sm font-semibold text-foreground border-b border-border"
                onClick={() => setMobileOpen(false)}
              >
                Новости
              </Link>
              <div className="mt-4 flex flex-col gap-2">
                <Link
                  href="/login"
                  className="flex items-center justify-center rounded-md bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground"
                  onClick={() => setMobileOpen(false)}
                >
                  Войти
                </Link>
              </div>
            </nav>
          </div>
        </div>
      )}
    </>
  )
}
