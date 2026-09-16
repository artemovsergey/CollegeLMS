"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  CalendarDays,
  Star,
  History,
  ShieldCheck,
  BookOpen,
} from "lucide-react"
import type { ReactNode } from "react"
import { useMaxContext } from "@/lib/max-context"

const BASE_TABS = [
  { href: "/max/schedule", label: "Расписание", icon: CalendarDays },
  { href: "/max/favorites", label: "Избранное", icon: Star },
  { href: "/max/changes", label: "Изменения", icon: History },
  { href: "/max/dispatcher", label: "Диспетчер", icon: ShieldCheck },
]

export default function MaxShell({ children }: { children: ReactNode }) {
  const pathname = usePathname()
  const { profile } = useMaxContext()

  const tabs =
    profile?.role === "Teacher"
      ? [...BASE_TABS, { href: "/max/journal", label: "Журнал", icon: BookOpen }]
      : BASE_TABS

  const isActive = (href: string) => pathname.startsWith(href)

  return (
    <div className="max-app">
      <div className="max-app__content pb-20">{children}</div>
      <nav className="max-app__tabbar" aria-label="Разделы">
        {tabs.map((tab) => (
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
      </nav>
    </div>
  )
}