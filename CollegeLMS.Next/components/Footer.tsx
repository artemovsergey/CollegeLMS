"use client"

import Link from "next/link"

const infoLinks = [
  { title: "Расписание занятий", href: "/schedule" },
  { title: "Новости", href: "/news" },
  { title: "Специальности", href: "/specialties" },
  { title: "Контакты", href: "/contacts" },
  { title: "Партнёры", href: "/partners" },
]

const footerColumns = [
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
      { label: "ИНН 2634092525", href: "#" },
      { label: "КПП 263401001", href: "#" },
      { label: "ОГРН 1132651000403", href: "#" },
    ],
  },
  {
    title: "Образование",
    items: [
      { label: "Специальности", href: "/specialties" },
      { label: "Профессии", href: "/professions" },
      { label: "Доп. образование", href: "/additional-education" },
    ],
  },
  {
    title: "Документы",
    items: [
      { label: "Устав", href: "/documents/charter" },
      { label: "Лицензия", href: "/documents/license" },
      { label: "Аккредитация", href: "/documents/accreditation" },
    ],
  },
]

export default function Footer() {
  return (
    <footer className="bg-muted border-t border-border">
      <div className="mx-auto max-w-7xl px-4 lg:px-8 py-12">
        <div className="grid gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
          <div>
            <Link href="/" className="inline-block mb-3">
              <span className="text-base font-bold text-primary">Ставропольский колледж связи</span>
            </Link>
            <p className="text-xs leading-relaxed text-muted-fg">
              ГБПОУ «Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова»
            </p>
          </div>

          {footerColumns.map((col) => (
            <div key={col.title}>
              <h3 className="mb-3 text-sm font-semibold text-primary">{col.title}</h3>
              <ul className="space-y-1.5">
                {col.items.map((item) => (
                  <li key={item.label}>
                    <Link
                      href={item.href}
                      className="text-sm text-muted-fg hover:text-primary hover:underline transition-all duration-200"
                    >
                      {item.label}
                    </Link>
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
            <Link href="/sveden" className="hover:text-muted-fg transition-colors">
              Сведения об образовательной организации
            </Link>
            <Link href="/privacy" className="hover:text-muted-fg transition-colors">
              Политика конфиденциальности
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
