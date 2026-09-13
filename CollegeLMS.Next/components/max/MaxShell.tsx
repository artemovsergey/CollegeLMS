"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { useEffect, useState } from "react"
import {
  Home,
  CalendarDays,
  Star,
  History,
  Settings,
  ShieldCheck,
} from "lucide-react"
import type { ReactNode } from "react"

const TABS = [
  { href: "/max", label: "Главная", icon: Home },
  { href: "/max/schedule", label: "Расписание", icon: CalendarDays },
  { href: "/max/favorites", label: "Избранное", icon: Star },
  { href: "/max/changes", label: "Изменения", icon: History },
  { href: "/max/settings", label: "Настройки", icon: Settings },
]

function useDispatcherSession() {
  const [hasDispatcher, setHasDispatcher] = useState(false)

  useEffect(() => {
    const read = () =>
      setHasDispatcher(
        typeof window !== "undefined" &&
          sessionStorage.getItem("dispatcherToken") !== null,
      )
    read()
    window.addEventListener("max:dispatcher", read)
    window.addEventListener("storage", read)
    return () => {
      window.removeEventListener("max:dispatcher", read)
      window.removeEventListener("storage", read)
    }
  }, [])

  return hasDispatcher
}

export default function MaxShell({ children }: { children: ReactNode }) {
  const pathname = usePathname()
  const hasDispatcher = useDispatcherSession()

  const isActive = (href: string) =>
    href === "/max" ? pathname === "/max" : pathname.startsWith(href)

  return (
    <div className="max-app">
      <div className="max-app__content pb-20">{children}</div>
      <nav className="max-app__tabbar" aria-label="Разделы">
        {TABS.map((tab) => (
          <Link
            key={tab.href}
            href={tab.href}
            className={`max-app__tab ${
              isActive(tab.href) ? "max-app__tab--active" : ""
            }`}
            aria-current={isActive(tab.href) ? "page" : undefined}
          >
            <tab.icon size={20} aria-hidden />
            <span className="max-app__tab-label">{tab.label}</span>
          </Link>
        ))}
        {hasDispatcher && (
          <Link
            href="/max/dispatcher"
            className={`max-app__tab ${
              pathname.startsWith("/max/dispatcher")
                ? "max-app__tab--active"
                : ""
            }`}
            aria-current={
              pathname.startsWith("/max/dispatcher") ? "page" : undefined
            }
          >
            <ShieldCheck size={20} aria-hidden />
            <span className="max-app__tab-label">Диспетчер</span>
          </Link>
        )}
      </nav>
    </div>
  )
}