"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { Search, User, Menu, X } from "lucide-react"
import { siteNavigation } from "@/data/site-content"
import { useAuth } from "@/lib/auth"
import MegaMenu from "./MegaMenu"

export default function HeaderTPU() {
  const [scrolled, setScrolled] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const { user } = useAuth()

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 80)
    window.addEventListener("scroll", onScroll)
    return () => window.removeEventListener("scroll", onScroll)
  }, [])

  const textColor = scrolled ? "text-gray-700" : "text-white/90"
  const textColorHover = scrolled ? "hover:text-[#0066cc]" : "hover:text-white"

  return (
    <header className={`header-tpu ${scrolled ? "scrolled" : "transparent"}`}>
      <div className="mx-auto flex h-full max-w-7xl items-center justify-between px-4 lg:px-8">
        <Link href="/" className="flex items-center gap-3 shrink-0">
          <img
            src="/logo.svg"
            alt="Колледж связи"
            className="object-contain"
            style={{ height: "56px" }}
          />
        </Link>

        <nav className="hidden lg:flex items-center gap-1">
          {siteNavigation.map((section) => (
            <div key={section.slug} className="group relative">
              <Link
                href={section.href}
                className={`px-4 py-2 text-sm font-medium transition-colors rounded-md ${textColor} ${textColorHover}`}
              >
                {section.title}
              </Link>
              <MegaMenu section={section} />
            </div>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <button
            className={`p-2 rounded-md transition-colors ${scrolled ? "text-gray-500 hover:text-[#0066cc]" : "text-white/80 hover:text-white"}`}
            aria-label="Поиск"
          >
            <Search size={20} />
          </button>
          {user ? (
            <Link
              href="/my/profile"
              className={`hidden sm:flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md transition-colors ${scrolled ? "bg-[#0066cc] text-white hover:bg-[#0052a3]" : "bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm"}`}
            >
              <User size={16} />
              {user.fullName}
            </Link>
          ) : (
            <Link
              href="/login"
              className={`hidden sm:flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors ${scrolled ? "bg-[#0066cc] text-white hover:bg-[#0052a3]" : "bg-white/20 text-white hover:bg-white/30 backdrop-blur-sm"}`}
            >
              Войти
            </Link>
          )}
          <button
            onClick={() => setMobileOpen(!mobileOpen)}
            className={`lg:hidden p-2 rounded-md ${scrolled ? "text-gray-700" : "text-white"}`}
            aria-label="Меню"
          >
            {mobileOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <div className="lg:hidden border-t border-gray-200 bg-white px-4 pb-4 pt-2 max-h-[80vh] overflow-y-auto">
          <nav className="flex flex-col gap-1">
            {siteNavigation.map((section) => (
              <div key={section.slug}>
                <Link
                  href={section.href}
                  className="block px-3 py-2 text-sm font-medium text-gray-900 rounded-md hover:bg-gray-50"
                  onClick={() => setMobileOpen(false)}
                >
                  {section.title}
                </Link>
                {section.subsections.map((sub) => (
                  <Link
                    key={sub.slug}
                    href={sub.href}
                    className="block pl-8 pr-3 py-1.5 text-sm text-gray-600 rounded-md hover:bg-gray-50"
                    onClick={() => setMobileOpen(false)}
                  >
                    {sub.title}
                  </Link>
                ))}
              </div>
            ))}
            {user ? (
              <div className="border-t border-gray-200 mt-2 pt-2">
                <Link
                  href="/my/profile"
                  className="block px-3 py-2 text-sm text-gray-700 rounded-md hover:bg-gray-50"
                  onClick={() => setMobileOpen(false)}
                >
                  Мой профиль
                </Link>
              </div>
            ) : (
              <Link
                href="/login"
                className="block px-3 py-2 text-sm font-medium text-center bg-[#0066cc] text-white rounded-md mt-2"
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
