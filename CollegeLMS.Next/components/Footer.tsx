"use client"

import Link from "next/link"

interface FooterLink {
  label: string
  href?: string
}

const footerColumns: { title: string; items: FooterLink[] }[] = [
  {
    title: "Приёмная комиссия",
    items: [
      { label: "+7 (8652) 24-25-27", href: "tel:+78652242527" },
      { label: "college@stvcc.ru", href: "mailto:college@stvcc.ru" },
      { label: "пр-д Черняховского, 3", href: "/contacts" },
    ],
  },
  {
    title: "Реквизиты",
    items: [
      { label: "ИНН 2634092525" },
      { label: "КПП 263401001" },
      { label: "ОГРН 1132651000403" },
    ],
  },
  {
    title: "Образование",
    items: [
      { label: "Специальности", href: "/specialties" },
      { label: "Курсы", href: "/education/kursyi" },
      { label: "Целевое обучение", href: "/education/tselevoe-obuchenie" },
    ],
  },
  {
    title: "Документы",
    items: [{ label: "Сведения об ОО", href: "/about" }],
  },
]

export default function Footer() {
  return (
    <footer className="bg-muted border-t border-border">
      <div className="mx-auto max-w-7xl px-4 lg:px-8 py-12">
        <div className="grid gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
          <div>
            <span className="inline-block mb-3 text-base font-bold text-primary">
              ГБПОУ «Ставропольский колледж связи
              <br />
              имени Героя Советского Союза В.А. Петрова»
            </span>
          </div>

          {footerColumns.map((col) => (
            <div key={col.title}>
              <h3 className="mb-3 text-sm font-semibold text-primary">{col.title}</h3>
              <ul className="space-y-1.5">
                {col.items.map((item) => (
                  <li key={item.label}>
                    {item.href ? (
                      <Link
                        href={item.href}
                        className="text-sm text-muted-fg hover:text-primary hover:underline transition-all duration-200"
                      >
                        {item.label}
                      </Link>
                    ) : (
                      <span className="text-sm text-muted-fg">{item.label}</span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-8 border-t border-border pt-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-accent-lighter">
            © {new Date().getFullYear()} ГБПОУ «Ставропольский колледж связи
            имени Героя Советского Союза В.А. Петрова». Все права защищены.
          </p>
          <div className="flex gap-4 text-xs text-accent-lighter">
            <Link href="/about" className="hover:text-muted-fg transition-colors">
              Сведения об образовательной организации
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
