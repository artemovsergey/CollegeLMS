"use client"

import Image from "next/image"
import Link from "next/link"

const infoLinks = [
  { title: "Расписание занятий", href: "/schedule" },
  { title: "Платное обучение", href: "/paid-education" },
  { title: "Общежития", href: "/dormitories" },
  { title: "Стипендии", href: "/scholarships" },
  { title: "Электронная информационно-образовательная среда", href: "/eios" },
  { title: "Противодействие коррупции", href: "/anti-corruption" },
  { title: "Доступная среда", href: "/accessible-environment" },
  { title: "Вакансии", href: "/vacancies" },
  { title: "Телефонный справочник", href: "/phonebook" },
  { title: "Образовательные кредиты", href: "/educational-loans" },
  { title: "Планы финансово-хозяйственной деятельности", href: "/financial-plans" },
  { title: "Обработка персональных данных", href: "/personal-data" },
  { title: "Охрана здоровья", href: "/health-protection" },
  { title: "Закупки", href: "/procurement" },
  { title: "Отчётность", href: "/reports" },
  { title: "Оценка условий труда", href: "/safety-assessment" },
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
    <footer className="bg-white border-t border-border">
      <div className="mx-auto max-w-7xl px-4 lg:px-8 py-12">
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Link href="/" className="inline-block mb-4">
              <Image
                src="/logo.svg"
                alt="Ставропольский колледж связи"
                width={0}
                height={0}
                sizes="100vw"
                className="object-contain h-auto w-auto"
                style={{ maxHeight: "80px", width: 'auto', height: '100%' }}
                unoptimized
              />
            </Link>
            <p className="text-sm leading-relaxed text-muted-fg">
              ГБПОУ «Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова»
            </p>
          </div>

          {footerColumns.map((col) => (
            <div key={col.title}>
              <h3 className="mb-4 text-sm font-semibold text-fg">{col.title}</h3>
              <ul className="space-y-2">
                {col.items.map((item) => (
                  <li key={item.label}>
                    <Link
                      href={item.href}
                      className="text-sm text-muted-fg hover:text-fg transition-colors"
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
          <p className="text-xs text-muted-fg/60">
            © {new Date().getFullYear()} ГБПОУ «Ставропольский колледж связи
            имени Героя Советского Союза В.А. Петрова». Все права защищены.
          </p>
          <div className="flex gap-4 text-xs text-muted-fg/60">
            <Link href="/sveden" className="hover:text-fg transition-colors">
              Сведения об образовательной организации
            </Link>
            <Link href="/privacy" className="hover:text-fg transition-colors">
              Политика конфиденциальности
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
