"use client"

import { useState } from "react"
import Link from "next/link"
import { ChevronDown } from "lucide-react"

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
  const [accordionOpen, setAccordionOpen] = useState(false)

  return (
    <footer className="bg-black text-[#999]">
      <div className="border-b border-white/10">
        <div className="mx-auto max-w-7xl px-4 lg:px-8">
          <button
            onClick={() => setAccordionOpen(!accordionOpen)}
            className="flex w-full items-center justify-between py-4 text-sm font-medium text-white/80 hover:text-white transition-colors"
          >
            Полезная информация
            <ChevronDown
              size={18}
              className={`transition-transform ${accordionOpen ? "rotate-180" : ""}`}
            />
          </button>
          <div
            className={`overflow-hidden transition-all duration-300 ${
              accordionOpen ? "max-h-[2000px] pb-6" : "max-h-0"
            }`}
          >
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-x-6 gap-y-1.5">
              {infoLinks.map((link) => (
                <Link
                  key={link.title}
                  href={link.href}
                  className="text-sm text-[#999] hover:text-white transition-colors py-0.5"
                >
                  {link.title}
                </Link>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-4 lg:px-8 py-12">
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <Link href="/" className="inline-block mb-4">
              <img
                src="/logo.svg"
                alt="Ставропольский колледж связи"
                className="object-contain brightness-0 invert opacity-80"
                style={{ height: "44px" }}
              />
            </Link>
            <p className="text-sm leading-relaxed text-[#999]">
              ГБПОУ «Ставропольский колледж связи<br />
              имени Героя Советского Союза В.А. Петрова»
            </p>
          </div>

          {footerColumns.map((col) => (
            <div key={col.title}>
              <h3 className="mb-4 text-sm font-semibold text-white">{col.title}</h3>
              <ul className="space-y-2">
                {col.items.map((item) => (
                  <li key={item.label}>
                    <Link
                      href={item.href}
                      className="text-sm text-[#999] hover:text-white transition-colors"
                    >
                      {item.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="mt-8 border-t border-white/10 pt-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-[#666]">
            © {new Date().getFullYear()} ГБПОУ «Ставропольский колледж связи
            имени Героя Советского Союза В.А. Петрова». Все права защищены.
          </p>
          <div className="flex gap-4 text-xs text-[#666]">
            <Link href="/sveden" className="hover:text-white transition-colors">
              Сведения об образовательной организации
            </Link>
            <Link href="/privacy" className="hover:text-white transition-colors">
              Политика конфиденциальности
            </Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
