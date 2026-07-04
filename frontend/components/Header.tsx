"use client"

import { useState } from "react"
import Link from "next/link"
import { Menu, X } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 border-b border-border bg-white">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src="/logo.png"
            alt="ГБПОУ СКС"
            className="h-9 w-9 rounded object-contain"
          />
          <div className="hidden flex-col sm:flex">
            <span className="text-sm font-semibold leading-tight text-[#152851]">
              ГБПОУ СКС
            </span>
            <span className="text-xs text-[#5a6a8a] hidden md:inline">
              Ставропольский колледж связи
            </span>
          </div>
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <div key={section.slug} className="group relative">
              <Link
                href={section.href}
                className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-[#5a6a8a] transition-colors hover:text-[#568cd6] rounded-md hover:bg-[#e4edf8]"
              >
                {section.title}
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none" className="mt-0.5">
                  <path d="M3 5L6 8L9 5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
              </Link>
              <div className="invisible group-hover:visible opacity-0 group-hover:opacity-100 absolute top-full left-0 mt-0 w-56 rounded-lg border border-border bg-white shadow-lg transition-all duration-200 z-50">
                <div className="py-1.5">
                  {section.subsections.map((sub) => (
                    <Link
                      key={sub.slug}
                      href={sub.href}
                      className="block px-4 py-2 text-sm text-[#5a6a8a] hover:text-[#568cd6] hover:bg-[#e4edf8] transition-colors"
                    >
                      {sub.title}
                    </Link>
                  ))}
                </div>
              </div>
            </div>
          ))}
          <Link
            href="/news"
            className="px-3 py-2 text-sm font-medium text-[#5a6a8a] transition-colors hover:text-[#568cd6] rounded-md hover:bg-[#e4edf8]"
          >
            Новости
          </Link>
        </nav>

        <div className="flex items-center gap-1">
          <AccessibilityToggle />
          <ThemeToggle />
          <Link
            href="/login"
            className="ml-2 rounded-md bg-[#568cd6] px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-[#3b6ea8] hidden sm:inline-block"
          >
            Войти
          </Link>
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="lg:hidden ml-2 rounded-md p-2 text-[#5a6a8a] hover:bg-[#e4edf8]"
            aria-label="Меню"
          >
            {mobileOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-t border-border bg-white px-4 pb-4 pt-2">
          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => (
              <div key={section.slug}>
                <Link
                  href={section.href}
                  className="block px-3 py-2 text-sm font-medium text-[#152851] rounded-md hover:bg-[#e4edf8]"
                  onClick={() => setMobileOpen(false)}
                >
                  {section.title}
                </Link>
                {section.subsections.map((sub) => (
                  <Link
                    key={sub.slug}
                    href={sub.href}
                    className="block pl-8 pr-3 py-1.5 text-sm text-[#5a6a8a] rounded-md hover:bg-[#e4edf8]"
                    onClick={() => setMobileOpen(false)}
                  >
                    {sub.title}
                  </Link>
                ))}
              </div>
            ))}
            <Link
              href="/news"
              className="block px-3 py-2 text-sm font-medium text-[#152851] rounded-md hover:bg-[#e4edf8]"
              onClick={() => setMobileOpen(false)}
            >
              Новости
            </Link>
            <Link
              href="/login"
              className="block px-3 py-2 text-sm font-medium text-white bg-[#568cd6] rounded-md text-center mt-2"
              onClick={() => setMobileOpen(false)}
            >
              Войти
            </Link>
          </nav>
        </div>
      )}
    </header>
  )
}
