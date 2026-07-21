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
        <path d="M15.684 0H8.316C3.732 0 0 3.732 0 8.316v7.368C0 20.268 3.732 24 8.316 24h7.368C20.268 24 24 20.268 24 15.684V8.316C24 3.732 20.268 0 15.684 0zm3.528 16.632h-1.476c-.6 0-.792-.468-1.872-1.548-.948-.9-1.356-1.044-1.596-1.044-.336 0-.432.132-.432.516v1.368c0 .36-.12.576-1.08.576-1.584 0-3.336-.972-4.572-2.784-1.188-1.596-1.488-2.616-1.488-2.844 0-.132.06-.252.228-.252h1.476c.348 0 .468.156.6.528.672 1.848 1.788 3.456 2.244 3.456.18 0 .252-.072.252-.468v-1.824c-.06-1.008-.588-1.092-.588-1.452 0-.18.144-.336.348-.336h2.352c.288 0 .384.156.384.504v2.712c0 .288.132.384.216.384.18 0 .324-.096.648-.42.84-.936 1.452-2.388 1.452-2.388.084-.168.204-.288.396-.288h1.476c.42 0 .516.216.42.516-.18.864-1.956 3.084-1.956 3.084-.156.252-.204.384 0 .648.144.216.636.636.96 1.02.588.708 1.056 1.308 1.176 1.728.12.408-.084.612-.48.612z"/>
      </svg>
    )
  }
  if (icon === "tg") {
    return (
      <svg viewBox="0 0 24 24" fill="currentColor" className={className}>
        <path d="M11.944 0A12 12 0 000 12a12 12 0 0012 12 12 12 0 0012-12A12 12 0 0012 0a12 12 0 00-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 01.171.325c.016.127.087.497.008 1-.001 0-1.564 6.692-2.238 8.996-.273.944-.546 1.233-.858 1.264-.716.07-1.377-.474-2.076-.93-.484-.317-1-.651-1.552-.674-.49-.02-.942.162-1.463.347-.453.16-.943.36-1.422.222a1.065 1.065 0 01-.563-.41c-.604-.863-.895-1.864-1.233-2.8l-.03-.087c-.338-.936-.865-1.745-1.244-2.612-.148-.336-.415-.657-.4-1.003.01-.314.268-.587.751-.81.961-.444 2.342-.936 3.738-1.182a823.42 823.42 0 015.638-1.355c1.134-.276 1.637-.488 1.857-.506z"/>
      </svg>
    )
  }
  if (icon === "yt") {
    return (
      <svg viewBox="0 0 24 24" fill="currentColor" className={className}>
        <path d="M23.498 6.186a3.016 3.016 0 00-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 00.502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 002.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 002.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/>
      </svg>
    )
  }
  return null
}

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [profileOpen, setProfileOpen] = useState(false)
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

  const initials = user
    ? user.fullName.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2)
    : "?"

  return (
    <header className="sticky top-0 z-50 bg-bg">
      {/* Row 1: Top bar */}
      <div className="border-b border-border bg-muted/50">
        <div className="mx-auto flex h-9 max-w-7xl items-center justify-between px-4 lg:px-8">
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
          </div>
        </div>
      </div>

      {/* Row 2: Main header */}
      <div className="border-b border-border bg-bg">
        <div className="mx-auto flex h-20 max-w-7xl items-center justify-between px-4 lg:px-8">
          <Link href="/" className="flex items-center gap-3 shrink-0">
            <img src="/logo.svg" alt="Ставропольский колледж связи" className="object-contain" style={{ height: "56px" }} />
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
            <button onClick={() => setMobileOpen(!mobileOpen)} className="lg:hidden ml-2 rounded-md p-2 text-muted-fg hover:bg-muted" aria-label="Меню">
              {mobileOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
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
