"use client"

import Link from "next/link"
import { siteNavigation } from "@/data/site-content"

export default function FooterTPU() {
  return (
    <footer className="bg-[var(--color-tpu-footer-bg)] text-[var(--color-tpu-footer-text)]">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12 lg:py-16">
        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Link href="/" className="inline-block mb-4">
              <img
                src="/logo.svg"
                alt="Колледж связи"
                className="object-contain brightness-0 invert"
                style={{ height: "48px" }}
              />
            </Link>
            <p className="text-sm leading-relaxed text-gray-400">
              ГБПОУ «Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова»
            </p>
          </div>

          {siteNavigation.slice(0, 3).map((section) => (
            <div key={section.slug}>
              <h3 className="mb-4 text-sm font-semibold text-white">{section.title}</h3>
              <ul className="space-y-2">
                {section.subsections.map((sub) => (
                  <li key={sub.slug}>
                    <Link
                      href={sub.href}
                      className="text-sm text-gray-400 hover:text-white transition-colors"
                    >
                      {sub.title}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-12 border-t border-white/10 pt-8 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-gray-500">
            © {new Date().getFullYear()} Ставропольский колледж связи. Все права защищены.
          </p>
          <div className="flex gap-4 text-xs text-gray-500">
            <Link href="/about" className="hover:text-white transition-colors">
              Сведения об образовательной организации
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
