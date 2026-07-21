"use client"

import { useState, useRef, useEffect } from "react"
import Link from "next/link"
import { Menu, X, Search, User, LogOut } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"

const socialLinks = [
  { href: "https://vk.com/stvcc_stav", label: "ВКонтакте", icon: "vk" },
  { href: "https://t.me/stvcc", label: "Telegram", icon: "tg" },
  { href: "https://youtube.com/@stvcc", label: "YouTube", icon: "yt" },
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
  if (icon === "yt") {
    return (
      <svg viewBox="0 0 24 24" fill="currentColor" className={className}>
        <path d="M21.593 7.203a2.506 2.506 0 00-1.762-1.766C18.265 5.007 12 5 12 5s-6.264-.007-7.831.404a2.56 2.56 0 00-1.766 1.778c-.413 1.566-.417 4.814-.417 4.814s-.004 3.264.406 4.814c.23.857.905 1.534 1.763 1.765 1.582.43 7.83.437 7.83.437s6.265.007 7.831-.403a2.515 2.515 0 001.767-1.763c.414-1.565.417-4.812.417-4.812s.02-3.265-.407-4.831zM9.996 15.005l.005-6 5.207 3.005-5.212 2.995z"/>
      </svg>
    )
  }
  return null
}

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)
  const { user, logout } = useAuth()
  const profileRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (profileRef.current && !profileRef.current.contains(e.target as Node)) {
        setProfileOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  useEffect(() => {
    function onScroll() {
      setScrolled(window.scrollY > 0)
    }
    window.addEventListener("scroll", onScroll, { passive: true })
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  const initials = user
    ? user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)
    : "?"

  return (
    <header className="sticky top-0 z-50 bg-bg">
      <div className="flex">
        {/* Logo column — full height, spans both rows */}
        <Link href="/" className="flex shrink-0 items-center border-b border-r border-border px-4 lg:px-6" style={{ maxWidth: "220px" }}>
          <img
            src="/logo.svg"
            alt="Ставропольский колледж связи"
            className="object-contain h-auto w-auto dark:brightness-0 dark:invert"
            style={{ maxHeight: "110px" }}
          />
        </Link>

        {/* Right side: two rows stacked */}
        <div className="flex min-w-0 flex-1 flex-col">
          {/* Row 1: Top bar */}
          <div className={`border-b border-border bg-muted/50 transition-transform duration-300 ${scrolled ? "-translate-y-full" : ""}`}>
            <div className="flex h-9 items-center justify-between px-4">
              <div className="flex items-center gap-2">
                {socialLinks.map((link) => (
                  <a
                    key={link.href}
                    href={link.href}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center justify-center h-7 w-7 rounded text-muted-fg hover:text-accent transition-colors"
                    aria-label={link.label}
                  >
                    <SocialIcon icon={link.icon} className="h-4 w-4" />
                  </a>
                ))}
              </div>
              <div className="flex items-center gap-2" ref={profileRef}>
                {user ? (
                  <div className="relative">
                    <button
                      onClick={() => setProfileOpen(!profileOpen)}
                      className="flex items-center gap-2 rounded-md px-2 py-1 text-xs font-medium text-muted-fg hover:text-accent hover:bg-muted transition-colors"
                    >
                      <span className="flex h-6 w-6 items-center justify-center rounded-full bg-accent/20 text-[10px] font-bold text-accent">
                        {initials}
                      </span>
                      <span className="max-w-[100px] truncate">{user.fullName}</span>
                    </button>
                    {profileOpen && (
                      <div className="absolute right-0 top-full z-50 mt-1 w-48 rounded-lg border border-border bg-card shadow-lg p-1">
                        <Link href="/my/profile" onClick={() => setProfileOpen(false)} className="flex items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-fg hover:bg-muted hover:text-fg transition-colors">
                          <User size={16} /> Личный кабинет
                        </Link>
                        <hr className="my-1 border-border" />
                        <button
                          onClick={() => { logout() }}
                          className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-fg hover:bg-muted hover:text-fg transition-colors"
                        >
                          <LogOut size={16} /> Выйти
                        </button>
                      </div>
                    )}
                  </div>
                ) : (
                  <Link href="/login" className="rounded-md bg-accent px-3 py-1 text-xs font-medium text-white transition-colors hover:bg-accent-hover">Войти</Link>
                )}
                <span className="mx-1 text-muted-fg/30">|</span>
                <Link href="/schedule" className="text-xs text-muted-fg hover:text-accent transition-colors">Расписание</Link>
                <span className="mx-1 text-muted-fg/30">|</span>
                <Link href="/contacts" className="text-xs text-muted-fg hover:text-accent transition-colors">Контакты</Link>
              </div>
            </div>
          </div>

          {/* Row 2: Navigation */}
          <div className="flex flex-1 items-center justify-between border-b border-border px-4">
            <nav className="hidden lg:flex items-center gap-1">
              {siteNavigation.map((section) => (
                <Link key={section.slug} href={section.href} className="px-3 py-2 text-sm font-medium text-muted-fg hover:text-accent transition-colors rounded-md">
                  {section.title}
                </Link>
              ))}
            </nav>

            <div className="flex items-center gap-2 ml-auto">
              <Link href="/search" className="hidden md:flex items-center justify-center h-9 w-9 rounded-md text-muted-fg hover:text-accent hover:bg-muted transition-colors" aria-label="Поиск"><Search size={18} /></Link>
              <AccessibilityToggle />
              <ThemeToggle />
              <button onClick={() => setMobileOpen(!mobileOpen)} className="lg:hidden ml-2 rounded-md p-2 text-muted-fg hover:bg-muted" aria-label="Меню">
                {mobileOpen ? <X size={20} /> : <Menu size={20} />}
              </button>
            </div>
          </div>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-b border-border bg-bg px-4 pb-4 pt-2">
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
