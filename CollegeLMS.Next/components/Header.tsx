"use client"

import { useState, useRef, useEffect } from "react"
import Link from "next/link"
import { Menu, X, Search, ChevronDown, User, LogIn } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"

const socialLinks = [
  { href: "https://vk.com/stvcc_stav", label: "ВКонтакте", icon: "vk" },
  { href: "https://t.me/stvcc", label: "Telegram", icon: "tg" },
  { href: "https://max.ru/id2634028465_gos", label: "Max", icon: "max" },
]

function SocialIcon({ icon, className }: { icon: string; className?: string }) {
  if (icon === "vk") {
    return (
      <svg viewBox="0 0 24 24" fill="currentColor" className={className}>
        <path fillRule="evenodd" clipRule="evenodd" d="M12.612 18C6.177 18 2.506 13.588 2.353 6.248h3.224c.106 5.388 2.482 7.67 4.364 8.14v-8.14h3.035v4.647c1.86-.2 3.812-2.318 4.47-4.647h3.036c-.506 2.87-2.623 4.988-4.13 5.858 1.506.706 3.918 2.553 4.836 5.894h-3.341c-.718-2.235-2.506-3.964-4.87-4.2V18h-.365z"/>
      </svg>
    )
  }
  if (icon === "tg") {
    return (
      <svg viewBox="0 0 24 24" fill="currentColor" className={className}>
        <path d="M20.665 3.717l-17.73 6.837c-1.21.486-1.203 1.16-.222 1.462l4.552 1.42L17.797 6.79c.498-.303.953-.14.579.192l-8.533 7.7h-.002l.002.002-.314 4.692c.46 0 .663-.211.921-.46l2.211-2.15 4.599 3.397c.848.467 1.457.227 1.668-.785L21.947 5.15c.309-1.24-.473-1.8-1.282-1.434z"/>
      </svg>
    )
  }
  if (icon === "max") {
    return (
      <svg viewBox="0 0 1000 1000" fill="currentColor" className={className}>
        <path fillRule="evenodd" clipRule="evenodd" d="M508.211 878.328c-75.007 0-109.864-10.95-170.453-54.75-38.325 49.275-159.686 87.783-164.979 21.9 0-49.456-10.95-91.248-23.36-136.873-14.782-56.21-31.572-118.807-31.572-209.508 0-216.626 177.754-379.597 388.357-379.597 210.785 0 375.947 171.001 375.947 381.604.707 207.346-166.595 376.118-373.94 377.224m3.103-571.585c-102.564-5.292-182.499 65.7-200.201 177.024-14.6 92.162 11.315 204.398 33.397 210.238 10.585 2.555 37.23-18.98 53.837-35.587a189.8 189.8 0 0 0 92.71 33.032c106.273 5.112 197.08-75.794 204.215-181.95 4.154-106.382-77.67-196.486-183.958-202.574Z"/>
      </svg>
    )
  }
  return null
}

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)
  const [openMenu, setOpenMenu] = useState<string | null>(null)
  const [openMobileSection, setOpenMobileSection] = useState<string | null>(null)
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    function onScroll() {
      setScrolled(window.scrollY > 0)
    }
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        setOpenMenu(null)
        setMobileOpen(false)
        setOpenMobileSection(null)
      }
    }
    function onClickOutside(e: MouseEvent) {
      if ((e.target as HTMLElement).closest("[data-nav-item]")) return
      setOpenMenu(null)
    }
    window.addEventListener("keydown", onKeyDown)
    window.addEventListener("click", onClickOutside)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
      window.removeEventListener("click", onClickOutside)
    }
  }, [])

  const openMenuDelayed = (slug: string) => {
    if (closeTimer.current) clearTimeout(closeTimer.current)
    setOpenMenu(slug)
  }
  const closeMenuDelayed = () => {
    if (closeTimer.current) clearTimeout(closeTimer.current)
    closeTimer.current = setTimeout(() => setOpenMenu(null), 120)
  }

  const navSections = siteNavigation.filter(s => s.inHeader !== false)

  return (
    <header className="sticky top-0 z-50 bg-accent">
      <div className="flex flex-col">
        {/* Row 1: Top bar — hides on scroll */}
        <div className={`overflow-hidden transition-all duration-300 ease-in-out ${scrolled ? "max-h-0 opacity-0 py-0 border-transparent" : "max-h-14 opacity-100"}`}>
          <div className="flex h-12 items-center justify-between px-4 lg:px-6">
            <div className="flex items-center gap-2">
              {socialLinks.map((link) => (
                <a
                  key={link.href}
                  href={link.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center h-7 w-7 rounded text-white/60 hover:text-white transition-colors"
                  aria-label={link.label}
                >
                  <SocialIcon icon={link.icon} className="h-4 w-4" />
                </a>
              ))}
              <span className="mx-1 text-white/20">|</span>
              <Link href="/contacts" className="text-xs text-white/70 hover:text-white transition-colors">Контакты</Link>
              <span className="mx-1 text-white/20">|</span>
              <Link href="/admissions" className="text-xs font-semibold text-amber-300 hover:text-amber-200 transition-colors">Приёмная кампания 2026</Link>
            </div>
            <div className="flex items-center gap-1">
              <Link href="/search" className="flex items-center justify-center h-8 w-8 rounded-md text-white/80 hover:text-white hover:bg-white/10 transition-colors" aria-label="Поиск"><Search size={16} /></Link>
              <div className="flex items-center [&_button]:text-white/80 [&_button]:hover:text-white [&_button]:hover:bg-white/10 [&_button]:rounded-md [&_button]:p-1.5">
                <AccessibilityToggle />
                <ThemeToggle />
              </div>
            </div>
          </div>
        </div>

        {/* Row 2: Logo text + Navigation */}
        <div className="flex items-center justify-between border-b border-white/10 px-4 lg:px-6">
          <Link href="/" className="flex shrink-0 flex-col py-3 leading-tight">
            <span className="text-base sm:text-lg font-bold text-white">Ставропольский колледж связи</span>
            <span className="text-[10px] sm:text-xs text-white/60">имени Героя Советского Союза В.А. Петрова</span>
          </Link>

          <nav className="hidden lg:flex items-center justify-center gap-0.5" aria-label="Главное меню">
            {navSections.map((section) => {
              const hasSubs = section.subsections.length > 0
              const isOpen = openMenu === section.slug
              return (
                <div
                  key={section.slug}
                  data-nav-item
                  className="relative"
                  onMouseEnter={() => hasSubs && openMenuDelayed(section.slug)}
                  onMouseLeave={() => hasSubs && closeMenuDelayed()}
                  onFocus={(e) => {
                    if (hasSubs && (e.target as HTMLElement).closest("[data-nav-item]")) openMenuDelayed(section.slug)
                  }}
                  onBlur={(e) => {
                    const next = e.relatedTarget as HTMLElement | null
                    if (hasSubs && !(next && e.currentTarget.contains(next))) closeMenuDelayed()
                  }}
                >
                  <Link
                    href={section.href}
                    aria-expanded={hasSubs ? isOpen : undefined}
                    aria-haspopup={hasSubs ? "true" : undefined}
                    className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-white/80 hover:text-white transition-colors rounded-md"
                  >
                    {section.title}
                    {hasSubs && (
                      <ChevronDown size={14} className={`transition-transform duration-200 ${isOpen ? "rotate-180" : ""}`} />
                    )}
                  </Link>

                  {hasSubs && (
                    <div
                      className={`absolute left-1/2 top-full z-50 w-64 -translate-x-1/2 pt-2 transition-all duration-150 ease-out ${
                        isOpen ? "visible translate-y-0 opacity-100" : "invisible -translate-y-1 opacity-0"
                      }`}
                    >
                      <div className="overflow-hidden rounded-lg border border-border bg-white shadow-xl">
                        {section.subsections.map((sub) => (
                          <Link
                            key={sub.slug}
                            href={sub.href}
                            className="block border-b border-border/50 px-4 py-2.5 text-sm text-fg transition-colors last:border-b-0 hover:bg-muted hover:text-primary"
                          >
                            {sub.title}
                          </Link>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )
            })}
          </nav>

          <div className="flex items-center justify-end gap-2">
            <Link
              href="/login"
              className="inline-flex items-center gap-1.5 whitespace-nowrap rounded-md border border-white/20 px-3 py-1.5 text-sm font-medium text-white/90 hover:bg-white/10 hover:text-white transition-colors"
            >
              <LogIn size={16} />
              <span className="hidden sm:inline">Войти</span>
              <span className="sm:hidden sr-only">Войти</span>
            </Link>
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="lg:hidden rounded-md p-2 text-white/80 hover:bg-white/10"
              aria-label={mobileOpen ? "Закрыть меню" : "Меню"}
              aria-expanded={mobileOpen}
            >
              {mobileOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-b border-white/10 bg-accent px-4 pb-4 pt-2">
          <nav className="flex flex-col gap-1">
            {navSections.map((section) => {
              const hasSubs = section.subsections.length > 0
              const isOpen = openMobileSection === section.slug
              return (
                <div key={section.slug}>
                  <div className="flex items-center justify-between gap-2">
                    <Link
                      href={section.href}
                      className="block flex-1 px-3 py-2 text-sm font-medium text-white/80 rounded-md hover:bg-white/10"
                      onClick={() => {
                        setMobileOpen(false)
                        setOpenMobileSection(null)
                      }}
                    >
                      {section.title}
                    </Link>
                    {hasSubs && (
                      <button
                        onClick={() => setOpenMobileSection(isOpen ? null : section.slug)}
                        className="rounded-md p-2 text-white/80 hover:bg-white/10"
                        aria-label={`Показать подпункты раздела ${section.title}`}
                        aria-expanded={isOpen}
                      >
                        <ChevronDown size={16} className={`transition-transform duration-200 ${isOpen ? "rotate-180" : ""}`} />
                      </button>
                    )}
                  </div>
                  {hasSubs && (
                    <div className={`overflow-hidden transition-all duration-200 ease-out ${isOpen ? "max-h-96" : "max-h-0"}`}>
                      <div className="ml-4 border-l border-white/20 pl-3">
                        {section.subsections.map((sub) => (
                          <Link
                            key={sub.slug}
                            href={sub.href}
                            onClick={() => {
                              setMobileOpen(false)
                              setOpenMobileSection(null)
                            }}
                            className="block px-3 py-1.5 text-sm text-white/70 rounded-md hover:bg-white/10 hover:text-white"
                          >
                            {sub.title}
                          </Link>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )
            })}

            <Link
              href="/login"
              onClick={() => {
                setMobileOpen(false)
                setOpenMobileSection(null)
              }}
              className="mt-2 flex items-center justify-center gap-1.5 rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-white hover:text-accent"
            >
              <User size={16} />
              Войти
            </Link>
          </nav>
        </div>
      )}
    </header>
  )
}
