"use client"

import { useState, useRef, useEffect } from "react"
import Link from "next/link"
import { Menu, X, Search, User, LogOut, Settings } from "lucide-react"
import ThemeToggle from "./ThemeToggle"
import AccessibilityToggle from "./AccessibilityToggle"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const { user, logout } = useAuth()

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setProfileOpen(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [])

  const initials = user
    ? user.fullName
        .split(" ")
        .map((w) => w[0])
        .join("")
        .toUpperCase()
        .slice(0, 2)
    : ""

  return (
      <header className="sticky top-0 z-50 border-b border-border bg-background">
      <div className="mx-auto flex h-20 max-w-7xl items-center justify-between px-4 sm:h-24 sm:px-6 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          <img
            src="/logo.svg"
            alt="Колледж связи"
            className="object-contain"
            style={{ maxHeight: "6rem" }}
          />
          <span className="hidden sm:inline text-base font-semibold text-primary">
            Колледж связи
          </span>
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <div key={section.slug} className="group relative">
              <Link
                href={section.href}
                className="flex items-center gap-1 px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-accent rounded-md hover:bg-muted"
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
                      className="block px-4 py-2 text-sm text-muted-foreground hover:text-accent hover:bg-muted transition-colors"
                    >
                      {sub.title}
                    </Link>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <Link
            href="/search"
            className="hidden md:flex items-center justify-center h-9 w-9 rounded-md text-muted-foreground hover:text-accent hover:bg-muted transition-colors"
            aria-label="Поиск"
          >
            <Search size={18} />
          </Link>
          <AccessibilityToggle />
          <ThemeToggle />
          {user ? (
            <div className="relative ml-2" ref={dropdownRef}>
              <button
                onClick={() => setProfileOpen(!profileOpen)}
                className="flex items-center gap-2 rounded-md p-1.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-accent"
              >
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-accent/20 text-xs font-bold text-accent">
                  {initials}
                </span>
                <span className="hidden md:inline max-w-[120px] truncate">
                  {user.fullName}
                </span>
              </button>
              {profileOpen && (
                <div className="absolute right-0 top-full mt-1 w-56 rounded-lg border border-border bg-card py-1 shadow-lg">
                  <div className="border-b border-border px-4 py-2">
                    <p className="text-sm font-medium text-foreground truncate">
                      {user.fullName}
                    </p>
                    <p className="text-xs text-muted-foreground">{user.email}</p>
                  </div>
                  <Link
                    href="/my/profile"
                    onClick={() => setProfileOpen(false)}
                    className="flex items-center gap-2 px-4 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-accent"
                  >
                    <Settings size={16} />
                    Мой профиль
                  </Link>
                  <button
                    onClick={() => {
                      setProfileOpen(false)
                      logout()
                    }}
                    className="flex w-full items-center gap-2 px-4 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-destructive"
                  >
                    <LogOut size={16} />
                    Выйти
                  </button>
                </div>
              )}
            </div>
          ) : (
            <Link
              href="/login"
              className="ml-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-accent-foreground transition-colors hover:bg-accent/90 hidden sm:inline-block"
            >
              Войти
            </Link>
          )}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className="lg:hidden ml-2 rounded-md p-2 text-muted-foreground hover:bg-muted"
            aria-label="Меню"
          >
            {mobileOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-t border-border bg-background px-4 pb-4 pt-2 max-h-[70vh] overflow-y-auto">

          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => (
              <div key={section.slug}>
                <Link
                  href={section.href}
                  className="block px-3 py-2 text-sm font-medium text-primary rounded-md hover:bg-muted"
                  onClick={() => setMobileOpen(false)}
                >
                  {section.title}
                </Link>
                {section.subsections.map((sub) => (
                  <Link
                    key={sub.slug}
                    href={sub.href}
                    className="block pl-8 pr-3 py-1.5 text-sm text-muted-foreground rounded-md hover:bg-muted"
                    onClick={() => setMobileOpen(false)}
                  >
                    {sub.title}
                  </Link>
                ))}
              </div>
            ))}
            {user ? (
              <>
                <div className="border-t border-border my-2 pt-2 px-3">
                  <p className="text-xs text-muted-foreground">{user.fullName}</p>
                </div>
                <Link
                  href="/my/profile"
                  className="block px-3 py-2 text-sm text-muted-foreground rounded-md hover:bg-muted"
                  onClick={() => setMobileOpen(false)}
                >
                  Мой профиль
                </Link>
                <button
                  onClick={() => {
                    setMobileOpen(false)
                    logout()
                  }}
                  className="block w-full text-left px-3 py-2 text-sm text-destructive rounded-md hover:bg-muted"
                >
                  Выйти
                </button>
              </>
            ) : (
              <Link
                href="/login"
                className="block px-3 py-2 text-sm font-medium text-accent-foreground bg-accent rounded-md text-center mt-2"
                onClick={() => setMobileOpen(false)}
              >
                Войти
              </Link>
            )}
          </nav>
        </div>
      )}
    </header>
  )
}
